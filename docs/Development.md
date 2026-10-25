# Desarrollo de la aplicación

## Arquitectura y decisiones

`GameSession` contiene reglas independientes de Unity: identidad de piezas, coincidencia de destino, conteo único, orden opcional y reinicio. `GameManager` coordina esas reglas con selección, ayuda y victoria. `DraggableBlock` y `DropTarget` reciben eventos de uGUI; `GameHud` construye y distribuye la interfaz usando el nuevo Input System. Los comentarios en inglés explican decisiones, condiciones y ciclos.

`PrimeGameAssets` centraliza los recursos existentes. Los comandos del Editor los encuentran por GUID para conservar sus nombres de archivo sin introducir nombres en español en el código. `GameText` carga los textos desde JSON y detecta entradas vacías, duplicadas o claves ausentes. `GamePalette` centraliza los colores. `FocusIndicator` añade foco visible a los controles estándar y limpia su estado al cerrar diálogos.

El encaje bloquea la pieza antes de animarla para evitar duplicados. Los movimientos usan tiempo no escalado y se cancelan antes de reiniciar; la opción de movimiento reducido los omite. Las piezas adoptan las dimensiones del destino. La confirmación protege el progreso parcial. Las preferencias se guardan con PlayerPrefs.

`AudioToggle` publica los mismos eventos que reproduce como sonidos. La interfaz los transforma en descripciones visibles incluso al silenciar, también dentro de los diálogos. La partida puede comprenderse y completarse sin oír los efectos.

## Uso de herramientas de IA durante esta implementación

Se utilizó Codex como asistencia para redactar y modificar código C#, documentación y pruebas. Los cambios se revisaron y se sometieron al compilador y al Test Framework de Unity. El historial atribuye esta implementación al responsable indicado por el usuario: Julian Tunjuelo. La asistencia de una herramienta no representa participación de otros integrantes.

No se generaron imágenes, sprites ni audios nuevos con IA generativa durante esta implementación. Los fondos redondeados y bordes se dibujan mediante código de la interfaz. Se integran los recursos que ya estaban en el repositorio.

El registro de procedencia, herramientas y procesamiento de los sprites y audios originales corresponde a su responsable de integración y queda fuera del alcance solicitado para esta implementación. Este documento no sustituye ese registro ni afirma que el requisito global de procedencia esté completado.

## Trabajo en Git

Se preserva el historial original y se usan commits reales con identidad explícita. No se crean commits a nombre de integrantes ausentes ni se reescribe el historial previo. La etiqueta anotada `v2.0` identifica la entrega verificada.

El guion se mantiene únicamente de forma local dentro de `local-only/`, excluido por `.gitignore`. No debe añadirse a commits, etiquetas ni archivos de distribución.
