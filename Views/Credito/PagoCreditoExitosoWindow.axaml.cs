using Avalonia.Controls;
using Avalonia.Interactivity;

using NovaCoreESDM.Models.Credito;

namespace NovaCoreESDM.Views.Credito;

public partial class PagoCreditoExitosoWindow : Window
{
    // =========================================================
    // RESPUESTA DEL ABONO
    // =========================================================

    private readonly RegistrarAbonoCreditoResponse
        _resultado;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PagoCreditoExitosoWindow(
        RegistrarAbonoCreditoResponse resultado)
    {
        InitializeComponent();


        _resultado =
            resultado;


        CargarInformacion();
    }


    // =========================================================
    // CARGAR INFORMACIÓN
    // =========================================================

    private void CargarInformacion()
    {
        if (
            _resultado.Data is null
        )
        {
            TextoMontoAplicado.Text =
                "0.00";

            TextoDocumentosAfectados.Text =
                "0";

            TextoEstadoAbono.Text =
                "SIN DATOS";

            ListaDocumentosAplicados.ItemsSource =
                null;

            return;
        }


        // =====================================================
        // MONTO
        // =====================================================

        TextoMontoAplicado.Text =
            _resultado.Data
                .MontoAplicado
                .ToString("N2");


        // =====================================================
        // DOCUMENTOS AFECTADOS
        // =====================================================

        TextoDocumentosAfectados.Text =
            _resultado.Data
                .DocumentosAfectados
                .ToString();


        // =====================================================
        // ESTADO
        // =====================================================

        TextoEstadoAbono.Text =
            string.IsNullOrWhiteSpace(
                _resultado.Data.EstadoAbono
            )
                ? "APLICADO"
                : _resultado.Data.EstadoAbono;


        // =====================================================
        // DETALLE DE DOCUMENTOS
        // =====================================================

        ListaDocumentosAplicados.ItemsSource =
            _resultado.Data
                .DocumentosAplicados;
    }


    // =========================================================
    // CERRAR
    // =========================================================

    private void Cerrar_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }
}