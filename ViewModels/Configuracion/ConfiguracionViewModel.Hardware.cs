using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NovaCoreESDM.ViewModels.Configuracion;

/// <summary>
/// Ancho del papel de la impresora (58 / 80 mm) y prueba del escáner.
/// </summary>
public partial class ConfiguracionViewModel
{
    // Letras por renglón: 80 mm (Fuente A) ≈ 42–48; 58 mm ≈ 32.
    public const int AnchoPapel80 = 42;
    public const int AnchoPapel58 = 32;

    private bool _cargandoPapel;

    [ObservableProperty]
    private bool papel80mm = true;

    [ObservableProperty]
    private bool papel58mm;

    [ObservableProperty]
    private string mensajePapel = string.Empty;


    /// <summary>Lee el ancho guardado (lo llama la vista al abrirse).</summary>
    public void CargarAnchoPapel()
    {
        _cargandoPapel = true;

        try
        {
            var es58 = _formatoConfiguracion.Obtener().AnchoCaracteres <= 36;
            Papel58mm = es58;
            Papel80mm = !es58;
        }
        finally
        {
            _cargandoPapel = false;
        }
    }

    partial void OnPapel80mmChanged(bool value)
    {
        if (value)
            GuardarAnchoPapel(AnchoPapel80, "80 mm");
    }

    partial void OnPapel58mmChanged(bool value)
    {
        if (value)
            GuardarAnchoPapel(AnchoPapel58, "58 mm");
    }

    private void GuardarAnchoPapel(int ancho, string texto)
    {
        if (_cargandoPapel)
            return;

        try
        {
            var formato = _formatoConfiguracion.Obtener();
            formato.AnchoCaracteres = ancho;
            _formatoConfiguracion.Guardar(formato);
            MensajePapel = $"Papel de {texto} guardado. Imprime una prueba para revisarlo.";
        }
        catch (Exception ex)
        {
            MensajePapel = $"No se pudo guardar el ancho del papel: {ex.Message}";
        }
    }


    // =========================================================
    // PRUEBA DEL ESCÁNER
    // =========================================================

    [ObservableProperty]
    private string resultadoEscaner = "Pon el cursor en el recuadro y escanea cualquier código.";

    /// <summary>null = sin prueba; true = lo reconoció; false = parece tecleado.</summary>
    [ObservableProperty]
    private bool? escanerReconocido;

    public void RegistrarPruebaEscaner(string codigo, double mayorEsperaMs, bool terminoConEnter, int limiteMs)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        if (!terminoConEnter)
        {
            EscanerReconocido = false;
            ResultadoEscaner =
                $"Llegó «{codigo}» pero sin Enter al final. Configura el escáner para que mande Enter (sufijo CR) con el código del manual.";
            return;
        }

        if (codigo.Length >= 3 && mayorEsperaMs <= limiteMs)
        {
            EscanerReconocido = true;
            ResultadoEscaner =
                $"Escáner reconocido. Código «{codigo}» ({codigo.Length} caracteres, máx. {mayorEsperaMs:0} ms entre letras).";
            return;
        }

        EscanerReconocido = false;
        ResultadoEscaner = codigo.Length < 3
            ? $"Llegó «{codigo}»: es muy corto para tomarlo como código de barras."
            : $"Llegó «{codigo}», pero con {mayorEsperaMs:0} ms entre letras (el límite es {limiteMs} ms): en la venta se tomaría como si alguien lo tecleara. Si fue el escáner, es lento: revisa su configuración o usa uno con cable.";
    }
}
