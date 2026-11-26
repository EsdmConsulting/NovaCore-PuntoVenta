namespace NovaCoreESDM.Services.Api;

/// <summary>
/// Servidor con el que trabaja este ejecutable. Se elige al compilar:
///
///   Pruebas (por defecto):  dotnet publish ...
///   Producción:             dotnet publish ... -p:Ambiente=Produccion
///
/// Cada ambiente guarda sus datos en su propia carpeta (ventas sin conexión
/// por enviar, caja asignada, copias del catálogo y del crédito), para que
/// una venta de pruebas nunca se envíe a producción si en la misma
/// computadora se usaron las dos versiones.
/// </summary>
public static class AmbienteServidor
{
#if PRODUCCION
    public const bool EsProduccion = true;
    public const string Nombre = "Producción";
    public const string Url = "https://plataformaluisfer.com/Plataforma_LuisFer/";
    public const string CarpetaDatos = "NovaCoreESDM-Produccion";
#else
    public const bool EsProduccion = false;
    public const string Nombre = "Pruebas (preproducción)";
    public const string Url = "https://nubeesdm8.ddns.net:4433/Plataforma_LuisFer/";
    public const string CarpetaDatos = "NovaCoreESDM";
#endif

    /// <summary>Se agrega al título de la ventana en la versión de pruebas.</summary>
    public static string SufijoTitulo => EsProduccion ? string.Empty : " · PRUEBAS (preproducción)";
}
