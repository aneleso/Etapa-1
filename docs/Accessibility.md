# Accesibilidad de The PrimeGame

Esta revisión aplica criterios seleccionados de WCAG 2.2 al minijuego nativo de Unity, junto con principios de Diseño Universal para el Aprendizaje. No declara conformidad integral ni certificación WCAG. Todavía se requiere evaluación con usuarios y tecnologías de asistencia.

## Medidas y evidencia

| Necesidad / referencia | Implementación | Comprobación |
|---|---|---|
| Contraste del texto, 1.4.3 | Tinta oscura sobre superficies claras y fuente legible sin adornos. | Siete combinaciones de texto, incluidos botones seleccionados y pulsados, exigen al menos 4.5:1 en AccessibilityTests. |
| Contraste no textual, 1.4.11 | Bordes oscuros adicionales en los destinos e indicador de foco. El arte pastel se conserva. | La paleta de bordes y foco supera 3:1 sobre blanco, fondo y bandeja. Revisión visual en Play. |
| Información independiente del color, 1.4.1 | Cada pieza y destino comparte un número; la bandeja añade el nombre del color. | La colocación usa identificadores y no el color de los píxeles. |
| Control del audio, 1.4.2 | Música tras una interacción intencional, botón Sonido y tecla M; silencio persistente. | Inicio silencioso, fuentes silenciadas y recarga de preferencias en PlayMode. |
| Alternativa al audio | Descripciones de música, selección, encaje y logro, también en los diálogos y con el audio silenciado. No hay diálogo hablado. | PlayMode comprueba las descripciones y la victoria con el audio silenciado. |
| Teclado y foco, 2.1.1 / 2.4.7 | Tab, Mayús+Tab, flechas, Enter/Espacio; foco visible y navegación acotada al diálogo. | Pruebas del Input System y de foco en botones. |
| Alternativa al arrastre, 2.5.7 | Clic en la pieza y clic en su destino, además de arrastrar. | Pruebas de clic, raycast y arrastre correcto/incorrecto en PlayMode. |
| Ritmo y movimiento | Sin cronómetro ni penalizaciones; ayuda, repetición y reducción de movimiento persistente. | Pruebas de reinicio, cancelación, victoria y preferencias. |

## Contraste calculado

Se usa luminancia relativa sRGB linealizada y la razón `(L mayor + 0.05) / (L menor + 0.05)`. Estos valores redondeados son informativos; las pruebas comparan el valor sin redondear.

| Combinación | Razón aproximada |
|---|---:|
| Texto principal / fondo | 10.70:1 |
| Números de destino / blanco | 7.10:1 |
| Mensaje y foco / fondo | 6.33:1 |
| Texto / bandeja | 10.12:1 |
| Texto / botón normal | 9.33:1 |
| Texto / botón seleccionado | 6.34:1 |
| Texto / botón pulsado | 5.54:1 |

La medición de la paleta no evalúa cada píxel del logo ni certifica el arte original. Los números y bordes aportan información adicional sobre los sprites existentes.

## DUA

La representación combina imagen, números, instrucciones y descripciones de sonido. La acción permite arrastre, dos clics y teclado. La participación permite elegir el orden, pedir ayuda, reducir movimiento, silenciar y repetir sin presión de tiempo.

## Límites y revisión pendiente

La entrega se valida en Windows. El soporte táctil y el diseño vertical requieren pruebas en dispositivos reales. uGUI no tiene en este proyecto un puente con lectores de pantalla del sistema ni audiodescripción narrada de la construcción. No se ha validado ampliación de texto al 200 %, todas las resoluciones, navegación por conmutadores ni una auditoría completa WCAG 2.2 AA.

## Referencias

- [W3C: contraste mínimo](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html).
- [W3C: contraste no textual](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html).
- [W3C: movimientos de arrastre](https://www.w3.org/WAI/WCAG22/Understanding/dragging-movements.html).
- [W3C: control del audio](https://www.w3.org/WAI/WCAG22/Understanding/audio-control.html).
- [CAST: pautas DUA](https://udlguidelines.cast.org/).
