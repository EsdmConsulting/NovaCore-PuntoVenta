using System;
using System.Globalization;

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NovaCoreESDM.Views.Credito;

public partial class PagoCreditoEfectivoWindow : Window
{
    // =========================================================
    // TOTAL
    // =========================================================

    private readonly decimal
        _total;


    // =========================================================
    // DATOS DEL PAGO
    // =========================================================

    public decimal CantidadRecibida
    {
        get;
        private set;
    }


    public decimal Cambio
    {
        get;
        private set;
    }


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PagoCreditoEfectivoWindow(
        decimal total)
    {
        InitializeComponent();


        _total =
            total;


        TextoTotal.Text =
            _total.ToString(
                "C2",
                CultureInfo.CurrentCulture
            );


        TextoCambio.Text =
            0m.ToString(
                "C2",
                CultureInfo.CurrentCulture
            );


        Opened +=
            (_, _) =>
            {
                CantidadRecibidaTextBox
                    .Focus();
            };
    }


    // =========================================================
    // CAMBIO EN CANTIDAD RECIBIDA
    // =========================================================

    private void CantidadRecibida_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        TextoMensaje.Text =
            string.Empty;


        var texto =
            CantidadRecibidaTextBox
                .Text?
                .Trim()
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                texto
            )
        )
        {
            CantidadRecibida =
                0m;

            Cambio =
                0m;


            TextoCambio.Text =
                0m.ToString(
                    "C2",
                    CultureInfo.CurrentCulture
                );


            ConfirmarButton.IsEnabled =
                false;


            return;
        }


        // =====================================================
        // LIMPIAR FORMATO
        // =====================================================

        texto =
            texto
                .Replace(
                    "$",
                    string.Empty
                )
                .Replace(
                    ",",
                    string.Empty
                )
                .Trim();


        // =====================================================
        // VALIDAR DECIMAL
        // =====================================================

        if (
            !decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var recibido
            )
        )
        {
            CantidadRecibida =
                0m;

            Cambio =
                0m;


            TextoCambio.Text =
                0m.ToString(
                    "C2",
                    CultureInfo.CurrentCulture
                );


            ConfirmarButton.IsEnabled =
                false;


            TextoMensaje.Text =
                "Captura un importe válido.";


            return;
        }


        // =====================================================
        // GUARDAR CANTIDAD
        // =====================================================

        CantidadRecibida =
            recibido;


        // =====================================================
        // VALIDAR SI ALCANZA
        // =====================================================

        if (
            recibido <
            _total
        )
        {
            Cambio =
                0m;


            TextoCambio.Text =
                0m.ToString(
                    "C2",
                    CultureInfo.CurrentCulture
                );


            ConfirmarButton.IsEnabled =
                false;


            TextoMensaje.Text =
                "El efectivo recibido es menor al total a pagar.";


            return;
        }


        // =====================================================
        // CALCULAR CAMBIO
        // =====================================================

        Cambio =
            recibido -
            _total;


        TextoCambio.Text =
            Cambio.ToString(
                "C2",
                CultureInfo.CurrentCulture
            );


        ConfirmarButton.IsEnabled =
            true;
    }


    // =========================================================
    // CANCELAR
    // =========================================================

    private void Cancelar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(
            false
        );
    }


    // =========================================================
    // CONFIRMAR
    // =========================================================

    private void Confirmar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (
            CantidadRecibida <
            _total
        )
        {
            TextoMensaje.Text =
                "El efectivo recibido es menor al total a pagar.";

            return;
        }


        Close(
            true
        );
    }
}