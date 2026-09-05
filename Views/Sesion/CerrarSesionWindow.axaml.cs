using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Sesion;

public partial class CerrarSesionWindow
    : Window
{
    public enum ResultadoCerrarSesion
    {
        Cancelar,
        IrACorte,
        CerrarSesion
    }


    private readonly bool
        _tieneTurnoActivo;


    public CerrarSesionWindow(
        bool tieneTurnoActivo,
        int idTurno = 0,
        string? nombreCaja = null)
    {
        InitializeComponent();


        _tieneTurnoActivo =
            tieneTurnoActivo;


        ConfigurarVista(
            idTurno,
            nombreCaja);
    }


    // ============================================================
    // CONFIGURAR VISTA
    // ============================================================

    private void ConfigurarVista(
        int idTurno,
        string? nombreCaja)
    {
        if (_tieneTurnoActivo)
        {
            TxtTitulo.Text =
                "Tienes un turno abierto";


            TxtDescripcion.Text =
                "Puedes realizar el corte antes de cerrar sesión o salir sin cerrar el turno.";


            BorderTurnoActivo.IsVisible =
                true;


            BtnIrACorte.IsVisible =
                true;


            var caja =
                !string.IsNullOrWhiteSpace(
                    nombreCaja)
                    ? nombreCaja
                    : "la caja actual";


            TxtTurnoActivo.Text =
                idTurno > 0
                    ? $"El turno #{idTurno} permanece abierto en {caja}."
                    : $"Existe un turno abierto en {caja}.";


            TxtInformacion.Text =
                "Si cierras sesión sin realizar el corte, el turno permanecerá abierto y podrá recuperarse al volver a iniciar sesión en esta caja.";


            BtnCerrarSesion.Content =
                "Cerrar sin corte";

            return;
        }


        // ========================================================
        // SIN TURNO ACTIVO
        // ========================================================

        TxtTitulo.Text =
            "Cerrar sesión";


        TxtDescripcion.Text =
            "¿Deseas cerrar la sesión actual?";


        BorderTurnoActivo.IsVisible =
            false;


        BtnIrACorte.IsVisible =
            false;


        TxtInformacion.Text =
            "La configuración de esta terminal y caja permanecerá guardada.";


        BtnCerrarSesion.Content =
            "Cerrar sesión";
    }


    // ============================================================
    // BOTONES
    // ============================================================

    private void Volver_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            ResultadoCerrarSesion.Cancelar);
    }


    private void IrACorte_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            ResultadoCerrarSesion.IrACorte);
    }


    private void CerrarSesion_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            ResultadoCerrarSesion.CerrarSesion);
    }
}