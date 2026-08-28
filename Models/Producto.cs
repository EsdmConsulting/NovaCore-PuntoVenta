namespace NovaCoreESDM.Models;

public class Producto
{
    public int Id { get; set; }

    public int IdPresentacion { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string CodigoBarras { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string NombrePresentacion { get; set; } =
        string.Empty;

    public string Categoria { get; set; } =
        string.Empty;

    public decimal Precio { get; set; }

    public decimal FactorConversion { get; set; } = 1;

    public string UnidadMedida { get; set; } =
        string.Empty;

    public string Inicial { get; set; } =
        string.Empty;

    public string FondoHex { get; set; } =
        "#EFF6FF";

    public string ColorHex { get; set; } =
        "#2563EB";
}