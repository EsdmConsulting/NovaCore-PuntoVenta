using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Cortes;

public partial class ConfirmarInicioCorteWindow
    : Window
{
    public ConfirmarInicioCorteWindow(
        int idTurno,
        string caja)
    {
        InitializeComponent();


        TxtDescripcion.Text =
            $"Estás por iniciar el corte del turno #{idTurno} " +
            $"de {caja}.";
    }


    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }


    private void Continuar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}