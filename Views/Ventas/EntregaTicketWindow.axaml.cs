using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NovaCoreESDM.Views.Ventas;

public partial class EntregaTicketWindow : Window
{
    public string OpcionSeleccionada { get; private set; }
        = string.Empty;


    public EntregaTicketWindow(
        string folioTicket,
        decimal total)
    {
        InitializeComponent();


        FolioTicketTextBlock.Text =
            folioTicket;


        TotalTextBlock.Text =
            $"${total:N2}";


        // Arranca con "Imprimir ticket" marcado para responder con Enter.
        Opened +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    () => BotonImprimir.Focus(NavigationMethod.Directional),
                    DispatcherPriority.Input);
    }


    // =========================================================
    // TECLADO
    // =========================================================
    //
    // 1-3   : elige la opción directamente.
    // ↑ / ↓ : se mueve entre las opciones.
    // Enter : lo resuelve el propio botón enfocado.
    // =========================================================

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        var opciones =
            new[]
            {
                BotonImprimir,
                BotonWhatsApp,
                BotonSinTicket
            };


        var indiceNumero =
            e.Key switch
            {
                Key.D1 or Key.NumPad1 => 0,
                Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2,
                _ => -1
            };


        if (indiceNumero >= 0)
        {
            e.Handled = true;

            opciones[indiceNumero].RaiseEvent(
                new RoutedEventArgs(Button.ClickEvent));

            return;
        }


        var actual =
            Array.FindIndex(
                opciones,
                b => b.IsFocused);


        var destino =
            e.Key switch
            {
                Key.Down when actual >= 0 && actual < opciones.Length - 1 => actual + 1,
                Key.Up when actual > 0 => actual - 1,
                _ => -1
            };


        if (destino >= 0)
        {
            e.Handled = true;

            opciones[destino].Focus(NavigationMethod.Directional);

            return;
        }


        base.OnKeyDown(e);
    }


    private void Imprimir_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button btn)
            btn.IsEnabled = false;

        OpcionSeleccionada =
            "IMPRIMIR";

        Close(true);
    }


    private void WhatsApp_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpcionSeleccionada =
            "WHATSAPP";

        Close(true);
    }


    private void SinTicket_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpcionSeleccionada =
            "SIN_TICKET";

        Close(true);
    }
}