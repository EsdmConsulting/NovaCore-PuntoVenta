using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Ventas;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Productos;
using NovaCoreESDM.Services.Ventas;
using NovaCoreESDM.Services.Printing;
namespace NovaCoreESDM.ViewModels.Ventas;
using NovaCoreESDM.Services.WhatsApp;
using System.Linq;
using NovaCoreESDM.Models.Tickets;


public partial class VentasViewModel : ViewModelBase
{
    // =========================================================
    // SERVICES
    // =========================================================

    private readonly ProductosService _productosService;

    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly VentasService _ventasService;
    
    private readonly TicketVentaService
        _ticketVentaService =
            new();
    
    private readonly WindowsRawPrinterService
        _printerService =
            new();
    
    private readonly WhatsAppService
        _whatsAppService;


    // =========================================================
    // COLECCIONES
    // =========================================================

    public ObservableCollection<ProductoCatalogo> Productos { get; }
        = new();

    public ObservableCollection<DetalleVenta> Carrito { get; }
        = new();


    // =========================================================
    // CLIENTE / BÚSQUEDA
    // =========================================================

    [ObservableProperty]
    private string _clienteSeleccionado =
        "Cliente mostrador";

    [ObservableProperty]
    private string _textoBusqueda =
        string.Empty;


    // =========================================================
    // MENSAJES / CARGA
    // =========================================================

    [ObservableProperty]
    private string _mensajeError =
        string.Empty;

    [ObservableProperty]
    private bool _estaCargandoProductos;

    [ObservableProperty]
    private bool _estaProcesandoVenta;


    // =========================================================
    // VENTA ACTUAL
    // =========================================================

    [ObservableProperty]
    private int _idVentaActual;

    [ObservableProperty]
    private string _folioVentaActual =
        string.Empty;

    [ObservableProperty]
    private string _uuidVentaActual =
        string.Empty;
    
    // =========================================================
    // FINALZIAR VENTA ACTUAL
    // ========================================================= 
    
    
    [ObservableProperty]
    private string _ultimoFolioTicket =
        string.Empty;

    [ObservableProperty]
    private bool _ventaFinalizadaCorrectamente;
    
    
// =========================================================
// GENERAR TICKETS
// =========================================================

    [ObservableProperty]
    private decimal _ultimoMontoRecibido;

    [ObservableProperty]
    private decimal _ultimoCambio;


    // =========================================================
    // PROPIEDADES CALCULADAS DEL CARRITO
    // =========================================================

    public bool CarritoVacio =>
        Carrito.Count == 0;

    public bool TieneProductos =>
        Carrito.Count > 0;

    public bool TieneProductosDisponibles =>
        Productos.Count > 0;

    public int CantidadProductos =>
        Carrito.Sum(item => item.Cantidad);

    public decimal Subtotal =>
        Carrito.Sum(item => item.Importe);

    public decimal Total =>
        Subtotal;

    public string TextoCantidadProductos =>
        CantidadProductos == 1
            ? "1 producto"
            : $"{CantidadProductos} productos";


// =========================================================
// EVENTOS
// =========================================================

/*
 * VentasView escucha este evento para abrir:
 *
 * SeleccionarPresentacionWindow
 */
    public event Action<ProductoCatalogo>?
        SolicitarSeleccionPresentacion;

    public event Action<DetalleVenta>?
        SolicitarEditarCantidad;

    public event Action?
        SolicitarCobro;

    public event Func<
        TicketVenta,
        Task<string?>
    >? SolicitarEntregaTicket;

    public event Func<
        Task<string?>
    >? SolicitarTelefonoWhatsApp;
    
    public event Func<
        string,
        Task
    >? SolicitarConfirmacionVentaFinalizada;
    
    
    
    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public VentasViewModel()
    {
        _productosService =
            new ProductosService();

        _terminalConfigurationService =
            new TerminalConfigurationService();

        _ventasService =
            new VentasService();

        _ticketVentaService =
            new TicketVentaService();

        _whatsAppService =
            new WhatsAppService();
    }


    // =========================================================
    // CARGAR CATÁLOGO
    // =========================================================

    [RelayCommand]
    public async Task CargarProductosAsync()
    {
        EstaCargandoProductos = true;
        MensajeError = string.Empty;

        try
        {
            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();

            if (configuracion is null)
            {
                MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }

            var resultado =
                await _productosService
                    .ObtenerCatalogoAsync();

            Productos.Clear();

            if (resultado.Status != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Message)
                        ? "No fue posible consultar el catálogo de productos."
                        : resultado.Message;

                return;
            }

            /*
             * Mostramos únicamente productos:
             *
             * - Habilitados para venta.
             * - Activos.
             * - Configurados para el Punto de Venta
             *   correspondiente a esta terminal.
             */
            var productosPuntoVenta =
                resultado.Data
                    .Where(p =>
                        p.AplicaVenta &&
                        p.Estatus != 3)
                    .Where(p =>
                        p.Disponibilidad.Any(d =>
                            d.UnidadCodigo.Equals(
                                configuracion.UnidadCodigo,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            d.Estatus != 3
                        ))
                    .OrderBy(p =>
                        p.NombreComercial);

            foreach (var producto in productosPuntoVenta)
            {
                Productos.Add(producto);
            }

            if (Productos.Count == 0)
            {
                MensajeError =
                    "No existen productos disponibles para este punto de venta.";
            }
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible cargar los productos: {ex.Message}";
        }
        finally
        {
            EstaCargandoProductos = false;

            OnPropertyChanged(
                nameof(TieneProductosDisponibles));
        }
    }


    // =========================================================
    // SELECCIONAR PRODUCTO
    // =========================================================

    [RelayCommand]
    private void SeleccionarProducto(
        ProductoCatalogo? producto)
    {
        if (producto is null)
            return;

        MensajeError = string.Empty;

        SolicitarSeleccionPresentacion?.Invoke(
            producto);
    }


    // =========================================================
    // ASEGURAR QUE EXISTA UNA VENTA
    // =========================================================

    private async Task<bool> AsegurarVentaCreadaAsync()
    {
        /*
         * Si ya tenemos una venta actual,
         * no creamos otra.
         */
        if (IdVentaActual > 0)
            return true;


        // =====================================================
        // VALIDAR TURNO
        // =====================================================

        if (!PosSession.TieneTurnoActivo)
        {
            MensajeError =
                "No existe un turno activo para realizar la venta.";

            return false;
        }


        // =====================================================
        // GENERAR FOLIO
        // =====================================================

        var folio =
            GenerarFolioTemporal();
        
        
        Console.WriteLine("====================================");
        Console.WriteLine("DATOS PARA CREAR VENTA:");
        Console.WriteLine($"IdEmpresa: {PosSession.IdEmpresa}");
        Console.WriteLine($"IdUnidadOperativa: {PosSession.IdUnidadOperativa}");
        Console.WriteLine($"IdCaja: {PosSession.IdCaja}");
        Console.WriteLine($"IdTurno: {PosSession.IdTurno}");
        Console.WriteLine($"Folio: {folio}");
        Console.WriteLine("====================================");


        // =====================================================
        // REQUEST
        // =====================================================

        var request =
            new CrearVentaRequest
            {
                IdEmpresa =
                    PosSession.IdEmpresa,

                IdUnidadOperativa =
                    PosSession.IdUnidadOperativa,

                IdCaja =
                    PosSession.IdCaja,

                IdTurno =
                    PosSession.IdTurno,

                /*
                 * Por ahora:
                 * Cliente mostrador = null.
                 *
                 * Cuando conectemos clientes,
                 * aquí irá el id_cliente real.
                 */
                IdCliente =
                    null,

                TipoCliente =
                    "PUBLICO_GENERAL",

                TipoVenta =
                    "CONTADO",

                RequiereFactura =
                    false,

                Folio =
                    folio,

                Observaciones =
                    string.Empty
            };


        // =====================================================
        // CREAR VENTA EN BACKEND
        // =====================================================

        var resultado =
            await _ventasService
                .CrearVentaAsync(request);


        if (resultado.Res != 1 ||
            resultado.Data is null)
        {
            MensajeError =
                string.IsNullOrWhiteSpace(
                    resultado.Msg)
                    ? "No fue posible crear la venta."
                    : resultado.Msg;

            return false;
        }


        // =====================================================
        // GUARDAR VENTA ACTUAL EN MEMORIA
        // =====================================================

        IdVentaActual =
            resultado.Data.Id;

        UuidVentaActual =
            resultado.Data.Uuid;

        FolioVentaActual =
            resultado.Data.Folio;


        return true;
    }
    
    
    [RelayCommand]
    private void Cobrar()
    {
        MensajeError =
            string.Empty;

        if (Carrito.Count == 0)
        {
            MensajeError =
                "No hay productos para cobrar.";

            return;
        }

        if (IdVentaActual <= 0)
        {
            MensajeError =
                "No existe una venta activa.";

            return;
        }

        if (EstaProcesandoVenta)
            return;

        SolicitarCobro?.Invoke();
    }


    // =========================================================
    // AGREGAR PRESENTACIÓN
    // =========================================================

public async Task AgregarPresentacionAlCarritoAsync(
    ProductoCatalogo productoCatalogo,
    PresentacionVentaItem presentacion)
{
    if (EstaProcesandoVenta)
        return;


    MensajeError =
        string.Empty;


    // =========================================================
    // VALIDAR PRECIO
    // =========================================================

    if (!presentacion.TienePrecio)
    {
        MensajeError =
            "La presentación seleccionada no tiene un precio de venta configurado.";

        return;
    }


    // =========================================================
    // BUSCAR SI YA ESTÁ EN EL CARRITO
    // =========================================================

    var detalleExistente =
        Carrito.FirstOrDefault(item =>
            item.Producto.Id ==
                productoCatalogo.Id
            &&
            item.Producto.IdPresentacion ==
                presentacion.IdPresentacion);


    /*
     * Por ahora NO manejaremos aquí el incremento
     * de una línea existente.
     *
     * Eso lo conectaremos con update_detalle.php
     * en el siguiente bloque.
     */
    if (detalleExistente is not null)
    {
        MensajeError =
            "Esta presentación ya se encuentra en el carrito.";

        return;
    }


    // =========================================================
    // CREAR MODELO VISUAL
    // =========================================================

    var producto =
        new Producto
        {
            Id =
                productoCatalogo.Id,

            IdPresentacion =
                presentacion.IdPresentacion,

            Codigo =
                productoCatalogo.Codigo,

            CodigoBarras =
                !string.IsNullOrWhiteSpace(
                    presentacion.CodigoBarras)
                    ? presentacion.CodigoBarras
                    : productoCatalogo.CodigoBarras,

            Nombre =
                productoCatalogo.NombreComercial,

            NombrePresentacion =
                presentacion.NombrePresentacion,

            Categoria =
                string.IsNullOrWhiteSpace(
                    productoCatalogo.Categoria)
                    ? "Sin categoría"
                    : productoCatalogo.Categoria,

            Precio =
                presentacion.Precio,

            FactorConversion =
                presentacion.FactorConversion,

            UnidadMedida =
                presentacion.UnidadMedida,

            Inicial =
                ObtenerInicial(
                    productoCatalogo.NombreComercial),

            FondoHex =
                "#EFF6FF",

            ColorHex =
                "#2563EB"
        };


    // =========================================================
    // AGREGAR INMEDIATAMENTE A LA UI
    // =========================================================

    var nuevoDetalle =
        new DetalleVenta(
            producto);


    nuevoDetalle.EstaSincronizando =
        true;


    nuevoDetalle.PropertyChanged +=
        (_, args) =>
        {
            if (
                args.PropertyName ==
                    nameof(DetalleVenta.Cantidad)
                ||
                args.PropertyName ==
                    nameof(DetalleVenta.Importe))
            {
                ActualizarTotales();
            }
        };


    Carrito.Add(
        nuevoDetalle);


    /*
     * IMPORTANTE:
     *
     * Desde aquí el usuario YA ve el producto.
     *
     * No esperamos PostgreSQL para actualizar
     * la pantalla.
     */
    ActualizarTotales();


    // =========================================================
    // AHORA SINCRONIZAMOS CON EL BACKEND
    // =========================================================

    EstaProcesandoVenta =
        true;


    try
    {
        // =====================================================
        // ASEGURAR VENTA
        // =====================================================

        var ventaLista =
            await AsegurarVentaCreadaAsync();


        if (!ventaLista)
        {
            /*
             * Si ni siquiera se pudo crear/recuperar
             * la venta, revertimos la UI.
             */
            Carrito.Remove(
                nuevoDetalle);

            ActualizarTotales();

            return;
        }


        // =====================================================
        // REQUEST DEL DETALLE
        // =====================================================

        var requestDetalle =
            new AgregarProductoVentaRequest
            {
                IdProducto =
                    producto.Id,

                IdPresentacion =
                    producto.IdPresentacion,

                CantidadComercial =
                    1,

                FactorConversion =
                    producto.FactorConversion,

                PrecioUnitario =
                    producto.Precio,

                DescuentoPorcentaje =
                    0,

                DescripcionSnapshot =
                    $"{producto.Nombre} - {producto.NombrePresentacion}"
            };


        // =====================================================
        // INSERTAR EN BACKEND
        // =====================================================

        var resultadoDetalle =
            await _ventasService
                .AgregarProductoAsync(
                    IdVentaActual,
                    requestDetalle);


        // =====================================================
        // ERROR
        // =====================================================

        if (resultadoDetalle.Res != 1 ||
            resultadoDetalle.Data is null)
        {
            nuevoDetalle.EstaSincronizando =
                false;

            nuevoDetalle.TieneErrorSincronizacion =
                true;


            /*
             * Como PostgreSQL rechazó la operación,
             * quitamos el producto provisional.
             *
             * Por ejemplo:
             * - existencia insuficiente
             * - producto inválido
             * - error servidor
             */
            Carrito.Remove(
                nuevoDetalle);


            MensajeError =
                string.IsNullOrWhiteSpace(
                    resultadoDetalle.Msg)
                    ? "No fue posible agregar el producto a la venta."
                    : resultadoDetalle.Msg;


            ActualizarTotales();

            return;
        }


        // =====================================================
        // BACKEND CONFIRMÓ
        // =====================================================

        nuevoDetalle.IdDetalle =
            resultadoDetalle.Data.IdDetalle;


        // =====================================================
        // INVENTARIO CONFIRMADO POR BACKEND
        // =====================================================

        nuevoDetalle.ExistenciaDisponible =
            resultadoDetalle.Data.ExistenciaDisponible;

        nuevoDetalle.MaximoLinea =
            nuevoDetalle.Cantidad +
            resultadoDetalle.Data.ExistenciaDisponible;


        // =====================================================
        // PRODUCTO SINCRONIZADO
        // =====================================================

        nuevoDetalle.TieneErrorSincronizacion =
            false;

        nuevoDetalle.EstaSincronizando =
            false;


        MensajeError =
            string.Empty;
    }
    catch (Exception ex)
    {
        // =====================================================
        // REVERTIR UI SI FALLÓ LA SINCRONIZACIÓN
        // =====================================================

        Carrito.Remove(
            nuevoDetalle);


        MensajeError =
            $"Ocurrió un error al agregar el producto: {ex.Message}";


        ActualizarTotales();
    }
    finally
    {
        EstaProcesandoVenta =
            false;
    }
}




[RelayCommand]
private void EditarCantidad(
    DetalleVenta? detalle)
{
    if (detalle is null)
        return;


    if (detalle.EstaSincronizando)
    {
        MensajeError =
            "Espera un momento mientras se actualiza el producto.";

        return;
    }


    if (
        detalle.IdDetalle <= 0 ||
        IdVentaActual <= 0
    )
    {
        MensajeError =
            "El producto todavía no está listo para modificar.";

        return;
    }


    MensajeError =
        string.Empty;


    SolicitarEditarCantidad?.Invoke(
        detalle);
}





    // =========================================================
    // INCREMENTAR CANTIDAD
    // =========================================================

    /*
     * IMPORTANTE:
     *
     * Por ahora este botón solamente modifica
     * el carrito local.
     *
     * En el siguiente paso lo sincronizaremos
     * también con tr_pos_ventas_detalle.
     */
[RelayCommand]
private async Task IncrementarCantidadAsync(
    DetalleVenta? detalle)
{
    if (detalle is null)
        return;

    if (detalle.EstaSincronizando)
        return;

    if (detalle.IdDetalle <= 0 ||
        IdVentaActual <= 0)
    {
        MensajeError =
            "El producto todavía no está listo para modificar.";

        return;
    }


    MensajeError =
        string.Empty;


    var cantidadAnterior =
        detalle.Cantidad;

    var cantidadNueva =
        cantidadAnterior + 1;


    // =========================================================
    // UI OPTIMISTA
    // =========================================================

    detalle.Cantidad =
        cantidadNueva;

    ActualizarTotales();

    detalle.EstaSincronizando =
        true;


    try
    {
        var resultado =
            await _ventasService
                .ActualizarCantidadProductoAsync(
                    IdVentaActual,
                    detalle.IdDetalle,
                    cantidadNueva);


        if (resultado.Res == 1)
        {
            // =====================================================
            // CANTIDAD CONFIRMADA POR BACKEND
            // =====================================================

            if (resultado.Data?.Detalle is not null)
            {
                detalle.Cantidad =
                    (int)resultado.Data
                        .Detalle
                        .CantidadComercial;
            }


            // =====================================================
            // INVENTARIO CONFIRMADO POR BACKEND
            // =====================================================

            if (resultado.Data?.Inventario is not null)
            {
                detalle.ExistenciaDisponible =
                    resultado.Data
                        .Inventario
                        .ExistenciaDisponible;

                detalle.MaximoLinea =
                    resultado.Data
                        .Inventario
                        .MaximoLinea;
            }


            MensajeError =
                string.Empty;

            ActualizarTotales();

            return;
        }


        // =====================================================
        // BACKEND RECHAZÓ
        // =====================================================

        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();

        MensajeError =
            string.IsNullOrWhiteSpace(resultado.Msg)
                ? "No fue posible aumentar la cantidad."
                : resultado.Msg;
    }
    catch (Exception ex)
    {
        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();

        MensajeError =
            $"No fue posible aumentar la cantidad: {ex.Message}";
    }
    finally
    {
        detalle.EstaSincronizando =
            false;
    }
}
    
    
    
    
    [RelayCommand]
public async Task CargarVentaActualAsync()
{
    MensajeError = string.Empty;

    if (!PosSession.TieneTurnoActivo)
        return;

    try
    {
        var resultado =
            await _ventasService
                .ObtenerVentaActualAsync(
                    PosSession.IdTurno
                );

        if (resultado.Res != 1)
        {
            MensajeError =
                string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "No fue posible recuperar la venta actual."
                    : resultado.Msg;

            return;
        }

        // No existe venta pendiente
        if (resultado.Data?.Venta is null)
        {
            IdVentaActual = 0;
            UuidVentaActual = string.Empty;
            FolioVentaActual = string.Empty;

            Carrito.Clear();

            ActualizarTotales();

            return;
        }

        var venta =
            resultado.Data.Venta;

        IdVentaActual =
            venta.Id;

        UuidVentaActual =
            venta.Uuid;

        FolioVentaActual =
            venta.Folio;
        
        // =========================================================
// RECUPERAR VENTA PAGADA PENDIENTE DE FINALIZAR
// =========================================================

        if (
            venta.Estado.Equals(
                "BORRADOR",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            venta.Total > 0
            &&
            venta.MontoPagado >= venta.Total
            &&
            venta.Saldo <= 0
        )
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "VENTA PAGADA PENDIENTE DE FINALIZAR");

            Console.WriteLine(
                $"ID Venta: {venta.Id}");

            Console.WriteLine(
                $"Total: {venta.Total}");

            Console.WriteLine(
                $"Pagado: {venta.MontoPagado}");

            Console.WriteLine(
                $"Saldo: {venta.Saldo}");

            Console.WriteLine(
                "Intentando finalizar automáticamente...");

            Console.WriteLine(
                "====================================");


            var finalizada =
                await FinalizarVentaAsync();


            if (finalizada)
            {
                Console.WriteLine(
                    "Venta pendiente recuperada y finalizada correctamente.");

                return;
            }


            // Si falla otra vez, NO cobramos de nuevo
            // y tampoco permitimos modificar el carrito.
            MensajeError =
                string.IsNullOrWhiteSpace(MensajeError)
                    ? "La venta ya está pagada, pero está pendiente de finalizar. Intente nuevamente."
                    : MensajeError;

            return;
        }
        
        
        
        

        Carrito.Clear();

        foreach (var detalle in resultado.Data.Detalles)
        {
            var producto =
                new Producto
                {
                    Id =
                        detalle.IdProducto,

                    IdPresentacion =
                        detalle.IdPresentacion,

                    Codigo =
                        detalle.Codigo,

                    CodigoBarras =
                        !string.IsNullOrWhiteSpace(
                            detalle.PresentacionCodigoBarras)
                            ? detalle.PresentacionCodigoBarras
                            : detalle.CodigoBarras,

                    Nombre =
                        detalle.NombreComercial,

                    NombrePresentacion =
                        detalle.NombrePresentacion,

                    Categoria =
                        "Sin categoría",

                    Precio =
                        detalle.PrecioUnitario,

                    FactorConversion =
                        detalle.FactorConversion,

                    UnidadMedida =
                        detalle.UnidadMedida,

                    Inicial =
                        ObtenerInicial(
                            detalle.NombreComercial),

                    FondoHex =
                        "#EFF6FF",

                    ColorHex =
                        "#2563EB"
                };

            var nuevoDetalle =
                new DetalleVenta(
                    producto,
                    detalle.IdDetalle
                );

            nuevoDetalle.Cantidad =
                (int)detalle.CantidadComercial;
            
            
            nuevoDetalle.ExistenciaDisponible =
                detalle.ExistenciaDisponible;

            nuevoDetalle.MaximoLinea =
                detalle.MaximoLinea;
            

            nuevoDetalle.PropertyChanged +=
                (_, args) =>
                {
                    if (
                        args.PropertyName ==
                        nameof(DetalleVenta.Cantidad)
                        ||
                        args.PropertyName ==
                        nameof(DetalleVenta.Importe))
                    {
                        ActualizarTotales();
                    }
                };

            Carrito.Add(
                nuevoDetalle);
        }

        ActualizarTotales();
    }
    catch (Exception ex)
    {
        MensajeError =
            $"No fue posible recuperar la venta actual: {ex.Message}";
    }
}



public async Task ActualizarCantidadManualAsync(
    DetalleVenta detalle,
    int cantidadNueva)
{
    if (detalle is null)
        return;


    if (cantidadNueva <= 0)
        return;


    if (detalle.EstaSincronizando)
        return;


    if (
        detalle.IdDetalle <= 0 ||
        IdVentaActual <= 0
    )
    {
        MensajeError =
            "El producto todavía no está listo para modificar.";

        return;
    }


    var cantidadAnterior =
        detalle.Cantidad;


    if (cantidadNueva == cantidadAnterior)
        return;


    // =========================================================
    // UI OPTIMISTA
    // =========================================================

    detalle.Cantidad =
        cantidadNueva;

    detalle.EstaSincronizando =
        true;

    ActualizarTotales();


    try
    {
        var resultado =
            await _ventasService
                .ActualizarCantidadProductoAsync(
                    IdVentaActual,
                    detalle.IdDetalle,
                    cantidadNueva);


        if (resultado.Res == 1)
        {
            if (resultado.Data?.Detalle is not null)
            {
                detalle.Cantidad =
                    (int)resultado.Data
                        .Detalle
                        .CantidadComercial;
            }


            if (resultado.Data?.Inventario is not null)
            {
                detalle.ExistenciaDisponible =
                    resultado.Data
                        .Inventario
                        .ExistenciaDisponible;

                detalle.MaximoLinea =
                    resultado.Data
                        .Inventario
                        .MaximoLinea;
            }


            MensajeError =
                string.Empty;


            ActualizarTotales();

            return;
        }


        // =====================================================
        // BACKEND RECHAZÓ
        // =====================================================

        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();


        MensajeError =
            string.IsNullOrWhiteSpace(resultado.Msg)
                ? "No fue posible actualizar la cantidad."
                : resultado.Msg;
    }
    catch (Exception ex)
    {
        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();


        MensajeError =
            $"No fue posible actualizar la cantidad: {ex.Message}";
    }
    finally
    {
        detalle.EstaSincronizando =
            false;
    }
}



// =========================================================
// REGISTRO DE PAGO
// =========================================================

/*
 * También está pendiente sincronizar esto
 * con el backend.
 */


    public async Task<bool> RegistrarPagoEfectivoAsync(
        decimal importeAplicado,
        decimal recibido,
        decimal cambio)
{
    if (IdVentaActual <= 0)
    {
        MensajeError =
            "No existe una venta activa.";

        return false;
    }


    if (importeAplicado <= 0)
    {
        MensajeError =
            "El importe del pago no es válido.";

        return false;
    }


    if (EstaProcesandoVenta)
        return false;


    EstaProcesandoVenta =
        true;

    MensajeError =
        string.Empty;


    try
    {
        var request =
            new RegistrarPagoRequest
            {
                IdFormaPago =
                    FormasPagoPos.Efectivo,

                IdBanco =
                    null,

                IdTpvBancaria =
                    null,

                Importe =
                    importeAplicado,

                Referencia =
                    string.Empty,

                Autorizacion =
                    string.Empty
            };


        var resultado =
            await _ventasService
                .RegistrarPagoAsync(
                    IdVentaActual,
                    request);


        if (
            resultado.Res != 1 ||
            resultado.Data is null
        )
        {
            MensajeError =
                string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "No fue posible registrar el pago."
                    : resultado.Msg;

            return false;
        }


        if (!resultado.Data.PagoCompleto)
        {
            MensajeError =
                $"El pago fue registrado, pero aún queda un saldo de ${resultado.Data.Saldo:N2}.";

            return false;
        }
        
        


// =========================================================
// PAGO COMPLETO → FINALIZAR AUTOMÁTICAMENTE
// =========================================================

        var finalizada =
            await FinalizarVentaAsync();


        if (!finalizada)
        {
            /*
             * IMPORTANTE:
             *
             * El pago YA quedó registrado.
             *
             * Por eso NO debemos volver a registrar
             * el pago si finalizar.php falla.
             *
             * Después podremos implementar recuperación
             * automática de ventas totalmente pagadas
             * pendientes de finalizar.
             */

            return false;
        }


        return true;


        return true;
    }
    catch (Exception ex)
    {
        MensajeError =
            $"No fue posible registrar el pago: {ex.Message}";

        return false;
    }
    finally
    {
        EstaProcesandoVenta =
            false;
    }
}

// =========================================================
// FINALIZAR VENTA
// =========================================================

public async Task<bool> FinalizarVentaAsync()
{
    if (IdVentaActual <= 0)
    {
        MensajeError =
            "No existe una venta activa para finalizar.";

        return false;
    }


    try
    {
        var idVentaFinalizar =
            IdVentaActual;


        // =====================================================
        // GUARDAR SNAPSHOT DEL CARRITO
        // =====================================================

        /*
         * Guardamos los productos antes de finalizar
         * porque después limpiaremos el carrito.
         */

        var productosTicket =
            Carrito
                .Select(
                    detalle =>
                        new TicketVentaDetalle
                        {
                            Producto =
                                detalle.Producto.Nombre,

                            Presentacion =
                                detalle.Producto.NombrePresentacion,

                            Cantidad =
                                detalle.Cantidad,

                            PrecioUnitario =
                                detalle.Producto.Precio,

                            Importe =
                                detalle.Importe
                        })
                .ToList();


        var subtotalTicket =
            Subtotal;


        // =====================================================
        // FINALIZAR VENTA EN BACKEND
        // =====================================================

        var resultado =
            await _ventasService
                .FinalizarVentaAsync(
                    idVentaFinalizar
                );


        if (
            resultado.Res != 1 ||
            resultado.Data is null
        )
        {
            MensajeError =
                string.IsNullOrWhiteSpace(
                    resultado.Msg
                )
                    ? "No fue posible finalizar la venta."
                    : resultado.Msg;

            return false;
        }


        // =====================================================
        // IMPORTANTE:
        // A PARTIR DE AQUÍ LA VENTA YA ESTÁ FINALIZADA
        // =====================================================

        /*
         * Ya existe en la BD.
         * Ya tiene folio de ticket.
         * Ya se descontó inventario.
         *
         * Lo que ocurra con impresión/WhatsApp
         * NO debe volver a finalizar la venta.
         */


        // =====================================================
        // GUARDAR DATOS DE LA VENTA FINALIZADA
        // =====================================================

        UltimoFolioTicket =
            resultado.Data.FolioTicket;


        VentaFinalizadaCorrectamente =
            true;


        // =====================================================
        // CONSTRUIR TICKET
        // =====================================================

        var ticket =
            new TicketVenta
            {
                Empresa =
                    "GRUPO LUISFER",

                Sistema =
                    "ESDM",

                UnidadOperativa =
                    resultado.Data.CodigoUnidadOperativa,

                FolioTicket =
                    resultado.Data.FolioTicket,

                Fecha =
                    DateTime.Now,

                Caja =
                    resultado.Data.CodigoCaja,

                Cajero =
                    PosSession.NombreUsuario,

                Productos =
                    productosTicket,

                Subtotal =
                    subtotalTicket,

                Total =
                    resultado.Data.Total,

                Efectivo =
                    resultado.Data.Efectivo,

                Tarjeta =
                    resultado.Data.Tarjetas,

                Transferencia =
                    resultado.Data.Transferencias,

                FormaPago =
                    resultado.Data.Efectivo > 0
                        ? "Efectivo"
                        : resultado.Data.Tarjetas > 0
                            ? "Tarjeta"
                            : resultado.Data.Transferencias > 0
                                ? "Transferencia"
                                : string.Empty,

                ContenidoQr =
                    "https://plataformaluisfer.com/Plataforma_LuisFer/auth/login"
            };


        // =====================================================
        // LOG DE VENTA FINALIZADA
        // =====================================================

        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "VENTA FINALIZADA:");

        Console.WriteLine(
            $"ID Venta: {resultado.Data.IdVenta}");

        Console.WriteLine(
            $"Folio interno: {resultado.Data.Folio}");

        Console.WriteLine(
            $"Ticket: {resultado.Data.FolioTicket}");

        Console.WriteLine(
            $"Subtotal: {subtotalTicket}");

        Console.WriteLine(
            $"Total: {resultado.Data.Total}");

        Console.WriteLine(
            $"Efectivo: {resultado.Data.Efectivo}");

        Console.WriteLine(
            $"Tarjetas: {resultado.Data.Tarjetas}");

        Console.WriteLine(
            $"Transferencias: {resultado.Data.Transferencias}");

        Console.WriteLine(
            $"Productos ticket: {ticket.Productos.Count}");

        Console.WriteLine(
            "====================================");


        // =====================================================
        // SOLICITAR FORMA DE ENTREGA DEL TICKET
        // =====================================================

        string? opcionEntrega =
            null;


        if (SolicitarEntregaTicket is not null)
        {
            try
            {
                opcionEntrega =
                    await SolicitarEntregaTicket(
                        ticket
                    );
            }
            catch (Exception ex)
            {
                /*
                 * La venta YA terminó.
                 *
                 * Si la ventana tiene un problema,
                 * no debemos marcar la venta como fallida.
                 */

                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "VENTA FINALIZADA, PERO NO FUE POSIBLE MOSTRAR LA ENTREGA DEL TICKET:");

                Console.WriteLine(
                    ex.Message);

                Console.WriteLine(
                    "====================================");
            }
        }


        // =====================================================
        // PROCESAR OPCIÓN
        // =====================================================

        switch (opcionEntrega)
        {
            // =================================================
            // IMPRIMIR
            // =================================================

            case "IMPRIMIR":

                try
                {
                    var impreso =
                        await _ticketVentaService
                            .ImprimirAsync(
                                ticket
                            );


                    if (impreso)
                    {
                        Console.WriteLine(
                            "====================================");

                        Console.WriteLine(
                            "TICKET ENVIADO A IMPRESORA:");

                        Console.WriteLine(
                            ticket.FolioTicket);

                        Console.WriteLine(
                            "====================================");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"No se pudo confirmar la impresión del ticket {ticket.FolioTicket}."
                        );
                    }
                }
                catch (Exception exImpresion)
                {
                    Console.WriteLine(
                        "====================================");

                    Console.WriteLine(
                        "VENTA FINALIZADA, PERO FALLÓ LA IMPRESIÓN:");

                    Console.WriteLine(
                        exImpresion.Message);

                    Console.WriteLine(
                        "====================================");
                }

                break;


            // =================================================
            // WHATSAPP
            // =================================================

            case "WHATSAPP":

                try
                {
                    string? telefono =
                        null;


                    if (SolicitarTelefonoWhatsApp is not null)
                    {
                        telefono =
                            await SolicitarTelefonoWhatsApp();
                    }


                    if (string.IsNullOrWhiteSpace(telefono))
                    {
                        Console.WriteLine(
                            $"Ticket {ticket.FolioTicket}: envío por WhatsApp cancelado."
                        );

                        break;
                    }


                    var abierto =
                        await _whatsAppService
                            .AbrirWhatsAppAsync(
                                ticket,
                                telefono
                            );


                    if (abierto)
                    {
                        Console.WriteLine(
                            "====================================");

                        Console.WriteLine(
                            "WHATSAPP ABIERTO:");

                        Console.WriteLine(
                            $"Ticket: {ticket.FolioTicket}");

                        Console.WriteLine(
                            $"Teléfono: {telefono}");

                        Console.WriteLine(
                            "====================================");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"No fue posible abrir WhatsApp para el ticket {ticket.FolioTicket}."
                        );
                    }
                }
                catch (Exception exWhatsApp)
                {
                    Console.WriteLine(
                        "====================================");

                    Console.WriteLine(
                        "VENTA FINALIZADA, PERO FALLÓ WHATSAPP:");

                    Console.WriteLine(
                        exWhatsApp.Message);

                    Console.WriteLine(
                        "====================================");
                }

                break;


            // =================================================
            // SIN TICKET
            // =================================================

            case "SIN_TICKET":

                Console.WriteLine(
                    $"Ticket {ticket.FolioTicket}: cliente no solicitó comprobante."
                );

                break;


            // =================================================
            // SIN OPCIÓN / VENTANA CERRADA
            // =================================================

            default:

                /*
                 * Incluso si el usuario cerró la ventana,
                 * la venta ya está finalizada.
                 */

                Console.WriteLine(
                    $"Ticket {ticket.FolioTicket}: no se seleccionó método de entrega."
                );

                break;
            
        }

        
        // =====================================================
        // MOSTRAR CONFIRMACIÓN FINAL
        // =====================================================

        if (SolicitarConfirmacionVentaFinalizada is not null)
        {
            try
            {
                await SolicitarConfirmacionVentaFinalizada(
                    ticket.FolioTicket
                );
            }
            catch (Exception exConfirmacion)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "VENTA FINALIZADA, PERO NO SE PUDO MOSTRAR LA CONFIRMACIÓN:");

                Console.WriteLine(
                    exConfirmacion.Message);

                Console.WriteLine(
                    "====================================");
            }
        }

        // =====================================================
        // LIMPIAR VENTA ACTUAL
        // =====================================================

        IdVentaActual =
            0;

        UuidVentaActual =
            string.Empty;

        FolioVentaActual =
            string.Empty;


        Carrito.Clear();


        ActualizarTotales();


        MensajeError =
            string.Empty;


        return true;
    }
    catch (Exception ex)
    {
        MensajeError =
            $"No fue posible finalizar la venta: {ex.Message}";

        return false;
    }
}




    // =========================================================
    // DISMINUIR CANTIDAD
    // =========================================================

    /*
     * También está pendiente sincronizar esto
     * con el backend.
     */
[RelayCommand]
private async Task DisminuirCantidadAsync(
    DetalleVenta? detalle)
{
    if (detalle is null)
        return;

    if (detalle.EstaSincronizando)
        return;

    if (detalle.IdDetalle <= 0 ||
        IdVentaActual <= 0)
    {
        MensajeError =
            "El producto todavía no está listo para modificar.";

        return;
    }


    MensajeError =
        string.Empty;


    // =========================================================
    // SI ESTÁ EN 1, USAMOS DELETE
    // =========================================================

    if (detalle.Cantidad <= 1)
    {
        await EliminarProductoAsync(
            detalle);

        return;
    }


    // =========================================================
    // SI ES MAYOR A 1, USAMOS PUT
    // =========================================================

    var cantidadAnterior =
        detalle.Cantidad;

    var cantidadNueva =
        cantidadAnterior - 1;


    // UI optimista
    detalle.Cantidad =
        cantidadNueva;

    ActualizarTotales();

    detalle.EstaSincronizando =
        true;


    try
    {
        var resultado =
            await _ventasService
                .ActualizarCantidadProductoAsync(
                    IdVentaActual,
                    detalle.IdDetalle,
                    cantidadNueva);


        if (resultado.Res == 1)
        {
            // =====================================================
            // CANTIDAD CONFIRMADA
            // =====================================================

            if (resultado.Data?.Detalle is not null)
            {
                detalle.Cantidad =
                    (int)resultado.Data
                        .Detalle
                        .CantidadComercial;
            }


            // =====================================================
            // INVENTARIO CONFIRMADO
            // =====================================================

            if (resultado.Data?.Inventario is not null)
            {
                detalle.ExistenciaDisponible =
                    resultado.Data
                        .Inventario
                        .ExistenciaDisponible;

                detalle.MaximoLinea =
                    resultado.Data
                        .Inventario
                        .MaximoLinea;
            }


            MensajeError =
                string.Empty;

            ActualizarTotales();

            return;
        }


        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();

        MensajeError =
            string.IsNullOrWhiteSpace(resultado.Msg)
                ? "No fue posible disminuir la cantidad."
                : resultado.Msg;
    }
    catch (Exception ex)
    {
        detalle.Cantidad =
            cantidadAnterior;

        ActualizarTotales();

        MensajeError =
            $"No fue posible disminuir la cantidad: {ex.Message}";
    }
    finally
    {
        detalle.EstaSincronizando =
            false;
    }
}


    // =========================================================
    // ELIMINAR PRODUCTO
    // =========================================================

    /*
     *
     * DELETE
     * /api/pos/ventas/{id}/productos/{detalle}
     */
    
    
[RelayCommand]
private async Task EliminarProductoAsync(
    DetalleVenta? detalle)
{
    if (detalle is null)
        return;


    MensajeError =
        string.Empty;


    // =========================================================
    // NO ELIMINAR MIENTRAS SE ESTÁ SINCRONIZANDO
    // =========================================================

    if (detalle.EstaSincronizando)
    {
        MensajeError =
            "Espera un momento mientras se registra el producto.";

        return;
    }


    // =========================================================
    // VALIDAR DETALLE
    // =========================================================

    if (detalle.IdDetalle <= 0)
    {
        MensajeError =
            "El producto todavía no tiene un identificador válido.";

        return;
    }


    if (IdVentaActual <= 0)
    {
        MensajeError =
            "No existe una venta activa.";

        return;
    }


    // =========================================================
    // GUARDAR DATOS ANTES DE MODIFICAR UI
    // =========================================================

    var idVenta =
        IdVentaActual;

    var idDetalle =
        detalle.IdDetalle;

    var indiceOriginal =
        Carrito.IndexOf(
            detalle);


    if (indiceOriginal < 0)
        return;


    // =========================================================
    // UI OPTIMISTA
    // =========================================================

    /*
     * El producto desaparece inmediatamente.
     *
     * El cajero NO espera la respuesta HTTP
     * para ver el cambio.
     */

    Carrito.Remove(
        detalle);

    ActualizarTotales();


    try
    {
        // =====================================================
        // SINCRONIZAR CON BACKEND
        // =====================================================

        var resultado =
            await _ventasService
                .EliminarProductoAsync(
                    idVenta,
                    idDetalle);


        // =====================================================
        // BACKEND CONFIRMÓ
        // =====================================================

        if (resultado.Res == 1)
        {
            MensajeError =
                string.Empty;


            // =================================================
            // ¿ERA EL ÚLTIMO PRODUCTO?
            // =================================================

            /*
             * delete_detalle.php elimina también el encabezado
             * BORRADOR cuando ya no quedan detalles.
             */

            if (
                resultado.Data?.VentaEliminada ==
                true
            )
            {
                IdVentaActual =
                    0;

                UuidVentaActual =
                    string.Empty;

                FolioVentaActual =
                    string.Empty;


                /*
                 * Por seguridad.
                 *
                 * Si backend dice que la venta desapareció,
                 * el carrito local también debe estar vacío.
                 */
                Carrito.Clear();

                ActualizarTotales();
            }


            return;
        }


        // =====================================================
        // BACKEND RECHAZÓ
        // =====================================================

        RestaurarDetalleEnCarrito(
            detalle,
            indiceOriginal);


        MensajeError =
            string.IsNullOrWhiteSpace(
                resultado.Msg)
                ? "No fue posible eliminar el producto."
                : resultado.Msg;
    }
    catch (Exception ex)
    {
        // =====================================================
        // ERROR RED / SERVIDOR
        // =====================================================

        RestaurarDetalleEnCarrito(
            detalle,
            indiceOriginal);


        MensajeError =
            $"No fue posible eliminar el producto: {ex.Message}";
    }
}


    // =========================================================
    // CANCELAR VENTA
    // =========================================================

    /*
     * IMPORTANTE:
     *
     * Como ahora sí existe una venta real en PostgreSQL,
     * este método todavía NO está terminado.
     *
     * En el siguiente paso conectaremos:
     *
     * POST
     * /api/pos/ventas/{id}/cancelar
     *
     * Por ahora únicamente limpia el carrito visual.
     */
[RelayCommand]
private async Task CancelarVentaAsync()
{
    MensajeError =
        string.Empty;


    // =========================================================
    // SI NO HAY CARRITO, NO HACEMOS NADA
    // =========================================================

    if (
        Carrito.Count == 0 ||
        IdVentaActual <= 0
    )
    {
        return;
    }


    if (EstaProcesandoVenta)
        return;


    // =========================================================
    // GUARDAR ESTADO ACTUAL
    // =========================================================

    var idVenta =
        IdVentaActual;


    var carritoAnterior =
        Carrito.ToList();


    var uuidAnterior =
        UuidVentaActual;


    var folioAnterior =
        FolioVentaActual;


    // =========================================================
    // UI OPTIMISTA
    // =========================================================

    /*
     * El cajero ve inmediatamente
     * el carrito vacío.
     */

    Carrito.Clear();

    IdVentaActual =
        0;

    UuidVentaActual =
        string.Empty;

    FolioVentaActual =
        string.Empty;


    ActualizarTotales();


    EstaProcesandoVenta =
        true;


    try
    {
        // =====================================================
        // BACKEND
        // =====================================================

        var resultado =
            await _ventasService
                .CancelarVentaAsync(
                    idVenta);


        // =====================================================
        // TODO CORRECTO
        // =====================================================

        if (
            resultado.Res == 1 &&
            resultado.Data?.VentaEliminada == true
        )
        {
            MensajeError =
                string.Empty;

            return;
        }


        // =====================================================
        // BACKEND RECHAZÓ
        // =====================================================

        RestaurarCarritoCancelado(
            carritoAnterior,
            idVenta,
            uuidAnterior,
            folioAnterior);


        MensajeError =
            string.IsNullOrWhiteSpace(resultado.Msg)
                ? "No fue posible cancelar el carrito."
                : resultado.Msg;
    }
    catch (Exception ex)
    {
        // =====================================================
        // ERROR RED / SERVIDOR
        // =====================================================

        RestaurarCarritoCancelado(
            carritoAnterior,
            idVenta,
            uuidAnterior,
            folioAnterior);


        MensajeError =
            $"No fue posible cancelar el carrito: {ex.Message}";
    }
    finally
    {
        EstaProcesandoVenta =
            false;
    }
    
    
}



    // =========================================================
    // IMPRESIÓN DE TICKER
    // =========================================================

    [RelayCommand]
    private async Task ImprimirPruebaAsync()
    {
        MensajeError =
            string.Empty;

        try
        {
            var resultado =
                await _printerService
                    .ImprimirPruebaAsync();

            if (!resultado)
            {
                MensajeError =
                    "La impresora no confirmó la impresión de prueba.";
            }
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible imprimir: {ex.Message}";
        }
    }
    
    
    
    // =========================================================
    // GENERAR FOLIO TEMPORAL
    // =========================================================

    private static string GenerarFolioTemporal()
    {
        /*
         * TEMPORAL.
         *
         * Luego utilizaremos la serie de ticket
         * y probablemente ultimo_folio de la caja.
         *
         * Ejemplo actual:
         *
         * POS-20260817123456789
         */

        return
            $"POS-{DateTime.Now:yyyyMMddHHmmssfff}";
    }


    // =========================================================
    // OBTENER INICIAL
    // =========================================================

    private static string ObtenerInicial(
        string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "?";

        return nombre
            .Trim()
            .Substring(0, 1)
            .ToUpperInvariant();
    }
     
    // =========================================================
    // Quitar el producto del carrito
    // =========================================================
    
    
    private void RestaurarDetalleEnCarrito(
        DetalleVenta detalle,
        int indiceOriginal)
    {
        if (Carrito.Contains(detalle))
            return;


        if (
            indiceOriginal >= 0 &&
            indiceOriginal <= Carrito.Count
        )
        {
            Carrito.Insert(
                indiceOriginal,
                detalle);
        }
        else
        {
            Carrito.Add(
                detalle);
        }


        ActualizarTotales();
    }
   
    // =========================================================
    // Quitar todos los productos del carrito
    // =========================================================

    
    private void RestaurarCarritoCancelado(
        System.Collections.Generic.List<DetalleVenta> carritoAnterior,
        int idVentaAnterior,
        string uuidAnterior,
        string folioAnterior)
    {
        IdVentaActual =
            idVentaAnterior;

        UuidVentaActual =
            uuidAnterior;

        FolioVentaActual =
            folioAnterior;


        Carrito.Clear();


        foreach (var detalle in carritoAnterior)
        {
            Carrito.Add(
                detalle);
        }


        ActualizarTotales();
    }


    // =========================================================
    // ACTUALIZAR TOTALES
    // =========================================================

    private void ActualizarTotales()
    {
        OnPropertyChanged(
            nameof(CarritoVacio));

        OnPropertyChanged(
            nameof(TieneProductos));

        OnPropertyChanged(
            nameof(CantidadProductos));

        OnPropertyChanged(
            nameof(Subtotal));

        OnPropertyChanged(
            nameof(Total));

        OnPropertyChanged(
            nameof(TextoCantidadProductos));
    }
}