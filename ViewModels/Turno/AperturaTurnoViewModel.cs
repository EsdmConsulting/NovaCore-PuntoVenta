using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Models.Turno;
using NovaCoreESDM.Services.Configuration;
using NovaCoreESDM.Services.Turnos;

namespace NovaCoreESDM.ViewModels.Turno;

public partial class AperturaTurnoViewModel : ViewModelBase
{
    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly TurnosService
        _turnosService;


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


    public event Action<decimal>?
        SolicitarConfirmacionTurno;


    public event Action?
        TurnoAbiertoCorrectamente;


    public AperturaTurnoViewModel()
    {
        _terminalConfigurationService =
            new TerminalConfigurationService();

        _turnosService =
            new TurnosService();
    }


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
            // =====================================================
            // 1. CONFIGURACIÓN LOCAL DE LA TERMINAL
            // =====================================================

            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();


            if (configuracion is null)
            {
                MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }


            // =====================================================
            // 2. REQUEST
            // =====================================================

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


            // =====================================================
            // DEBUG
            // =====================================================

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "DATOS PARA ABRIR TURNO:");

            Console.WriteLine(
                $"IdCaja: {request.IdCaja}");

            Console.WriteLine(
                $"FondoInicial: {request.FondoInicial}");

            Console.WriteLine(
                $"Observaciones: {request.Observaciones}");

            Console.WriteLine(
                $"PosSession.IdUsuario: {PosSession.IdUsuario}");

            Console.WriteLine(
                $"PosSession.Usuario: {PosSession.Usuario}");

            Console.WriteLine(
                $"PosSession.IdRol: {PosSession.IdRol}");

            Console.WriteLine(
                "====================================");


            // =====================================================
            // 3. LLAMAR BACKEND
            // =====================================================

            var resultado =
                await _turnosService
                    .AbrirTurnoAsync(
                        request);


            // =====================================================
            // 4. OBTENER ID REAL DEL TURNO
            // =====================================================

            /*
             * Nuestro modelo TurnoAbierto ya contempla:
             *
             * apertura normal:
             * data.id
             *
             * turno existente:
             * data.id_turno
             */

            var idTurno =
                resultado.Data?.IdTurno
                ?? 0;


            // =====================================================
            // 5. SI EL BACKEND DEVOLVIÓ ERROR
            // =====================================================

            if (resultado.Res != 1)
            {
                /*
                 * Caso especial:
                 *
                 * El backend nos está diciendo:
                 *
                 * "Ya existe un turno abierto para esta caja."
                 *
                 * Eso NO es un error operativo para NovaCore.
                 * Simplemente recuperamos ese turno.
                 */

                var esTurnoExistente =
                    idTurno > 0
                    &&
                    !string.IsNullOrWhiteSpace(
                        resultado.Msg)
                    &&
                    resultado.Msg.Contains(
                        "Ya existe un turno abierto",
                        StringComparison.OrdinalIgnoreCase);


                if (!esTurnoExistente)
                {
                    MensajeError =
                        string.IsNullOrWhiteSpace(
                            resultado.Msg)
                            ? "No fue posible iniciar el turno."
                            : resultado.Msg;

                    return;
                }


                Console.WriteLine(
                    "====================================");

                Console.WriteLine(
                    "TURNO YA EXISTENTE RECUPERADO:");

                Console.WriteLine(
                    $"IdTurno: {idTurno}");

                Console.WriteLine(
                    "====================================");
            }


            // =====================================================
            // 6. VALIDAR ID DEL TURNO
            // =====================================================

            if (idTurno <= 0)
            {
                MensajeError =
                    "El servidor no devolvió un identificador válido del turno.";

                return;
            }


            // =====================================================
            // 7. SESIÓN OPERATIVA DEL POS
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
            // 8. GUARDAR TURNO ACTIVO LOCALMENTE
            // =====================================================

            await _terminalConfigurationService
                .GuardarTurnoActivoAsync(
                    idTurno);


            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "TURNO GUARDADO LOCALMENTE:");

            Console.WriteLine(
                $"IdTurno: {PosSession.IdTurno}");

            Console.WriteLine(
                "====================================");


            // =====================================================
            // 9. LISTO
            // =====================================================

            TurnoAbiertoCorrectamente?.Invoke();
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
}