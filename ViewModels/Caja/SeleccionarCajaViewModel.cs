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

    [ObservableProperty]
    private CajaModel? _cajaSeleccionada;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    public bool PuedeContinuar =>
        CajaSeleccionada is not null && !EstaCargando;

    public bool TieneCajas =>
        Cajas.Count > 0;

    public bool NoTieneCajas =>
        !EstaCargando && Cajas.Count == 0;

    public event Action<CajaModel>? CajaConfirmada;

    public SeleccionarCajaViewModel()
    {
        _cajasService = new CajasService();
    }

    partial void OnCajaSeleccionadaChanged(CajaModel? value)
    {
        MensajeError = string.Empty;

        OnPropertyChanged(nameof(PuedeContinuar));
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        OnPropertyChanged(nameof(PuedeContinuar));
        OnPropertyChanged(nameof(NoTieneCajas));
    }

    [RelayCommand]
    public async Task CargarCajasAsync()
    {
        EstaCargando = true;
        MensajeError = string.Empty;

        try
        {
            var resultado =
                await _cajasService.ObtenerCajasAsync();

            Cajas.Clear();

            if (resultado.Res != 1)
            {
                MensajeError =
                    string.IsNullOrWhiteSpace(resultado.Msg)
                        ? "No fue posible consultar las cajas."
                        : resultado.Msg;

                return;
            }

            foreach (var caja in resultado.Data)
            {
                Cajas.Add(caja);
            }

            if (Cajas.Count == 0)
            {
                MensajeError =
                    "No existen cajas disponibles para seleccionar.";
            }
        }
        finally
        {
            EstaCargando = false;

            OnPropertyChanged(nameof(TieneCajas));
            OnPropertyChanged(nameof(NoTieneCajas));
        }
    }

    [RelayCommand]
    private void ConfirmarCaja()
    {
        MensajeError = string.Empty;

        if (CajaSeleccionada is null)
        {
            MensajeError =
                "Selecciona una caja para continuar.";

            return;
        }

        CajaConfirmada?.Invoke(CajaSeleccionada);
    }
}