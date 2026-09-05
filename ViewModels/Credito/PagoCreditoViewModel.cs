using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NovaCoreESDM.Models.Credito;
using NovaCoreESDM.Models.Clientes;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Credito;
using NovaCoreESDM.Services.Clientes;

namespace NovaCoreESDM.ViewModels.Credito;
using NovaCoreESDM.ViewModels;
using System.Globalization;

public partial class PagoCreditoViewModel : ViewModelBase
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly PagoCreditoService
        _pagoCreditoService;
    
    private readonly ClientesService
        _clientesService;
    
    // =========================================================
    // EVENTO: PAGO APLICADO CORRECTAMENTE
    // =========================================================

    public event Action<RegistrarAbonoCreditoResponse>?
        PagoAplicadoCorrectamente;
    
    // =========================================================
    // EVENTO: SOLICITAR CONFIRMACIÓN DE PAGO
    // =========================================================

    public event Action<PagoCreditoPreparado>?
        PagoSolicitado;

    // =========================================================
    // CLIENTE
    // =========================================================

    [ObservableProperty]
    private ClientePos?
        _clienteSeleccionado;


    // =========================================================
    // RESUMEN DE CRÉDITO
    // =========================================================

    [ObservableProperty]
    private EstadoCreditoData?
        _estadoCredito;
    
    // =========================================================
    // BUSCADOR DE CLIENTES
    // =========================================================

    private bool
        _ignorandoCambioBusquedaCliente;

    private int
        _versionBusquedaCliente;


    [ObservableProperty]
    private string
        _textoBusquedaCliente =
            string.Empty;


    [ObservableProperty]
    private bool
        _mostrarSugerenciasClientes;


    public ObservableCollection<ClientePos>
        ClientesEncontrados { get; } =
        new();


    // =========================================================
    // DOCUMENTOS
    // =========================================================

    public ObservableCollection<DocumentoPagoCreditoItem>
        DocumentosPendientes { get; } =
            new();


    // =========================================================
    // ESTADOS DE PANTALLA
    // =========================================================

    [ObservableProperty]
    private bool
        _estaCargando;


    [ObservableProperty]
    private bool
        _estaProcesandoPago;


    [ObservableProperty]
    private string
        _mensajeError =
            string.Empty;


    [ObservableProperty]
    private string
        _mensajeExito =
            string.Empty;
    
    public string[] FormasPago { get; } =
    {
        "EFECTIVO",
        "TARJETA",
        "TRANSFERENCIA"
    };


    // =========================================================
    // FORMA DE PAGO
    // =========================================================

    [ObservableProperty]
    private string
        _formaPagoSeleccionada =
            "EFECTIVO";


    [ObservableProperty]
    private string
        _origenPagoSeleccionado =
            "CAJA";


    [ObservableProperty]
    private string
        _referencia =
            string.Empty;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PagoCreditoViewModel()
    {
        _pagoCreditoService =
            new PagoCreditoService();


        _clientesService =
            new ClientesService();
    }


    // =========================================================
    // PROPIEDADES CALCULADAS DEL CRÉDITO
    // =========================================================

    public decimal SaldoTotal
    {
        get
        {
            return EstadoCredito?
                       .SaldoDeudor
                   ?? 0;
        }
    }


    public decimal CreditoDisponible
    {
        get
        {
            return EstadoCredito?
                       .Disponible
                   ?? 0;
        }
    }


    public decimal LimiteCredito
    {
        get
        {
            return EstadoCredito?
                       .LimiteCredito
                   ?? 0;
        }
    }


    public decimal SaldoVencido
    {
        get
        {
            return EstadoCredito?
                       .SaldoVencido
                   ?? 0;
        }
    }


    public string Semaforo
    {
        get
        {
            return EstadoCredito?
                       .Semaforo
                   ?? string.Empty;
        }
    }
    
    
    // =========================================================
    // TEXTOS FORMATEADOS
    // =========================================================

    public string SaldoTotalTexto =>
        SaldoTotal.ToString("N2");

    public string CreditoDisponibleTexto =>
        CreditoDisponible.ToString("N2");

    public string LimiteCreditoTexto =>
        LimiteCredito.ToString("N2");

    public string SaldoVencidoTexto =>
        SaldoVencido.ToString("N2");

    public string TotalSeleccionadoTexto =>
        TotalSeleccionado.ToString("N2");


    // =========================================================
    // TOTAL SELECCIONADO
    // =========================================================

    public decimal TotalSeleccionado
    {
        get
        {
            return DocumentosPendientes
                .Where(
                    d =>
                        d.Seleccionado
                )
                .Sum(
                    d =>
                        d.MontoAplicar
                );
        }
    }


    public int DocumentosSeleccionados
    {
        get
        {
            return DocumentosPendientes
                .Count(
                    d =>
                        d.Seleccionado
                );
        }
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


    // =====================================================
    // DEBOUNCE
    // =====================================================

    await Task.Delay(
        250
    );


    if (
        versionActual !=
        _versionBusquedaCliente
    )
    {
        return;
    }


    // =====================================================
    // CONSULTAR CLIENTES
    // =====================================================

    var resultado =
        await _clientesService
            .BuscarClientesAsync(
                texto
            );


    /*
     * Mientras esperaba la API,
     * el usuario pudo continuar escribiendo.
     */

    if (
        versionActual !=
        _versionBusquedaCliente
    )
    {
        return;
    }


    ClientesEncontrados.Clear();


    // =====================================================
    // ERROR
    // =====================================================

    if (resultado.Res != 1)
    {
        MostrarSugerenciasClientes =
            false;


        MensajeError =
            resultado.Msg
            ?? "No fue posible buscar clientes.";

        return;
    }


    // =====================================================
    // RESULTADOS
    // =====================================================

    foreach (
        var cliente
        in resultado.Data
    )
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

    MensajeExito =
        string.Empty;


    // =====================================================
    // ACTUALIZAR BUSCADOR
    // =====================================================

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
    // CARGAR CLIENTE + CRÉDITO
    // =====================================================

    await CargarClienteAsync(
        cliente
    );
}


// =========================================================
// LIMPIAR CLIENTE
// =========================================================

[RelayCommand]
private void LimpiarCliente()
{
    ClienteSeleccionado =
        null;


    EstadoCredito =
        null;


    DocumentosPendientes.Clear();


    ClientesEncontrados.Clear();


    _ignorandoCambioBusquedaCliente =
        true;


    TextoBusquedaCliente =
        string.Empty;


    _ignorandoCambioBusquedaCliente =
        false;


    MostrarSugerenciasClientes =
        false;


    MensajeError =
        string.Empty;


    MensajeExito =
        string.Empty;


    NotificarResumen();
}


    // =========================================================
    // CARGAR CLIENTE
    // =========================================================

    public async Task<bool> CargarClienteAsync(
        ClientePos cliente)
    {
        if (cliente is null)
            return false;


        ClienteSeleccionado =
            cliente;


        return await
            CargarResumenCreditoAsync();
    }


    // =========================================================
    // CARGAR RESUMEN DE CRÉDITO
    // =========================================================
    
    public async Task<bool> CargarResumenCreditoAsync()
    {
        if (ClienteSeleccionado is null)
        {
            MensajeError =
                "Selecciona un cliente.";

            return false;
        }


        if (EstaCargando)
            return false;


        EstaCargando =
            true;


        MensajeError =
            string.Empty;


        MensajeExito =
            string.Empty;


        try
        {
            var resultado =
                await _pagoCreditoService
                    .ObtenerResumenClienteAsync(
                        ClienteSeleccionado.Id
                    );


            if (
                resultado.Res != 1
                ||
                resultado.Data is null
            )
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)
                        ? "No fue posible consultar el crédito del cliente."
                        : resultado.Msg;

                LimpiarCredito();

                return false;
            }


            EstadoCredito =
                resultado.Data.Credito;


            DocumentosPendientes.Clear();


            foreach (
                var documento
                in resultado.Data
                    .DocumentosPendientes
            )
            {
                DocumentosPendientes.Add(
                    new DocumentoPagoCreditoItem(
                        documento,
                        ActualizarTotalesSeleccion
                    )
                );
            }


            NotificarResumen();


            return true;
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible consultar el crédito: {ex.Message}";


            LimpiarCredito();


            return false;
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // SELECCIONAR TODO
    // =========================================================

    [RelayCommand]
    private void SeleccionarTodos()
    {
        foreach (
            var documento
            in DocumentosPendientes
        )
        {
            documento.Seleccionado =
                true;


            documento.MontoAplicar =
                documento.Saldo;
        }


        ActualizarTotalesSeleccion();
    }


    // =========================================================
    // LIMPIAR SELECCIÓN
    // =========================================================

    [RelayCommand]
    private void LimpiarSeleccion()
    {
        foreach (
            var documento
            in DocumentosPendientes
        )
        {
            documento.Seleccionado =
                false;


            documento.MontoAplicar =
                0;
        }


        ActualizarTotalesSeleccion();
    }
    
    // =========================================================
// VALIDAR CONTEXTO POS
// =========================================================

    private bool ValidarContextoPos()
    {
        if (!PosSession.TieneTurnoActivo)
        {
            MensajeError =
                "No existe un turno activo para registrar el pago.";

            return false;
        }


        if (PosSession.IdEmpresa <= 0)
        {
            MensajeError =
                "No existe una empresa válida en la sesión.";

            return false;
        }


        if (PosSession.IdUnidadOperativa <= 0)
        {
            MensajeError =
                "No existe una unidad operativa válida.";

            return false;
        }


        if (PosSession.IdCaja <= 0)
        {
            MensajeError =
                "No existe una caja válida para registrar el pago.";

            return false;
        }


        if (PosSession.IdTurno <= 0)
        {
            MensajeError =
                "No existe un turno válido para registrar el pago.";

            return false;
        }


        return true;
    }

// =========================================================
// COMANDO: PAGAR DOCUMENTOS SELECCIONADOS
// =========================================================

[RelayCommand]
private async Task PagarSeleccionados()
{
    await PagarSeleccionadosAsync();
}


// =========================================================
// PAGAR DOCUMENTOS SELECCIONADOS
// =========================================================

public async Task<bool> PagarSeleccionadosAsync()
{
    if (ClienteSeleccionado is null)
    {
        MensajeError =
            "Selecciona un cliente.";

        return false;
    }


    if (!ValidarContextoPos())
        return false;


    var seleccionados =
        DocumentosPendientes
            .Where(
                d =>
                    d.Seleccionado
            )
            .ToList();


    if (seleccionados.Count == 0)
    {
        MensajeError =
            "Selecciona al menos un documento.";

        return false;
    }


    foreach (
        var documento
        in seleccionados
    )
    {
        if (documento.MontoAplicar <= 0)
        {
            MensajeError =
                $"Captura un monto válido para el documento {documento.Folio}.";

            return false;
        }


        if (
            documento.MontoAplicar
            >
            documento.Saldo
        )
        {
            MensajeError =
                $"El monto aplicado al documento {documento.Folio} no puede superar su saldo.";

            return false;
        }
    }


    var request =
        new RegistrarAbonoCreditoRequest
        {
            IdCliente =
                ClienteSeleccionado.Id,

            IdEmpresa =
                PosSession.IdEmpresa,

            IdUnidadOperativa =
                PosSession.IdUnidadOperativa,

            IdCaja =
                PosSession.IdCaja,

            IdTurno =
                PosSession.IdTurno,

            ModoAplicacion =
                "DOCUMENTOS",

            TipoPagador =
                "CLIENTE",

            IdClientePagador =
                ClienteSeleccionado.Id,

            PagadorRazonSocial =
                ClienteSeleccionado.Nombre,

            PagadorRfc =
                ClienteSeleccionado.Rfc,

            OrigenPago =
                OrigenPagoSeleccionado,

            FormaPago =
                FormaPagoSeleccionada,

            Referencia =
                string.IsNullOrWhiteSpace(
                    Referencia)
                    ? null
                    : Referencia.Trim(),

            Aplicaciones =
                seleccionados
                    .Select(
                        d =>
                            new AplicacionDocumentoCredito
                            {
                                IdDocumentoCredito =
                                    d.Id,

                                Monto =
                                    d.MontoAplicar
                            }
                    )
                    .ToList()
        };


// =====================================================
// PREPARAR PAGO
// =====================================================

    var pagoPreparado =
        new PagoCreditoPreparado
        {
            Request =
                request,

            Total =
                seleccionados.Sum(
                    d =>
                        d.MontoAplicar
                ),

            FormaPago =
                FormaPagoSeleccionada,

            Cliente =
                ClienteSeleccionado.Nombre,

            Documentos =
                seleccionados
        };


// =====================================================
// AVISAR A LA VISTA
// =====================================================

    PagoSolicitado?
        .Invoke(
            pagoPreparado
        );


    return true;
}


// =========================================================
// COMANDO: PAGAR TODO
// =========================================================

[RelayCommand]
private async Task PagarTodo()
{
    await PagarTodoAsync();
}


// =========================================================
// PAGAR TODO EL CRÉDITO
// =========================================================

public async Task<bool> PagarTodoAsync()
{
    if (ClienteSeleccionado is null)
    {
        MensajeError =
            "Selecciona un cliente.";

        return false;
    }


    if (!ValidarContextoPos())
        return false;


    if (SaldoTotal <= 0)
    {
        MensajeError =
            "El cliente no tiene saldo pendiente.";

        return false;
    }


    var request =
        new RegistrarAbonoCreditoRequest
        {
            IdCliente =
                ClienteSeleccionado.Id,

            IdEmpresa =
                PosSession.IdEmpresa,

            IdUnidadOperativa =
                PosSession.IdUnidadOperativa,

            IdCaja =
                PosSession.IdCaja,

            IdTurno =
                PosSession.IdTurno,

            ModoAplicacion =
                "TOTAL",

            TipoPagador =
                "CLIENTE",

            IdClientePagador =
                ClienteSeleccionado.Id,

            PagadorRazonSocial =
                ClienteSeleccionado.Nombre,

            PagadorRfc =
                ClienteSeleccionado.Rfc,

            OrigenPago =
                OrigenPagoSeleccionado,

            FormaPago =
                FormaPagoSeleccionada,

            Referencia =
                string.IsNullOrWhiteSpace(
                    Referencia)
                    ? null
                    : Referencia.Trim()
        };


// =====================================================
// PREPARAR DOCUMENTOS PARA CONFIRMACIÓN
// =====================================================

var documentosParaPago =
    DocumentosPendientes
        .Select(
            documento =>
            {
                var copia =
                    new DocumentoPagoCreditoItem(
                        new DocumentoCreditoPendiente
                        {
                            Id =
                                documento.Id,

                            TipoDocumento =
                                documento.TipoDocumento,

                            Folio =
                                documento.Folio,

                            FechaEmision =
                                documento.FechaEmision,

                            FechaVencimiento =
                                documento.FechaVencimiento,

                            ImporteTotal =
                                documento.ImporteTotal,

                            ImportePagado =
                                documento.ImportePagado,

                            Saldo =
                                documento.Saldo,

                            Estado =
                                documento.Estado,

                            RequiereRep =
                                documento.RequiereRep
                        },

                        () => { }
                    );


                copia.MontoAplicar =
                    copia.Saldo;


                return copia;
            }
        )
        .ToList();


        // =====================================================
        // PREPARAR PAGO
        // =====================================================

        var pagoPreparado =
            new PagoCreditoPreparado
            {
                Request =
                    request,

                Total =
                    SaldoTotal,

                FormaPago =
                    FormaPagoSeleccionada,

                Cliente =
                    ClienteSeleccionado.Nombre,

                Documentos =
                    documentosParaPago
            };


        // =====================================================
        // AVISAR A LA VISTA
        // =====================================================

        PagoSolicitado?
            .Invoke(
                pagoPreparado
            );


        return true;

}


    // =========================================================
    // EJECUTAR PAGO
    // =========================================================

    public async Task<bool> EjecutarPagoAsync(
        RegistrarAbonoCreditoRequest request)
    {
        if (EstaProcesandoPago)
            return false;


        EstaProcesandoPago =
            true;


        MensajeError =
            string.Empty;


        MensajeExito =
            string.Empty;


        try
        {
            // =====================================================
            // REGISTRAR ABONO
            // =====================================================

            var resultado =
                await _pagoCreditoService
                    .RegistrarAbonoAsync(
                        request
                    );


            // =====================================================
            // ERROR
            // =====================================================

            if (
                resultado.Res != 1
                ||
                resultado.Data is null
            )
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)
                        ? "No fue posible registrar el pago."
                        : resultado.Msg;

                return false;
            }


            // =====================================================
            // EL PAGO YA FUE REGISTRADO CORRECTAMENTE
            // =====================================================

            /*
             * IMPORTANTE:
             *
             * Llegados aquí, el backend ya confirmó
             * que el dinero fue aplicado.
             *
             * Guardamos el mensaje después de recargar
             * porque CargarResumenCreditoAsync()
             * limpia los mensajes de la pantalla.
             */


            // =====================================================
            // RECARGAR CRÉDITO REAL
            // =====================================================

            await CargarResumenCreditoAsync();


            // =====================================================
            // LIMPIAR DATOS DEL FORMULARIO
            // =====================================================

            Referencia =
                string.Empty;


            // =====================================================
            // MENSAJE DE ÉXITO
            // =====================================================

            MensajeExito =
                string.IsNullOrWhiteSpace(
                    resultado.Msg)
                    ? "Pago aplicado correctamente."
                    : resultado.Msg;


            // =====================================================
            // ABRIR CONFIRMACIÓN VISUAL
            // =====================================================

            PagoAplicadoCorrectamente?
                .Invoke(
                    resultado
                );


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
            EstaProcesandoPago =
                false;
        }
    }


    // =========================================================
    // ACTUALIZAR TOTALES
    // =========================================================

    private void ActualizarTotalesSeleccion()
    {
        OnPropertyChanged(
            nameof(
                TotalSeleccionado
            )
        );


        OnPropertyChanged(
            nameof(
                TotalSeleccionadoTexto
            )
        );


        OnPropertyChanged(
            nameof(
                DocumentosSeleccionados
            )
        );
    }


    // =========================================================
    // NOTIFICAR RESUMEN
    // =========================================================

    private void NotificarResumen()
    {
        OnPropertyChanged(
            nameof(
                SaldoTotal
            )
        );

        OnPropertyChanged(
            nameof(
                SaldoTotalTexto
            )
        );


        OnPropertyChanged(
            nameof(
                CreditoDisponible
            )
        );

        OnPropertyChanged(
            nameof(
                CreditoDisponibleTexto
            )
        );


        OnPropertyChanged(
            nameof(
                LimiteCredito
            )
        );

        OnPropertyChanged(
            nameof(
                LimiteCreditoTexto
            )
        );


        OnPropertyChanged(
            nameof(
                SaldoVencido
            )
        );

        OnPropertyChanged(
            nameof(
                SaldoVencidoTexto
            )
        );


        OnPropertyChanged(
            nameof(
                Semaforo
            )
        );


        ActualizarTotalesSeleccion();
    }


    // =========================================================
    // LIMPIAR
    // =========================================================

    private void LimpiarCredito()
    {
        EstadoCredito =
            null;


        DocumentosPendientes.Clear();


        NotificarResumen();
    }
}


// =============================================================
// PAGO PREPARADO PARA CONFIRMACIÓN
// =============================================================

public class PagoCreditoPreparado
{
    public RegistrarAbonoCreditoRequest Request { get; set; } =
        new();


    public decimal Total { get; set; }


    public string FormaPago { get; set; } =
        string.Empty;


    public string Cliente { get; set; } =
        string.Empty;


    public System.Collections.Generic.List<DocumentoPagoCreditoItem>
        Documentos { get; set; } =
        new();
}

// =============================================================
// DOCUMENTO SELECCIONABLE PARA LA UI
// =============================================================

public partial class DocumentoPagoCreditoItem :
    ObservableObject
{
    private readonly Action
        _onCambio;


    public DocumentoPagoCreditoItem(
        DocumentoCreditoPendiente documento,
        Action onCambio)
    {
        _onCambio =
            onCambio;


        Id =
            documento.Id;


        TipoDocumento =
            documento.TipoDocumento;


        Folio =
            documento.Folio;


        FechaEmision =
            documento.FechaEmision;


        FechaVencimiento =
            documento.FechaVencimiento;


        ImporteTotal =
            documento.ImporteTotal;


        ImportePagado =
            documento.ImportePagado;


        Saldo =
            documento.Saldo;


        Estado =
            documento.Estado;


        RequiereRep =
            documento.RequiereRep;
    }


    // =========================================================
    // DATOS
    // =========================================================

    public int Id { get; }


    public string TipoDocumento { get; }


    public string Folio { get; }


    public string? FechaEmision { get; }


    public string? FechaVencimiento { get; }


    public decimal ImporteTotal { get; }


    public decimal ImportePagado { get; }


    public decimal Saldo { get; }
    
    public string SaldoTexto =>
        Saldo.ToString("N2");

    public string ImporteTotalTexto =>
        ImporteTotal.ToString("N2");

    public string ImportePagadoTexto =>
        ImportePagado.ToString("N2");

    public int Estado { get; }


    public bool RequiereRep { get; }


    // =========================================================
    // SELECCIÓN
    // =========================================================

    [ObservableProperty]
    private bool
        _seleccionado;

    [ObservableProperty]
    private decimal
        _montoAplicar;
    
    // =========================================================
    // MONTO PARA TEXTBOX
    // =========================================================

    private bool
        _sincronizandoMontoTexto;


    [ObservableProperty]
    private string
        _montoAplicarTexto =
            "0.00";
    
    
// =========================================================
// CAMBIO DE SELECCIÓN
// =========================================================

partial void OnSeleccionadoChanged(
    bool value)
{
    /*
     * Al marcar el documento proponemos
     * pagar automáticamente todo su saldo.
     */

    if (
        value
        &&
        MontoAplicar <= 0
    )
    {
        MontoAplicar =
            Saldo;
    }


    /*
     * Al desmarcarlo eliminamos el monto.
     */

    if (!value)
    {
        MontoAplicar =
            0;
    }


    _onCambio();
}


// =========================================================
// CAMBIO DEL MONTO DECIMAL
// =========================================================

partial void OnMontoAplicarChanged(
    decimal value)
{
    // =====================================================
    // SINCRONIZAR TEXTO
    // =====================================================

    if (!_sincronizandoMontoTexto)
    {
        _sincronizandoMontoTexto =
            true;


        MontoAplicarTexto =
            value <= 0
                ? "0"
                : value.ToString(
                    "0.##",
                    CultureInfo.CurrentCulture
                );


        _sincronizandoMontoTexto =
            false;
    }


    // =====================================================
    // MONTO MAYOR A CERO → SELECCIONAR
    // =====================================================

    if (
        value > 0
        &&
        !Seleccionado
    )
    {
        Seleccionado =
            true;
    }


    // =====================================================
    // MONTO CERO → DESMARCAR
    // =====================================================

    if (
        value <= 0
        &&
        Seleccionado
    )
    {
        Seleccionado =
            false;
    }


    _onCambio();
}


// =========================================================
// CAMBIO DEL TEXTO ESCRITO POR EL CAJERO
// =========================================================

partial void OnMontoAplicarTextoChanged(
    string value)
{
    if (_sincronizandoMontoTexto)
        return;


    var texto =
        value?.Trim()
        ?? string.Empty;


    // =====================================================
    // TEXTBOX VACÍO
    // =====================================================

    if (string.IsNullOrWhiteSpace(
            texto))
    {
        MontoAplicar =
            0;

        return;
    }


    // =====================================================
    // CONVERTIR TEXTO
    // =====================================================

    if (
        decimal.TryParse(
            texto,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out var monto
        )
    )
    {
        MontoAplicar =
            monto;

        return;
    }


    // =====================================================
    // TEXTO INVÁLIDO
    // =====================================================

    MontoAplicar =
        0;
}


// =========================================================
// FORMATEAR AL TERMINAR DE ESCRIBIR
// =========================================================

public void FormatearMontoAplicar()
{
    _sincronizandoMontoTexto =
        true;


    MontoAplicarTexto =
        MontoAplicar.ToString(
            "N2",
            CultureInfo.CurrentCulture
        );


    _sincronizandoMontoTexto =
        false;
}


    // =========================================================
    // ESTADO VISUAL
    // =========================================================

    public string EstadoTexto
    {
        get
        {
            return Estado switch
            {
                1 =>
                    "Pendiente",

                2 =>
                    "Parcial",

                3 =>
                    "Pagado",

                4 =>
                    "Vencido",

                5 =>
                    "Cancelado",

                6 =>
                    "Sustituido",

                _ =>
                    "Desconocido"
            };
        }
    }


    public bool EstaVencido
    {
        get
        {
            return Estado == 4;
        }
    }
}