using Avalonia;
using System;
using System.Threading.Tasks;

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Interactivity;

using NovaCoreESDM.Models;
using NovaCoreESDM.Models.Productos;
using NovaCoreESDM.Models.Tickets;

using NovaCoreESDM.Services.Configuration;

using NovaCoreESDM.ViewModels.Ventas;
using Avalonia.VisualTree;

using NovaCoreESDM.Desktop.Services.MercadoPago;
using NovaCoreESDM.Desktop.Views.MercadoPago;


namespace NovaCoreESDM.Views.Ventas;


public partial class VentasView : UserControl
{
    
    public ICommand EnfocarControlCommand { get; }
    
    private readonly TerminalConfigurationService
        _terminalConfigurationService;

    private readonly MercadoPagoService
        _mercadoPagoService;

    private VentasViewModel?
        _viewModel;


    private bool
        _productosCargados;
    
    private Window? _parentWindow;

    // =========================================================
    // CHECADORA / ESCÁNER DE CÓDIGO DE BARRAS (TECLADO HID)
    // =========================================================

    private readonly System.Text.StringBuilder _scannerBuffer = new();
    private DateTime _ultimoTiempoTeclaScanner = DateTime.MinValue;
    private const int ScannerMaxDelayMs = 70;
    private readonly System.Threading.SemaphoreSlim _semaforoEscaneo = new(1, 1);


    public VentasView()
    {
        AvaloniaXamlLoader.Load(this);
        

        _terminalConfigurationService =
            new TerminalConfigurationService();
        _mercadoPagoService =
            new MercadoPagoService();

        AttachedToVisualTree +=
            OnAttachedToVisualTree;
        DetachedFromVisualTree +=
            OnDetachedFromVisualTree;
    }

    

    // =========================================================
    // ADJUNTAR VISTA
    // =========================================================

    private async void OnAttachedToVisualTree(
        object? sender,
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        
        _parentWindow = TopLevel.GetTopLevel(this) as Window;
                if (_parentWindow != null)
                {
                    //_parentWindow.KeyDown += ParentWindow_KeyDown;
                    _parentWindow.AddHandler(InputElement.KeyDownEvent, ParentWindow_KeyDown, RoutingStrategies.Tunnel);
                }
        
        if (DataContext is not VentasViewModel viewModel)
            return;
        
        
        // =========================================================
        // PREPARAR PANTALLA PARA NUEVA OPERACIÓN
        // =========================================================

        viewModel.TextoBusqueda =
            string.Empty;

        viewModel.TextoBusquedaCliente =
            string.Empty;

        viewModel.MensajeError =
            string.Empty;


        if (_viewModel != viewModel)
        {
            // =================================================
            // DESVINCULAR VIEWMODEL ANTERIOR
            // =================================================

            if (_viewModel is not null)
            {
                _viewModel.SolicitarSeleccionPresentacion -=
                    OnSolicitarSeleccionPresentacion;

                _viewModel.SolicitarEditarCantidad -=
                    OnSolicitarEditarCantidad;

                _viewModel.SolicitarCobro -=
                    OnSolicitarCobro;

                _viewModel.SolicitarEntregaTicket -=
                    OnSolicitarEntregaTicket;

                _viewModel.SolicitarTelefonoWhatsApp -=
                    OnSolicitarTelefonoWhatsApp;

                _viewModel.SolicitarConfirmacionVentaFinalizada -=
                    OnSolicitarConfirmacionVentaFinalizada;

                _viewModel.SolicitarCambioPrecio -=
                    OnSolicitarCambioPrecio;
            }


            // =================================================
            // GUARDAR VIEWMODEL
            // =================================================

            _viewModel =
                viewModel;


// =================================================
// VINCULAR EVENTOS
// =================================================

            _viewModel.SolicitarSeleccionPresentacion +=
                OnSolicitarSeleccionPresentacion;

            _viewModel.SolicitarEditarCantidad +=
                OnSolicitarEditarCantidad;

            _viewModel.SolicitarCobro +=
                OnSolicitarCobro;

            _viewModel.SolicitarEntregaTicket +=
                OnSolicitarEntregaTicket;

            _viewModel.SolicitarTelefonoWhatsApp +=
                OnSolicitarTelefonoWhatsApp;

            _viewModel.SolicitarConfirmacionVentaFinalizada +=
                OnSolicitarConfirmacionVentaFinalizada;

            _viewModel.SolicitarCambioPrecio +=
                OnSolicitarCambioPrecio;
        }


        if (_productosCargados)
        {
            EnfocarBuscadorProductos();

            return;
        }


        _productosCargados =
            true;


        await viewModel
            .CargarProductosAsync();


        await viewModel
            .CargarVentaActualAsync();
        // =====================================================
        // DEJAR ESCÁNER LISTO
        // =====================================================

        EnfocarBuscadorProductos();
    }


// =========================================================
// PROCESAMIENTO CENTRALIZADO DE CÓDIGO DE BARRAS / CHECADORA
// =========================================================

    private async Task ProcesarCodigoBarrasEscaneadoAsync(string codigo)
    {
        if (_viewModel is null || string.IsNullOrWhiteSpace(codigo))
            return;

        await _semaforoEscaneo.WaitAsync();
        try
        {
            // Limpiamos cualquier texto que haya podido entrar al buscador
            _viewModel.TextoBusqueda = string.Empty;
            if (BuscadorProductosTextBox != null)
            {
                BuscadorProductosTextBox.Text = string.Empty;
            }

            // Si el buscador de clientes tenía foco y le cayó el carácter inicial, lo restauramos
            if (BuscadorClienteTextBox != null && BuscadorClienteTextBox.IsFocused && !string.IsNullOrEmpty(BuscadorClienteTextBox.Text))
            {
                if (BuscadorClienteTextBox.Text.Length > 0 && BuscadorClienteTextBox.Text[^1] == codigo[0])
                {
                    BuscadorClienteTextBox.Text = BuscadorClienteTextBox.Text[..^1];
                }
            }

            // Consultar producto por código de barras y agregar automáticamente al carrito
            var encontrado = await _viewModel.ProcesarCodigoEscaneadoAsync(codigo);

            if (!encontrado)
            {
                // En caso de no ser encontrado: queda listo en el buscador para revisión o búsqueda manual
                _viewModel.TextoBusqueda = codigo;
                if (BuscadorProductosTextBox != null)
                {
                    BuscadorProductosTextBox.Text = codigo;
                    EnfocarBuscadorProductos();
                }
            }
            else
            {
                // Producto agregado con éxito al carrito
                EnfocarBuscadorProductos();
            }
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible procesar el código: {ex.Message}";
        }
        finally
        {
            _semaforoEscaneo.Release();
        }
    }

    private static char? ObtenerCaracterDeTecla(KeyEventArgs e)
    {
        // 1. Si Avalonia proporciona KeySymbol imprimible
        if (!string.IsNullOrEmpty(e.KeySymbol) && e.KeySymbol.Length == 1)
        {
            char c = e.KeySymbol[0];
            if (!char.IsControl(c) && c >= 32 && c <= 126)
                return c;
        }

        // 2. Mapeo por Key enum para compatibilidad universal con lectores USB HID
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
            return (char)('0' + (e.Key - Key.D0));

        if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            return (char)('0' + (e.Key - Key.NumPad0));

        if (e.Key >= Key.A && e.Key <= Key.Z)
        {
            char c = (char)('A' + (e.Key - Key.A));
            return e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? c : char.ToLower(c);
        }

        if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            return '-';

        return null;
    }

// =========================================================
// ESCÁNER / ENTER DESDE EL INPUT MANUAL
// =========================================================

    private async void BuscadorProductosTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down || e.Key == Key.Right)
        {
            var listBox = this.FindControl<ListBox>("CatalogoProductosListBox");
            if (listBox != null && listBox.IsVisible && listBox.ItemCount > 0)
            {
                e.Handled = true;
                listBox.Focus();
                if (listBox.SelectedIndex < 0) listBox.SelectedIndex = 0;
            }
            return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        var codigo = BuscadorProductosTextBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        await ProcesarCodigoBarrasEscaneadoAsync(codigo);
    }
    
    
    // =========================================================
    // SELECCIONAR PRESENTACIÓN
    // =========================================================

    private async void OnSolicitarSeleccionPresentacion(
        ProductoCatalogo producto)
    {
        if (_viewModel is null)
            return;

        try
        {
            var parentWindow =
                TopLevel.GetTopLevel(this)
                    as Window;

            if (parentWindow is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir el selector de presentación.";

                return;
            }


            var configuracion =
                await _terminalConfigurationService
                    .ObtenerConfiguracionAsync();

            if (configuracion is null)
            {
                _viewModel.MensajeError =
                    "No existe una caja configurada para este equipo.";

                return;
            }


            var selector =
                new SeleccionarPresentacionWindow(
                    producto,
                    configuracion.UnidadCodigo
                );


            var resultado =
                await selector
                    .ShowDialog<bool>(
                        parentWindow
                    );


            if (!resultado)
                return;


            if (selector.PresentacionSeleccionada is null)
                return;


            // ================================================
            // VENTA POR PESO (KG / GRA)
            // ================================================
            var presentacionSel = selector.PresentacionSeleccionada;
            var esKg = presentacionSel.UnidadMedida.Equals("KG", StringComparison.OrdinalIgnoreCase)
                    || presentacionSel.UnidadMedida.Equals("GRA", StringComparison.OrdinalIgnoreCase)
                    || presentacionSel.UnidadMedida.Equals("GR", StringComparison.OrdinalIgnoreCase);

            if (esKg)
            {
                var pesoWindow = new CapturarPesoWindow(
                    producto.NombreComercial,
                    presentacionSel.NombrePresentacion,
                    presentacionSel.UnidadMedida,
                    presentacionSel.Precio);

                var confirmado = await pesoWindow.ShowDialog<bool>(parentWindow);

                if (!confirmado || pesoWindow.PesoIngresado <= 0m)
                    return;

                // Ajustar la cantidad comercial al peso real
                presentacionSel = new PresentacionVentaItem
                {
                    IdPresentacion    = selector.PresentacionSeleccionada.IdPresentacion,
                    NombrePresentacion= selector.PresentacionSeleccionada.NombrePresentacion,
                    UnidadMedida      = selector.PresentacionSeleccionada.UnidadMedida,
                    FactorConversion  = selector.PresentacionSeleccionada.FactorConversion,
                    CodigoBarras      = selector.PresentacionSeleccionada.CodigoBarras,
                    EsBase            = selector.PresentacionSeleccionada.EsBase,
                    Precio            = selector.PresentacionSeleccionada.Precio,
                    TienePrecio       = selector.PresentacionSeleccionada.TienePrecio,
                    CantidadPeso      = pesoWindow.PesoIngresado
                };
            }

            await _viewModel
                .AgregarPresentacionAlCarritoAsync(
                    producto,
                    presentacionSel
                );
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible seleccionar la presentación: {ex.Message}";
        }
    }


    // =========================================================
    // SELECCIONAR PRESENTACIÓN
    // =========================================================

    private async void OnSolicitarCambioPrecio(
        DetalleVenta detalle)
    {
        if (_viewModel is null)
            return;

        try
        {
            var parentWindow =
                TopLevel.GetTopLevel(this)
                    as Window;

            if (parentWindow is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir el selector de precio.";

                return;
            }


            var window =
                new SeleccionarPrecioWindow(
                    detalle
                );


            var resultado =
                await window
                    .ShowDialog<SeleccionPrecioResultado?>(
                        parentWindow
                    );


            if (resultado is null)
                return;


            if (
                string.IsNullOrWhiteSpace(
                    resultado.TipoPrecio
                )
            )
            {
                _viewModel.MensajeError =
                    "No se seleccionó un tipo de precio válido.";

                return;
            }


           await _viewModel
                .EstablecerPrecioManualAsync(
                    detalle,
                    resultado.TipoPrecio,
                    resultado.PrecioPersonalizado 
                );
        }
        catch (Exception ex)
        {
            _viewModel.MensajeError =
                $"No fue posible cambiar el precio: {ex.Message}";
        }
    }


    // =========================================================
    // EDITAR CANTIDAD
    // =========================================================

    private async void OnSolicitarEditarCantidad(
        DetalleVenta detalle)
    {
        if (detalle is null)
            return;

        var owner =
            TopLevel.GetTopLevel(this)
                as Window;

        if (owner is null)
            return;


        // =========================================================
        // VENTA POR PESO
        // KG / GRA
        // =========================================================

        if (detalle.EsVentaPorPeso)
        {
            var ventanaPeso =
                new EditarPesoWindow(
                    detalle.Cantidad
                );

            var resultadoPeso =
                await ventanaPeso.ShowDialog<bool>(
                    owner
                );

            if (!resultadoPeso)
                return;

            await _viewModel
                .ActualizarCantidadManualAsync(
                    detalle,
                    ventanaPeso.PesoSeleccionado
                );

            return;
        }


        // =========================================================
        // VENTA POR UNIDADES
        // PZA / CAJ / PQ / BT / ETC.
        // =========================================================

        /*
         * EditarCantidadWindow sigue trabajando con int
         * porque esta ventana representa unidades completas.
         *
         * Llegados a este punto sabemos que NO es KG ni GRA.
         */
        var cantidadActual =
            decimal.ToInt32(
                detalle.Cantidad
            );

        var ventanaCantidad =
            new EditarCantidadWindow(
                cantidadActual
            );

        var resultadoCantidad =
            await ventanaCantidad.ShowDialog<bool>(
                owner
            );

        if (!resultadoCantidad)
            return;

        await _viewModel
            .ActualizarCantidadManualAsync(
                detalle,
                ventanaCantidad.CantidadSeleccionada
            );
    }
    

    // =========================================================
    // COBRAR
    // =========================================================

    private async void OnSolicitarCobro()
    {
        if (_viewModel is null)
            return;


        try
        {
            var owner =
                TopLevel.GetTopLevel(this)
                as Window;


            if (owner is null)
            {
                _viewModel.MensajeError =
                    "No fue posible abrir la ventana de cobro.";

                return;
            }


            // =========================================================
            // FLUJO DE COBRO
            // =========================================================

            while (true)
            {
                var saldoACobrar =
                    _viewModel.SaldoPendiente;

                if (saldoACobrar <= 0 || _viewModel.VentaFinalizadaCorrectamente)
                    return;

                // =====================================================
                // SELECTOR DE MÉTODO DE PAGO
                // =====================================================

                var ventana =
                    new CobroWindow(
                        saldoACobrar,
                        _viewModel
                    );


                var resultado =
                    await ventana
                        .ShowDialog<bool>(
                            owner
                        );


                // =====================================================
                // CERRÓ / CANCELÓ TODO EL COBRO
                // =====================================================

                if (!resultado)
                    return;


                // =====================================================
                // EFECTIVO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "EFECTIVO"
                )
                {
                    var pagoEfectivo =
                        new PagoEfectivoWindow(
                            saldoACobrar
                        );


                    var pagoConfirmado =
                        await pagoEfectivo
                            .ShowDialog<bool>(
                                owner
                            );


                    // =================================================
                    // CANCELÓ EFECTIVO
                    // =================================================
                    //
                    // Regresamos al selector de método de pago.
                    // =================================================

                    if (!pagoConfirmado)
                        continue;


                    // =================================================
                    // REGISTRAR PAGO
                    // =================================================

                    var pagoRegistrado =
                        await _viewModel
                            .RegistrarPagoEfectivoAsync(
                                pagoEfectivo.ImporteAplicado,
                                pagoEfectivo.CantidadRecibida,
                                pagoEfectivo.Cambio
                            );


                    if (!pagoRegistrado)
                        return;


                    // =================================================
                    // VALIDAR SI LA VENTA TERMINÓ O QUEDA SALDO
                    // =================================================

                    if (_viewModel.SaldoPendiente <= 0 || _viewModel.VentaFinalizadaCorrectamente)
                        return;

                    // Queda saldo restante (pago mixto) -> continuar cobrando
                    continue;
                }
                
                
                // =====================================================
                // TRANSFERENCIA
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "TRANSFERENCIA"
                )
                {
                    // =================================================
                    // VALIDAR VENTA
                    // =================================================

                    if (_viewModel.IdVentaActual <= 0)
                    {
                        _viewModel.MensajeError =
                            "No existe una venta activa para cobrar.";

                        return;
                    }


                    // =================================================
                    // MOSTRAR DATOS PARA TRANSFERENCIA
                    // =================================================

                    var pagoTransferencia =
                        new PagoTransferenciaWindow(
                            saldoACobrar
                        );


                    var transferenciaConfirmada =
                        await pagoTransferencia
                            .ShowDialog<bool>(
                                owner
                            );


                    // =================================================
                    // CANCELÓ / REGRESÓ
                    // =================================================
                    //
                    // Volvemos al selector de método de pago.
                    // La venta continúa exactamente como estaba.
                    // =================================================

                    if (!transferenciaConfirmada)
                        continue;

// =================================================
// REGISTRAR TRANSFERENCIA
// =================================================

                    var pagoRegistrado =
                        await _viewModel
                            .RegistrarPagoTransferenciaAsync(
                                saldoACobrar
                            );


                    if (!pagoRegistrado)
                    {
                        /*
                         * El ViewModel ya dejó el motivo en MensajeError.
                         *
                         * IMPORTANTE:
                         * No intentamos registrar automáticamente
                         * una segunda transferencia.
                         */

                        return;
                    }


                    // =================================================
                    // VALIDAR SI LA VENTA TERMINÓ O QUEDA SALDO
                    // =================================================

                    if (_viewModel.SaldoPendiente <= 0 || _viewModel.VentaFinalizadaCorrectamente)
                        return;

                    // Queda saldo restante (pago mixto) -> continuar cobrando
                    continue;
                }


                // =====================================================
                // CRÉDITO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "CREDITO"
                )
                {
                    // =================================================
                    // FINALIZAR VENTA A CRÉDITO
                    // =================================================
                    //
                    // Este método:
                    //
                    // 1. Actualiza tipo_venta = CREDITO.
                    // 2. Conserva al cliente seleccionado.
                    // 3. Llama FinalizarVentaAsync().
                    // 4. El backend vuelve a validar el crédito.
                    // 5. Genera REMISIÓN PPD + CARGO.
                    // 6. Descuenta inventario.
                    // 7. Genera ticket.
                    // =================================================

                    var ventaCreditoFinalizada =
                        await _viewModel
                            .FinalizarVentaCreditoAsync();


                    // =================================================
                    // ERROR / RECHAZO
                    // =================================================

                    if (!ventaCreditoFinalizada)
                    {
                        /*
                         * No hacemos retry automático.
                         *
                         * MensajeError contendrá el motivo
                         * entregado por backend.
                         */

                        return;
                    }


                    // =================================================
                    // VENTA A CRÉDITO TERMINADA
                    // =================================================

                    return;
                }
                
                
                // =====================================================
                // TARJETA / MERCADO PAGO
                // =====================================================

                if (
                    ventana.MetodoSeleccionado ==
                    "TARJETA"
                )
                {
                    // =================================================
                    // VALIDAR VENTA
                    // =================================================

                    if (_viewModel.IdVentaActual <= 0)
                    {
                        _viewModel.MensajeError =
                            "No existe una venta activa para cobrar.";

                        return;
                    }


                    try
                    {
                        // =================================================
                        // 1. CONSULTAR TERMINALES MERCADO PAGO
                        // =================================================
                       
                        Console.WriteLine("====================================");
                        Console.WriteLine("MP: CONSULTANDO TERMINALES...");
                        Console.WriteLine($"ID VENTA: {_viewModel.IdVentaActual}");
                        Console.WriteLine("====================================");
                        

                        var terminales =
                            await _mercadoPagoService
                                .ObtenerTerminalesAsync(
                                    _viewModel.IdVentaActual
                                );
                        
                        Console.WriteLine("====================================");
                        Console.WriteLine("MP: RESPUESTA DE TERMINALES RECIBIDA");
                        Console.WriteLine($"RES: {terminales.Res}");
                        Console.WriteLine($"MSG: {terminales.Msg}");
                        Console.WriteLine(
                            $"TERMINALES: {terminales.Data?.Terminales.Count ?? 0}"
                        );
                        Console.WriteLine("====================================");


                        if (
                            terminales.Data is null
                            ||
                            terminales.Data.Terminales.Count == 0
                        )
                        {
                            _viewModel.MensajeError =
                                "No existen terminales Mercado Pago disponibles.";

                            continue;
                        }


                        // =================================================
                        // 2. DETERMINAR TERMINAL
                        // =================================================

                        var terminalSeleccionada =
                            terminales.Data.Terminales.Count == 1
                                ? terminales.Data.Terminales[0]
                                : null;


                        // =================================================
                        // 3. SI HAY VARIAS → MOSTRAR SELECTOR
                        // =================================================

                        if (terminalSeleccionada is null)
                        {
                            var selectorTerminal =
                                new MercadoPagoTerminalWindow(
                                    saldoACobrar,
                                    terminales
                                );


                            var terminalConfirmada =
                                await selectorTerminal
                                    .ShowDialog<bool>(
                                        owner
                                    );


                            // Canceló selección de terminal.
                            // Regresamos al selector Efectivo/Tarjeta/etc.
                            if (!terminalConfirmada)
                                continue;


                            terminalSeleccionada =
                                selectorTerminal
                                    .TerminalSeleccionada;


                            if (terminalSeleccionada is null)
                            {
                                _viewModel.MensajeError =
                                    "No se seleccionó una terminal Mercado Pago.";

                                continue;
                            }
                        }


                        // =================================================
                        // 4. ABRIR PROCESAMIENTO DE MERCADO PAGO
                        // =================================================

                        var pagoMercadoPago =
                            new MercadoPagoPagoWindow(
                                _viewModel.IdVentaActual,
                                terminalSeleccionada.IdTpvBancaria,
                                saldoACobrar,
                                terminalSeleccionada.NombreVisual
                            );


                        await pagoMercadoPago
                            .ShowDialog(
                                owner
                            );


                        // =================================================
                        // 5. OBTENER RESULTADO
                        // =================================================

                        var resultadoMercadoPago =
                            pagoMercadoPago.Resultado;


                        if (resultadoMercadoPago is null)
                        {
                            /*
                             * No tenemos confirmación de pago.
                             *
                             * MUY IMPORTANTE:
                             *
                             * No registramos ningún pago aquí.
                             * No finalizamos la venta.
                             * No volvemos a cobrar automáticamente.
                             */

                            _viewModel.MensajeError =
                                "No fue posible confirmar el resultado del pago con Mercado Pago.";

                            return;
                        }


                        // =================================================
                        // 6. REQUIERE CONCILIACIÓN
                        // =================================================

                        if (resultadoMercadoPago.RequiereConciliacion)
                        {
                            /*
                             * Puede existir un pago real en Mercado Pago
                             * cuyo estado local todavía no conocemos.
                             *
                             * NO permitir un segundo cobro automático.
                             */

                            _viewModel.MensajeError =
                                string.IsNullOrWhiteSpace(
                                    resultadoMercadoPago.Mensaje
                                )
                                    ? "El estado del pago requiere verificación. No vuelva a cobrar la venta."
                                    : resultadoMercadoPago.Mensaje;

                            return;
                        }


                        // =================================================
                        // 7. PAGO APROBADO Y REGISTRADO
                        // =================================================

                        if (resultadoMercadoPago.PagoRegistrado)
                        {
                            /*
                             * IMPORTANTE:
                             *
                             * estado_orden.php YA insertó el pago en:
                             *
                             * tr_pos_ventas_pagos
                             *
                             * Recargamos la venta actual para reflejar
                             * el nuevo monto pagado y saldo en memoria.
                             */

                            await _viewModel
                                .CargarVentaActualAsync();

                            if (
                                resultadoMercadoPago.PagoCompleto
                                ||
                                _viewModel.SaldoPendiente <= 0
                            )
                            {
                                var finalizada =
                                    await _viewModel
                                        .FinalizarVentaAsync();

                                if (!finalizada)
                                {
                                    /*
                                     * El dinero YA fue cobrado.
                                     *
                                     * Si falla la finalización:
                                     *
                                     * NO volver a cobrar.
                                     *
                                     * CargarVentaActualAsync() podrá recuperar
                                     * posteriormente la venta pagada pendiente
                                     * de finalizar.
                                     */

                                    return;
                                }

                                return;
                            }
                            else
                            {
                                // Queda saldo restante (pago mixto) -> continuar cobrando
                                continue;
                            }
                        }


                        // =================================================
                        // 8. CANCELADO / RECHAZADO / EXPIRADO
                        // =================================================

                        if (
                            resultadoMercadoPago.Cancelado
                            ||
                            resultadoMercadoPago.Rechazado
                            ||
                            resultadoMercadoPago.Expirado
                        )
                        {
                            /*
                             * No existe pago aplicado en NovaCore.
                             *
                             * La venta continúa BORRADOR.
                             *
                             * Regresamos al selector para permitir:
                             *
                             * - volver a intentar tarjeta;
                             * - elegir efectivo;
                             * - elegir crédito;
                             * - etc.
                             */

                            _viewModel.MensajeError =
                                resultadoMercadoPago.Mensaje;

                            continue;
                        }


                        // =================================================
                        // 9. ESTADO NO CONCLUYENTE
                        // =================================================

                        _viewModel.MensajeError =
                            string.IsNullOrWhiteSpace(
                                resultadoMercadoPago.Mensaje
                            )
                                ? "No fue posible determinar el estado final del pago."
                                : resultadoMercadoPago.Mensaje;

                        return;
                    }
                    catch (MercadoPagoException ex)
                    {
                        Console.WriteLine("====================================");
                        Console.WriteLine("ERROR MERCADO PAGO");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("====================================");
                        
                        _viewModel.MensajeError =
                            ex.Message;

                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("====================================");
                        Console.WriteLine("ERROR GENERAL MERCADO PAGO");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("====================================");
                        
                        _viewModel.MensajeError =
                            $"No fue posible procesar el pago con Mercado Pago: {ex.Message}";

                        return;
                    }
                }
                
            }
        }
        finally
        {
            /*
             * Si:
             *
             * - cerró Cobro
             * - canceló efectivo
             * - terminó correctamente
             * - terminó crédito
             *
             * siempre dejamos el POS preparado
             * para el siguiente escaneo.
             */

            EnfocarBuscadorProductos();
        }
    }


    // =========================================================
    // ENTREGA DE TICKET
    // =========================================================

    private async Task<string?> OnSolicitarEntregaTicket(
        TicketVenta ticket)
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;


        if (owner is null)
            return null;


        var ventana =
            new EntregaTicketWindow(
                ticket.FolioTicket,
                ticket.Total
            );


        var resultado =
            await ventana
                .ShowDialog<bool>(
                    owner
                );


        if (!resultado)
            return null;


        return
            ventana.OpcionSeleccionada;
    }


    // =========================================================
    // SOLICITAR TELÉFONO PARA WHATSAPP
    // =========================================================

    private async Task<string?>
        OnSolicitarTelefonoWhatsApp()
    {
        var owner =
            TopLevel.GetTopLevel(this)
            as Window;


        if (owner is null)
            return null;


        var ventana =
            new EnviarWhatsAppWindow();


        var resultado =
            await ventana
                .ShowDialog<bool>(
                    owner
                );


        if (!resultado)
            return null;


        return
            ventana.Telefono;
    }


// =========================================================
    // CONFIRMACIÓN DE VENTA FINALIZADA
    // =========================================================

    private async Task OnSolicitarConfirmacionVentaFinalizada(
        string folioTicket)
    {

        if (_viewModel is not null)
        {
            _viewModel.TextoBusqueda = string.Empty;
            _viewModel.TextoBusquedaCliente = string.Empty;


            if (_viewModel.QuitarClienteCommand != null && _viewModel.QuitarClienteCommand.CanExecute(null))
            {
                _viewModel.QuitarClienteCommand.Execute(null);
            }
        }

        var buscadorCliente = this.FindControl<TextBox>("BuscadorClienteTextBox");
        if (buscadorCliente != null)
        {
            buscadorCliente.Text = string.Empty;
        }

        var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
        if (buscadorProductos != null)
        {
            buscadorProductos.Text = string.Empty;
        }

        var owner =
            TopLevel.GetTopLevel(this)
            as Window;

        if (owner is null)
            return;

        var ventana =
            new VentaFinalizadaWindow(
                folioTicket
            );

        await ventana
            .ShowDialog(
                owner
            );
    }


// =========================================================
// ENFOCAR BUSCADOR / ESCÁNER
// =========================================================

    private void EnfocarBuscadorProductos()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (BuscadorProductosTextBox is null)
                    return;

                if (!BuscadorProductosTextBox.IsAttachedToVisualTree())
                    return;

                BuscadorProductosTextBox.Focus();

                BuscadorProductosTextBox.SelectAll();
            },
            DispatcherPriority.Background
        );
    }


    // =========================================================
    // DESVINCULAR EVENTOS
    // =========================================================

    private void OnDetachedFromVisualTree(
        object? sender,
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        
        if (_parentWindow != null)
        {
            //_parentWindow.KeyDown -= ParentWindow_KeyDown;
            _parentWindow.RemoveHandler(InputElement.KeyDownEvent, ParentWindow_KeyDown);
            _parentWindow = null;
        }
        
        if (_viewModel is not null)
        {
            
            _viewModel.SolicitarSeleccionPresentacion -=
                OnSolicitarSeleccionPresentacion;


            _viewModel.SolicitarEditarCantidad -=
                OnSolicitarEditarCantidad; 


            _viewModel.SolicitarCobro -=
                OnSolicitarCobro;


            _viewModel.SolicitarEntregaTicket -=
                OnSolicitarEntregaTicket;


            _viewModel.SolicitarTelefonoWhatsApp -=
                OnSolicitarTelefonoWhatsApp;


            _viewModel.SolicitarConfirmacionVentaFinalizada -=
                OnSolicitarConfirmacionVentaFinalizada;
            
            _viewModel.SolicitarCambioPrecio -=
                OnSolicitarCambioPrecio;
        }


        _viewModel =
            null;
    }
    
   private void ParentWindow_KeyDown(object? sender, KeyEventArgs e)
{
    var ahora = DateTime.UtcNow;
    var diff = (ahora - _ultimoTiempoTeclaScanner).TotalMilliseconds;

    // =================================================
    // DETECCIÓN DE CHECADORA / PISTOLA DE CÓDIGO DE BARRAS
    // =================================================

    // 1. Detectar ENTER (fin de lectura de la checadora)
    if (e.Key == Key.Enter || e.Key == Key.Return)
    {
        if (_scannerBuffer.Length >= 3 && diff <= 95)
        {
            e.Handled = true;
            var codigoEscaneado = _scannerBuffer.ToString().Trim();
            _scannerBuffer.Clear();

            _ = ProcesarCodigoBarrasEscaneadoAsync(codigoEscaneado);
            return;
        }

        _scannerBuffer.Clear();
    }
    else
    {
        var caracter = ObtenerCaracterDeTecla(e);
        if (caracter.HasValue)
        {
            if (diff > ScannerMaxDelayMs)
            {
                _scannerBuffer.Clear();
            }

            _scannerBuffer.Append(caracter.Value);
            _ultimoTiempoTeclaScanner = ahora;

            // Si ya van 2 o más teclas en ráfaga rápida (< 70ms),
            // corresponde indiscutiblemente a la checadora.
            // Consumimos el evento para evitar que contamine controles con foco.
            if (_scannerBuffer.Length >= 2 && diff <= ScannerMaxDelayMs)
            {
                e.Handled = true;
                return;
            }
        }
        else
        {
            // Teclas de control (flechas, escape, etc.) que no sean modificadores
            if (e.Key != Key.LeftShift && e.Key != Key.RightShift &&
                e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
                e.Key != Key.LeftAlt && e.Key != Key.RightAlt)
            {
                _scannerBuffer.Clear();
            }
        }
    }


    // =================================================
    // F5 - CONSULTAR PRODUCTO SELECCIONADO
    // =================================================
    if (e.Key == Key.F5)
    {
        e.Handled = true;
        var listBox = this.FindControl<ListBox>("CatalogoProductosListBox");
        
        if (listBox != null && listBox.SelectedItem != null)
        {
            if (DataContext is VentasViewModel viewModel)
            {
                viewModel.SeleccionarProductoCommand.Execute(listBox.SelectedItem);
            }
        }
    }

    // =================================================
    // F9 - ENFOCAR BUSCADOR DE CLIENTES
    // =================================================
    else if (e.Key == Key.F9)
    {
        e.Handled = true;
        var buscadorCliente = this.FindControl<TextBox>("BuscadorClienteTextBox");
        
        if (buscadorCliente != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                buscadorCliente.Focus();
                buscadorCliente.SelectAll();
            }, DispatcherPriority.Input);
        }
    }

    // =================================================
    // F10 - COBRAR VENTA
    // =================================================
    else if (e.Key == Key.F10)
    {
        e.Handled = true;
        if (DataContext is VentasViewModel viewModel)
        {
            if (viewModel.CobrarCommand.CanExecute(null))
            {
                viewModel.CobrarCommand.Execute(null);
            }
        }
    }
    
    // =================================================
    // F11 - ENFOCAR BUSCADOR DE PRODUCTOS
    // =================================================
    else if (e.Key == Key.F11)
    {
        e.Handled = true;
        var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
                   
        if (buscadorProductos != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                buscadorProductos.Focus();
                buscadorProductos.SelectAll();
            }, DispatcherPriority.Input);
        }
    }
    
    
    // =================================================
    // F12 - ELIMINAR ÚLTIMO REGISTRO
    // =================================================
    else if (e.Key == Key.F12)
    {
        e.Handled = true;
        if (DataContext is VentasViewModel viewModel)
        {
            var ultimoProducto = System.Linq.Enumerable.LastOrDefault(viewModel.Carrito);
            
            if (ultimoProducto != null && viewModel.EliminarProductoCommand.CanExecute(ultimoProducto))
            {
                if (!ultimoProducto.EstaSincronizando)
                {
                    viewModel.EliminarProductoCommand.Execute(ultimoProducto);
                }
                else
                {
                    viewModel.MensajeError = "El último producto aún se está registrando, espera un momento.";
                }
            }
        }
    }




}
        
private void BuscadorClienteTextBox_KeyDown(object? sender, KeyEventArgs e)
{
    var listBox = this.FindControl<ListBox>("SugerenciasClientesListBox");

    if (listBox == null || !listBox.IsVisible || listBox.ItemCount == 0)
        return;

    if (e.Key == Key.Down)
    {
        e.Handled = true;
        if (listBox.SelectedIndex < listBox.ItemCount - 1)
        {
            listBox.SelectedIndex++;
        }
    }
    else if (e.Key == Key.Up)
    {
        e.Handled = true;
        if (listBox.SelectedIndex > 0)
        {
            listBox.SelectedIndex--;
        }
    }

    else if (e.Key == Key.Enter)
    {
        if (listBox.SelectedItem != null)
        {
            e.Handled = true;
            
            if (DataContext is VentasViewModel viewModel)
            {
                viewModel.SeleccionarClienteCommand.Execute(listBox.SelectedItem);

                var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
                if (buscadorProductos != null)
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                        buscadorProductos.Focus();
                        buscadorProductos.SelectAll();
                    }, DispatcherPriority.Input);
                }
            }
        }
    }
}

private void CatalogoProductosListBox_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        // Un solo clic sobre una tarjeta abre el producto.
        // Se toma el producto de la tarjeta tocada para que un clic
        // en un espacio vacío no vuelva a abrir el último seleccionado.
        var item = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true);
        if (item?.DataContext == null) return;

        if (DataContext is VentasViewModel viewModel)
        {
            viewModel.SeleccionarProductoCommand.Execute(item.DataContext);

            var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
            if (buscadorProductos != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    buscadorProductos.Focus();
                    buscadorProductos.SelectAll();
                }, DispatcherPriority.Input);
            }
        }
    }

    private void CatalogoProductosListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox == null) return;

        if (e.Key == Key.F5 || e.Key == Key.Enter)
        {
            if (listBox.SelectedItem != null)
            {
                e.Handled = true;
                
                if (DataContext is VentasViewModel viewModel)
                {
                    viewModel.SeleccionarProductoCommand.Execute(listBox.SelectedItem);

                    var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
                    if (buscadorProductos != null)
                    {
                        Dispatcher.UIThread.Post(() => 
                        {
                            buscadorProductos.Focus();
                            buscadorProductos.SelectAll();
                        }, DispatcherPriority.Input);
                    }
                }
            }
        }

        else if ((e.Key == Key.Up || e.Key == Key.Left) && listBox.SelectedIndex == 0)
        {
            e.Handled = true;
            var buscadorProductos = this.FindControl<TextBox>("BuscadorProductosTextBox");
            if (buscadorProductos != null)
            {
                Dispatcher.UIThread.Post(() => 
                {
                    buscadorProductos.Focus();
                    buscadorProductos.CaretIndex = buscadorProductos.Text?.Length ?? 0;
                }, DispatcherPriority.Input);
            }
        }
    }

    

    
}