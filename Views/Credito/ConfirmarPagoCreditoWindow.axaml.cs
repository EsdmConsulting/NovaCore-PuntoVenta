using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;

using NovaCoreESDM.ViewModels.Credito;

namespace NovaCoreESDM.Views.Credito;

public partial class ConfirmarPagoCreditoWindow : Window
{
    // =========================================================
    // RESULTADO
    // =========================================================

    public bool Confirmado { get; private set; }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ConfirmarPagoCreditoWindow(
        string cliente,
        decimal total,
        string formaPago,
        IEnumerable<DocumentoPagoCreditoItem> documentos)
    {
        InitializeComponent();


        TextoCliente.Text =
            cliente;


        TextoTotal.Text =
            $"${total:N2}";


        TextoFormaPago.Text =
            formaPago;


        ListaDocumentos.ItemsSource =
            documentos;
    }


    // =========================================================
    // CANCELAR
    // =========================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Confirmado =
            false;

        Close(false);
    }


    // =========================================================
    // CONTINUAR
    // =========================================================

    private void Continuar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Confirmado =
            true;

        Close(true);
    }
}