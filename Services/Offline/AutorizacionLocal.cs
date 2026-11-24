using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NovaCoreESDM.Models.Cancelaciones;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Autorización de administradores sin internet (cancelaciones). Cuando un
/// administrador autoriza con internet en esta caja se guarda una huella de
/// su contraseña (PBKDF2, nunca la contraseña); sin internet se compara
/// contra ella. También guarda la lista de administradores para elegirlo.
/// </summary>
public static class AutorizacionLocal
{
    private const int Iteraciones = 120_000;
    private static readonly TimeSpan Vigencia = TimeSpan.FromDays(30);

    private sealed class AdministradorGuardado
    {
        public string Usuario { get; set; } = string.Empty;
        public string Sal { get; set; } = string.Empty;
        public string Huella { get; set; } = string.Empty;
        public DateTime UltimaAutorizacionEnLinea { get; set; }
        public AutorizarCancelacionData Datos { get; set; } = new();
    }

    private static string RutaHuellas => AlmacenLocal.Ruta("administradores.json");
    private static string RutaLista => AlmacenLocal.Ruta("administradores-lista.json");

    /// <summary>Administrador autorizado sin conexión en la pantalla de cancelaciones.</summary>
    public static AutorizarCancelacionData? AutorizadoSinConexion { get; private set; }

    public static void OlvidarAutorizacion() => AutorizadoSinConexion = null;


    public static void GuardarLista(IEnumerable<AdministradorItem> administradores)
    {
        try
        {
            AlmacenLocal.Guardar(RutaLista, administradores.ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo guardar la lista de administradores: {ex.Message}");
        }
    }

    public static List<AdministradorItem> ObtenerLista() =>
        AlmacenLocal.Leer<List<AdministradorItem>>(RutaLista) ?? new List<AdministradorItem>();


    /// <summary>Se llama cuando el servidor autorizó al administrador.</summary>
    public static void Recordar(string usuario, string contrasena, AutorizarCancelacionData datos)
    {
        try
        {
            var sal = RandomNumberGenerator.GetBytes(16);
            var registro = new AdministradorGuardado
            {
                Usuario = Normalizar(usuario),
                Sal = Convert.ToBase64String(sal),
                Huella = Convert.ToBase64String(Huella(contrasena, sal)),
                UltimaAutorizacionEnLinea = DateTime.Now,
                Datos = datos
            };

            var lista = Leer()
                .Where(a => a.Usuario != registro.Usuario)
                .Append(registro)
                .ToList();

            AlmacenLocal.Guardar(RutaHuellas, lista);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo recordar al administrador para el modo sin conexión: {ex.Message}");
        }
    }


    /// <summary>Autoriza sin internet. null con el motivo si no se puede.</summary>
    public static AutorizarCancelacionData? Autorizar(string usuario, string contrasena, out string motivo)
    {
        var registro = Leer().FirstOrDefault(a => a.Usuario == Normalizar(usuario));

        if (registro is null || registro.Datos.IdUsuario <= 0)
        {
            motivo = "Sin internet solo puede autorizar un administrador que ya haya autorizado antes con internet en esta caja.";
            return null;
        }

        if (DateTime.Now - registro.UltimaAutorizacionEnLinea > Vigencia)
        {
            motivo = "Sin internet: este administrador no ha autorizado con internet en esta caja en más de 30 días.";
            return null;
        }

        var esperada = Convert.FromBase64String(registro.Huella);
        var calculada = Huella(contrasena, Convert.FromBase64String(registro.Sal));

        if (!CryptographicOperations.FixedTimeEquals(esperada, calculada))
        {
            motivo = "Contraseña del administrador incorrecta.";
            return null;
        }

        motivo = string.Empty;
        AutorizadoSinConexion = registro.Datos;
        return registro.Datos;
    }


    private static List<AdministradorGuardado> Leer() =>
        AlmacenLocal.Leer<List<AdministradorGuardado>>(RutaHuellas) ?? new List<AdministradorGuardado>();

    private static string Normalizar(string usuario) =>
        usuario.Trim().ToLowerInvariant();

    private static byte[] Huella(string contrasena, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones,
            HashAlgorithmName.SHA256, 32);
}
