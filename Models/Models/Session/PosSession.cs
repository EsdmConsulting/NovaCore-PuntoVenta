namespace NovaCoreESDM.Models.Session;

public static class PosSession
{
    // =========================================================
    // USUARIO
    // =========================================================

    public static int IdUsuario { get; set; }

    public static string NombreUsuario { get; set; }
        = string.Empty;

    public static string Usuario { get; set; }
        = string.Empty;

    public static int IdRol { get; set; }

    public static string Rol { get; set; }
        = string.Empty;

    public static bool EsAdministrador { get; set; }

    public static bool EsCajero { get; set; }


    // =========================================================
    // OPERACIÓN
    // =========================================================

    public static int IdEmpresa { get; set; }

    public static int IdUnidadOperativa { get; set; }

    public static int IdCaja { get; set; }

    public static int IdTurno { get; set; }

    public static string CodigoCaja { get; set; }
        = string.Empty;

    public static string NombreCaja { get; set; }
        = string.Empty;


    public static bool TieneTurnoActivo =>
        IdTurno > 0;


    public static bool TieneUsuarioActivo =>
        IdUsuario > 0;


    public static void LimpiarTurno()
    {
        IdTurno = 0;
    }


    public static void LimpiarSesion()
    {
        IdUsuario = 0;
        NombreUsuario = string.Empty;
        Usuario = string.Empty;
        IdRol = 0;
        Rol = string.Empty;

        EsAdministrador = false;
        EsCajero = false;

        IdTurno = 0;
    }
}