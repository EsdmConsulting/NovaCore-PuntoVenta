using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NovaCoreESDM.ViewModels.Devoluciones;

namespace NovaCoreESDM.Views.Devoluciones;

public partial class DevolucionesView : UserControl
{
    public DevolucionesView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is DevolucionesViewModel vm)
            {
                vm.SolicitarAutorizacionAsync = AutorizarAsync;
                vm.ConfirmarAsync = ConfirmarAsync;
            }
        };

        AttachedToVisualTree += (_, _) =>
            Dispatcher.UIThread.Post(() => CajaBusqueda.Focus(), DispatcherPriority.Input);
    }


    // El escáner escribe el código y manda Enter.
    private void Busqueda_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not DevolucionesViewModel vm)
            return;

        e.Handled = true;

        if (vm.BuscarCommand.CanExecute(null))
            vm.BuscarCommand.Execute(null);
    }


    private async Task<string?> AutorizarAsync(string titulo, string descripcion)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return null;

        var ventana = new NovaCoreESDM.Views.Sesion.AutorizacionAdministradorWindow(titulo, descripcion);
        return await ventana.ShowDialog<string?>(owner);
    }


    private async Task<bool> ConfirmarAsync(string mensaje)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return false;

        var resultado = false;

        var ventana = new Window
        {
            Title = "Confirmar devolución",
            Width = 460,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush.Parse("#F4F6F8")
        };

        var si = new Button
        {
            Content = "Sí, devolver",
            Height = 46,
            Padding = new Avalonia.Thickness(20, 0),
            CornerRadius = new Avalonia.CornerRadius(12),
            Background = Brush.Parse("#16A34A"),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsDefault = true
        };

        var no = new Button
        {
            Content = "Cancelar",
            Height = 46,
            Padding = new Avalonia.Thickness(20, 0),
            CornerRadius = new Avalonia.CornerRadius(12),
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#E2E8F0"),
            BorderThickness = new Avalonia.Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center,
            IsCancel = true
        };

        si.Click += (_, _) => { resultado = true; ventana.Close(); };
        no.Click += (_, _) => ventana.Close();

        ventana.Content = new Border
        {
            Margin = new Avalonia.Thickness(14),
            Padding = new Avalonia.Thickness(22),
            CornerRadius = new Avalonia.CornerRadius(16),
            Background = Brushes.White,
            Child = new StackPanel
            {
                Spacing = 18,
                Children =
                {
                    new TextBlock { Text = "¿Confirmar devolución?", FontSize = 19, FontWeight = FontWeight.Bold, Foreground = Brush.Parse("#0B1220") },
                    new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = Brush.Parse("#334155") },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { no, si }
                    }
                }
            }
        };

        await ventana.ShowDialog(owner);
        return resultado;
    }
}
