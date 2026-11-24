using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NovaCoreESDM.Services.Api;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Vigila si el servidor responde. Cualquier respuesta HTTP (aunque sea
/// un error) cuenta como "en línea": lo que importa es que haya red
/// hasta el servidor.
/// </summary>
public static class ConexionService
{
    private static readonly TimeSpan IntervaloEnLinea = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan IntervaloSinConexion = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan TiempoMaximoPrueba = TimeSpan.FromSeconds(5);

    private static CancellationTokenSource? _cancelacion;
    private static readonly SemaphoreSlim _probando = new(1, 1);

    public static bool EstaEnLinea { get; private set; } = true;

    /// <summary>Se dispara (en segundo plano) cuando cambia el estado.</summary>
    public static event Action<bool>? CambioEstado;


    public static void Iniciar()
    {
        if (_cancelacion is not null)
            return;

        _cancelacion = new CancellationTokenSource();
        var token = _cancelacion.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await VerificarAsync();
                try
                {
                    await Task.Delay(
                        EstaEnLinea ? IntervaloEnLinea : IntervaloSinConexion,
                        token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }


    public static void Detener()
    {
        _cancelacion?.Cancel();
        _cancelacion = null;
    }


    /// <summary>Hace una prueba rápida al servidor y actualiza el estado.</summary>
    public static async Task<bool> VerificarAsync()
    {
        await _probando.WaitAsync();
        try
        {
            using var cancelacion = new CancellationTokenSource(TiempoMaximoPrueba);
            using var request = new HttpRequestMessage(HttpMethod.Head, "");
            using var response = await ApiClient.Http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancelacion.Token);

            Establecer(true);
        }
        catch
        {
            Establecer(false);
        }
        finally
        {
            _probando.Release();
        }

        return EstaEnLinea;
    }


    /// <summary>
    /// Lo llaman los servicios cuando una petición falla por red, para
    /// pasar a modo sin conexión sin esperar a la siguiente prueba.
    /// </summary>
    public static void ReportarFalloDeRed() => Establecer(false);


    /// <summary>Indica si una excepción es de red (sin internet, servidor caído, tiempo agotado).</summary>
    public static bool EsErrorDeRed(Exception ex) =>
        ex is HttpRequestException
            or TaskCanceledException
            or OperationCanceledException
            or System.Net.Sockets.SocketException
        || ex.InnerException is not null && EsErrorDeRed(ex.InnerException);


    private static void Establecer(bool enLinea)
    {
        if (EstaEnLinea == enLinea)
            return;

        EstaEnLinea = enLinea;
        CambioEstado?.Invoke(enLinea);
    }
}
