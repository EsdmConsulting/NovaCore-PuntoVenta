using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NovaCoreESDM.Desktop.Models.MercadoPago;
using NovaCoreESDM.Desktop.ViewModels.MercadoPago;

namespace NovaCoreESDM.Desktop.Views.MercadoPago
{
    public partial class MercadoPagoPagoWindow : Window
    {
        private MercadoPagoPagoViewModel? _viewModel;

        private bool _permitirCierre;


        // ============================================================
        // CONSTRUCTOR VACÍO
        // ============================================================
        //
        // Avalonia necesita este constructor.
        //
        // ============================================================

        public MercadoPagoPagoWindow()
        {
            InitializeComponent();
        }


        // ============================================================
        // CONSTRUCTOR DEL COBRO
        // ============================================================

        public MercadoPagoPagoWindow(
            long idVenta,
            int idTpvBancaria,
            decimal importe,
            string? nombreTerminal = null)
            : this()
        {
            _viewModel =
                new MercadoPagoPagoViewModel(
                    idVenta,
                    idTpvBancaria,
                    importe,
                    nombreTerminal);


            DataContext =
                _viewModel;


            _viewModel.SolicitarCerrar +=
                ViewModel_SolicitarCerrar;


            Opened +=
                Window_Opened;


            Closing +=
                Window_Closing;


            Closed +=
                Window_Closed;
        }


        // ============================================================
        // RESULTADO
        // ============================================================

        public MercadoPagoResultadoCobro? Resultado =>
            _viewModel?.Resultado;


        // ============================================================
        // OPENED
        // ============================================================

        private async void Window_Opened(
            object? sender,
            EventArgs e)
        {
            if (_viewModel == null)
                return;


            try
            {
                await _viewModel.IniciarAsync();
            }
            catch
            {
                /*
                 * El ViewModel ya controla y muestra sus errores.
                 *
                 * Evitamos cerrar la ventana automáticamente ante
                 * una excepción porque podría existir una operación
                 * Mercado Pago que todavía necesite verificarse.
                 */
            }
        }


        // ============================================================
        // VIEWMODEL SOLICITA CERRAR
        // ============================================================

        private void ViewModel_SolicitarCerrar()
        {
            _permitirCierre =
                true;


            Close();
        }


        // ============================================================
        // BLOQUEAR CIERRE PELIGROSO
        // ============================================================
        //
        // Mientras exista una operación en proceso NO permitimos:
        //
        //      X de la ventana
        //      Alt + F4
        //      cierre accidental
        //
        //
        // El cajero debe utilizar "Cancelar".
        //
        // ¿Por qué?
        //
        // Porque cerrar solamente la ventana NO cancela la Order
        // que está viviendo en Mercado Pago.
        //
        // ============================================================

        private void Window_Closing(
            object? sender,
            WindowClosingEventArgs e)
        {
            // ========================================================
            // CIERRE AUTORIZADO POR EL PROPIO FLUJO
            // ========================================================
            //
            // Ejemplos:
            // - Pago aprobado
            // - Cancelación confirmada
            // - CerrarCommand
            //
            // ========================================================

            if (_permitirCierre)
                return;


            if (_viewModel == null)
                return;


            // ========================================================
            // EL VIEWMODEL DETERMINA SI ES SEGURO SALIR
            // ========================================================

            if (_viewModel.PuedeCerrar)
                return;


            // ========================================================
            // CUALQUIER OTRO ESTADO:
            // NO PERMITIR CERRAR
            // ========================================================
            //
            // Esto protege incluso estados intermedios donde todavía
            // no tenemos IdOperacion pero estamos creando la Order.
            //
            // ========================================================

            e.Cancel =
                true;
        }


        // ============================================================
        // CLOSED
        // ============================================================

        private void Window_Closed(
            object? sender,
            EventArgs e)
        {
            if (_viewModel == null)
                return;


            _viewModel.SolicitarCerrar -=
                ViewModel_SolicitarCerrar;


            _viewModel.Dispose();


            _viewModel =
                null;
        }


        // ============================================================
        // CIERRE MANUAL SEGURO
        // ============================================================
        //
        // Nos servirá después si queremos agregar un botón "Cerrar"
        // para estados como:
        //
        //      RECHAZADO
        //      EXPIRADO
        //      ERROR ANTES DE CREAR ORDER
        //
        // ============================================================

        public void CerrarSeguro()
        {
            _permitirCierre =
                true;


            Close();
        }
    }
}