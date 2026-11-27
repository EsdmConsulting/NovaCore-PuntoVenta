using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Models.Auth;
using NovaCoreESDM.Models.Configuration;
using NovaCoreESDM.Services.Auth;
using NovaCoreESDM.Services.Configuration;

namespace NovaCoreESDM.ViewModels;

/// <summary>
/// Un nombre en la lista del inicio de sesión.
/// </summary>
public sealed class UsuarioCatalogoItem
{
    private static readonly string[] Colores =
    {
        "#16A34A", "#2563EB", "#C61F2B", "#B5651D", "#7C3AED", "#0891B2", "#DB2777", "#CA8A04"
    };

    public UsuarioCatalogoItem(UsuarioCatalogo datos)
    {
        Datos = datos;

        var partes = (datos.Nombre ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Iniciales = partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => (partes[0][..1] + partes[1][..1]).ToUpperInvariant()
        };

        // Siempre el mismo color para la misma persona.
        var semilla = datos.IdUsuario > 0 ? datos.IdUsuario : (datos.Nombre ?? string.Empty).Length;
        ColorAvatar = Brush.Parse(Colores[Math.Abs(semilla) % Colores.Length]);

        Rol = datos.EsAdministrador ? "Administrador" : Bonito(datos.Rol);

        TextoBusqueda = SinAcentos($"{datos.Nombre} {datos.Usuario} {datos.Rol}");
    }

    public UsuarioCatalogo Datos { get; }
    public string Nombre => Datos.Nombre;
    public string Usuario => Datos.Usuario;
    public string Iniciales { get; }
    public IBrush ColorAvatar { get; }
    public string Rol { get; }
    public bool EsAdministrador => Datos.EsAdministrador;
    public string TextoBusqueda { get; }


    // "CAJERO PUNTO DE VENTA" → "Cajero punto de venta"
    private static string Bonito(string? rol)
    {
        if (string.IsNullOrWhiteSpace(rol))
            return string.Empty;

        var minusculas = rol.Trim().ToLower(new CultureInfo("es-MX"));
        return char.ToUpper(minusculas[0], new CultureInfo("es-MX")) + minusculas[1..];
    }

    public static string SinAcentos(string texto)
    {
        var normal = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normal.Length);

        foreach (var c in normal)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}


/// <summary>
/// CHG-0003: inicio de sesión eligiendo el nombre de una lista (los usuarios
/// de la caja de este equipo) y escribiendo solo la contraseña. Si no hay
/// lista (equipo sin caja, servidor sin el cambio, sin internet y sin copia)
/// queda el formulario de siempre, con el usuario escrito a mano.
/// </summary>
public partial class LoginViewModel
{
    public ObservableCollection<UsuarioCatalogoItem> UsuariosFiltrados { get; } = new();

    private UsuarioCatalogoItem[] _todos = Array.Empty<UsuarioCatalogoItem>();

    /// <summary>Se pidió escribir el usuario a mano aunque haya lista.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarLista), nameof(MostrarFormulario), nameof(MostrarCampoUsuario))]
    private bool _modoManual;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarLista), nameof(MostrarFormulario), nameof(MostrarCampoUsuario), nameof(MostrarVolverALista))]
    private bool _hayCatalogo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarLista), nameof(MostrarFormulario), nameof(MostrarCampoUsuario), nameof(HayUsuarioSeleccionado))]
    private UsuarioCatalogoItem? _usuarioSeleccionado;

    [ObservableProperty]
    private bool _cargandoCatalogo;

    [ObservableProperty]
    private string _busqueda = string.Empty;

    [ObservableProperty]
    private string _textoCaja = string.Empty;

    /// <summary>Aviso bajo la lista (p. ej. "lista guardada, sin internet").</summary>
    [ObservableProperty]
    private string _avisoCatalogo = string.Empty;

    [ObservableProperty]
    private bool _sinResultados;

    public bool MostrarLista => HayCatalogo && !ModoManual && UsuarioSeleccionado is null;
    public bool MostrarFormulario => !MostrarLista;
    public bool MostrarCampoUsuario => UsuarioSeleccionado is null;
    public bool HayUsuarioSeleccionado => UsuarioSeleccionado is not null;
    public bool MostrarVolverALista => HayCatalogo;

    /// <summary>La vista pone el foco en la contraseña.</summary>
    public event Action? UsuarioElegido;

    /// <summary>La vista pone el foco en la búsqueda.</summary>
    public event Action? ListaMostrada;


    public async Task CargarCatalogoAsync()
    {
        TerminalConfiguration? configuracion;

        try
        {
            configuracion = await new TerminalConfigurationService().ObtenerConfiguracionAsync();
        }
        catch
        {
            configuracion = null;
        }

        // Equipo nuevo, sin caja: el primer acceso es a mano (lo hace un administrador).
        if (configuracion is null || string.IsNullOrWhiteSpace(configuracion.UuidCaja))
            return;

        TextoCaja = string.Join(" · ", new[] { configuracion.NombreCaja, configuracion.UnidadNombre }
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        CargandoCatalogo = true;

        try
        {
            var resultado = await UsuariosCajaService.ObtenerAsync(configuracion.UuidCaja);

            if (resultado.Res != 1 || resultado.Data is null || resultado.Data.Usuarios.Count == 0)
                return;

            if (resultado.Data.Caja is { } caja)
            {
                var texto = string.Join(" · ", new[] { caja.Nombre, caja.UnidadNombre }
                    .Where(t => !string.IsNullOrWhiteSpace(t)));

                if (!string.IsNullOrWhiteSpace(texto))
                    TextoCaja = texto;
            }

            _todos = resultado.Data.Usuarios
                .Where(u => !string.IsNullOrWhiteSpace(u.Usuario))
                .Select(u => new UsuarioCatalogoItem(u))
                .ToArray();

            AvisoCatalogo = resultado.SinConexion
                ? "Sin internet: se muestra la última lista guardada. Solo pueden entrar quienes ya iniciaron sesión antes en esta caja."
                : string.Empty;

            Filtrar();

            HayCatalogo = _todos.Length > 0;

            if (HayCatalogo)
                ListaMostrada?.Invoke();
        }
        finally
        {
            CargandoCatalogo = false;
        }
    }


    partial void OnBusquedaChanged(string value) => Filtrar();


    private void Filtrar()
    {
        var texto = UsuarioCatalogoItem.SinAcentos(Busqueda.Trim());
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        UsuariosFiltrados.Clear();

        foreach (var u in _todos.Where(u => palabras.All(p => u.TextoBusqueda.Contains(p))))
            UsuariosFiltrados.Add(u);

        SinResultados = _todos.Length > 0 && UsuariosFiltrados.Count == 0;
    }


    [RelayCommand]
    private void SeleccionarUsuario(UsuarioCatalogoItem? usuario)
    {
        if (usuario is null)
            return;

        MensajeError = string.Empty;
        Contrasena = string.Empty;
        Usuario = usuario.Usuario;
        UsuarioSeleccionado = usuario;

        UsuarioElegido?.Invoke();
    }


    /// <summary>Enter en la búsqueda: si queda una sola persona, se elige.</summary>
    public void ElegirUnicoResultado()
    {
        if (UsuariosFiltrados.Count == 1)
            SeleccionarUsuario(UsuariosFiltrados[0]);
    }


    [RelayCommand]
    private void VolverALista()
    {
        if (!HayCatalogo)
            return;

        MensajeError = string.Empty;
        Contrasena = string.Empty;
        Usuario = string.Empty;
        UsuarioSeleccionado = null;
        ModoManual = false;
        Busqueda = string.Empty;

        ListaMostrada?.Invoke();
    }


    [RelayCommand]
    private void EscribirUsuario()
    {
        MensajeError = string.Empty;
        Contrasena = string.Empty;
        Usuario = string.Empty;
        UsuarioSeleccionado = null;
        ModoManual = true;
    }
}
