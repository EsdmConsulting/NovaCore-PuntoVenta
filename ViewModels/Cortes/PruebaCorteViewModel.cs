using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NovaCoreESDM.Models.Cortes;
using NovaCoreESDM.Models.Session;
using NovaCoreESDM.Services.Cajas;
using NovaCoreESDM.Services.Cortes;

namespace NovaCoreESDM.ViewModels.Cortes;

// =============================================================
// PRUEBA DE CORTE DE CAJA (SIMULACIÓN)
// =============================================================
//
// Calcula el corte de una caja en cualquier rango de fechas sin
// cerrar turnos ni modificar la caja
// (GET /api/pos/cortes/simular, misma cuenta que el cierre de
// turno). Cada simulación queda en la lista para compararla con
// las demás (por ejemplo, agosto–septiembre contra
// agosto–noviembre).
//
// Las cajas del POS externo traen los tickets reales copiados;
// para ellas se muestra además si el periodo cuadra contra el
// efectivo que reportó la caja externa en sus cortes.
// =============================================================

public partial class PruebaCorteViewModel : ViewModelBase
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-MX");

    private static readonly string[] MesesCortos =
    {
        "ene", "feb", "mar", "abr", "may", "jun",
        "jul", "ago", "sep", "oct", "nov", "dic"
    };

    // Alto máximo (px) de las barras de la gráfica.
    private const double AltoMaximoBarra = 150;

    // Límite para que una simulación no se vuelva eterna.
    private const int DiasMaximos = 731;


    private readonly CajasService _cajasService = new();

    private readonly CortesService _cortesService = new();

    private bool _cajasCargadas;


    // =========================================================
    // CAJAS
    // =========================================================

    public ObservableCollection<CajaSimulacionItem> Cajas { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSimular))]
    private CajaSimulacionItem? _cajaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSimular))]
    private bool _cargandoCajas;

    public bool PuedeSimular =>
        CajaSeleccionada is not null && !CargandoCajas && !EstaSimulando;


    // =========================================================
    // PERIODO
    // =========================================================

    [ObservableProperty]
    private DateTimeOffset? _fechaDesde;

    [ObservableProperty]
    private DateTimeOffset? _fechaHasta;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsHoy))]
    [NotifyPropertyChangedFor(nameof(EsSemana))]
    [NotifyPropertyChangedFor(nameof(EsMes))]
    [NotifyPropertyChangedFor(nameof(EsMesAnterior))]
    [NotifyPropertyChangedFor(nameof(EsTrimestre))]
    [NotifyPropertyChangedFor(nameof(EsAnio))]
    private string _rangoActivo = "Mes";

    public bool EsHoy => RangoActivo == "Hoy";
    public bool EsSemana => RangoActivo == "Semana";
    public bool EsMes => RangoActivo == "Mes";
    public bool EsMesAnterior => RangoActivo == "MesAnterior";
    public bool EsTrimestre => RangoActivo == "Trimestre";
    public bool EsAnio => RangoActivo == "Anio";


    // =========================================================
    // ESTADO
    // =========================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarVacio))]
    [NotifyPropertyChangedFor(nameof(MostrarResultado))]
    [NotifyPropertyChangedFor(nameof(PuedeSimular))]
    private bool _estaSimulando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarVacio))]
    [NotifyPropertyChangedFor(nameof(MostrarResultado))]
    private bool _tieneResultado;

    [ObservableProperty]
    private string _mensajeProceso = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    public bool MostrarVacio =>
        !TieneResultado && !EstaSimulando;

    public bool MostrarResultado =>
        TieneResultado && !EstaSimulando;


    // =========================================================
    // RESULTADO
    // =========================================================

    [ObservableProperty]
    private int _numeroSimulacion;

    [ObservableProperty]
    private string _cajaTexto = string.Empty;

    [ObservableProperty]
    private string _rangoTexto = "Elige un periodo";

    [ObservableProperty]
    private string _diasTexto = string.Empty;

    [ObservableProperty]
    private string _turnosTexto = string.Empty;

    [ObservableProperty]
    private string _totalVentasTexto = "$0.00";

    [ObservableProperty]
    private string _ticketsTexto = "0";

    [ObservableProperty]
    private string _ticketPromedioTexto = "$0.00";

    [ObservableProperty]
    private string _ventasCreditoTexto = "$0.00";

    [ObservableProperty]
    private string _ticketsCreditoTexto = "0 tickets";

    [ObservableProperty]
    private string _fondoInicialTexto = "$0.00";

    [ObservableProperty]
    private string _ventasEfectivoTexto = "$0.00";

    [ObservableProperty]
    private string _ingresosTexto = "$0.00";

    [ObservableProperty]
    private string _egresosTexto = "$0.00";

    [ObservableProperty]
    private string _retirosTexto = "$0.00";

    [ObservableProperty]
    private string _devolucionesTexto = "$0.00";

    [ObservableProperty]
    private string _cancelacionesTexto = "$0.00";

    [ObservableProperty]
    private string _cancelacionesDetalle = "0 tickets";

    [ObservableProperty]
    private string _efectivoEsperadoTexto = "$0.00";

    [ObservableProperty]
    private string _tituloGrafica = "Ventas por día";

    [ObservableProperty]
    private string _mejorPeriodoTexto = string.Empty;

    [ObservableProperty]
    private string _avisoFuturo = string.Empty;


    // Comparación contra la caja externa

    [ObservableProperty]
    private bool _tieneComparacion;

    [ObservableProperty]
    private bool _comparacionCuadra;

    [ObservableProperty]
    private bool _comparacionSinCortes;

    [ObservableProperty]
    private string _comparacionTitulo = string.Empty;

    [ObservableProperty]
    private string _comparacionDetalle = string.Empty;

    [ObservableProperty]
    private string _efectivoSistemaTexto = "$0.00";

    [ObservableProperty]
    private string _efectivoCajaExternaTexto = "$0.00";

    [ObservableProperty]
    private string _diferenciaExternaTexto = "$0.00";


    public ObservableCollection<FormaPagoSimuladaItem> FormasPago { get; } = new();

    public ObservableCollection<BarraPeriodoItem> Barras { get; } = new();

    public ObservableCollection<CorteExternoItem> CortesExternos { get; } = new();

    public ObservableCollection<SimulacionItem> Simulaciones { get; } = new();


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PruebaCorteViewModel()
    {
        AplicarRango("Mes");
    }


    // =========================================================
    // CARGAR CAJAS
    // =========================================================
    //
    // La vista lo llama al mostrarse. Incluye las cajas del POS
    // externo (traen los tickets reales copiados).

    public async Task CargarCajasAsync(bool forzar = false)
    {
        if ((_cajasCargadas && !forzar) || CargandoCajas)
        {
            return;
        }

        CargandoCajas = true;
        MensajeError = string.Empty;

        try
        {
            var respuesta =
                await _cajasService.ObtenerCajasAsync(
                    incluirExternas: true);

            if (respuesta.Res != 1)
            {
                MensajeError = respuesta.Msg ?? "No fue posible consultar las cajas.";
                return;
            }

            var idAnterior = CajaSeleccionada?.Id ?? 0;

            Cajas.Clear();

            // Primero las del POS externo (tienen los tickets reales).
            foreach (var caja in respuesta.Data
                         .OrderByDescending(c => c.EsExterna)
                         .ThenBy(c => c.UnidadNombre)
                         .ThenBy(c => c.Codigo))
            {
                Cajas.Add(new CajaSimulacionItem
                {
                    Id = caja.Id,
                    Nombre = string.IsNullOrWhiteSpace(caja.Nombre) ? caja.Codigo : caja.Nombre,
                    Detalle = string.Join("  ·  ", new[] { caja.Codigo, caja.UnidadNombre }
                        .Where(t => !string.IsNullOrWhiteSpace(t))),
                    EsExterna = caja.EsExterna
                });
            }

            CajaSeleccionada =
                Cajas.FirstOrDefault(c => c.Id == idAnterior)
                ?? Cajas.FirstOrDefault(c => c.Id == PosSession.IdCaja)
                ?? Cajas.FirstOrDefault();

            _cajasCargadas = true;

            if (Cajas.Count == 0)
            {
                MensajeError = "No hay cajas para simular.";
            }
        }
        finally
        {
            CargandoCajas = false;
        }
    }


    // =========================================================
    // RANGOS RÁPIDOS
    // =========================================================

    [RelayCommand]
    private void AplicarRango(string rango)
    {
        var hoy = DateTime.Today;

        (DateTime desde, DateTime hasta) = rango switch
        {
            "Hoy" => (hoy, hoy),
            "Semana" => (hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)), hoy),
            "MesAnterior" => (
                new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1),
                new DateTime(hoy.Year, hoy.Month, 1).AddDays(-1)),
            "Trimestre" => (new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-2), hoy),
            "Anio" => (new DateTime(hoy.Year, 1, 1), hoy),
            _ => (new DateTime(hoy.Year, hoy.Month, 1), hoy)
        };

        FechaDesde = new DateTimeOffset(desde);
        FechaHasta = new DateTimeOffset(hasta);
        RangoActivo = rango;
    }

    // Si el usuario mueve las fechas a mano, ya no aplica el rango rápido.
    partial void OnFechaDesdeChanged(DateTimeOffset? value) =>
        RangoActivo = string.Empty;

    partial void OnFechaHastaChanged(DateTimeOffset? value) =>
        RangoActivo = string.Empty;


    // =========================================================
    // SIMULAR
    // =========================================================

    [RelayCommand]
    private async Task SimularAsync()
    {
        if (EstaSimulando)
        {
            return;
        }

        MensajeError = string.Empty;

        var caja = CajaSeleccionada;

        if (caja is null)
        {
            MensajeError = "Selecciona la caja que quieres simular.";
            return;
        }

        var desde = (FechaDesde?.Date ?? DateTime.Today).Date;
        var hasta = (FechaHasta?.Date ?? desde).Date;

        if (hasta < desde)
        {
            MensajeError = "La fecha \"Hasta\" no puede ser anterior a la fecha \"Desde\".";
            return;
        }

        if ((hasta - desde).TotalDays + 1 > DiasMaximos)
        {
            MensajeError = "El periodo máximo para simular es de 2 años.";
            return;
        }

        EstaSimulando = true;
        MensajeProceso = $"Leyendo los tickets de {caja.Nombre}…";

        try
        {
            var respuesta =
                await _cortesService.SimularCorteAsync(caja.Id, desde, hasta);

            if (respuesta.Res != 1 || respuesta.Data is null)
            {
                MensajeError = respuesta.Msg ?? "No fue posible simular el corte.";
                return;
            }

            var simulacion = RegistrarSimulacion(caja, desde, hasta, respuesta.Data);

            MostrarResultadoDe(simulacion);

            TieneResultado = true;
        }
        finally
        {
            EstaSimulando = false;
        }
    }


    [RelayCommand]
    private void VolverASimular(SimulacionItem? simulacion)
    {
        if (simulacion is null || EstaSimulando)
        {
            return;
        }

        MostrarResultadoDe(simulacion);
    }


    [RelayCommand]
    private void LimpiarSimulaciones()
    {
        Simulaciones.Clear();
        TieneResultado = false;
        NumeroSimulacion = 0;
        RangoTexto = "Elige un periodo";
        TotalVentasTexto = "$0.00";
        TicketsTexto = "0";
        VentasCreditoTexto = "$0.00";
    }


    // =========================================================
    // MOSTRAR RESULTADO
    // =========================================================

    private void MostrarResultadoDe(SimulacionItem simulacion)
    {
        var datos = simulacion.Datos;
        var ventas = datos.Ventas;
        var efectivo = datos.Efectivo;
        var desde = simulacion.Desde;
        var hasta = simulacion.Hasta;

        NumeroSimulacion = simulacion.Numero;
        CajaTexto = simulacion.CajaTexto;
        RangoTexto = simulacion.RangoTexto;
        DiasTexto = simulacion.DiasTexto;

        var turnos = datos.Periodo?.Turnos ?? 0;
        TurnosTexto = turnos == 1 ? "1 turno" : $"{turnos:N0} turnos";

        TotalVentasTexto = Dinero(ventas.TotalVentas);
        TicketsTexto = ventas.Tickets.ToString("N0", Cultura);
        TicketPromedioTexto = Dinero(ventas.Tickets == 0 ? 0 : ventas.TotalVentas / ventas.Tickets);
        VentasCreditoTexto = Dinero(ventas.VentasCredito);
        TicketsCreditoTexto = $"{Tickets(ventas.TicketsCredito)} a crédito";

        FondoInicialTexto = Dinero(efectivo.FondoInicial);
        VentasEfectivoTexto = Dinero(efectivo.VentasEfectivo);
        IngresosTexto = Dinero(efectivo.Ingresos);
        EgresosTexto = Dinero(efectivo.Egresos);
        RetirosTexto = Dinero(efectivo.Retiros);
        DevolucionesTexto = Dinero(efectivo.Devoluciones);
        EfectivoEsperadoTexto = Dinero(efectivo.EfectivoEsperado);

        CancelacionesTexto = Dinero(datos.Canceladas.Total);
        CancelacionesDetalle = Tickets(datos.Canceladas.Tickets);

        AvisoFuturo = hasta > DateTime.Today
            ? "Los días posteriores a hoy todavía no tienen tickets; se sumarán conforme se vayan registrando ventas."
            : string.Empty;

        // Formas de pago (el porcentaje es sobre lo cobrado en total)
        var cobrado =
            ventas.VentasEfectivo + ventas.VentasTarjetas + ventas.VentasTransferencias
            + ventas.VentasCredito + ventas.VentasOtras;

        FormasPago.Clear();
        AgregarFormaPago("Efectivo", ventas.VentasEfectivo, cobrado, "#16A34A", "#EAF7EF", ventas.TicketsEfectivo);
        AgregarFormaPago("Tarjeta", ventas.VentasTarjetas, cobrado, "#2563EB", "#EAF1FE", ventas.TicketsTarjetas);
        AgregarFormaPago("Transferencia", ventas.VentasTransferencias, cobrado, "#0F766E", "#E6F4F2", ventas.TicketsTransferencias);
        AgregarFormaPago("Crédito", ventas.VentasCredito, cobrado, "#6D4AE0", "#F1EEFE", ventas.TicketsCredito);

        if (ventas.VentasOtras != 0)
        {
            AgregarFormaPago("Otras (vales, etc.)", ventas.VentasOtras, cobrado, "#B88A00", "#FEF7DC", ventas.TicketsOtras);
        }

        LlenarGrafica(desde, hasta, datos.PorDia);
        LlenarComparacion(datos.Comparacion);

        foreach (var item in Simulaciones)
        {
            item.EsActual = ReferenceEquals(item, simulacion);
        }
    }


    // Gráfica: por día hasta un mes, por mes en periodos largos.
    // Los días o meses sin ventas también aparecen (en cero).
    private void LlenarGrafica(DateTime desde, DateTime hasta, IReadOnlyList<SimulacionDia> porDia)
    {
        var totales = new Dictionary<DateTime, decimal>();

        foreach (var dia in porDia)
        {
            if (DateTime.TryParseExact(dia.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                totales[fecha.Date] = dia.Total;
            }
        }

        var totalDias = (int)(hasta - desde).TotalDays + 1;
        var porMes = totalDias > 31;

        TituloGrafica = porMes ? "Ventas por mes" : "Ventas por día";

        var grupos = new List<(string Etiqueta, string Detalle, decimal Monto)>();

        if (porMes)
        {
            for (var mes = new DateTime(desde.Year, desde.Month, 1); mes <= hasta; mes = mes.AddMonths(1))
            {
                var monto = totales
                    .Where(t => t.Key.Year == mes.Year && t.Key.Month == mes.Month)
                    .Sum(t => t.Value);

                grupos.Add((MesesCortos[mes.Month - 1], mes.Year.ToString(), monto));
            }
        }
        else
        {
            for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
            {
                grupos.Add((dia.Day.ToString(), MesesCortos[dia.Month - 1], totales.GetValueOrDefault(dia)));
            }
        }

        var maximo = grupos.Count == 0 ? 0 : grupos.Max(g => g.Monto);

        Barras.Clear();

        for (var i = 0; i < grupos.Count; i++)
        {
            var grupo = grupos[i];

            Barras.Add(new BarraPeriodoItem
            {
                Etiqueta = grupo.Etiqueta,
                Detalle = grupo.Detalle,
                MontoTexto = Dinero(grupo.Monto),
                AltoBarra = maximo <= 0 ? 4 : Math.Max(4, (double)(grupo.Monto / maximo) * AltoMaximoBarra),
                EsMejor = maximo > 0 && grupo.Monto == maximo,
                // Con muchos días solo se rotula uno de cada cinco.
                MostrarEtiqueta = porMes || grupos.Count <= 16 || i % 5 == 0
            });
        }

        if (maximo <= 0)
        {
            MejorPeriodoTexto = "Sin ventas en el periodo";
            return;
        }

        var mejor = grupos.First(g => g.Monto == maximo);

        MejorPeriodoTexto = porMes
            ? $"Mejor mes: {mejor.Etiqueta} {mejor.Detalle} · {Dinero(maximo)}"
            : $"Mejor día: {mejor.Etiqueta} {mejor.Detalle} · {Dinero(maximo)}";
    }


    private void LlenarComparacion(SimulacionComparacion? comparacion)
    {
        CortesExternos.Clear();

        TieneComparacion = comparacion is not null;

        if (comparacion is null)
        {
            return;
        }

        ComparacionSinCortes = comparacion.Cortes == 0;
        ComparacionCuadra = comparacion.Cuadra;

        EfectivoSistemaTexto = Dinero(comparacion.EfectivoSistema);
        EfectivoCajaExternaTexto = Dinero(comparacion.EfectivoCajaExterna);
        DiferenciaExternaTexto = DineroConSigno(comparacion.Diferencia);

        if (comparacion.Cortes == 0)
        {
            ComparacionTitulo = "Sin cortes de la caja externa";
            ComparacionDetalle = "No hay cortes copiados del POS externo en este periodo para comparar.";
        }
        else if (comparacion.Cuadra)
        {
            ComparacionTitulo = "Cuadra con la caja externa";
            ComparacionDetalle = comparacion.Cortes == 1
                ? "El único corte del periodo coincide con el efectivo que reportó la caja externa."
                : $"Los {comparacion.Cortes} cortes del periodo coinciden con el efectivo que reportó la caja externa.";
        }
        else
        {
            ComparacionTitulo = "No cuadra con la caja externa";
            ComparacionDetalle = comparacion.CortesQueNoCuadran == 1
                ? $"1 de {comparacion.Cortes} cortes tiene diferencia contra el efectivo de la caja externa."
                : $"{comparacion.CortesQueNoCuadran} de {comparacion.Cortes} cortes tienen diferencia contra el efectivo de la caja externa.";
        }

        // Primero los que no cuadran.
        foreach (var corte in comparacion.Detalle
                     .OrderBy(c => c.Cuadra)
                     .ThenBy(c => c.FechaCorte))
        {
            CortesExternos.Add(new CorteExternoItem
            {
                Folio = corte.Folio ?? $"Corte {corte.Id}",
                Fecha = corte.FechaCorte ?? string.Empty,
                EfectivoSistemaTexto = Dinero(corte.EfectivoSistema),
                EfectivoCajaExternaTexto = Dinero(corte.EfectivoCajaExterna),
                DiferenciaTexto = corte.Cuadra ? "CUADRA" : DineroConSigno(corte.Diferencia),
                Cuadra = corte.Cuadra,
                Sobrante = !corte.Cuadra && corte.Diferencia > 0
            });
        }
    }


    private void AgregarFormaPago(string nombre, decimal monto, decimal total, string color, string fondo, int tickets)
    {
        var porcentaje = total <= 0 ? 0 : (double)(monto / total * 100);

        FormasPago.Add(new FormaPagoSimuladaItem
        {
            Nombre = nombre,
            MontoTexto = Dinero(monto),
            Porcentaje = porcentaje,
            PorcentajeTexto = $"{porcentaje:0.#}%",
            TicketsTexto = Tickets(tickets),
            Color = SolidColorBrush.Parse(color),
            Fondo = SolidColorBrush.Parse(fondo)
        });
    }


    private SimulacionItem RegistrarSimulacion(
        CajaSimulacionItem caja,
        DateTime desde,
        DateTime hasta,
        SimulacionCorteData datos)
    {
        var total = datos.Ventas.TotalVentas;
        var totalDias = (int)(hasta - desde).TotalDays + 1;

        // Solo se compara contra la simulación anterior de la misma caja.
        var anterior = Simulaciones.FirstOrDefault(s => s.IdCaja == caja.Id);

        var item = new SimulacionItem
        {
            Numero = (Simulaciones.FirstOrDefault()?.Numero ?? 0) + 1,
            IdCaja = caja.Id,
            CajaTexto = caja.Nombre,
            Desde = desde,
            Hasta = hasta,
            RangoTexto = desde == hasta
                ? FormatearFecha(desde)
                : $"{FormatearFecha(desde)} – {FormatearFecha(hasta)}",
            DiasTexto = totalDias == 1 ? "1 día" : $"{totalDias:N0} días",
            Total = total,
            TotalTexto = Dinero(total),
            TicketsTexto = Tickets(datos.Ventas.Tickets),
            CreditoTexto = $"Crédito {Dinero(datos.Ventas.VentasCredito)}",
            Datos = datos
        };

        if (anterior is not null)
        {
            var diferencia = total - anterior.Total;

            item.TieneComparacion = true;
            item.EsMayor = diferencia >= 0;
            item.DiferenciaTexto = $"{(diferencia >= 0 ? "+" : "−")}{Dinero(Math.Abs(diferencia))} vs #{anterior.Numero}";
        }

        Simulaciones.Insert(0, item);

        return item;
    }


    // =========================================================
    // FORMATO
    // =========================================================

    private static string Dinero(decimal monto) =>
        monto.ToString("C2", Cultura);

    private static string DineroConSigno(decimal monto) =>
        monto == 0
            ? Dinero(0)
            : $"{(monto > 0 ? "+" : "−")}{Dinero(Math.Abs(monto))}";

    private static string Tickets(int cantidad) =>
        cantidad == 1 ? "1 ticket" : $"{cantidad.ToString("N0", Cultura)} tickets";

    private static string FormatearFecha(DateTime fecha) =>
        $"{fecha.Day:00} {MesesCortos[fecha.Month - 1]} {fecha.Year}";
}


// =============================================================
// ELEMENTOS DE LA VISTA
// =============================================================

public class CajaSimulacionItem
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
    public bool EsExterna { get; init; }
}


public class FormaPagoSimuladaItem
{
    public string Nombre { get; init; } = string.Empty;
    public string MontoTexto { get; init; } = string.Empty;
    public double Porcentaje { get; init; }
    public string PorcentajeTexto { get; init; } = string.Empty;
    public string TicketsTexto { get; init; } = string.Empty;
    public IBrush Color { get; init; } = Brushes.Gray;
    public IBrush Fondo { get; init; } = Brushes.WhiteSmoke;
}


public class BarraPeriodoItem
{
    public string Etiqueta { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
    public string MontoTexto { get; init; } = string.Empty;
    public double AltoBarra { get; init; }
    public bool EsMejor { get; init; }
    public bool MostrarEtiqueta { get; init; } = true;

    public string Tooltip => $"{Etiqueta} {Detalle} · {MontoTexto}";
}


public class CorteExternoItem
{
    public string Folio { get; init; } = string.Empty;
    public string Fecha { get; init; } = string.Empty;
    public string EfectivoSistemaTexto { get; init; } = string.Empty;
    public string EfectivoCajaExternaTexto { get; init; } = string.Empty;
    public string DiferenciaTexto { get; init; } = string.Empty;
    public bool Cuadra { get; init; }
    public bool Sobrante { get; init; }
}


public partial class SimulacionItem : ObservableObject
{
    public int Numero { get; init; }
    public int IdCaja { get; init; }
    public string CajaTexto { get; init; } = string.Empty;
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public string RangoTexto { get; init; } = string.Empty;
    public string DiasTexto { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string TotalTexto { get; init; } = string.Empty;
    public string TicketsTexto { get; init; } = string.Empty;
    public string CreditoTexto { get; init; } = string.Empty;
    public SimulacionCorteData Datos { get; init; } = new();

    public bool TieneComparacion { get; set; }
    public bool EsMayor { get; set; }
    public string DiferenciaTexto { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _esActual;
}
