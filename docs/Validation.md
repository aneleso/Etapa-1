# Validación de The PrimeGame v2.0

Fecha local: 23 de octubre de 2026 (America/Bogota).
Editor: Unity 6000.3.0f1, Windows, Direct3D 12.

## Resultados automáticos

| Ejecución | Aprobadas | Fallidas | Omitidas |
|---|---:|---:|---:|
| EditMode | 15 | 0 | 0 |
| PlayMode | 14 | 0 | 0 |
| Total | **29** | **0** | **0** |

Resultados exportados por Unity Test Framework a TestResults/EditMode.xml y TestResults/PlayMode.xml. La ejecución de PlayMode inició a las 14:01:36 UTC y terminó a las 14:01:45 UTC. Las pruebas usan la escena Level1 y dispositivos virtuales del Input System de Unity cuando corresponde.

Se verificaron reglas, conteo único, destino incorrecto, orden opcional, reinicio, clic, teclado, arrastre con retorno y encaje, bloqueo modal, cancelación del reinicio, victoria, repetición, preferencias, contraste de la paleta, descripciones de audio al silenciar, foco de controles y separación entre nombres de objetos en inglés y textos localizados.

Las capturas de inicio y victoria se revisaron a 1101 × 506 píxeles. Las etiquetas, los números, los bordes, el foco de repetición y la descripción del logro son visibles. Estas comprobaciones no sustituyen pruebas con usuarios o hardware de asistencia.

## Compilación y comprobación de Windows

Unity BuildPipeline informó Succeeded para StandaloneWindows64, versión de producto 2.0, sin errores. La compilación terminó a las 14:02:46 UTC. Se abrió el ejecutable actualizado y se comprobaron silencio, descripción de selección y encaje por clic con contador en una pieza. No se afirma prueba manual con teclado físico, arrastre físico ni teléfono.

Unity emitió un aviso al intentar subir símbolos nativos a su servicio de informes: respuesta HTTP 403. Esto no impidió generar ni ejecutar la aplicación. El reporte real del proceso se exportó a TestResults/BuildSummary.json, sin copiar registros completos de cuenta o sesión.

## Entorno y límites

La copia de trabajo usa una ruta corta para evitar el límite de rutas de Windows. Unity 6000.3.0f1 conserva los avisos conocidos de firmas de paquetes; no impidieron compilar ni ejecutar estas pruebas. Conviene migrar posteriormente a una revisión corregida, previa validación del equipo. No se deshabilitó la validación de firmas.

La validación de esta entrega se limita a Windows. No incluye certificación integral WCAG 2.2, lectores de pantalla, ampliación de texto al 200 % ni dispositivos táctiles reales. Véase [Accessibility.md](Accessibility.md).

## Repetir la validación

1. Abre el proyecto con la versión indicada y espera a que termine la importación.
2. Ejecuta The PrimeGame > Validate game (EditMode and PlayMode).
3. Revisa los XML y validation-status.txt en TestResults; comprueba que no haya fallos ni omisiones.
4. Ejecuta The PrimeGame > Build for Windows y revisa BuildSummary.json.
5. Abre el ejecutable y comprueba la interfaz. Los resultados de una ejecución anterior no garantizan cambios futuros.
