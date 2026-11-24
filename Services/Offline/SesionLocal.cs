using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NovaCoreESDM.Models.Auth;

namespace NovaCoreESDM.Services.Offline;

/// <summary>
/// Recuerda a los usuarios que ya iniciaron sesión con internet en esta
/// caja, para dejarlos entrar cuando no hay conexión. La contraseña nunca
/// se guarda: solo una huella (PBKDF2 con sal) para comprobarla.
/// </summary>
public static class SesionLocal
{
    private const int Iteraciones = 120_000;
    private static readonly TimeSpan Vigencia = TimeSpan.FromDays(30);

    private sealed class UsuarioGuardado
    {
        public string Usuario { get; set; } = string.Empty;
        public string Sal { get; set; } = string.Empty;
        public string Huella { get; set; } = string.Empty;
        public DateTime UltimoAccesoEnLinea { get; set; }
        public LoginData Datos { get; set; } = new();
    }

    private static string Ruta => AlmacenLocal.Ruta("usuarios.json");

    /// <summary>
    /// Usuario y contraseña del inicio de sesión actual, solo en memoria
    /// (nunca en disco), para volver a abrir la sesión del servidor al
    /// enviar las ventas pendientes.
    /// </summary>
    public static (string Usuario, string Contrasena) CredencialesEnMemoria { get; private set; }

    public static void RecordarEnMemoria(string usuario, string contrasena) =>
        CredencialesEnMemoria = (usuario.Trim(), contrasena);

    public static void OlvidarEnMemoria() =>
        CredencialesEnMemoria = (string.Empty, string.Empty);


    public static void Recordar(string usuario, string contrasena, LoginData datos)
    {
        try
        {
            var sal = RandomNumberGenerator.GetBytes(16);
            var registro = new UsuarioGuardado
            {
                Usuario = Normalizar(usuario),
                Sal = Convert.ToBase64String(sal),
                Huella = Convert.ToBase64String(Huella(contrasena, sal)),
                UltimoAccesoEnLinea = DateTime.Now,
                Datos = datos
            };

            var usuarios = Leer()
                .Where(u => u.Usuario != registro.Usuario)
                .Append(registro)
                .ToList();

            AlmacenLocal.Guardar(Ruta, usuarios);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo recordar al usuario para el modo sin conexión: {ex.Message}");
        }
    }


    /// <summary>
    /// Comprueba usuario y contraseña contra lo guardado. Devuelve los datos
    /// del usuario o null con el motivo en <paramref name="motivo"/>.
    /// </summary>
    public static LoginData? Validar(string usuario, string contrasena, out string motivo)
    {
        var registro = Leer().FirstOrDefault(u => u.Usuario == Normalizar(usuario));

        if (registro is null)
        {
            motivo = "No hay internet y este usuario nunca ha iniciado sesión en esta caja. Debe entrar una vez con internet.";
            return null;
        }

        if (DateTime.Now - registro.UltimoAccesoEnLinea > Vigencia)
        {
            motivo = "No hay internet y este usuario no ha entrado con internet en más de 30 días.";
            return null;
        }

        var esperada = Convert.FromBase64String(registro.Huella);
        var calculada = Huella(contrasena, Convert.FromBase64String(registro.Sal));

        if (!CryptographicOperations.FixedTimeEquals(esperada, calculada))
        {
            motivo = "Usuario o contraseña incorrectos.";
            return null;
        }

        motivo = string.Empty;
        return registro.Datos;
    }


    private static List<UsuarioGuardado> Leer() =>
        AlmacenLocal.Leer<List<UsuarioGuardado>>(Ruta) ?? new List<UsuarioGuardado>();

    private static string Normalizar(string usuario) =>
        usuario.Trim().ToLowerInvariant();

    private static byte[] Huella(string contrasena, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(contrasena), sal, Iteraciones,
            HashAlgorithmName.SHA256, 32);
}
