using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Turno;

using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Turnos;
using NovaCoreESDM.Models.Configuration;

namespace NovaCoreESDM.ViewModels.Turno;

public partial class AperturaTurnoViewModel
    : ViewModelBase
{
    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly TurnosService
        _turnosService;


    // =========================================================
    // PROPIEDADES
    // =========================================================

    [ObservableProperty]
    private decimal? _fondoInicial;


    [ObservableProperty]
    private string _mensajeError =
        string.Empty;


    [ObservableProperty]
    private bool _estaCargando;


    public bool PuedeIniciarTurno =>
        FondoInicial.HasValue &&
        FondoInicial.Value >= 0 &&
        !EstaCargando;


    // =========================================================
    // EVENTOS
    // =========================================================

    /*
     * Confirmación normal antes de crear un turno.
     */
    public event Action<decimal>?
        SolicitarConfirmacionTurno;


    /*
     * NUEVO:
     *
     * Se dispara cuando la caja ya tiene un turno abierto.
     *
     * La vista deberá mostrar una ventana preguntando:
     *
     * ¿Deseas continuar con este turno?
     */
    public event Action<TurnoAbierto, bool>?
        SolicitarConfirmacionTurnoExistente;


    /*
     * Se dispara cuando:
     *
     * - Se abrió un turno nuevo.
     * - El usuario confirmó continuar uno existente.
     */
    public event Action?
        TurnoAbiertoCorrectamente;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AperturaTurnoViewModel()
    {
        _terminalConfigurationService =
            new TerminalConfigurationService();

        _turnosService =
            new TurnosService();
    }


    // =========================================================
    // CAMBIOS DE PROPIEDADES
    // =========================================================

    partial void OnFondoInicialChanged(
        decimal? value)
    {
        MensajeError =
            string.Empty;

        OnPropertyChanged(
            nameof(PuedeIniciarTurno));
    }


    partial void OnEstaCargandoChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(PuedeIniciarTurno));
    }


    // =========================================================
    // INICIAR TURNO
    // =========================================================

    [RelayCommand]
    private void IniciarTurno()
    {
        MensajeError =
            string.Empty;


        if (!FondoInicial.HasValue)
        {
            MensajeError =
                "Ingresa el fondo inicial.";

            return;
        }


        if (FondoInicial.Value < 0)
        {
            MensajeError =
                "El fondo inicial no puede ser negativo.";

            return;
        }


        SolicitarConfirmacionTurno?.Invoke(
            FondoInicial.Value);
    }


    // =========================================================
    // CONFIRMAR APERTURA
    // =========================================================

    public async Task ConfirmarAperturaAsync(
        decimal fondoInicial)
    {
        EstaCargando =
            true;

        MensajeError =
            string.Empty;


        try
        {
            // =================================================
            // 1. CONFIGURACIÓN DE TERMINAL
            // =================================================

            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            if (configuracion is null)
            {
                MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }


            // =================================================
            // 2. REQUEST
            // =================================================

            var request =
                new AbrirTurnoRequest
                {
                    IdCaja =
                        configuracion.IdCaja,

                    FondoInicial =
                        fondoInicial,

                    Observaciones =
                        string.Empty
                };


            // =================================================
            // DEBUG
            // =================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "DATOS PARA ABRIR TURNO:");

            Console.WriteLine(
                $"IdCaja: {request.IdCaja}");

            Console.WriteLine(
                $"FondoInicial: {request.FondoInicial}");

            Console.WriteLine(
                $"PosSession.IdUsuario: {PosSession.IdUsuario}");

            Console.WriteLine(
                $"PosSession.Usuario: {PosSession.Usuario}");

            Console.WriteLine(
                $"PosSession.IdRol: {PosSession.IdRol}");

            Console.WriteLine(
                "====================================");


            // =================================================
            // 3. BACKEND
            // =================================================

            var resultado =
                await _turnosService
                    .AbrirTurnoAsync(
                        request);


            // =================================================
            // 4. TURNO NUEVO
            // =================================================

            if (resultado.Res == 1)
            {
                var turno =
                    resultado.Data?.Turno;


                if (turno is null ||
                    turno.IdTurno <= 0)
                {
                    MensajeError =
                        "El servidor no devolvió un turno válido.";

                    return;
                }


                await EstablecerTurnoActivoAsync(
                    turno.IdTurno,
                    configuracion);


                TurnoAbiertoCorrectamente?.Invoke();

                return;
            }


            // =================================================
            // 5. TURNO YA EXISTENTE
            // =================================================

            if (resultado.YaExisteTurno)
            {
                var turnoExistente =
                    resultado.Data?.TurnoExistente;


                if (turnoExistente is null ||
                    turnoExistente.IdTurno <= 0)
                {
                    MensajeError =
                        "La caja tiene un turno abierto, pero el servidor no devolvió su información.";

                    return;
                }


                var puedeContinuar =
                    resultado.Data?.PuedeContinuar
                    ?? false;


                if (!puedeContinuar)
                {
                    MensajeError =
                        "El turno existente no puede ser recuperado.";

                    return;
                }


                var mismoUsuario =
                    resultado.Data?.MismoUsuario
                    ?? false;


                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "TURNO EXISTENTE DETECTADO:");

                Console.WriteLine(
                    $"IdTurno: {turnoExistente.IdTurno}");

                Console.WriteLine(
                    $"Caja: {turnoExistente.CajaNombre}");

                Console.WriteLine(
                    $"Usuario apertura: {turnoExistente.UsuarioAperturaNombre}");

                Console.WriteLine(
                    $"Mismo usuario: {mismoUsuario}");

                Console.WriteLine(
                    "====================================");


                /*
                 * IMPORTANTE:
                 *
                 * Aquí YA NO guardamos el turno automáticamente.
                 *
                 * Primero la interfaz debe preguntarle al usuario.
                 */

                SolicitarConfirmacionTurnoExistente?.Invoke(
                    turnoExistente,
                    mismoUsuario);

                return;
            }


            // =================================================
            // 6. ERROR NORMAL
            // =================================================

            MensajeError =
                string.IsNullOrWhiteSpace(
                    resultado.Msg)

                    ? "No fue posible iniciar el turno."

                    : resultado.Msg;
        }
        catch (Exception ex)
        {
            MensajeError =
                $"Ocurrió un error al iniciar el turno: {ex.Message}";
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // CONTINUAR TURNO EXISTENTE
    // =========================================================

    public async Task ContinuarTurnoExistenteAsync(
        TurnoAbierto turno)
    {
        EstaCargando =
            true;

        MensajeError =
            string.Empty;


        try
        {
            if (turno.IdTurno <= 0)
            {
                MensajeError =
                    "El turno seleccionado no es válido.";

                return;
            }


            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            if (configuracion is null)
            {
                MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }


            /*
             * Seguridad adicional:
             *
             * El turno recuperado debe pertenecer exactamente
             * a la caja configurada en esta terminal.
             */

            if (turno.IdCaja !=
                configuracion.IdCaja)
            {
                MensajeError =
                    "El turno abierto pertenece a una caja diferente.";

                return;
            }


            await EstablecerTurnoActivoAsync(
                turno.IdTurno,
                configuracion);


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "TURNO EXISTENTE CONFIRMADO:");

            Console.WriteLine(
                $"IdTurno: {turno.IdTurno}");

            Console.WriteLine(
                $"Caja: {turno.CajaNombre}");

            Console.WriteLine(
                "====================================");


            TurnoAbiertoCorrectamente?.Invoke();
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible continuar con el turno: {ex.Message}";
        }
        finally
        {
            EstaCargando =
                false;
        }
    }


    // =========================================================
    // ESTABLECER TURNO ACTIVO
    // =========================================================

private async Task EstablecerTurnoActivoAsync(
    int idTurno,
    TerminalConfiguration configuracion)
{
    /*
     * Toda la lógica para establecer el turno activo
     * queda centralizada aquí.
     *
     * Se utiliza tanto cuando:
     *
     * - Se crea un turno nuevo.
     * - Se confirma continuar un turno existente.
     */

    if (idTurno <= 0)
    {
        throw new ArgumentException(
            "El identificador del turno no es válido.",
            nameof(idTurno));
    }


    // =====================================================
    // SESIÓN OPERATIVA
    // =====================================================

    PosSession.IdEmpresa =
        configuracion.IdEmpresa;

    PosSession.IdUnidadOperativa =
        configuracion.IdUnidadOperativa;

    PosSession.IdCaja =
        configuracion.IdCaja;

    PosSession.CodigoCaja =
        configuracion.CodigoCaja;

    PosSession.NombreCaja =
        configuracion.NombreCaja;

    PosSession.IdTurno =
        idTurno;


    // =====================================================
    // PERSISTENCIA LOCAL
    // =====================================================

    await _terminalConfigurationService
        .GuardarTurnoActivoAsync(
            idTurno);


    // =====================================================
    // DEBUG
    // =====================================================

    Console.WriteLine(
        "====================================");

    Console.WriteLine(
        "TURNO ACTIVO ESTABLECIDO:");

    Console.WriteLine(
        $"IdTurno: {PosSession.IdTurno}");

    Console.WriteLine(
        $"IdCaja: {PosSession.IdCaja}");

    Console.WriteLine(
        $"Caja: {PosSession.NombreCaja}");

    Console.WriteLine(
        $"CodigoCaja: {PosSession.CodigoCaja}");

    Console.WriteLine(
        "====================================");
}
}