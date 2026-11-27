# Puente del lector BBVA (TOTAL POS SDK)

Programa chico en Java que corre **en la misma computadora que la caja**. El POS
le manda el importe por la red local y el puente cobra con el lector (Pin Pad)
de BBVA usando su SDK, o con el **simulador** mientras no llega el kit.

```
POS (.NET) ──http://127.0.0.1:8091──► Puente (Java) ──SDK──► Pin Pad ──► BBVA
```

El POS ya sabía hablar con una "terminal de red" (Configuración › Terminales de
cobro › Inalámbrica). El puente usa ese mismo contrato (`api/v1`), con los
datos del banco agregados (`bank`) y la cancelación / devolución (`void`).

## Modos

| `puente.modo` | Qué hace |
|---|---|
| `SIMULADOR` | No cobra. Responde como el banco según los centavos (ver abajo). Lo que imprime el POS dice **PRUEBA - SIN VALOR**. |
| `BBVA` | Cobro real con el TOTAL POS SDK. Necesita el `.jar` de BBVA en `lib/` y los datos de `bbva.*`. |

### Simulador: resultado según los centavos

| Centavos | Resultado |
|---|---|
| `.51` | Fondos insuficientes (51) |
| `.05` | Declinada (05) |
| `.33` | Tarjeta vencida (33) |
| `.55` | NIP incorrecto (55) |
| `.19` | El banco no responde: queda "sin confirmar" y a los ~20 s se reversa sola (no cobró) |
| `.02` | Aprobada con **débito** |
| `.01` | Aprobada con banda y **firma autógrafa** (sale línea de firma) |
| `.07` | Aprobada **sin contacto** (sin firma) |
| otro | Aprobada, crédito con chip y NIP (firma electrónica) |

## Compilar

Necesita Java (JDK) 17 o superior para compilar; el resultado corre con Java 8+.
**No** necesita el `.jar` de BBVA (se usa por reflexión).

```sh
./compilar.sh          # deja dist/puente-bbva.jar
```

## Instalar en una caja (Windows)

1. Instalar Java (JRE 8 o superior). BBVA pide JDK 7+; el SDK es de 32 o 64 bits según el kit.
2. Instalar los **drivers del Pin Pad que da BBVA** con el lector desconectado (ver el manual del kit).
3. Copiar `dist/` a `C:\NovaCore\PuenteBBVA\`, poner el `.jar` del SDK en `lib\`.
4. Copiar `puente.properties.ejemplo` como `puente.properties` y llenar:
   - `puente.modo=BBVA`, `bbva.paquete=` (el paquete de las clases del SDK)
   - `bbva.afiliacion`, `bbva.terminal`, `bbva.mac`, `bbva.id_aplicacion`, `bbva.clave_secreta`, `bbva.clave_bines`
   - `bbva.pinpad.conexion=COMx` (puerto real del lector en el Administrador de dispositivos)
   - `bbva.ambiente=TESTING` para pruebas/certificación; `PRODUCCION` al salir a producción.
5. Ejecutar `iniciar.bat` (o registrarlo como tarea al iniciar Windows).
6. En el POS: **Configuración › Terminales de cobro › Agregar** → Inalámbrica,
   dirección `127.0.0.1`, puerto `8091`, código `BBVA`, clave = `datos\clave.txt`.
   Botón **Probar conexión**.

## Qué hace por su cuenta

- **Inicializa el SDK** al arrancar y cada 10 min: carga bines y manda los
  **reversos** pendientes (cobros que se quedaron sin respuesta).
- **Carga de llaves** (los 3 casos que pide BBVA): manual (`POST /api/v1/admin/load-keys`),
  con la bandera `pinpad.carga.llaves=1` de `pinpad.config` al inicializar, y
  automática cuando el SDK avisa `PinPadKeysException`.
- **Un cobro a la vez.** Si el banco no respondió, no acepta otro cobro hasta
  que el reverso se confirma; el POS le dice al cajero que no cobre de nuevo.
- **Cancelación o devolución**: el puente decide. Mismo día bancario (antes de
  la 1:00 a.m. CDMX) y por el total → **cancelación** (sin tarjeta). Si no →
  **devolución** (pide la tarjeta del cliente).
- Guarda los cobros 15 días en `datos/cobros.json` y una bitácora diaria en
  `datos/puente-AAAAMMDD.log`. **Nunca** guarda el número completo de la tarjeta.

## API (`api/v1`, todas con `Authorization: Bearer <clave>` menos health)

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/v1/health` | Estado del lector |
| GET | `/api/v1/terminals` | El lector configurado |
| POST | `/api/v1/payments` | Cobrar: `terminal_code`, `amount_cents`, `external_reference`, `operator` + `Idempotency-Key` |
| GET | `/api/v1/payments/{ref}` | Estado del cobro (el POS consulta cada segundo) |
| GET | `/api/v1/payments?q=` | Cobros de una venta |
| POST | `/api/v1/payments/{ref}/cancel` | Cancelar mientras espera la tarjeta |
| POST | `/api/v1/payments/{ref}/void` | Regresar el cargo (cancelación o devolución) |
| POST | `/api/v1/admin/load-keys` | Carga de llaves manual |

## Pendiente con el kit real

- Confirmar el paquete de las clases y los nombres de los getters de `Respuesta`
  (están tomados del PDF de especificaciones 4.1.2).
- Pruebas con el Pin Pad y la matriz de certificación de BBVA.
