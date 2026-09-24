using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Credito;
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
using System.Collections.Generic;

using NovaCoreESDM.Models.Clientes;
using NovaCoreESDM.Services.Clientes;
using NovaCoreESDM.Services.Credito;
using NovaCoreESDM.Desktop.Services.MercadoPago;


public partial class VentasViewModel : ViewModelBase
{
    // =========================================================
    // SERVICES
    // =========================================================

    private readonly ProductosService _productosService;

    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly VentasService _ventasService;
    
    private readonly MercadoPagoService _mercadoPagoService;
    
    private readonly TicketVentaService
        _ticketVentaService =
            new();
    
    private readonly WindowsRawPrinterService
        _printerService =
            new();
    
    private readonly WhatsAppService
        _whatsAppService;
    
    private readonly ProductoImagenService
        _productoImagenService;
    
    private readonly List<ProductoCatalogo>
        _productosCatalogoCompleto =
            new();
    
    private readonly ClientesService
        _clientesService;
    
    private readonly CreditoService
        _creditoService;


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
    
    // =========================================================
    // CLIENTES
    // =========================================================

    public ObservableCollection<ClientePos>
        ClientesEncontrados { get; } =
        new();


    [ObservableProperty]
    private string _textoBusquedaCliente =
        string.Empty;


    [ObservableProperty]
    private ClientePos? _clientePosSeleccionado;


    [ObservableProperty]
    private bool _mostrarSugerenciasClientes;


    // Evita volver a buscar cuando nosotros mismos
    // escribimos el nombre del cliente seleccionado.
    private bool
        _ignorandoCambioBusquedaCliente;


    // Sirve para ignorar respuestas viejas si el cajero
    // escribe muy rápido.
    private int
        _versionBusquedaCliente;
    

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
    // SOLICITAR CAMBIO DE PRECIO
    // =========================================================

    public event Action<DetalleVenta>?
        SolicitarCambioPrecio;
    
    
    
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
        
        _mercadoPagoService =
            new MercadoPagoService();

        _ticketVentaService =
            new TicketVentaService();

        _whatsAppService =
            new WhatsAppService();
        
        _productoImagenService =
            new ProductoImagenService();
        
        _clientesService =
            new ClientesService();
        
        _creditoService =
            new CreditoService();
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

        _productosCatalogoCompleto.Clear();

        foreach (var producto in productosPuntoVenta)
        {
            _productosCatalogoCompleto.Add(
                producto
            );

            Productos.Add(
                producto
            );
        }


        // =====================================================
        // CARGAR IMÁGENES EN SEGUNDO PLANO
        // =====================================================

        /*
         * No usamos await aquí.
         *
         * Queremos que los productos aparezcan inmediatamente
         * y que las fotografías se carguen poco a poco
         * sin bloquear el Punto de Venta.
         */

        _ = CargarImagenesProductosAsync();


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
                    ClientePosSeleccionado?.Id,

                TipoCliente =
                    ClientePosSeleccionado is null
                        ? "PUBLICO_GENERAL"
                        : "FRECUENTE",

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
// PRECIO CONFIRMADO POR BACKEND
// =====================================================

        nuevoDetalle.PrecioUnitario =
            resultadoDetalle.Data.PrecioUnitario;


        nuevoDetalle.PrecioAutomatico =
            resultadoDetalle.Data.PrecioAutomatico;


        nuevoDetalle.TipoPrecioMaximo =
            resultadoDetalle.Data.TipoPrecioMaximo;


        nuevoDetalle.TipoPrecioAplicado =
            resultadoDetalle.Data.TipoPrecioAplicado;


        nuevoDetalle.IdPrecioAplicado =
            resultadoDetalle.Data.IdPrecioAplicado;


        nuevoDetalle.IdReglaAplicada =
            resultadoDetalle.Data.IdReglaAplicada;


        nuevoDetalle.CantidadMinimaPrecio =
            resultadoDetalle.Data.CantidadMinimaPrecio;


        nuevoDetalle.TiposDisponibles =
            resultadoDetalle.Data.TiposDisponibles
            ?? new List<TipoPrecioDisponible>();


        nuevoDetalle.NivelAutomaticoPorCantidad =
            resultadoDetalle.Data.NivelAutomaticoPorCantidad;


        nuevoDetalle.SiguienteNivel =
            resultadoDetalle.Data.SiguienteNivel;


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


// =========================================================
// CARGAR IMÁGENES DE PRODUCTOS
// =========================================================

private async Task CargarImagenesProductosAsync()
{
    foreach (var producto in Productos)
    {
        // =====================================================
        // PRODUCTO SIN IMAGEN
        // =====================================================

        if (producto.ImagenUrl is null)
            continue;


        try
        {
            // =================================================
            // DESCARGAR IMAGEN
            // =================================================

            var bitmap =
                await _productoImagenService
                    .CargarImagenAsync(
                        producto.ImagenUrl
                    );


            if (bitmap is null)
                continue;


            // =================================================
            // ASIGNAR BITMAP AL PRODUCTO
            // =================================================

            producto.ImagenBitmap =
                bitmap;
        }
        catch (Exception ex)
        {
            /*
             * Una imagen dañada o inexistente
             * NO debe impedir que el POS funcione.
             */

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "ERROR CARGANDO IMAGEN DE PRODUCTO:");

            Console.WriteLine(
                producto.NombreComercial);

            Console.WriteLine(
                producto.ImagenUrl);

            Console.WriteLine(
                ex.Message);

            Console.WriteLine(
                "====================================");
        }
    }
}

// =========================================================
// PROCESAR CÓDIGO ESCANEADO
// =========================================================

public async Task ProcesarCodigoEscaneadoAsync(
    string codigo)
{
    if (string.IsNullOrWhiteSpace(codigo))
        return;


    codigo =
        codigo.Trim();


    MensajeError =
        string.Empty;


    // =====================================================
    // 1. BUSCAR PRIMERO POR CÓDIGO DE PRESENTACIÓN
    // =====================================================

    foreach (var producto in
             _productosCatalogoCompleto)
    {
        if (producto is null)
            continue;


        if (producto.Presentaciones is null)
            continue;


        var presentacion =
            producto.Presentaciones
                .FirstOrDefault(
                    p =>
                        p is not null
                        &&
                        p.Estatus == 1
                        &&
                        !string.IsNullOrWhiteSpace(
                            p.CodigoBarras
                        )
                        &&
                        string.Equals(
                            p.CodigoBarras.Trim(),
                            codigo,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


        if (presentacion is null)
            continue;


        // =================================================
        // CÓDIGO IDENTIFICA EXACTAMENTE UNA PRESENTACIÓN
        // =================================================

        /*
         * Aquí NO mostramos selector.
         *
         * Ejemplo:
         *
         * Coca Cola 600 ml
         * código = 7501234567890
         *
         * Ya sabemos exactamente qué presentación
         * escaneó el cajero.
         */

        await AgregarPresentacionEscaneadaAsync(
            producto,
            presentacion
        );


        return;
    }


    // =====================================================
    // 2. BUSCAR POR CÓDIGO GENERAL DEL PRODUCTO
    // =====================================================

    var productoEncontrado =
        _productosCatalogoCompleto
            .FirstOrDefault(
                p =>
                    p is not null
                    &&
                    p.Estatus == 1
                    &&
                    p.AplicaVenta
                    &&
                    !string.IsNullOrWhiteSpace(
                        p.CodigoBarras
                    )
                    &&
                    string.Equals(
                        p.CodigoBarras.Trim(),
                        codigo,
                        StringComparison.OrdinalIgnoreCase
                    )
            );


    // =====================================================
    // 3. NO EXISTE EL CÓDIGO
    // =====================================================

    if (productoEncontrado is null)
    {
        MensajeError =
            $"No se encontró ningún producto con el código de barras {codigo}.";

        return;
    }


    // =====================================================
    // 4. VALIDAR PRESENTACIONES
    // =====================================================

    if (
        productoEncontrado.Presentaciones is null
        ||
        productoEncontrado.Presentaciones.Count == 0
    )
    {
        MensajeError =
            $"El producto {productoEncontrado.NombreComercial} no tiene presentaciones configuradas.";

        return;
    }


    var presentacionesActivas =
        productoEncontrado
            .Presentaciones
            .Where(
                p =>
                    p is not null
                    &&
                    p.Estatus == 1
            )
            .ToList();


    if (presentacionesActivas.Count == 0)
    {
        MensajeError =
            $"El producto {productoEncontrado.NombreComercial} no tiene presentaciones activas.";

        return;
    }


    // =====================================================
    // 5. SOLO TIENE UNA PRESENTACIÓN
    // =====================================================

    if (presentacionesActivas.Count == 1)
    {
        /*
         * Si solamente existe una presentación,
         * no tiene sentido preguntarle al cajero.
         */

        await AgregarPresentacionEscaneadaAsync(
            productoEncontrado,
            presentacionesActivas[0]
        );


        return;
    }


    // =====================================================
    // 6. TIENE VARIAS PRESENTACIONES
    // =====================================================

    /*
     * Aquí el código pertenece al producto general.
     *
     * Ejemplo:
     *
     * Coca Cola
     *
     * Presentaciones:
     *
     * - 600 ml
     * - 1.5 L
     * - 2 L
     *
     * Como no sabemos cuál quiere vender,
     * abrimos automáticamente el selector.
     */

    SolicitarSeleccionPresentacion?.Invoke(
        productoEncontrado
    );
}


// =========================================================
// AGREGAR PRESENTACIÓN DESDE ESCÁNER
// =========================================================

private async Task AgregarPresentacionEscaneadaAsync(
    ProductoCatalogo producto,
    PresentacionProducto presentacion)
{
    // =====================================================
    // 1. ¿YA ESTÁ EN EL CARRITO?
    // =====================================================

    var detalleExistente =
        Carrito
            .FirstOrDefault(
                detalle =>
                    detalle.Producto.Id ==
                        producto.Id
                    &&
                    detalle.Producto.IdPresentacion ==
                        presentacion.Id
            );


    // =====================================================
    // 2. YA EXISTE → AUMENTAR CANTIDAD
    // =====================================================

    if (detalleExistente is not null)
    {
        /*
         * No resolvemos ningún precio aquí.
         *
         * IncrementarCantidadAsync envía la nueva cantidad
         * al backend y update_detalle.php decide:
         *
         * - El nivel correspondiente.
         * - El precio unitario.
         * - El siguiente nivel.
         *
         * respetando además si la línea está automática
         * o tiene un precio manual.
         */

        await IncrementarCantidadAsync(
            detalleExistente
        );


        return;
    }


    // =====================================================
    // 3. VALIDAR PRESENTACIÓN
    // =====================================================

    if (presentacion.Id <= 0)
    {
        MensajeError =
            "La presentación escaneada no es válida.";

        return;
    }


    // =====================================================
    // 4. CONVERTIR A PresentacionVentaItem
    // =====================================================

    /*
     * IMPORTANTE:
     *
     * Ya NO buscamos:
     *
     * MENUDEO
     * MAYOREO
     * DISTRIBUIDOR
     * etc.
     *
     * Tampoco intentamos calcular cuál es el nivel base.
     *
     * Esa información pertenece a:
     *
     * tm_productos_reglas_precios
     * +
     * tm_productos_precios
     *
     * y la resuelve detalle.php.
     *
     *
     * Precio = 0 aquí es únicamente un valor provisional.
     *
     * En cuanto detalle.php confirma la inserción,
     * AgregarPresentacionAlCarritoAsync sustituye
     * PrecioUnitario por el precio oficial devuelto
     * por el backend.
     *
     * Si la presentación tiene una configuración inválida
     * de precios, detalle.php rechazará la operación y
     * mostrará el mensaje correspondiente.
     */

    var presentacionVenta =
        new PresentacionVentaItem
        {
            IdPresentacion =
                presentacion.Id,

            NombrePresentacion =
                presentacion.NombrePresentacion,

            UnidadMedida =
                presentacion.UnidadMedida,

            FactorConversion =
                presentacion.FactorConversion,

            CodigoBarras =
                presentacion.CodigoBarras,

            /*
             * Precio provisional.
             *
             * NO es el precio de venta definitivo.
             */
            Precio =
                0m,

            /*
             * Permitimos llegar al backend para que sea
             * éste quien valide la configuración real.
             */
            TienePrecio =
                true
        };


    // =====================================================
    // 5. AGREGAR AL CARRITO
    // =====================================================

    await AgregarPresentacionAlCarritoAsync(
        producto,
        presentacionVenta
    );
}


// =========================================================
// CAMBIAR PRECIO
// =========================================================

[RelayCommand]
private void CambiarPrecio(
    DetalleVenta? detalle)
{
    if (detalle is null)
        return;

    if (detalle.EstaSincronizando)
        return;

    if (detalle.IdDetalle <= 0)
    {
        MensajeError =
            "El producto todavía no está sincronizado.";

        return;
    }

    SolicitarCambioPrecio?.Invoke(
        detalle);
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
// APLICAR RESPUESTA DEL BACKEND AL DETALLE
// =========================================================
//
// Centralizamos aquí toda la información que devuelve
// update_detalle.php.
//
// De esta forma:
//
// - Incrementar cantidad
// - Disminuir cantidad
// - Editar cantidad
// - Precio manual
// - Regresar a automático
//
// actualizan exactamente las mismas propiedades.
//
// El backend sigue siendo la autoridad del precio.
// =========================================================

private void AplicarActualizacionDetalle(
    DetalleVenta detalle,
    ActualizarCantidadVentaResponse resultado)
{
    if (detalle is null)
        return;


    // =====================================================
    // DETALLE GUARDADO
    // =====================================================

    if (resultado.Data?.Detalle is not null)
    {
        var detalleBackend =
            resultado.Data.Detalle;


        detalle.Cantidad =
            (int)detalleBackend.CantidadComercial;


        detalle.PrecioUnitario =
            detalleBackend.PrecioUnitario;


        detalle.PrecioAutomatico =
            detalleBackend.PrecioAutomatico;


        detalle.TipoPrecioMaximo =
            detalleBackend.TipoPrecioMaximo;


        detalle.TipoPrecioAplicado =
            detalleBackend.TipoPrecioAplicado;


        detalle.IdPrecioAplicado =
            detalleBackend.IdPrecioAplicado;
    }


    // =====================================================
    // INFORMACIÓN COMPLETA DEL PRECIO
    // =====================================================

    if (resultado.Data?.Precio is not null)
    {
        var precio =
            resultado.Data.Precio;


        detalle.PrecioAutomatico =
            precio.PrecioAutomatico;


        detalle.TipoPrecioMaximo =
            precio.TipoPrecioMaximo;


        detalle.TipoPrecioAplicado =
            precio.TipoPrecioAplicado;


        detalle.IdPrecioAplicado =
            precio.IdPrecioAplicado;


        detalle.IdReglaAplicada =
            precio.IdReglaAplicada;


        detalle.CantidadMinimaPrecio =
            precio.CantidadMinima;


        detalle.PrecioUnitario =
            precio.PrecioUnitario;


        detalle.TiposDisponibles =
            precio.TiposDisponibles
            ?? new List<TipoPrecioDisponible>();


        detalle.NivelAutomaticoPorCantidad =
            precio.NivelAutomaticoPorCantidad;


        detalle.SiguienteNivel =
            precio.SiguienteNivel;
    }


    // =====================================================
    // INVENTARIO
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


    // =====================================================
    // ACTUALIZAR TOTALES
    // =====================================================

    ActualizarTotales();
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
            AplicarActualizacionDetalle(
                detalle,
                resultado);


            MensajeError =
                string.Empty;


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
            
            
            // =====================================================
            // RECUPERAR ESTADO DEL PRECIO
            // =====================================================

            nuevoDetalle.PrecioUnitario =
                detalle.PrecioUnitario;

            nuevoDetalle.PrecioAutomatico =
                detalle.PrecioAutomatico;

            nuevoDetalle.TipoPrecioMaximo =
                detalle.TipoPrecioMaximo;

            nuevoDetalle.TipoPrecioAplicado =
                detalle.TipoPrecioAplicado;

            nuevoDetalle.IdPrecioAplicado =
                detalle.IdPrecioAplicado;
            
            
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
            AplicarActualizacionDetalle(
                detalle,
                resultado);


            MensajeError =
                string.Empty;


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
        UltimoMontoRecibido =
            recibido;

        UltimoCambio =
            cambio;


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
                    request
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


        // =====================================================
        // PAGO COMPLETO → ABRIR CAJÓN
        // =====================================================

        try
        {
            var cajonAbierto =
                await _printerService
                    .AbrirCajonAsync();


            if (cajonAbierto)
            {
                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "CAJÓN DE EFECTIVO ABIERTO");

                Console.WriteLine(
                    $"Recibido: ${recibido:N2}");

                Console.WriteLine(
                    $"Cambio: ${cambio:N2}");

                Console.WriteLine(
                    "====================================");
            }
            else
            {
                Console.WriteLine(
                    "El pago fue registrado, pero el cajón no confirmó la apertura."
                );
            }
        }
        catch (Exception exCajon)
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "PAGO REGISTRADO, PERO FALLÓ LA APERTURA DEL CAJÓN:");

            Console.WriteLine(
                exCajon.Message);

            Console.WriteLine(
                "====================================");
        }


        // =====================================================
        // PAGO COMPLETO → FINALIZAR VENTA
        // =====================================================

        var finalizada =
            await FinalizarVentaAsync();


        if (!finalizada)
        {
            return false;
        }


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
// FINALIZAR VENTA A CRÉDITO
// =========================================================

public async Task<bool> FinalizarVentaCreditoAsync()
{
    // =====================================================
    // VALIDAR VENTA
    // =====================================================

    if (IdVentaActual <= 0)
    {
        MensajeError =
            "No existe una venta activa.";

        return false;
    }
    


    // =====================================================
    // VALIDAR CLIENTE
    // =====================================================

    if (ClientePosSeleccionado is null)
    {
        MensajeError =
            "Selecciona un cliente para realizar la venta a crédito.";

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
        // =================================================
        // 1. MARCAR LA VENTA COMO CRÉDITO
        // =================================================

        var request =
            new ActualizarVentaRequest
            {
                IdCliente =
                    ClientePosSeleccionado.Id,

                TipoCliente =
                    "FRECUENTE",

                TipoVenta =
                    "CREDITO"
            };


        var resultadoActualizar =
            await _ventasService
                .ActualizarVentaAsync(
                    IdVentaActual,
                    request
                );


        if (resultadoActualizar.Res != 1)
        {
            MensajeError =
                string.IsNullOrWhiteSpace(
                    resultadoActualizar.Msg)
                    ? "No fue posible preparar la venta a crédito."
                    : resultadoActualizar.Msg;

            return false;
        }


        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "VENTA MARCADA COMO CRÉDITO");

        Console.WriteLine(
            $"ID Venta: {IdVentaActual}");

        Console.WriteLine(
            $"Cliente: {ClientePosSeleccionado.Nombre}");

        Console.WriteLine(
            $"Total: ${Total:N2}");

        Console.WriteLine(
            "====================================");


        // =================================================
        // 2. FINALIZAR
        // =================================================
        //
        // Aquí entra nuestro finalizar.php actualizado.
        //
        // El backend:
        //
        // - vuelve a validar el crédito;
        // - descuenta inventario;
        // - genera ticket;
        // - finaliza la venta;
        // - genera REMISIÓN PPD;
        // - genera movimiento CARGO.
        //
        // Y FinalizarVentaAsync después procesa
        // impresión / WhatsApp / ticket.
        // =================================================

        var finalizada =
            await FinalizarVentaAsync();


        if (!finalizada)
        {
            /*
             * NO regresamos automáticamente a CONTADO.
             *
             * Esto es intencional.
             *
             * Si hubo un problema de red justo después
             * del COMMIT, no queremos hacer otro cambio
             * sin conocer el estado real del servidor.
             *
             * La venta se puede recuperar posteriormente.
             */

            return false;
        }


        return true;
    }
    catch (Exception ex)
    {
        MensajeError =
            $"No fue posible realizar la venta a crédito: {ex.Message}";

        return false;
    }
    finally
    {
        EstaProcesandoVenta =
            false;
    }
}



// =========================================================
// ESTABLECER PRECIO MANUAL
// =========================================================

public async Task<bool> EstablecerPrecioManualAsync(
    DetalleVenta detalle,
    string tipoPrecio)
{
    if (detalle is null)
        return false;

    if (IdVentaActual <= 0 ||
        detalle.IdDetalle <= 0)
    {
        MensajeError =
            "No existe una venta válida para modificar el precio.";

        return false;
    }

    if (string.IsNullOrWhiteSpace(tipoPrecio))
    {
        MensajeError =
            "Debe seleccionar un tipo de precio.";

        return false;
    }

    if (detalle.EstaSincronizando)
        return false;


    MensajeError =
        string.Empty;

    detalle.EstaSincronizando =
        true;

    detalle.TieneErrorSincronizacion =
        false;


    try
    {
        var resultado =
            await _ventasService
                .EstablecerPrecioManualAsync(
                    IdVentaActual,
                    detalle.IdDetalle,
                    tipoPrecio);


        if (resultado.Res != 1)
        {
            detalle.TieneErrorSincronizacion =
                true;

            MensajeError =
                string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "No fue posible cambiar el precio."
                    : resultado.Msg;

            return false;
        }


        AplicarActualizacionDetalle(
            detalle,
            resultado);


        MensajeError =
            string.Empty;

        return true;
    }
    catch (Exception ex)
    {
        detalle.TieneErrorSincronizacion =
            true;

        MensajeError =
            $"No fue posible cambiar el precio: {ex.Message}";

        return false;
    }
    finally
    {
        detalle.EstaSincronizando =
            false;
    }
}

// =========================================================
// ESTABLECER PRECIO AUTOMÁTICO
// =========================================================

public async Task<bool> EstablecerPrecioAutomaticoAsync(
    DetalleVenta detalle)
{
    if (detalle is null)
        return false;

    if (IdVentaActual <= 0 ||
        detalle.IdDetalle <= 0)
    {
        MensajeError =
            "No existe una venta válida para modificar el precio.";

        return false;
    }

    if (detalle.EstaSincronizando)
        return false;


    MensajeError =
        string.Empty;

    detalle.EstaSincronizando =
        true;

    detalle.TieneErrorSincronizacion =
        false;


    try
    {
        var resultado =
            await _ventasService
                .EstablecerPrecioAutomaticoAsync(
                    IdVentaActual,
                    detalle.IdDetalle);


        if (resultado.Res != 1)
        {
            detalle.TieneErrorSincronizacion =
                true;

            MensajeError =
                string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "No fue posible regresar al precio automático."
                    : resultado.Msg;

            return false;
        }


        AplicarActualizacionDetalle(
            detalle,
            resultado);


        MensajeError =
            string.Empty;

        return true;
    }
    catch (Exception ex)
    {
        detalle.TieneErrorSincronizacion =
            true;

        MensajeError =
            $"No fue posible regresar al precio automático: {ex.Message}";

        return false;
    }
    finally
    {
        detalle.EstaSincronizando =
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
                                detalle.PrecioUnitario,

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
// PROBAR ESTADO DE CRÉDITO
// =========================================================

public async Task<bool> ConsultarCreditoActualAsync()
{
    MensajeError =
        string.Empty;


    // =====================================================
    // VALIDAR CLIENTE
    // =====================================================

    if (ClientePosSeleccionado is null)
    {
        MensajeError =
            "Selecciona un cliente para realizar una venta a crédito.";

        return false;
    }


    // =====================================================
    // VALIDAR VENTA
    // =====================================================

    if (Carrito.Count == 0)
    {
        MensajeError =
            "Agrega productos antes de consultar el crédito.";

        return false;
    }


    if (Total <= 0)
    {
        MensajeError =
            "El total de la venta no es válido.";

        return false;
    }


    // =====================================================
    // CONSULTAR MOTOR CENTRAL
    // =====================================================

    var resultado =
        await _creditoService
            .ConsultarEstadoAsync(
                ClientePosSeleccionado.Id,
                Total
            );


    if (
        resultado.Res != 1 ||
        resultado.Data is null
    )
    {
        MensajeError =
            string.IsNullOrWhiteSpace(
                resultado.Msg)
                ? "No fue posible consultar el crédito del cliente."
                : resultado.Msg;

        return false;
    }


    // =====================================================
    // CRÉDITO NO AUTORIZADO
    // =====================================================

    if (!resultado.Data.PuedeVenderCredito)
    {
        var motivos =
            resultado.Data
                .MotivosBloqueo;


        MensajeError =
            motivos.Count > 0
                ? string.Join(
                    " ",
                    motivos
                )
                : "El cliente no tiene crédito disponible para esta venta.";

        return false;
    }


    // =====================================================
    // CRÉDITO AUTORIZADO
    // =====================================================

    Console.WriteLine(
        "====================================");

    Console.WriteLine(
        "CRÉDITO AUTORIZADO");

    Console.WriteLine(
        $"Cliente: {ClientePosSeleccionado.Nombre}");

    Console.WriteLine(
        $"Id cliente: {ClientePosSeleccionado.Id}");

    Console.WriteLine(
        $"Total venta: ${Total:N2}");

    Console.WriteLine(
        "====================================");


    MensajeError =
        string.Empty;


    return true;
}


public async Task<EstadoCreditoResponse?>
    ObtenerCreditoActualAsync()
{
    MensajeError =
        string.Empty;


    if (ClientePosSeleccionado is null)
    {
        MensajeError =
            "Selecciona un cliente para realizar una venta a crédito.";

        return null;
    }


    if (Carrito.Count == 0)
    {
        MensajeError =
            "Agrega productos antes de consultar el crédito.";

        return null;
    }


    if (Total <= 0)
    {
        MensajeError =
            "El total de la venta no es válido.";

        return null;
    }


    var resultado =
        await _creditoService
            .ConsultarEstadoAsync(
                ClientePosSeleccionado.Id,
                Total
            );


    if (
        resultado.Res != 1 ||
        resultado.Data is null
    )
    {
        MensajeError =
            string.IsNullOrWhiteSpace(resultado.Msg)
                ? "No fue posible consultar el crédito del cliente."
                : resultado.Msg;

        return null;
    }


    return resultado;
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
            AplicarActualizacionDetalle(
                detalle,
                resultado);


            MensajeError =
                string.Empty;


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
// ¿REQUIERE CONCILIACIÓN CON MERCADO PAGO?
// =====================================================

if (
    resultado.RequiereConciliacionMp &&
    resultado.IdOperacionMp.HasValue &&
    resultado.IdOperacionMp.Value > 0
)
{
    var conciliacion =
        await ConciliarOperacionMercadoPagoAsync(
            resultado.IdOperacionMp.Value);


    // =================================================
    // TERMINÓ SIN PAGO
    // =================================================

    if (conciliacion.PuedeReintentarOperacion)
    {
        /*
         * estado_orden.php ya confirmó que la operación
         * terminó sin pago.
         *
         * Volvemos a ejecutar delete_detalle.php.
         *
         * El PHP nuevo podrá eliminar las operaciones MP
         * seguras y después eliminar el producto.
         */

        var segundoIntento =
            await _ventasService
                .EliminarProductoAsync(
                    idVenta,
                    idDetalle);


        if (segundoIntento.Res == 1)
        {
            MensajeError =
                string.Empty;


            if (
                segundoIntento.Data?.VentaEliminada ==
                true
            )
            {
                IdVentaActual =
                    0;

                UuidVentaActual =
                    string.Empty;

                FolioVentaActual =
                    string.Empty;

                Carrito.Clear();

                ActualizarTotales();
            }


            return;
        }


        /*
         * Por seguridad NO hacemos un tercer intento
         * automáticamente.
         */

        RestaurarDetalleEnCarrito(
            detalle,
            indiceOriginal);


        MensajeError =
            string.IsNullOrWhiteSpace(
                segundoIntento.Msg)
                ? "No fue posible eliminar el producto después de conciliar Mercado Pago."
                : segundoIntento.Msg;


        return;
    }


    // =================================================
    // ACREDITADO O TODAVÍA INCIERTO
    // =================================================

    RestaurarDetalleEnCarrito(
        detalle,
        indiceOriginal);


    if (conciliacion.PagoRegistrado)
    {
        /*
         * Recuperamos la venta desde backend.
         *
         * Esto es especialmente importante si
         * estado_orden.php acaba de insertar el pago.
         */

        await CargarVentaActualAsync();
    }


    MensajeError =
        conciliacion.Mensaje;


    return;
}


// =====================================================
// BACKEND RECHAZÓ POR OTRO MOTIVO
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
        // ¿REQUIERE CONCILIACIÓN CON MERCADO PAGO?
        // =====================================================

        if (
            resultado.RequiereConciliacionMp &&
            resultado.IdOperacionMp.HasValue &&
            resultado.IdOperacionMp.Value > 0
        )
        {
            var conciliacion =
                await ConciliarOperacionMercadoPagoAsync(
                    resultado.IdOperacionMp.Value);


            // =================================================
            // TERMINÓ SIN PAGO → REINTENTAR CANCELACIÓN
            // =================================================

            if (conciliacion.PuedeReintentarOperacion)
            {
                var segundoIntento =
                    await _ventasService
                        .CancelarVentaAsync(
                            idVenta);


                if (segundoIntento.Res == 1)
                {
                    /*
                     * La UI ya estaba limpia por tu operación
                     * optimista.
                     *
                     * Confirmamos que tampoco quede referencia
                     * a la venta anterior.
                     */

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


                    return;
                }


                // Segundo intento rechazado.
                RestaurarCarritoCancelado(
                    carritoAnterior,
                    idVenta,
                    uuidAnterior,
                    folioAnterior);


                MensajeError =
                    string.IsNullOrWhiteSpace(
                        segundoIntento.Msg)
                        ? "No fue posible cancelar el carrito después de conciliar Mercado Pago."
                        : segundoIntento.Msg;


                return;
            }


            // =================================================
            // ACREDITADO O ESTADO INCIERTO
            // =================================================

            RestaurarCarritoCancelado(
                carritoAnterior,
                idVenta,
                uuidAnterior,
                folioAnterior);


            if (conciliacion.PagoRegistrado)
            {
                await CargarVentaActualAsync();
            }


            MensajeError =
                conciliacion.Mensaje;


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
// PRUEBA DE CAJÓN
// =========================================================

    [RelayCommand]
    private async Task AbrirCajonPruebaAsync()
    {
        MensajeError =
            string.Empty;


        try
        {
            var resultado =
                await _printerService
                    .AbrirCajonAsync();


            if (!resultado)
            {
                MensajeError =
                    "La impresora no confirmó el comando de apertura del cajón.";

                return;
            }


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "COMANDO DE APERTURA DEL CAJÓN ENVIADO");

            Console.WriteLine(
                "====================================");
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible abrir el cajón: {ex.Message}";
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
// CONCILIAR OPERACIÓN MERCADO PAGO
// =========================================================

private async Task<ResultadoConciliacionMercadoPago>
    ConciliarOperacionMercadoPagoAsync(
        long idOperacionMp)
{
    if (idOperacionMp <= 0)
    {
        return new ResultadoConciliacionMercadoPago
        {
            PuedeReintentarOperacion = false,
            PagoRegistrado = false,
            Mensaje =
                "La operación de Mercado Pago no es válida."
        };
    }


    try
    {
        Console.WriteLine(
            "====================================");

        Console.WriteLine(
            "CONCILIANDO OPERACIÓN MERCADO PAGO");

        Console.WriteLine(
            $"ID operación MP: {idOperacionMp}");

        Console.WriteLine(
            "====================================");


        var resultado =
            await _mercadoPagoService
                .ObtenerEstadoOrdenAsync(
                    idOperacionMp);


        // =====================================================
        // PAGO YA REGISTRADO EN NOVACORE
        // =====================================================

        if (
            resultado.Data?.PagoRegistrado == true
        )
        {
            return new ResultadoConciliacionMercadoPago
            {
                PuedeReintentarOperacion = false,
                PagoRegistrado = true,
                Mensaje =
                    "El pago con Mercado Pago fue acreditado. " +
                    "La venta ya no puede modificarse."
            };
        }


        // =====================================================
        // OPERACIÓN TERMINÓ SIN PAGO
        // =====================================================

        var status =
            resultado.Data?.Status?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        if (
            status == "CANCELED" ||
            status == "CANCELLED" ||
            status == "EXPIRED" ||
            status == "FAILED" ||
            status == "REJECTED" ||
            status == "CANCELADO" ||
            status == "EXPIRADO" ||
            status == "RECHAZADO"
        )
        {
            return new ResultadoConciliacionMercadoPago
            {
                PuedeReintentarOperacion = true,
                PagoRegistrado = false,
                Mensaje =
                    "La operación de Mercado Pago terminó sin pago."
            };
        }


        // =====================================================
        // TODAVÍA NO SABEMOS CON CERTEZA EL RESULTADO
        // =====================================================

        return new ResultadoConciliacionMercadoPago
        {
            PuedeReintentarOperacion = false,
            PagoRegistrado = false,
            Mensaje =
                string.IsNullOrWhiteSpace(resultado.Msg)
                    ? "La operación de Mercado Pago todavía está pendiente. No se modificó la venta."
                    : resultado.Msg
        };
    }
    catch (Exception ex)
    {
        /*
         * MUY IMPORTANTE:
         *
         * Un error de red NO significa que el pago falló.
         *
         * Por eso nunca permitimos borrar/modificar
         * la venta solamente porque esta consulta falló.
         */

        return new ResultadoConciliacionMercadoPago
        {
            PuedeReintentarOperacion = false,
            PagoRegistrado = false,
            Mensaje =
                $"No fue posible confirmar el estado del pago con Mercado Pago: {ex.Message}"
        };
    }
}


    private sealed class ResultadoConciliacionMercadoPago
    {
        public bool PuedeReintentarOperacion { get; init; }

        public bool PagoRegistrado { get; init; }

        public string Mensaje { get; init; } =
            string.Empty;
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
// FILTRAR PRODUCTOS
// =========================================================

private void FiltrarProductos(
    string texto)
{
    texto =
        NormalizarTexto(
            texto
        );


    // =====================================================
    // SIN BÚSQUEDA → MOSTRAR TODO
    // =====================================================

    if (string.IsNullOrWhiteSpace(texto))
    {
        Productos.Clear();

        foreach (var producto in
                 _productosCatalogoCompleto)
        {
            Productos.Add(
                producto
            );
        }

        OnPropertyChanged(
            nameof(TieneProductosDisponibles));

        return;
    }


    // =====================================================
    // BUSCAR Y CALCULAR RELEVANCIA
    // =====================================================

    var resultados =
        _productosCatalogoCompleto
            .Select(
                producto =>
                    new
                    {
                        Producto =
                            producto,

                        Puntaje =
                            CalcularPuntajeBusqueda(
                                producto,
                                texto
                            )
                    })
            .Where(
                x =>
                    x.Puntaje > 0
            )
            .OrderByDescending(
                x =>
                    x.Puntaje
            )
            .ThenBy(
                x =>
                    x.Producto
                        .NombreComercial
            )
            .Select(
                x =>
                    x.Producto
            )
            .ToList();


    // =====================================================
    // ACTUALIZAR RESULTADOS VISIBLES
    // =====================================================

    Productos.Clear();


    foreach (var producto in resultados)
    {
        Productos.Add(
            producto
        );
    }


    OnPropertyChanged(
        nameof(TieneProductosDisponibles));
}


// =========================================================
// CALCULAR RELEVANCIA DE BÚSQUEDA
// =========================================================

private static int CalcularPuntajeBusqueda(
    ProductoCatalogo producto,
    string busqueda)
{
    var nombre =
        NormalizarTexto(
            producto.NombreComercial
        );


    var codigo =
        NormalizarTexto(
            producto.Codigo
        );


    var sku =
        NormalizarTexto(
            producto.Sku
        );


    var codigoBarras =
        NormalizarTexto(
            producto.CodigoBarras
        );


    // =====================================================
    // COINCIDENCIA EXACTA
    // =====================================================

    if (nombre == busqueda)
        return 1000;


    // =====================================================
    // NOMBRE COMIENZA CON LO ESCRITO
    // =====================================================

    if (nombre.StartsWith(
            busqueda))
    {
        return 900;
    }


    // =====================================================
    // EL NOMBRE CONTIENE LO ESCRITO
    // =====================================================

    if (nombre.Contains(
            busqueda))
    {
        return 800;
    }


    // =====================================================
    // CÓDIGO / SKU / BARRAS
    // =====================================================

    if (
        codigo.Contains(busqueda) ||
        sku.Contains(busqueda) ||
        codigoBarras.Contains(busqueda)
    )
    {
        return 700;
    }


    // =====================================================
    // BUSCAR PALABRAS INDIVIDUALES
    // =====================================================

    var palabrasNombre =
        nombre.Split(
            ' ',
            StringSplitOptions
                .RemoveEmptyEntries
        );


    foreach (var palabra in palabrasNombre)
    {
        if (palabra.StartsWith(
                busqueda))
        {
            return 650;
        }


        var distancia =
            CalcularDistanciaLevenshtein(
                palabra,
                busqueda
            );


        /*
         * Permitimos:
         *
         * palabra corta -> 1 error
         * palabra más larga -> hasta 2 errores
         */

        var tolerancia =
            Math.Max(
                palabra.Length,
                busqueda.Length
            ) >= 7
                ? 2
                : 1;


        if (distancia <= tolerancia)
        {
            return
                500 - distancia;
        }
    }


    return 0;
}


private static string NormalizarTexto(
    string? texto)
{
    if (string.IsNullOrWhiteSpace(
            texto))
    {
        return string.Empty;
    }


    var normalizado =
        texto
            .Trim()
            .ToLowerInvariant()
            .Normalize(
                System.Text
                    .NormalizationForm
                    .FormD
            );


    var caracteres =
        normalizado
            .Where(
                c =>
                    System.Globalization
                        .CharUnicodeInfo
                        .GetUnicodeCategory(c)
                    !=
                    System.Globalization
                        .UnicodeCategory
                        .NonSpacingMark
            )
            .ToArray();


    return new string(
            caracteres
        )
        .Normalize(
            System.Text
                .NormalizationForm
                .FormC
        );
}

private static int CalcularDistanciaLevenshtein(
    string origen,
    string destino)
{
    if (string.IsNullOrEmpty(origen))
        return destino.Length;


    if (string.IsNullOrEmpty(destino))
        return origen.Length;


    var matriz =
        new int[
            origen.Length + 1,
            destino.Length + 1
        ];


    for (
        var i = 0;
        i <= origen.Length;
        i++)
    {
        matriz[i, 0] =
            i;
    }


    for (
        var j = 0;
        j <= destino.Length;
        j++)
    {
        matriz[0, j] =
            j;
    }


    for (
        var i = 1;
        i <= origen.Length;
        i++)
    {
        for (
            var j = 1;
            j <= destino.Length;
            j++)
        {
            var costo =
                origen[i - 1] ==
                destino[j - 1]
                    ? 0
                    : 1;


            matriz[i, j] =
                Math.Min(
                    Math.Min(
                        matriz[i - 1, j] + 1,
                        matriz[i, j - 1] + 1
                    ),
                    matriz[i - 1, j - 1]
                    + costo
                );
        }
    }


    return matriz[
        origen.Length,
        destino.Length
    ];
}

partial void OnTextoBusquedaChanged(
    string value)
{
    FiltrarProductos(
        value
    );
}



    // =========================================================
    // CAMBIO EN TEXTO DE BÚSQUEDA DE CLIENTE
    // =========================================================

    partial void OnTextoBusquedaClienteChanged(
        string value)
    {
        if (_ignorandoCambioBusquedaCliente)
            return;


        _ =
            BuscarClientesAsync(
                value
            );
    }
    
    // =========================================================
// BUSCAR CLIENTES
// =========================================================

    private async Task BuscarClientesAsync(
        string texto)
    {
        texto =
            texto?.Trim()
            ?? string.Empty;


        // Cada nueva escritura obtiene una versión distinta.
        var versionActual =
            ++_versionBusquedaCliente;


        // =====================================================
        // MENOS DE 2 CARACTERES
        // =====================================================

        if (texto.Length < 2)
        {
            ClientesEncontrados.Clear();

            MostrarSugerenciasClientes =
                false;

            return;
        }


        /*
         * Pequeño debounce.
         *
         * Si escribe:
         *
         * E
         * ES
         * EST
         * ESTE
         *
         * evitamos intentar actualizar visualmente
         * una respuesta vieja.
         */

        await Task.Delay(
            250
        );


        if (versionActual !=
            _versionBusquedaCliente)
        {
            return;
        }


        var resultado =
            await _clientesService
                .BuscarClientesAsync(
                    texto
                );


        /*
         * Mientras la API respondía pudo haber
         * cambiado nuevamente el texto.
         */

        if (versionActual !=
            _versionBusquedaCliente)
        {
            return;
        }


        ClientesEncontrados.Clear();


        if (resultado.Res != 1)
        {
            MostrarSugerenciasClientes =
                false;

            MensajeError =
                resultado.Msg
                ?? "No fue posible buscar clientes.";

            return;
        }


        foreach (var cliente in
                 resultado.Data)
        {
            ClientesEncontrados.Add(
                cliente
            );
        }


        MostrarSugerenciasClientes =
            ClientesEncontrados.Count > 0;
    }
    
// =========================================================
// SELECCIONAR CLIENTE
// =========================================================

[RelayCommand]
private async Task SeleccionarClienteAsync(
    ClientePos? cliente)
{
    if (cliente is null)
        return;


    MensajeError =
        string.Empty;


    // =====================================================
    // GUARDAR ESTADO ANTERIOR
    // =====================================================

    var clienteAnterior =
        ClientePosSeleccionado;

    var nombreAnterior =
        ClienteSeleccionado;


    // =====================================================
    // UI OPTIMISTA
    // =====================================================

    ClientePosSeleccionado =
        cliente;

    ClienteSeleccionado =
        cliente.Nombre;


    _ignorandoCambioBusquedaCliente =
        true;

    TextoBusquedaCliente =
        cliente.Nombre;

    _ignorandoCambioBusquedaCliente =
        false;


    ClientesEncontrados.Clear();

    MostrarSugerenciasClientes =
        false;


    // =====================================================
    // TODAVÍA NO EXISTE VENTA
    // =====================================================

    /*
     * No hacemos PUT.
     *
     * Cuando se agregue el primer producto,
     * create.php utilizará ClientePosSeleccionado.Id.
     */

    if (IdVentaActual <= 0)
        return;


    // =====================================================
    // LA VENTA YA EXISTE → ACTUALIZAR CLIENTE
    // =====================================================

    var request =
        new ActualizarVentaRequest
        {
            IdCliente =
                cliente.Id,

            TipoCliente =
                "FRECUENTE"
        };


    var resultado =
        await _ventasService
            .ActualizarVentaAsync(
                IdVentaActual,
                request
            );


    if (resultado.Res == 1)
        return;


    // =====================================================
    // BACKEND RECHAZÓ → RESTAURAR
    // =====================================================

    ClientePosSeleccionado =
        clienteAnterior;

    ClienteSeleccionado =
        nombreAnterior;


    _ignorandoCambioBusquedaCliente =
        true;

    TextoBusquedaCliente =
        clienteAnterior?.Nombre
        ?? string.Empty;

    _ignorandoCambioBusquedaCliente =
        false;


    MensajeError =
        string.IsNullOrWhiteSpace(
            resultado.Msg)
            ? "No fue posible asignar el cliente a la venta."
            : resultado.Msg;
}
    
// =========================================================
// VOLVER A CLIENTE MOSTRADOR
// =========================================================

[RelayCommand]
private async Task QuitarClienteAsync()
{
    MensajeError =
        string.Empty;


    // =====================================================
    // GUARDAR ESTADO ANTERIOR
    // =====================================================

    var clienteAnterior =
        ClientePosSeleccionado;

    var nombreAnterior =
        ClienteSeleccionado;


    // =====================================================
    // UI OPTIMISTA
    // =====================================================

    ClientePosSeleccionado =
        null;

    ClienteSeleccionado =
        "Cliente mostrador";


    _ignorandoCambioBusquedaCliente =
        true;

    TextoBusquedaCliente =
        string.Empty;

    _ignorandoCambioBusquedaCliente =
        false;


    ClientesEncontrados.Clear();

    MostrarSugerenciasClientes =
        false;


    // =====================================================
    // TODAVÍA NO EXISTE VENTA
    // =====================================================

    if (IdVentaActual <= 0)
        return;


    // =====================================================
    // VENTA EXISTENTE → QUITAR CLIENTE
    // =====================================================

    var request =
        new ActualizarVentaRequest
        {
            IdCliente =
                null,

            TipoCliente =
                "PUBLICO_GENERAL"
        };


    var resultado =
        await _ventasService
            .ActualizarVentaAsync(
                IdVentaActual,
                request
            );


    if (resultado.Res == 1)
        return;


    // =====================================================
    // BACKEND RECHAZÓ → RESTAURAR
    // =====================================================

    ClientePosSeleccionado =
        clienteAnterior;

    ClienteSeleccionado =
        nombreAnterior;


    _ignorandoCambioBusquedaCliente =
        true;

    TextoBusquedaCliente =
        clienteAnterior?.Nombre
        ?? string.Empty;

    _ignorandoCambioBusquedaCliente =
        false;


    MensajeError =
        string.IsNullOrWhiteSpace(
            resultado.Msg)
            ? "No fue posible cambiar la venta a cliente mostrador."
            : resultado.Msg;
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