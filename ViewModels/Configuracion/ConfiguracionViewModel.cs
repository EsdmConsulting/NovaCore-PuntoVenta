using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Printing;

namespace NovaCoreESDM.ViewModels.Configuracion;

public partial class ConfiguracionViewModel : ViewModelBase
{
    // =========================================================
    // ¿QUITAR LA IMPRESORA REQUIERE ADMINISTRADOR?
    //
    // true  -> para cambiar de impresora hay que validar a un admin
    // false -> cualquier usuario puede quitarla
    // =========================================================

    private const bool RequiereAdminParaQuitarImpresora = true;


    private readonly PrinterConfigurationService _impresoraConfiguracion = new();
    private readonly PrinterDiscoveryService _descubrimiento = new();
    private readonly TicketFormatConfigurationService _formatoConfiguracion = new();
    private readonly WindowsRawPrinterService _printer = new();
    private readonly TicketVentaService _ticketService = new();
    private readonly CancelacionesService _cancelaciones = new();


    // La asigna MainViewModel (abre la ventana de administrador).
    public Func<string, string, Task<string?>>?
        SolicitarAutorizacionAdministradorAsync { get; set; }


    // =========================================================
    // IMPRESORA FIJA
    // =========================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayImpresoraConfigurada))]
    [NotifyPropertyChangedFor(nameof(NoHayImpresoraConfigurada))]
    private string impresoraConfigurada = string.Empty;

    public bool HayImpresoraConfigurada =>
        !string.IsNullOrWhiteSpace(ImpresoraConfigurada);

    public bool NoHayImpresoraConfigurada =>
        !HayImpresoraConfigurada;

    [ObservableProperty]
    private string estadoImpresora = string.Empty;

    [ObservableProperty]
    private bool estadoImpresoraOk;


    // =========================================================
    // LISTA PARA ELEGIR
    // =========================================================

    public ObservableCollection<string> ImpresorasDisponibles { get; } = new();

    [ObservableProperty]
    private string? impresoraSeleccionada;

    public bool LecturaDisponible =>
        _descubrimiento.EsCompatible;


    // =========================================================
    // RUTAS (informativas)
    // =========================================================

    public string RutaConfiguracionImpresora =>
        _impresoraConfiguracion.ObtenerRuta();

    public string RutaFormatoTicket =>
        _formatoConfiguracion.ObtenerRuta();


    // =========================================================
    // ESTADO DE PANTALLA
    // =========================================================

    [ObservableProperty]
    private bool estaOcupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayError))]
    private string mensajeError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayExito))]
    private string mensajeExito = string.Empty;

    public bool HayError =>
        !string.IsNullOrWhiteSpace(MensajeError);

    public bool HayExito =>
        !string.IsNullOrWhiteSpace(MensajeExito);


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ConfiguracionViewModel()
    {
        Cargar();
    }


    // =========================================================
    // CARGAR (se llama al abrir la pantalla)
    // =========================================================

    public void Cargar()
    {
        LimpiarMensajes();

        var configuracion = _impresoraConfiguracion.Obtener();

        ImpresoraConfigurada =
            configuracion?.NombreImpresora ?? string.Empty;

        ActualizarEstadoImpresora();

        // Solo se leen las impresoras si todavía no hay una fija.
        if (!HayImpresoraConfigurada)
            CargarListaImpresoras(mostrarMensaje: false);
    }


    // =========================================================
    // LEER IMPRESORAS INSTALADAS
    // =========================================================

    [RelayCommand]
    private void LeerImpresoras()
    {
        LimpiarMensajes();
        CargarListaImpresoras(mostrarMensaje: true);
    }


    private void CargarListaImpresoras(bool mostrarMensaje)
    {
        ImpresorasDisponibles.Clear();
        ImpresoraSeleccionada = null;

        if (!_descubrimiento.EsCompatible)
        {
            MensajeError =
                "La lectura de impresoras solo está disponible en Windows.";
            return;
        }

        try
        {
            var lista = _descubrimiento.ObtenerImpresoras();

            foreach (var nombre in lista)
                ImpresorasDisponibles.Add(nombre);

            if (lista.Count == 0)
            {
                MensajeError =
                    "Windows no reporta impresoras instaladas. " +
                    "Instala el driver de la impresora y vuelve a leer.";
            }
            else if (mostrarMensaje)
            {
                MensajeExito =
                    $"Se encontraron {lista.Count} impresora(s).";
            }
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible leer las impresoras: {ex.Message}";
        }
    }


    // =========================================================
    // FIJAR IMPRESORA
    // =========================================================

    [RelayCommand]
    private void GuardarImpresora()
    {
        LimpiarMensajes();

        if (string.IsNullOrWhiteSpace(ImpresoraSeleccionada))
        {
            MensajeError = "Selecciona una impresora de la lista.";
            return;
        }

        if (!_descubrimiento.ImpresoraExiste(ImpresoraSeleccionada))
        {
            MensajeError =
                "Windows no pudo abrir esa impresora. " +
                "Verifica que esté instalada y conectada.";
            return;
        }

        try
        {
            _impresoraConfiguracion.Guardar(
                new PrinterConfiguration
                {
                    NombreImpresora = ImpresoraSeleccionada,
                    FechaConfiguracion = DateTime.Now,
                    ConfiguradaPor = PosSession.NombreUsuario
                });
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible guardar la configuración: {ex.Message}";
            return;
        }

        ImpresoraConfigurada = ImpresoraSeleccionada;
        ActualizarEstadoImpresora();

        MensajeExito =
            $"Impresora '{ImpresoraConfigurada}' fijada para este equipo.";
    }


    // =========================================================
    // QUITAR IMPRESORA (para poder elegir otra)
    // =========================================================

    [RelayCommand]
    private async Task QuitarImpresoraAsync()
    {
        LimpiarMensajes();

        if (!HayImpresoraConfigurada)
            return;

        if (RequiereAdminParaQuitarImpresora)
        {
            var administrador =
                SolicitarAutorizacionAdministradorAsync is null
                    ? null
                    : await SolicitarAutorizacionAdministradorAsync(
                        "Autorizar cambio de impresora",
                        "Para quitar la impresora de este equipo se requiere " +
                        "la autorización de un administrador.");

            if (string.IsNullOrWhiteSpace(administrador))
                return;

            // La autorización era solo para este paso: se cierra.
            try
            {
                await _cancelaciones.CerrarAutorizacionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error cerrando autorización: {ex.Message}");
            }
        }

        try
        {
            _impresoraConfiguracion.Quitar();
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible quitar la impresora: {ex.Message}";
            return;
        }

        ImpresoraConfigurada = string.Empty;
        ActualizarEstadoImpresora();
        CargarListaImpresoras(mostrarMensaje: false);

        MensajeExito =
            "Impresora quitada. Selecciona la nueva impresora y fíjala.";
    }


    // =========================================================
    // PRUEBAS
    // =========================================================

    [RelayCommand]
    private Task ImprimirPruebaAsync() =>
        EjecutarImpresionAsync(
            _printer.ImprimirPruebaAsync,
            "Prueba enviada a la impresora.",
            "La impresora no confirmó la impresión de prueba.");


    [RelayCommand]
    private Task ImprimirMuestraAsync() =>
        EjecutarImpresionAsync(
            _ticketService.ImprimirMuestraAsync,
            "Ticket de muestra enviado a la impresora.",
            "La impresora no confirmó la impresión del ticket.");


    [RelayCommand]
    private Task AbrirCajonAsync() =>
        EjecutarImpresionAsync(
            _printer.AbrirCajonAsync,
            "Comando de apertura enviado al cajón.",
            "La impresora no confirmó el comando del cajón.");


    private async Task EjecutarImpresionAsync(
        Func<Task<bool>> accion,
        string mensajeExito,
        string mensajeFallo)
    {
        LimpiarMensajes();

        if (EstaOcupado)
            return;

        try
        {
            EstaOcupado = true;

            if (await accion())
                MensajeExito = mensajeExito;
            else
                MensajeError = mensajeFallo;
        }
        catch (ImpresoraNoConfiguradaException ex)
        {
            MensajeError = ex.Message;
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible imprimir: {ex.Message}";
        }
        finally
        {
            EstaOcupado = false;
        }
    }


    // =========================================================
    // AUXILIARES
    // =========================================================

    private void ActualizarEstadoImpresora()
    {
        if (!HayImpresoraConfigurada)
        {
            EstadoImpresora = string.Empty;
            EstadoImpresoraOk = false;
            return;
        }

        var existe =
            _descubrimiento.ImpresoraExiste(ImpresoraConfigurada);

        EstadoImpresoraOk = existe;

        EstadoImpresora =
            existe
                ? "Lista en este equipo"
                : "No se encuentra en este equipo " +
                  "(¿fue desinstalada o cambió de nombre?)";
    }


    private void LimpiarMensajes()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }
}
