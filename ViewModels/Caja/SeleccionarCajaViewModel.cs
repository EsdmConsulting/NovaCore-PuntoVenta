using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaCoreESDM.Services.Cajas;

using CajaModel = NovaCoreESDM.Models.Caja.Caja;

namespace NovaCoreESDM.ViewModels.Caja;

public partial class SeleccionarCajaViewModel : ViewModelBase
{
    private readonly CajasService _cajasService;

    public ObservableCollection<CajaModel> Cajas { get; } = new();


    // =====================================================
    // CAJA SELECCIONADA
    // =====================================================

    [ObservableProperty]
    private CajaModel? _cajaSeleccionada;


    // =====================================================
    // MENSAJES
    // =====================================================

    [ObservableProperty]
    private string _mensajeError = string.Empty;


    // =====================================================
    // ESTADO DE CARGA
    // =====================================================

    [ObservableProperty]
    private bool _estaCargando;


    // =====================================================
    // PROPIEDADES CALCULADAS
    // =====================================================

    public bool PuedeContinuar =>
        CajaSeleccionada is not null &&
        !CajaSeleccionada.EnUso &&
        !EstaCargando;


    public bool TieneCajas =>
        Cajas.Count > 0;


    public bool NoTieneCajas =>
        !EstaCargando &&
        Cajas.Count == 0;


    // =====================================================
    // EVENTO
    // =====================================================
    //
    // IMPORTANTE:
    //
    // Este evento únicamente se dispara DESPUÉS de que
    // el backend haya confirmado que la caja pudo ser
    // asignada correctamente.
    //
    // =====================================================

    public event Action<CajaModel>? CajaConfirmada;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public SeleccionarCajaViewModel()
    {
        _cajasService =
            new CajasService();
    }


    // =====================================================
    // CAMBIO DE CAJA SELECCIONADA
    // =====================================================

    partial void OnCajaSeleccionadaChanged(CajaModel? value)
    {
        MensajeError = string.Empty;

        if (value is not null &&
            value.EnUso)
        {
            MensajeError =
                "Esta caja ya está siendo utilizada por otra terminal.";
        }

        OnPropertyChanged(nameof(PuedeContinuar));
    }


    // =====================================================
    // CAMBIO DE ESTADO DE CARGA
    // =====================================================

    partial void OnEstaCargandoChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(PuedeContinuar));

        OnPropertyChanged(
            nameof(NoTieneCajas));
    }


    // =====================================================
    // CARGAR CAJAS
    // =====================================================

    [RelayCommand]
    public async Task CargarCajasAsync()
    {
        EstaCargando = true;

        MensajeError =
            string.Empty;


        try
        {
            var resultado =
                await _cajasService
                    .ObtenerCajasAsync();


            Cajas.Clear();


            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)

                        ? "No fue posible consultar las cajas."

                        : resultado.Msg;

                return;
            }


            // =================================================
            // AGREGAMOS TODAS LAS CAJAS
            // =================================================
            //
            // IMPORTANTE:
            //
            // NO filtramos las cajas con EnUso = true.
            //
            // Queremos que aparezcan en pantalla para que el
            // usuario pueda ver que existen, pero se mostrarán
            // en rojo y no podrán utilizarse.
            //
            // =================================================

            foreach (var caja in resultado.Data)
            {
                Cajas.Add(caja);
            }


            if (Cajas.Count == 0)
            {
                MensajeError =
                    "No existen cajas configuradas.";
            }
        }
        finally
        {
            EstaCargando = false;


            OnPropertyChanged(
                nameof(TieneCajas));

            OnPropertyChanged(
                nameof(NoTieneCajas));
        }
    }


    // =====================================================
    // CONFIRMAR CAJA
    // =====================================================
    //
    // Flujo:
    //
    // 1. Validar selección.
    // 2. Validar que no esté ocupada.
    // 3. Solicitar asignación al backend.
    // 4. Backend cambia en_uso = TRUE.
    // 5. Solamente si responde correctamente:
    //      -> disparar CajaConfirmada.
    //
    // =====================================================

    [RelayCommand]
    private async Task ConfirmarCajaAsync()
    {
        MensajeError =
            string.Empty;


        // =================================================
        // VALIDAR SELECCIÓN
        // =================================================

        if (CajaSeleccionada is null)
        {
            MensajeError =
                "Selecciona una caja para continuar.";

            return;
        }


        // =================================================
        // VALIDAR ESTADO LOCAL
        // =================================================
        //
        // Esto evita intentar utilizar una caja que ya
        // sabemos que está ocupada.
        //
        // La validación definitiva sigue estando en PHP.
        //
        // =================================================

        if (CajaSeleccionada.EnUso)
        {
            MensajeError =
                "Esta caja ya está siendo utilizada por otra terminal.";

            return;
        }


        EstaCargando = true;


        try
        {
            // =================================================
            // INTENTAR ASIGNAR LA CAJA EN EL SERVIDOR
            // =================================================

            var resultado =
                await _cajasService
                    .AsignarCajaAsync(
                        CajaSeleccionada.Id);


            // =================================================
            // ASIGNACIÓN RECHAZADA
            // =================================================

            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(
                        resultado.Msg)

                        ? "No fue posible asignar la caja."

                        : resultado.Msg;


                // ---------------------------------------------
                // Volvemos a consultar las cajas.
                //
                // Esto es especialmente importante si otra
                // computadora tomó la caja unos segundos
                // antes que nosotros.
                // ---------------------------------------------

                await RecargarCajasAsync();

                return;
            }


            // =================================================
            // ASIGNACIÓN CORRECTA
            // =================================================
            //
            // Preferimos utilizar la caja devuelta por PHP
            // porque ya contiene el estado definitivo:
            //
            //     en_uso = true
            //
            // Si por alguna razón el backend no regresara
            // data, utilizamos la caja seleccionada.
            //
            // =================================================

            var cajaConfirmada =
                resultado.Data ??
                CajaSeleccionada;


            cajaConfirmada.EnUso =
                true;


            // =================================================
            // NOTIFICAR A LA VENTANA
            // =================================================
            //
            // Aquí continúa el flujo que ya tenías:
            //
            //     CajaConfirmada
            //          ↓
            //     guardar terminal-config.json
            //          ↓
            //     cerrar ventana
            //          ↓
            //     continuar al POS
            //
            // =================================================

            CajaConfirmada?.Invoke(
                cajaConfirmada);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERROR CONFIRMANDO CAJA:");

            Console.WriteLine(ex);


            MensajeError =
                "Ocurrió un error al asignar la caja.";
        }
        finally
        {
            EstaCargando = false;
        }
    }


    // =====================================================
    // RECARGAR CAJAS
    // =====================================================
    //
    // Se utiliza cuando el servidor rechaza una asignación.
    //
    // A diferencia de CargarCajasAsync(), este método no
    // modifica EstaCargando porque se ejecuta dentro del
    // proceso de confirmación.
    //
    // =====================================================

    private async Task RecargarCajasAsync()
    {
        try
        {
            var resultado =
                await _cajasService
                    .ObtenerCajasAsync();


            if (resultado.Res != 1)
            {
                return;
            }


            Cajas.Clear();


            foreach (var caja in resultado.Data)
            {
                Cajas.Add(caja);
            }


            CajaSeleccionada =
                null;


            OnPropertyChanged(
                nameof(TieneCajas));

            OnPropertyChanged(
                nameof(NoTieneCajas));
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERROR RECARGANDO CAJAS:");

            Console.WriteLine(ex);
        }
    }
}