using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Turnos;
using NovaCoreESDM.Services.Cortes;
using NovaCoreESDM.Models.Turno;

namespace NovaCoreESDM.ViewModels.Cortes;

public partial class CortesViewModel
    : NovaCoreESDM.ViewModels.ViewModelBase,
      INotifyPropertyChanged
{
    private readonly TurnosService
        _turnosService;

    private readonly CortesService
        _cortesService;
    
    private ResumenTurnoData? _resumenActual;

    public ResumenTurnoData? ResumenActual
    {
        get => _resumenActual;

        private set =>
            SetField(
                ref _resumenActual,
                value);
    }

    // ============================================================
    // ESTADO GENERAL
    // ============================================================

    private bool _estaCargando;

    public bool EstaCargando
    {
        get => _estaCargando;

        set
        {
            if (SetField(
                    ref _estaCargando,
                    value))
            {
                OnPropertyChanged(
                    nameof(PuedeRealizarCorte));
            }
        }
    }


    private bool _tieneTurnoActivo;

    public bool TieneTurnoActivo
    {
        get => _tieneTurnoActivo;

        set
        {
            if (SetField(
                    ref _tieneTurnoActivo,
                    value))
            {
                OnPropertyChanged(
                    nameof(NoTieneTurnoActivo));

                OnPropertyChanged(
                    nameof(PuedeRealizarCorte));
            }
        }
    }


    public bool NoTieneTurnoActivo =>
        !TieneTurnoActivo;


    private bool _puedeCerrarTurno;

    public bool PuedeCerrarTurno
    {
        get => _puedeCerrarTurno;

        set
        {
            if (SetField(
                    ref _puedeCerrarTurno,
                    value))
            {
                OnPropertyChanged(
                    nameof(PuedeRealizarCorte));
            }
        }
    }


    public bool PuedeRealizarCorte =>
        TieneTurnoActivo &&
        PuedeCerrarTurno &&
        !EstaCargando;


    private string _mensajeError =
        string.Empty;

    public string MensajeError
    {
        get => _mensajeError;
        set => SetField(
            ref _mensajeError,
            value);
    }


    // ============================================================
    // INFORMACIÓN DEL TURNO
    // ============================================================

    private int _idTurno;

    public int IdTurno
    {
        get => _idTurno;
        set => SetField(
            ref _idTurno,
            value);
    }


    private string _estadoTurno =
        "SIN TURNO";

    public string EstadoTurno
    {
        get => _estadoTurno;
        set => SetField(
            ref _estadoTurno,
            value);
    }


    private string _cajaNombre =
        "Sin caja activa";

    public string CajaNombre
    {
        get => _cajaNombre;
        set => SetField(
            ref _cajaNombre,
            value);
    }


    private string _cajaCodigo =
        "—";

    public string CajaCodigo
    {
        get => _cajaCodigo;
        set => SetField(
            ref _cajaCodigo,
            value);
    }


    private string _usuarioApertura =
        "—";

    public string UsuarioApertura
    {
        get => _usuarioApertura;
        set => SetField(
            ref _usuarioApertura,
            value);
    }


    private string _fechaApertura =
        "—";

    public string FechaApertura
    {
        get => _fechaApertura;
        set => SetField(
            ref _fechaApertura,
            value);
    }


    private string _horaApertura =
        "--:--";

    public string HoraApertura
    {
        get => _horaApertura;
        set => SetField(
            ref _horaApertura,
            value);
    }


    // ============================================================
    // INDICADORES DEL TURNO
    // ============================================================

    private decimal _totalVentas;

    public decimal TotalVentas
    {
        get => _totalVentas;

        set
        {
            if (SetField(
                    ref _totalVentas,
                    value))
            {
                OnPropertyChanged(
                    nameof(TotalVentasTexto));
            }
        }
    }


    public string TotalVentasTexto =>
        TotalVentas.ToString("C2");


    private int _tickets;

    public int Tickets
    {
        get => _tickets;

        set
        {
            if (SetField(
                    ref _tickets,
                    value))
            {
                OnPropertyChanged(
                    nameof(TicketsTexto));
            }
        }
    }


    public string TicketsTexto =>
        Tickets.ToString("N0");


    private decimal _ventasEfectivo;

    public decimal VentasEfectivo
    {
        get => _ventasEfectivo;

        set
        {
            if (SetField(
                    ref _ventasEfectivo,
                    value))
            {
                OnPropertyChanged(
                    nameof(VentasEfectivoTexto));
            }
        }
    }


    public string VentasEfectivoTexto =>
        VentasEfectivo.ToString("C2");


    private decimal _ventasTarjeta;

    public decimal VentasTarjeta
    {
        get => _ventasTarjeta;

        set
        {
            if (SetField(
                    ref _ventasTarjeta,
                    value))
            {
                OnPropertyChanged(
                    nameof(VentasTarjetaTexto));
            }
        }
    }


    public string VentasTarjetaTexto =>
        VentasTarjeta.ToString("C2");


    private decimal _ventasTransferencia;

    public decimal VentasTransferencia
    {
        get => _ventasTransferencia;

        set
        {
            if (SetField(
                    ref _ventasTransferencia,
                    value))
            {
                OnPropertyChanged(
                    nameof(VentasTransferenciaTexto));
            }
        }
    }


    public string VentasTransferenciaTexto =>
        VentasTransferencia.ToString("C2");


    private decimal _ventasCredito;

    public decimal VentasCredito
    {
        get => _ventasCredito;

        set
        {
            if (SetField(
                    ref _ventasCredito,
                    value))
            {
                OnPropertyChanged(
                    nameof(VentasCreditoTexto));
            }
        }
    }


    public string VentasCreditoTexto =>
        VentasCredito.ToString("C2");


    // ============================================================
    // EFECTIVO / MOVIMIENTOS
    // ============================================================

    private decimal _fondoInicial;

    public decimal FondoInicial
    {
        get => _fondoInicial;

        set
        {
            if (SetField(
                    ref _fondoInicial,
                    value))
            {
                OnPropertyChanged(
                    nameof(FondoInicialTexto));
            }
        }
    }


    public string FondoInicialTexto =>
        FondoInicial.ToString("C2");


    private decimal _abonosCreditoEfectivo;

    public decimal AbonosCreditoEfectivo
    {
        get => _abonosCreditoEfectivo;

        set
        {
            if (SetField(
                    ref _abonosCreditoEfectivo,
                    value))
            {
                OnPropertyChanged(
                    nameof(AbonosCreditoEfectivoTexto));
            }
        }
    }


    public string AbonosCreditoEfectivoTexto =>
        AbonosCreditoEfectivo.ToString("C2");


    private decimal _otrosIngresos;

    public decimal OtrosIngresos
    {
        get => _otrosIngresos;

        set
        {
            if (SetField(
                    ref _otrosIngresos,
                    value))
            {
                OnPropertyChanged(
                    nameof(OtrosIngresosTexto));
            }
        }
    }


    public string OtrosIngresosTexto =>
        OtrosIngresos.ToString("C2");


    private decimal _retiros;

    public decimal Retiros
    {
        get => _retiros;

        set
        {
            if (SetField(
                    ref _retiros,
                    value))
            {
                OnPropertyChanged(
                    nameof(RetirosTexto));
            }
        }
    }


    public string RetirosTexto =>
        Retiros.ToString("C2");


    private decimal _egresos;

    public decimal Egresos
    {
        get => _egresos;

        set
        {
            if (SetField(
                    ref _egresos,
                    value))
            {
                OnPropertyChanged(
                    nameof(EgresosTexto));
            }
        }
    }


    public string EgresosTexto =>
        Egresos.ToString("C2");


    private decimal _devoluciones;

    public decimal Devoluciones
    {
        get => _devoluciones;

        set
        {
            if (SetField(
                    ref _devoluciones,
                    value))
            {
                OnPropertyChanged(
                    nameof(DevolucionesTexto));
            }
        }
    }


    public string DevolucionesTexto =>
        Devoluciones.ToString("C2");


    private decimal _efectivoEsperado;

    public decimal EfectivoEsperado
    {
        get => _efectivoEsperado;

        set
        {
            if (SetField(
                    ref _efectivoEsperado,
                    value))
            {
                OnPropertyChanged(
                    nameof(EfectivoEsperadoTexto));
            }
        }
    }


    public string EfectivoEsperadoTexto =>
        EfectivoEsperado.ToString("C2");


    // ============================================================
    // ABONOS CRÉDITO NO EFECTIVO
    // ============================================================

    private decimal _abonosCreditoTarjeta;

    public decimal AbonosCreditoTarjeta
    {
        get => _abonosCreditoTarjeta;

        set
        {
            if (SetField(
                    ref _abonosCreditoTarjeta,
                    value))
            {
                OnPropertyChanged(
                    nameof(AbonosCreditoTarjetaTexto));
            }
        }
    }


    public string AbonosCreditoTarjetaTexto =>
        AbonosCreditoTarjeta.ToString("C2");


    private decimal _abonosCreditoTransferencia;

    public decimal AbonosCreditoTransferencia
    {
        get => _abonosCreditoTransferencia;

        set
        {
            if (SetField(
                    ref _abonosCreditoTransferencia,
                    value))
            {
                OnPropertyChanged(
                    nameof(AbonosCreditoTransferenciaTexto));
            }
        }
    }


    public string AbonosCreditoTransferenciaTexto =>
        AbonosCreditoTransferencia.ToString("C2");


    // ============================================================
    // HISTORIAL
    // ============================================================

    public ObservableCollection<CorteHistorialItemViewModel> Cortes
    {
        get;
    } = new();


    public bool TieneCortes =>
        Cortes.Count > 0;


    public bool NoTieneCortes =>
        Cortes.Count == 0;


    // ============================================================
    // EVENTOS HACIA LA VISTA
    // ============================================================

    public event EventHandler?
        RealizarCorteSolicitado;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public CortesViewModel()
    {
        _turnosService =
            new TurnosService();


        _cortesService =
            new CortesService();


        EstablecerSinTurno();
    }


    // ============================================================
    // CARGAR INFORMACIÓN
    // ============================================================

    public async Task CargarAsync()
    {
        if (EstaCargando)
        {
            return;
        }


        MensajeError =
            string.Empty;


        // ========================================================
        // VALIDAR SESIÓN
        // ========================================================

        if (PosSession.IdTurno <= 0)
        {
            EstablecerSinTurno();

            return;
        }


        EstaCargando =
            true;


        try
        {
            var idTurnoActual =
                PosSession.IdTurno;


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "CARGANDO CORTES / RESUMEN DEL TURNO");

            Console.WriteLine(
                $"IdTurno: {idTurnoActual}");

            Console.WriteLine(
                "========================================");


            // ====================================================
            // RESUMEN DEL TURNO
            // ====================================================

            var resultado =
                await _turnosService
                    .ObtenerResumenTurnoAsync(
                        idTurnoActual);


            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)

                        ? "No fue posible cargar el resumen del turno."

                        : resultado.Msg;


                Console.WriteLine(
                    $"ERROR RESUMEN CORTE: {MensajeError}");

                return;
            }
            
            if (resultado.Data is null)
            {
                ResumenActual =
                    null;

                MensajeError =
                    "El servidor no devolvió información del turno.";

                return;
            }


            ResumenActual =
                resultado.Data;


            var resumen =
                resultado.Data;


            // ====================================================
            // TURNO
            // ====================================================

            IdTurno =
                resumen.Turno?.Id ??
                idTurnoActual;


            EstadoTurno =
                string.IsNullOrWhiteSpace(
                    resumen.Turno?.Estado)

                    ? "TURNO ABIERTO"

                    : resumen.Turno!.Estado;


            TieneTurnoActivo =
                string.Equals(
                    resumen.Turno?.Estado,
                    "ABIERTO",
                    StringComparison.OrdinalIgnoreCase);


            PuedeCerrarTurno =
                resumen.Turno?.PuedeCerrar ??
                false;


            CajaNombre =
                resumen.Turno?.CajaNombre ??
                PosSession.NombreCaja ??
                "Caja";


            CajaCodigo =
                string.IsNullOrWhiteSpace(
                    resumen.Turno?.CajaCodigo)

                    ? "—"

                    : resumen.Turno!.CajaCodigo!;


            UsuarioApertura =
                ObtenerUsuarioApertura(
                    resumen.Turno?.UsuarioAperturaNombre,
                    resumen.Turno?.UsuarioApertura);


            EstablecerFechaApertura(
                resumen.Turno?.FechaApertura);


            // ====================================================
            // VENTAS
            // ====================================================

            TotalVentas =
                resumen.Ventas?.Total ??
                0m;


            Tickets =
                resumen.Ventas?.Tickets ??
                0;


            VentasEfectivo =
                resumen.Ventas?.Efectivo ??
                0m;


            VentasTarjeta =
                resumen.Ventas?.Tarjetas ??
                0m;


            VentasTransferencia =
                resumen.Ventas?.Transferencias ??
                0m;


            VentasCredito =
                resumen.Ventas?.Credito ??
                0m;


            // ====================================================
            // ABONOS DE CRÉDITO
            // ====================================================

            AbonosCreditoEfectivo =
                resumen.AbonosCredito?.Efectivo ??
                0m;


            AbonosCreditoTarjeta =
                resumen.AbonosCredito?.Tarjetas ??
                0m;


            AbonosCreditoTransferencia =
                resumen.AbonosCredito?.Transferencias ??
                0m;


            // ====================================================
            // CAJA / EFECTIVO FÍSICO
            // ====================================================

            FondoInicial =
                resumen.Caja?.FondoInicial ??
                resumen.Turno?.FondoInicial ??
                0m;


            Retiros =
                resumen.Caja?.Retiros ??
                0m;


            Egresos =
                resumen.Caja?.Egresos ??
                0m;


            Devoluciones =
                resumen.Caja?.Devoluciones ??
                0m;


            EfectivoEsperado =
                resumen.Caja?.EfectivoEsperado ??
                0m;


            // ====================================================
            // OTROS INGRESOS
            // ====================================================
            //
            // IMPORTANTE:
            //
            // resumen.Caja.Ingresos ya incluye los abonos de
            // crédito realizados en efectivo.
            //
            // Como en pantalla mostramos esos abonos por separado,
            // debemos restarlos para no mostrarlos dos veces.
            // ====================================================

            var ingresosCaja =
                resumen.Caja?.Ingresos ??
                0m;


            OtrosIngresos =
                Math.Max(
                    0m,
                    ingresosCaja -
                    AbonosCreditoEfectivo);
            
            // ====================================================
            // HISTORIAL DE CORTES
            // ====================================================

            await CargarHistorialAsync();


            // ====================================================
            // DEBUG
            // ====================================================

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "RESUMEN DE CORTE CARGADO");

            Console.WriteLine(
                $"Turno: {IdTurno}");

            Console.WriteLine(
                $"Estado: {EstadoTurno}");

            Console.WriteLine(
                $"Caja: {CajaNombre}");

            Console.WriteLine(
                $"Ventas: {TotalVentas:C2}");

            Console.WriteLine(
                $"Ventas efectivo: {VentasEfectivo:C2}");

            Console.WriteLine(
                $"Abonos crédito efectivo: {AbonosCreditoEfectivo:C2}");

            Console.WriteLine(
                $"Otros ingresos: {OtrosIngresos:C2}");

            Console.WriteLine(
                $"Efectivo esperado: {EfectivoEsperado:C2}");

            Console.WriteLine(
                $"Puede cerrar: {PuedeCerrarTurno}");

            Console.WriteLine(
                "========================================");
        }
        catch (Exception ex)
        {
            MensajeError =
                $"No fue posible cargar la información del turno: {ex.Message}";


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "ERROR CARGANDO CORTES:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "========================================");
        }
        finally
        {
            EstaCargando =
                false;
        }
    }
    
    
    // ============================================================
    // CARGAR HISTORIAL DE CORTES
    // ============================================================

    public async Task CargarHistorialAsync()
    {
        try
        {
            var resultado =
                await _cortesService
                    .ObtenerCortesAsync(
                        limit: 20);


            Cortes.Clear();


            if (resultado.Res != 1)
            {
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "NO FUE POSIBLE CARGAR HISTORIAL:");

                Console.WriteLine(
                    resultado.Msg ??
                    "Sin mensaje del servidor.");

                Console.WriteLine(
                    "========================================");


                NotificarHistorial();

                return;
            }


            if (resultado.Data?.Cortes is null)
            {
                NotificarHistorial();

                return;
            }


            foreach (var corte in
                     resultado.Data.Cortes)
            {
                var fecha =
                    ObtenerFechaCorte(
                        corte.FechaCorte);


                Cortes.Add(
                    new CorteHistorialItemViewModel
                    {
                        IdCorte =
                            corte.Id,

                        IdTurno =
                            corte.IdTurno,

                        Caja =
                            ObtenerNombreCaja(
                                corte.CajaNombre,
                                corte.CajaCodigo),

                        Fecha =
                            fecha.Fecha,

                        Hora =
                            fecha.Hora,

                        EfectivoEsperado =
                            corte.EfectivoEsperado,

                        EfectivoDeclarado =
                            corte.EfectivoDeclarado,

                        Diferencia =
                            corte.Diferencia,

                        EstadoDiferencia =
                            string.IsNullOrWhiteSpace(
                                corte.EstadoDiferencia)

                                ? ObtenerEstadoDiferencia(
                                    corte.Diferencia)

                                : corte.EstadoDiferencia!
                    });
            }


            NotificarHistorial();


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "HISTORIAL DE CORTES CARGADO:");

            Console.WriteLine(
                $"Cortes: {Cortes.Count}");

            Console.WriteLine(
                "========================================");
        }
        catch (Exception ex)
        {
            Cortes.Clear();

            NotificarHistorial();


            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "ERROR CARGANDO HISTORIAL:");

            Console.WriteLine(
                ex);

            Console.WriteLine(
                "========================================");
        }
    }


    // ============================================================
    // SOLICITAR CORTE
    // ============================================================

    public void SolicitarRealizarCorte()
    {
        // Si todavía estamos cargando información,
        // evitamos ejecutar el proceso.
        if (EstaCargando)
        {
            return;
        }


        // La vista será la encargada de explicar
        // por qué puede o no realizarse el corte.
        RealizarCorteSolicitado?.Invoke(
            this,
            EventArgs.Empty);
    }


    // ============================================================
    // ESTADO SIN TURNO
    // ============================================================

    public void EstablecerSinTurno()
    {
        IdTurno =
            0;


        TieneTurnoActivo =
            false;


        PuedeCerrarTurno =
            false;


        EstadoTurno =
            "SIN TURNO ABIERTO";


        CajaNombre =
            "No hay una caja con turno activo";


        CajaCodigo =
            "—";


        UsuarioApertura =
            "—";


        FechaApertura =
            "—";


        HoraApertura =
            "--:--";


        TotalVentas =
            0m;


        Tickets =
            0;


        VentasEfectivo =
            0m;


        VentasTarjeta =
            0m;


        VentasTransferencia =
            0m;


        VentasCredito =
            0m;


        FondoInicial =
            0m;


        AbonosCreditoEfectivo =
            0m;


        AbonosCreditoTarjeta =
            0m;


        AbonosCreditoTransferencia =
            0m;


        OtrosIngresos =
            0m;


        Retiros =
            0m;


        Egresos =
            0m;


        Devoluciones =
            0m;


        EfectivoEsperado =
            0m;
        
        ResumenActual =
            null;
    }


    // ============================================================
    // FECHA DE APERTURA
    // ============================================================

    private void EstablecerFechaApertura(
        string? fecha)
    {
        if (string.IsNullOrWhiteSpace(
                fecha))
        {
            FechaApertura =
                "—";

            HoraApertura =
                "--:--";

            return;
        }


        if (DateTimeOffset.TryParse(
                fecha,
                out var fechaApertura))
        {
            FechaApertura =
                fechaApertura
                    .ToLocalTime()
                    .ToString(
                        "dd/MM/yyyy");


            HoraApertura =
                fechaApertura
                    .ToLocalTime()
                    .ToString(
                        "HH:mm");

            return;
        }


        if (DateTime.TryParse(
                fecha,
                out var fechaSimple))
        {
            FechaApertura =
                fechaSimple.ToString(
                    "dd/MM/yyyy");


            HoraApertura =
                fechaSimple.ToString(
                    "HH:mm");

            return;
        }


        FechaApertura =
            fecha;


        HoraApertura =
            "--:--";
    }


    // ============================================================
    // USUARIO DE APERTURA
    // ============================================================

    private static string ObtenerUsuarioApertura(
        string? nombre,
        string? usuario)
    {
        if (!string.IsNullOrWhiteSpace(
                nombre))
        {
            return nombre;
        }


        if (!string.IsNullOrWhiteSpace(
                usuario))
        {
            return usuario;
        }


        return "—";
    }


    // ============================================================
    // NOTIFICACIÓN DE HISTORIAL
    // ============================================================

    public void NotificarHistorial()
    {
        OnPropertyChanged(
            nameof(TieneCortes));


        OnPropertyChanged(
            nameof(NoTieneCortes));
    }


    // ============================================================
    // INotifyPropertyChanged
    // ============================================================

    public new event PropertyChangedEventHandler?
        PropertyChanged;


    protected bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (Equals(
                field,
                value))
        {
            return false;
        }


        field =
            value;


        OnPropertyChanged(
            propertyName);


        return true;
    }


    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
    
    
    // ============================================================
    // FORMATEAR FECHA DE CORTE
    // ============================================================

    private static (
        string Fecha,
        string Hora)
        ObtenerFechaCorte(
            string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return (
                "—",
                "--:--");
        }


        if (DateTimeOffset.TryParse(
                valor,
                out var fechaOffset))
        {
            var local =
                fechaOffset.ToLocalTime();


            return (
                local.ToString("dd/MM/yyyy"),
                local.ToString("HH:mm"));
        }


        if (DateTime.TryParse(
                valor,
                out var fecha))
        {
            return (
                fecha.ToString("dd/MM/yyyy"),
                fecha.ToString("HH:mm"));
        }


        return (
            valor,
            "--:--");
    }


    // ============================================================
    // NOMBRE DE CAJA
    // ============================================================

    private static string ObtenerNombreCaja(
        string? nombre,
        string? codigo)
    {
        if (!string.IsNullOrWhiteSpace(
                nombre))
        {
            return nombre;
        }


        if (!string.IsNullOrWhiteSpace(
                codigo))
        {
            return codigo;
        }


        return "Caja";
    }


    // ============================================================
    // ESTADO DE DIFERENCIA
    // ============================================================

    private static string ObtenerEstadoDiferencia(
        decimal diferencia)
    {
        if (diferencia > 0m)
        {
            return "SOBRANTE";
        }


        if (diferencia < 0m)
        {
            return "FALTANTE";
        }


        return "CUADRADO";
    }
}




// ================================================================
// ITEM DEL HISTORIAL
// ================================================================

public class CorteHistorialItemViewModel
{
    public int IdCorte
    {
        get;
        set;
    }


    public int IdTurno
    {
        get;
        set;
    }


    public string Caja
    {
        get;
        set;
    } = string.Empty;


    public string Fecha
    {
        get;
        set;
    } = string.Empty;


    public string Hora
    {
        get;
        set;
    } = string.Empty;


    public decimal EfectivoEsperado
    {
        get;
        set;
    }


    public decimal EfectivoDeclarado
    {
        get;
        set;
    }


    public decimal Diferencia
    {
        get;
        set;
    }


    public string EstadoDiferencia
    {
        get;
        set;
    } = string.Empty;


    public string EfectivoEsperadoTexto =>
        EfectivoEsperado.ToString(
            "C2");


    public string EfectivoDeclaradoTexto =>
        EfectivoDeclarado.ToString(
            "C2");


    public string DiferenciaTexto =>
        Diferencia.ToString(
            "C2");
}