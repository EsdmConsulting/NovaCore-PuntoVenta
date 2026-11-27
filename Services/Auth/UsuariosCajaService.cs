using System;
using System.Text.Json;
using System.Threading.Tasks;
using NovaCoreESDM.Models.Auth;
using NovaCoreESDM.Services.Api;
using NovaCoreESDM.Services.Offline;

namespace NovaCoreESDM.Services.Auth;

/// <summary>
/// CHG-0003: lista de usuarios que pueden entrar en la caja de este equipo,
/// para el inicio de sesión con catálogo. Se pide sin sesión, con el uuid de
/// la caja. La última lista buscada se guarda en el equipo para que, sin
/// internet, se pueda elegir el nombre igual que siempre.
/// </summary>
public static class UsuariosCajaService
{
    private sealed class CopiaGuardada
    {
        public string UuidCaja { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public UsuariosCajaData Datos { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string Ruta => AlmacenLocal.Ruta("usuarios-caja.json");


    /// <summary>
    /// Pide la lista al servidor; sin conexión regresa la copia guardada
    /// (marcada con <see cref="UsuariosCajaResponse.SinConexion"/>).
    /// </summary>
    public static async Task<UsuariosCajaResponse> ObtenerAsync(string uuidCaja)
    {
        if (string.IsNullOrWhiteSpace(uuidCaja))
            return new UsuariosCajaResponse { Res = 0, Msg = "Este equipo no tiene caja configurada." };

        try
        {
            var response = await ApiClient.Http.GetAsync(
                $"api/pos/auth/usuarios?uuid_caja={Uri.EscapeDataString(uuidCaja)}");

            var contenido = await response.Content.ReadAsStringAsync();

            var inicio = contenido.IndexOf('{');
            if (inicio < 0)
            {
                // Servidor sin este cambio todavía (404 en HTML) u otra falla:
                // se usa la copia si existe, si no se escribe el usuario.
                return DesdeCopia(uuidCaja, sinConexion: false)
                       ?? new UsuariosCajaResponse { Res = 0, Msg = $"HTTP {(int)response.StatusCode}" };
            }

            var resultado = JsonSerializer.Deserialize<UsuariosCajaResponse>(contenido[inicio..], Json)
                            ?? new UsuariosCajaResponse { Res = 0, Msg = "Respuesta inválida." };

            if (resultado.Res == 1 && resultado.Data is not null)
            {
                try
                {
                    AlmacenLocal.Guardar(Ruta, new CopiaGuardada
                    {
                        UuidCaja = uuidCaja,
                        Fecha = DateTime.Now,
                        Datos = resultado.Data
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"No se guardó la lista de usuarios: {ex.Message}");
                }
            }

            return resultado;
        }
        catch (Exception ex) when (ConexionService.EsErrorDeRed(ex))
        {
            ConexionService.ReportarFalloDeRed();

            return DesdeCopia(uuidCaja, sinConexion: true)
                   ?? new UsuariosCajaResponse { Res = 0, SinConexion = true, Msg = "Sin conexión." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR LISTA DE USUARIOS: {ex}");

            return DesdeCopia(uuidCaja, sinConexion: false)
                   ?? new UsuariosCajaResponse { Res = 0, Msg = ex.Message };
        }
    }


    private static UsuariosCajaResponse? DesdeCopia(string uuidCaja, bool sinConexion)
    {
        CopiaGuardada? copia;

        try
        {
            copia = AlmacenLocal.Leer<CopiaGuardada>(Ruta);
        }
        catch
        {
            return null;
        }

        // Una lista de otra caja (equipo reasignado) no sirve.
        if (copia is null || copia.UuidCaja != uuidCaja || copia.Datos.Usuarios.Count == 0)
            return null;

        return new UsuariosCajaResponse
        {
            Res = 1,
            SinConexion = sinConexion,
            Data = copia.Datos
        };
    }
}
