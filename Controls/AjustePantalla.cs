using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace NovaCoreESDM.Controls;

// Adapta todas las ventanas a pantallas chicas (por ejemplo 1366 x 768 o
// Windows con escala al 125 %). Si una ventana no cabe, se reduce al área
// visible y su contenido se escala de forma proporcional, como un zoom
// automático, para que nada se corte. En pantallas grandes no cambia nada.
// Solo es visual: no toca datos, comandos ni el flujo de las ventanas.
public static class AjustePantalla
{
    // Espacio útil para el que está diseñada la ventana principal.
    private const double AnchoDisenoPrincipal = 1366;
    private const double AltoDisenoPrincipal = 740;

    // Debajo de esto el texto ya sería difícil de leer.
    private const double EscalaMinima = 0.6;

    // Reserva para la barra de título y los bordes del sistema.
    private const double MargenHorizontal = 16;
    private const double MargenVertical = 48;

    private const string NombreEnvoltura = "AjustePantallaEnvoltura";


    public static void Registrar()
    {
        Window.WindowOpenedEvent.AddClassHandler<Window>(
            (ventana, _) => Aplicar(ventana));
    }


    private static void Aplicar(Window ventana)
    {
        if (ventana.Content is not Control contenido
            || contenido.Name == NombreEnvoltura)
        {
            return;
        }

        var pantalla =
            ventana.Screens.ScreenFromWindow(ventana)
            ?? ventana.Screens.Primary;

        // Sin datos de la pantalla no se limita el tamaño, solo el zoom en vivo.
        var area = pantalla?.WorkingArea ?? new PixelRect(0, 0, 100000, 100000);
        var escalaSistema = pantalla?.Scaling ?? 1;

        var anchoDisponible = area.Width / escalaSistema - MargenHorizontal;
        var altoDisponible = area.Height / escalaSistema - MargenVertical;

        var maximizada = ventana.WindowState == WindowState.Maximized;

        var anchoDiseno = maximizada
            ? AnchoDisenoPrincipal
            : !double.IsNaN(ventana.Width) ? ventana.Width : ventana.ClientSize.Width;

        var altoDiseno = maximizada
            ? AltoDisenoPrincipal
            : !double.IsNaN(ventana.Height) ? ventana.Height : ventana.ClientSize.Height;

        if (anchoDiseno <= 0 || altoDiseno <= 0)
        {
            return;
        }

        var dinamica =
            maximizada
            || (ventana.CanResize && ventana.SizeToContent == SizeToContent.Manual);

        var escalaInicial = Limitar(Math.Min(
            anchoDisponible / anchoDiseno,
            altoDisponible / altoDiseno));

        // Una ventana fija que cabe completa se queda exactamente igual.
        if (!dinamica && escalaInicial >= 1)
        {
            return;
        }

        // Los tamaños mínimos nunca deben ser mayores que la pantalla.
        ventana.MinWidth = Math.Min(ventana.MinWidth, Math.Floor(anchoDisponible));
        ventana.MinHeight = Math.Min(ventana.MinHeight, Math.Floor(altoDisponible));

        // Envuelve el contenido para poder escalarlo.
        var envoltura = new LayoutTransformControl
        {
            Name = NombreEnvoltura
        };

        ventana.Content = null;
        envoltura.Child = contenido;
        ventana.Content = envoltura;

        AplicarEscala(envoltura, escalaInicial);

        // Si no cabe, reduce la ventana al área visible y la vuelve a centrar.
        if (!maximizada && escalaInicial < 1)
        {
            var anchoNuevo = Math.Floor(anchoDiseno * escalaInicial);
            var altoNuevo = Math.Floor(altoDiseno * escalaInicial);

            ventana.MinWidth = Math.Min(ventana.MinWidth, anchoNuevo);
            ventana.MinHeight = Math.Min(ventana.MinHeight, altoNuevo);

            ventana.Width = anchoNuevo;

            if (ventana.SizeToContent == SizeToContent.Manual)
            {
                ventana.Height = altoNuevo;
            }

            ventana.Position = new PixelPoint(
                area.X + (int)((area.Width - anchoNuevo * escalaSistema) / 2),
                area.Y + (int)Math.Max(0, (area.Height - (altoNuevo + MargenVertical) * escalaSistema) / 2));
        }

        // Las ventanas que cambian de tamaño ajustan el zoom en vivo.
        if (dinamica)
        {
            ventana.PropertyChanged += (_, e) =>
            {
                if (e.Property != TopLevel.ClientSizeProperty)
                {
                    return;
                }

                var cliente = ventana.ClientSize;

                if (cliente.Width <= 0 || cliente.Height <= 0)
                {
                    return;
                }

                AplicarEscala(envoltura, Limitar(Math.Min(
                    cliente.Width / anchoDiseno,
                    cliente.Height / altoDiseno)));
            };
        }
    }


    private static double Limitar(double escala) =>
        Math.Clamp(escala, EscalaMinima, 1);


    private static void AplicarEscala(LayoutTransformControl envoltura, double escala)
    {
        escala = Math.Round(escala, 3);

        envoltura.LayoutTransform = escala >= 1
            ? null
            : new ScaleTransform(escala, escala);
    }
}
