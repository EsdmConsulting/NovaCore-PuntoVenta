using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using Avalonia.Interactivity;

using NovaCoreESDM.Desktop.Models.MercadoPago;

namespace NovaCoreESDM.Desktop.Views.MercadoPago
{
    public partial class MercadoPagoTerminalWindow :
        Window,
        INotifyPropertyChanged
    {
        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public new event PropertyChangedEventHandler?
            PropertyChanged;


        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName
                )
            );
        }


        // ============================================================
        // TERMINALES
        // ============================================================

        public ObservableCollection<MercadoPagoTerminal>
            Terminales { get; } =
                new();


        // ============================================================
        // TERMINAL SELECCIONADA
        // ============================================================

        private MercadoPagoTerminal?
            _terminalSeleccionada;


        public MercadoPagoTerminal?
            TerminalSeleccionada
        {
            get =>
                _terminalSeleccionada;

            set
            {
                if (_terminalSeleccionada == value)
                    return;


                _terminalSeleccionada =
                    value;


                OnPropertyChanged();
            }
        }


        // ============================================================
        // IMPORTE
        // ============================================================

        public decimal Importe { get; }


        // ============================================================
        // CONSTRUCTOR VACÍO
        // ============================================================
        //
        // Lo dejamos para Avalonia / diseñador.
        //
        // ============================================================

        public MercadoPagoTerminalWindow()
        {
            InitializeComponent();

            DataContext =
                this;
        }


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public MercadoPagoTerminalWindow(
            decimal importe,
            MercadoPagoTerminalesResponse respuesta)
            : this()
        {
            Importe =
                importe;


            // ========================================================
            // CARGAR TERMINALES
            // ========================================================

            if (respuesta.Data?.Terminales is not null)
            {
                foreach (
                    var terminal
                    in respuesta.Data.Terminales
                )
                {
                    Terminales.Add(
                        terminal
                    );
                }
            }


            // ========================================================
            // SELECCIONAR PREDETERMINADA
            // ========================================================

            foreach (
                var terminal
                in Terminales
            )
            {
                if (!terminal.EsPredeterminada)
                    continue;


                TerminalSeleccionada =
                    terminal;


                break;
            }


            // ========================================================
            // SI NO HAY PREDETERMINADA Y SOLO HAY UNA
            // ========================================================

            if (
                TerminalSeleccionada is null
                &&
                Terminales.Count == 1
            )
            {
                TerminalSeleccionada =
                    Terminales[0];
            }
        }


        // ============================================================
        // CONTINUAR
        // ============================================================

        private void Continuar_Click(
            object? sender,
            RoutedEventArgs e)
        {
            if (TerminalSeleccionada is null)
                return;


            Close(true);
        }


        // ============================================================
        // CANCELAR
        // ============================================================

        private void Cancelar_Click(
            object? sender,
            RoutedEventArgs e)
        {
            Close(false);
        }
    }
}