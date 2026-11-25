using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Bascula;
using NovaCoreESDM.Services.Cancelaciones;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Printing;
using NovaCoreESDM.Services.TerminalPago;
using System.Linq;

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
    private readonly ScaleConfigurationService _basculaConfiguracion = new();
    private readonly TerminalPagoConfigurationService _terminalesPago = new();
    private readonly TerminalPagoClient _terminalPagoClient = new();


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

        CargarTerminalesPago();

        // Solo se leen las impresoras si todavía no hay una fija.
        if (!HayImpresoraConfigurada)
            CargarListaImpresoras(mostrarMensaje: false);

        CargarBascula();
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
    // BÁSCULA
    // =========================================================

    public ObservableCollection<string> PuertosDisponibles { get; } = new();

    public IReadOnlyList<int> VelocidadesDisponibles { get; } =
        BasculaLector.VelocidadesComunes;

    /// <summary>Últimas tramas que mandó la báscula (diagnóstico).</summary>
    public ObservableCollection<string> TramasBascula { get; } = new();

    [ObservableProperty]
    private string? puertoSeleccionado;

    [ObservableProperty]
    private int velocidadSeleccionada = 9600;

    [ObservableProperty]
    private string comandoBascula = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenBascula))]
    private bool basculaHabilitada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResumenBascula))]
    private bool hayBasculaGuardada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoHayLecturaBascula))]
    private bool probandoBascula;

    [ObservableProperty]
    private bool detectandoBascula;

    [ObservableProperty]
    private string progresoDeteccion = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoHayLecturaBascula))]
    private string pesoPrueba = string.Empty;

    [ObservableProperty]
    private string estadoPesoPrueba = string.Empty;

    [ObservableProperty]
    private bool pesoPruebaEstable;

    public bool NoHayLecturaBascula =>
        string.IsNullOrEmpty(PesoPrueba);

    public bool HayTramasBascula => TramasBascula.Count > 0;

    public string ResumenBascula =>
        !HayBasculaGuardada
            ? "Sin báscula: el peso se escribe a mano."
            : BasculaHabilitada
                ? "Activa: al vender por KG el peso se toma de la báscula."
                : "Guardada pero desactivada: el peso se escribe a mano.";

    public string RutaConfiguracionBascula =>
        _basculaConfiguracion.ObtenerRuta();

    private BasculaLector? _lectorPrueba;
    private CancellationTokenSource? _detencionCts;


    private void CargarBascula()
    {
        DetenerPruebaBascula();

        CargarPuertos();

        var configuracion = _basculaConfiguracion.Obtener();

        HayBasculaGuardada = configuracion is not null &&
                             !string.IsNullOrWhiteSpace(configuracion.Puerto);

        if (configuracion is null)
        {
            BasculaHabilitada = false;
            return;
        }

        BasculaHabilitada = configuracion.Habilitada;
        VelocidadSeleccionada = configuracion.BaudRate > 0 ? configuracion.BaudRate : 9600;
        ComandoBascula = configuracion.Comando ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(configuracion.Puerto))
        {
            if (!PuertosDisponibles.Contains(configuracion.Puerto))
                PuertosDisponibles.Add(configuracion.Puerto);

            PuertoSeleccionado = configuracion.Puerto;
        }
    }


    private void CargarPuertos()
    {
        var anterior = PuertoSeleccionado;

        PuertosDisponibles.Clear();
        foreach (var puerto in BasculaLector.ListarPuertos())
            PuertosDisponibles.Add(puerto);

        PuertoSeleccionado =
            anterior is not null && PuertosDisponibles.Contains(anterior)
                ? anterior
                : PuertosDisponibles.Count == 1 ? PuertosDisponibles[0] : anterior;
    }


    [RelayCommand]
    private void LeerPuertos()
    {
        LimpiarMensajes();
        CargarPuertos();

        if (PuertosDisponibles.Count == 0)
            MensajeError =
                "No se encontró ningún puerto. Conecta la báscula por USB " +
                "y verifica que el driver CH341 esté instalado.";
        else
            MensajeExito = $"Se encontraron {PuertosDisponibles.Count} puerto(s).";
    }


    // ---------- Detectar automáticamente ----------

    [RelayCommand]
    private async Task DetectarBasculaAsync()
    {
        LimpiarMensajes();

        if (DetectandoBascula)
        {
            _detencionCts?.Cancel();
            return;
        }

        DetenerPruebaBascula();
        CargarPuertos();

        if (PuertosDisponibles.Count == 0)
        {
            MensajeError =
                "No hay puertos. Conecta la báscula por USB e instala el driver CH341.";
            return;
        }

        // Si el cajero eligió un puerto, se prueba primero ese.
        var puertos = new List<string>(PuertosDisponibles);
        if (PuertoSeleccionado is not null && puertos.Remove(PuertoSeleccionado))
            puertos.Insert(0, PuertoSeleccionado);

        TramasBascula.Clear();
        OnPropertyChanged(nameof(HayTramasBascula));

        _detencionCts = new CancellationTokenSource();
        DetectandoBascula = true;

        try
        {
            var resultado = await BasculaLector.DetectarAsync(
                puertos,
                3,
                new Progress<string>(m => ProgresoDeteccion = m),
                trama => Dispatcher.UIThread.Post(() => AgregarTrama(trama)),
                _detencionCts.Token);

            if (resultado is null)
            {
                MensajeError =
                    "No se encontró la báscula. Verifica el cable USB, que esté " +
                    "encendida y con algo encima. Si abajo aparecen datos, mándamelos.";
                return;
            }

            PuertoSeleccionado = resultado.Puerto;
            VelocidadSeleccionada = resultado.BaudRate;
            ComandoBascula = resultado.Comando;

            MensajeExito =
                $"Báscula encontrada en {resultado.Puerto} a {resultado.BaudRate} baudios " +
                $"({BasculaLector.DescribirComando(resultado.Comando).ToLowerInvariant()}). " +
                $"Leyó {resultado.Muestra.PesoKg:0.000} kg. Presiona «Guardar báscula».";
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Búsqueda cancelada.";
        }
        catch (Exception ex)
        {
            MensajeError = $"No fue posible buscar la báscula: {ex.Message}";
        }
        finally
        {
            DetectandoBascula = false;
            ProgresoDeteccion = string.Empty;
            _detencionCts?.Dispose();
            _detencionCts = null;
        }
    }


    // ---------- Probar lectura en vivo ----------

    [RelayCommand]
    private void ProbarBascula()
    {
        LimpiarMensajes();

        if (ProbandoBascula)
        {
            DetenerPruebaBascula();
            return;
        }

        if (string.IsNullOrWhiteSpace(PuertoSeleccionado))
        {
            MensajeError = "Elige el puerto de la báscula o usa «Detectar báscula».";
            return;
        }

        TramasBascula.Clear();
        OnPropertyChanged(nameof(HayTramasBascula));
        PesoPrueba = string.Empty;
        EstadoPesoPrueba = "Esperando datos de la báscula…";
        PesoPruebaEstable = false;

        var lector = new BasculaLector(ConfiguracionBasculaActual());

        lector.PesoLeido += lectura => Dispatcher.UIThread.Post(() =>
        {
            if (_lectorPrueba != lector)
                return;

            PesoPrueba = $"{lectura.PesoKg:0.000} kg";
            PesoPruebaEstable = lectura.Estable;
            EstadoPesoPrueba = lectura.Estable ? "Estable" : "Pesando…";
        });

        lector.TramaRecibida += trama =>
            Dispatcher.UIThread.Post(() => AgregarTrama(trama));

        lector.Fallo += mensaje => Dispatcher.UIThread.Post(() =>
        {
            if (_lectorPrueba != lector)
                return;

            MensajeError = mensaje;
            DetenerPruebaBascula();
        });

        if (!lector.Iniciar(out var error))
        {
            MensajeError = error;
            lector.Dispose();
            return;
        }

        _lectorPrueba = lector;
        ProbandoBascula = true;
    }


    /// <summary>
    /// Se llama al salir de la pantalla para liberar el puerto.
    /// </summary>
    public void DetenerPruebaBascula()
    {
        _detencionCts?.Cancel();

        var lector = _lectorPrueba;
        _lectorPrueba = null;
        ProbandoBascula = false;
        PesoPrueba = string.Empty;
        EstadoPesoPrueba = string.Empty;

        if (lector is not null)
            Task.Run(lector.Dispose);
    }


    private void AgregarTrama(string trama)
    {
        TramasBascula.Insert(0, trama);

        while (TramasBascula.Count > 8)
            TramasBascula.RemoveAt(TramasBascula.Count - 1);

        OnPropertyChanged(nameof(HayTramasBascula));
    }


    // ---------- Guardar / quitar ----------

    [RelayCommand]
    private void GuardarBascula()
    {
        LimpiarMensajes();

        if (string.IsNullOrWhiteSpace(PuertoSeleccionado))
        {
            MensajeError = "Elige el puerto de la báscula o usa «Detectar báscula».";
            return;
        }

        DetenerPruebaBascula();

        try
        {
            var configuracion = ConfiguracionBasculaActual();
            configuracion.Habilitada = true;

            _basculaConfiguracion.Guardar(configuracion);
        }
        catch (Exception ex)
        {
            MensajeError = $"No fue posible guardar la báscula: {ex.Message}";
            return;
        }

        HayBasculaGuardada = true;
        BasculaHabilitada = true;

        MensajeExito =
            $"Báscula guardada en {PuertoSeleccionado}. " +
            "Al vender por KG el peso se tomará automáticamente.";
    }


    partial void OnBasculaHabilitadaChanged(bool value)
    {
        // Solo el interruptor: guarda el cambio si ya hay báscula.
        if (!HayBasculaGuardada)
            return;

        var configuracion = _basculaConfiguracion.Obtener();
        if (configuracion is null || configuracion.Habilitada == value)
            return;

        try
        {
            configuracion.Habilitada = value;
            _basculaConfiguracion.Guardar(configuracion);
        }
        catch (Exception ex)
        {
            MensajeError = $"No fue posible guardar el cambio: {ex.Message}";
        }
    }


    [RelayCommand]
    private void QuitarBascula()
    {
        LimpiarMensajes();
        DetenerPruebaBascula();

        try
        {
            _basculaConfiguracion.Quitar();
        }
        catch (Exception ex)
        {
            MensajeError = $"No fue posible quitar la báscula: {ex.Message}";
            return;
        }

        HayBasculaGuardada = false;
        BasculaHabilitada = false;
        ComandoBascula = string.Empty;
        VelocidadSeleccionada = 9600;

        MensajeExito = "Báscula quitada. El peso se escribirá a mano.";
    }


    private ScaleConfiguration ConfiguracionBasculaActual() =>
        new()
        {
            Habilitada = BasculaHabilitada,
            Puerto = PuertoSeleccionado ?? string.Empty,
            BaudRate = VelocidadSeleccionada > 0 ? VelocidadSeleccionada : 9600,
            Comando = ComandoBascula?.Trim() ?? string.Empty,
            DecimalesSinPunto = 3,
            FechaConfiguracion = DateTime.Now,
            ConfiguradaPor = PosSession.NombreUsuario
        };


    // =========================================================
    // AUXILIARES
    // =========================================================

    // =========================================================
    // TERMINALES DE COBRO CON TARJETA
    // =========================================================
    //
    // Alámbrica   -> solo se registra (cobro manual con terminaciones).
    // Inalámbrica -> datos de red para enviarle el importe y recibir
    //                el resultado automáticamente.
    // =========================================================

    public ObservableCollection<TerminalPagoConfiguration> TerminalesPago { get; } = new();

    public bool HayTerminalesPago => TerminalesPago.Count > 0;

    public bool NoHayTerminalesPago => !HayTerminalesPago;

    public string RutaTerminalesPago => _terminalesPago.ObtenerRuta();


    // ----- Formulario -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloFormularioTerminal))]
    private string? terminalEnEdicionId;

    [ObservableProperty]
    private bool formularioTerminalVisible;

    [ObservableProperty]
    private string formNombreTerminal = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormEsAlambrica))]
    private bool formEsInalambrica = true;

    public bool FormEsAlambrica
    {
        get => !FormEsInalambrica;
        set
        {
            if (value)
                FormEsInalambrica = false;
        }
    }

    [ObservableProperty]
    private string formHostTerminal = string.Empty;

    [ObservableProperty]
    private string formPuertoTerminal = "8091";

    [ObservableProperty]
    private bool formUsarHttps;

    [ObservableProperty]
    private string formCodigoTerminal = string.Empty;

    [ObservableProperty]
    private string formClaveTerminal = string.Empty;

    [ObservableProperty]
    private string formTimeoutTerminal = "5";

    [ObservableProperty]
    private bool formActivaTerminal = true;

    public string TituloFormularioTerminal =>
        string.IsNullOrEmpty(TerminalEnEdicionId)
            ? "Nueva terminal"
            : "Editar terminal";


    // ----- Mensajes propios de la sección -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayMensajeTerminal))]
    private string mensajeTerminal = string.Empty;

    [ObservableProperty]
    private bool mensajeTerminalEsError;

    public bool HayMensajeTerminal =>
        !string.IsNullOrWhiteSpace(MensajeTerminal);

    private void InformarTerminal(string mensaje, bool error)
    {
        MensajeTerminalEsError = error;
        MensajeTerminal = mensaje;
    }


    private void CargarTerminalesPago()
    {
        TerminalesPago.Clear();

        foreach (var t in _terminalesPago.ObtenerTodas())
            TerminalesPago.Add(t);

        OnPropertyChanged(nameof(HayTerminalesPago));
        OnPropertyChanged(nameof(NoHayTerminalesPago));
    }


    [RelayCommand]
    private void NuevaTerminal()
    {
        MensajeTerminal = string.Empty;

        TerminalEnEdicionId = null;
        FormNombreTerminal = string.Empty;
        FormEsInalambrica = true;
        FormHostTerminal = string.Empty;
        FormPuertoTerminal = "8091";
        FormUsarHttps = false;
        FormCodigoTerminal = string.Empty;
        FormClaveTerminal = string.Empty;
        FormTimeoutTerminal = "5";
        FormActivaTerminal = true;

        FormularioTerminalVisible = true;
    }


    [RelayCommand]
    private void EditarTerminal(TerminalPagoConfiguration? terminal)
    {
        if (terminal is null)
            return;

        MensajeTerminal = string.Empty;

        TerminalEnEdicionId = terminal.Id;
        FormNombreTerminal = terminal.Nombre;
        FormEsInalambrica = terminal.EsInalambrica;
        FormHostTerminal = terminal.Host;
        FormPuertoTerminal = terminal.Puerto.ToString();
        FormUsarHttps = terminal.UsarHttps;
        FormCodigoTerminal = terminal.CodigoTerminal;
        FormClaveTerminal = terminal.ClaveApi;
        FormTimeoutTerminal = terminal.TimeoutSegundos.ToString();
        FormActivaTerminal = terminal.Activa;

        FormularioTerminalVisible = true;
    }


    [RelayCommand]
    private void CancelarFormularioTerminal()
    {
        FormularioTerminalVisible = false;
        MensajeTerminal = string.Empty;
    }


    /// <summary>Arma la terminal del formulario o devuelve el error de validación.</summary>
    private TerminalPagoConfiguration? ConstruirTerminalDelFormulario(out string error)
    {
        error = string.Empty;

        var nombre = FormNombreTerminal.Trim();

        if (nombre.Length == 0)
        {
            error = "Escribe un nombre para la terminal (ej. «Terminal caja 1»).";
            return null;
        }

        var existente =
            TerminalesPago.FirstOrDefault(t => t.Id == TerminalEnEdicionId);

        var terminal = new TerminalPagoConfiguration
        {
            Id = existente?.Id ?? Guid.NewGuid().ToString("N"),
            Nombre = nombre,
            Tipo = FormEsInalambrica
                ? TipoConexionTerminal.Inalambrica
                : TipoConexionTerminal.Alambrica,
            Activa = FormActivaTerminal,
            FechaConfiguracion = DateTime.Now,
            ConfiguradaPor = PosSession.NombreUsuario
        };

        if (!terminal.EsInalambrica)
            return terminal;

        var host = FormHostTerminal.Trim();

        // Solo host o IP: sin esquema, ruta ni puerto.
        if (host.Length == 0 ||
            host.Contains("://") || host.Contains('/') || host.Contains(' ') ||
            (host.Contains(':') && !System.Net.IPAddress.TryParse(host, out _)))
        {
            error = "Escribe solo la IP o el nombre de host de la terminal (ej. 192.168.1.50), sin http:// ni puerto.";
            return null;
        }

        if (!int.TryParse(FormPuertoTerminal.Trim(), out var puerto) || puerto is < 1 or > 65535)
        {
            error = "El puerto debe ser un número entre 1 y 65535.";
            return null;
        }

        var codigo = FormCodigoTerminal.Trim().ToUpperInvariant();

        if (!System.Text.RegularExpressions.Regex.IsMatch(codigo, "^[A-Z0-9_-]{3,32}$"))
        {
            error = "El código de la terminal debe tener de 3 a 32 caracteres (letras, números, - o _).";
            return null;
        }

        var clave = FormClaveTerminal.Trim();

        if (clave.Length < 16)
        {
            error = "La clave de acceso de la terminal debe tener al menos 16 caracteres.";
            return null;
        }

        if (!int.TryParse(FormTimeoutTerminal.Trim(), out var timeout) || timeout is < 1 or > 60)
        {
            error = "El tiempo de espera debe estar entre 1 y 60 segundos.";
            return null;
        }

        terminal.Host = host;
        terminal.Puerto = puerto;
        terminal.UsarHttps = FormUsarHttps;
        terminal.CodigoTerminal = codigo;
        terminal.ClaveApi = clave;
        terminal.TimeoutSegundos = timeout;

        return terminal;
    }


    [RelayCommand]
    private void GuardarTerminal()
    {
        var terminal = ConstruirTerminalDelFormulario(out var error);

        if (terminal is null)
        {
            InformarTerminal(error, error: true);
            return;
        }

        try
        {
            _terminalesPago.Guardar(terminal);
        }
        catch (Exception ex)
        {
            InformarTerminal($"No fue posible guardar la terminal: {ex.Message}", error: true);
            return;
        }

        FormularioTerminalVisible = false;
        CargarTerminalesPago();

        InformarTerminal(
            $"Terminal «{terminal.Nombre}» guardada ({terminal.TipoTexto.ToLowerInvariant()}).",
            error: false);
    }


    [RelayCommand]
    private async Task ProbarTerminalAsync(TerminalPagoConfiguration? terminal)
    {
        if (terminal is null || EstaOcupado)
            return;

        await ProbarConexionAsync(terminal);
    }


    [RelayCommand]
    private async Task ProbarFormularioTerminalAsync()
    {
        if (EstaOcupado)
            return;

        var terminal = ConstruirTerminalDelFormulario(out var error);

        if (terminal is null)
        {
            InformarTerminal(error, error: true);
            return;
        }

        await ProbarConexionAsync(terminal);
    }


    private async Task ProbarConexionAsync(TerminalPagoConfiguration terminal)
    {
        MensajeTerminal = string.Empty;

        if (!terminal.EsInalambrica)
        {
            InformarTerminal(
                "Una terminal alámbrica no usa la red: el cobro se captura manualmente.",
                error: false);
            return;
        }

        try
        {
            EstaOcupado = true;

            InformarTerminal($"Probando conexión con «{terminal.Nombre}»…", error: false);

            var resultado = await _terminalPagoClient.ProbarConexionAsync(terminal);

            InformarTerminal(
                resultado.Ok ? resultado.Dato ?? "Conexión correcta." : resultado.Mensaje,
                error: !resultado.Ok);
        }
        finally
        {
            EstaOcupado = false;
        }
    }


    [RelayCommand]
    private async Task QuitarTerminalAsync(TerminalPagoConfiguration? terminal)
    {
        if (terminal is null)
            return;

        MensajeTerminal = string.Empty;

        // Mismo criterio que la impresora: quitar requiere administrador.
        if (RequiereAdminParaQuitarImpresora)
        {
            var administrador =
                SolicitarAutorizacionAdministradorAsync is null
                    ? null
                    : await SolicitarAutorizacionAdministradorAsync(
                        "Autorizar cambio de terminal",
                        $"Para quitar la terminal «{terminal.Nombre}» se requiere " +
                        "la autorización de un administrador.");

            if (string.IsNullOrWhiteSpace(administrador))
                return;

            try
            {
                await _cancelaciones.CerrarAutorizacionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cerrando autorización: {ex.Message}");
            }
        }

        try
        {
            _terminalesPago.Quitar(terminal.Id);
        }
        catch (Exception ex)
        {
            InformarTerminal($"No fue posible quitar la terminal: {ex.Message}", error: true);
            return;
        }

        if (TerminalEnEdicionId == terminal.Id)
            FormularioTerminalVisible = false;

        CargarTerminalesPago();

        InformarTerminal($"Terminal «{terminal.Nombre}» quitada.", error: false);
    }


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
