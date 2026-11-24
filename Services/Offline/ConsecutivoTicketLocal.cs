using System;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Último número de ticket de la caja (UO-SERIE-00000000). Cada caja tiene
/// su propia serie y solo ella la avanza, así que sin internet puede seguir
/// la numeración: el servidor respeta el número al sincronizar.
/// </summary>
public static class ConsecutivoTicketLocal
{
    private static readonly object Candado = new();

    private sealed class Estado
    {
        public string CodigoUnidad { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public long UltimoNumero { get; set; }
        public DateTime Actualizado { get; set; }
    }

    private static string Ruta(int idCaja) => AlmacenLocal.Ruta($"consecutivo-{idCaja}.json");


    /// <summary>Registra un número de ticket que ya usó la caja (nunca retrocede).</summary>
    public static void Aprender(int idCaja, string? codigoUnidad, string? serie, long numero)
    {
        if (idCaja <= 0 || string.IsNullOrWhiteSpace(serie) || numero < 0)
            return;

        try
        {
            lock (Candado)
            {
                var estado = AlmacenLocal.Leer<Estado>(Ruta(idCaja)) ?? new Estado();
                var serieNueva = serie.Trim().ToUpperInvariant();

                // Si cambió la serie de la caja, el consecutivo empieza de nuevo.
                if (!string.Equals(estado.Serie, serieNueva, StringComparison.Ordinal))
                {
                    estado.Serie = serieNueva;
                    estado.UltimoNumero = 0;
                }

                if (!string.IsNullOrWhiteSpace(codigoUnidad))
                    estado.CodigoUnidad = codigoUnidad.Trim().ToUpperInvariant();

                estado.UltimoNumero = Math.Max(estado.UltimoNumero, numero);
                estado.Actualizado = DateTime.Now;

                AlmacenLocal.Guardar(Ruta(idCaja), estado);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo guardar el consecutivo de tickets: {ex.Message}");
        }
    }


    /// <summary>
    /// Aparta el siguiente número para una venta sin conexión. null si la
    /// caja todavía no conoce su serie (nunca ha vendido con internet).
    /// </summary>
    public static (string Folio, string Serie, long Numero)? Reservar(int idCaja)
    {
        lock (Candado)
        {
            var estado = AlmacenLocal.Leer<Estado>(Ruta(idCaja));

            if (estado is null ||
                string.IsNullOrWhiteSpace(estado.Serie) ||
                string.IsNullOrWhiteSpace(estado.CodigoUnidad))
                return null;

            estado.UltimoNumero++;
            estado.Actualizado = DateTime.Now;

            // Se guarda antes de imprimir: un número nunca se usa dos veces.
            AlmacenLocal.Guardar(Ruta(idCaja), estado);

            return (Formatear(estado.CodigoUnidad, estado.Serie, estado.UltimoNumero),
                    estado.Serie,
                    estado.UltimoNumero);
        }
    }


    public static string Formatear(string codigoUnidad, string serie, long numero) =>
        $"{codigoUnidad}-{serie}-{numero:D8}";
}
