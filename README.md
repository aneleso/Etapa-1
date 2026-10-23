# The PrimeGame · v2.0

Juego de encaje tranquilo con cuatro piezas. Utiliza los sprites y los cuatro sonidos originales del proyecto de la rama `selena_ortiz`.

## Abrir y jugar

1. En Unity Hub, añade la carpeta `ThePrimeGame`, que contiene `Assets`, `Packages` y `ProjectSettings`.
2. Ábrela con **Unity 6000.3.0f1** y espera a que termine de importar los paquetes.
3. Abre `Assets/Scenes/Level1.unity` y pulsa **Play**. La interfaz se genera al iniciar el juego.

En este equipo, la copia de trabajo registrada en Hub está en `C:\Users\User\Documents\Codex\PrimeGame`. Utiliza esa entrada para evitar el límite de rutas largas de Windows. Si extraes el proyecto ZIP en otro lugar, elige una ruta corta, por ejemplo `C:\UnityProjects\ThePrimeGame`.

No es necesario descargar C# por separado: el Editor compila los scripts del proyecto. Visual Studio sirve para editarlos y depurarlos.

## Cómo se juega

Coloca cada bloque sobre el destino que tiene el mismo número. El azul (1) va abajo, el menta (2) en medio, el crema (3) arriba y el coral (4) a la derecha. Puedes colocarlos en cualquier orden. Al terminar aparece una estrella y puedes volver a jugar o ver tu construcción.

- **Ratón o pantalla táctil:** arrastra y suelta. También puedes pulsar una pieza y después su destino.
- **Teclado:** Tab / Mayús+Tab o flechas para recorrer controles; Enter o Espacio para activarlos. Los números 1–4 seleccionan una pieza.
- **H / Ayuda:** resalta el destino de una pieza pendiente.
- **M / Sonido:** activa o desactiva el audio.
- **R / Reiniciar:** vuelve a empezar; si hay progreso, pide confirmación.
- **Esc:** cancela la selección o cierra el diálogo.
- **Animaciones:** permite quitar los movimientos de encaje, retorno y celebración. La preferencia y el silencio se conservan entre sesiones.

La música comienza después de la primera interacción con una pieza. No hay cronómetro, puntuación negativa ni sonido de error. Una pieza que se suelta fuera de su lugar vuelve a la bandeja; una pieza colocada queda fija hasta reiniciar.

## Recursos y correcciones

- Conserva el arte original, sin redibujar las piezas ni los botones.
- `PrimeGameAssets.asset` reúne las referencias a los sprites y al audio.
- `Level1` sustituye la escena inicial sin comportamiento y conserva su GUID.
- Los destinos tienen nombres e identificadores estables. Se corrige la asociación entre los destinos superior e inferior descrita de forma invertida en el documento.
- El bloque adopta el tamaño de su destino al encajar; no se mantiene la diferencia de escalas de la escena anterior.
- La interfaz utiliza uGUI y el nuevo Input System que ya tenía configurado el proyecto.
- Se utiliza el logo válido; desaparecen de la escena las dos referencias antiguas que estaban rotas.
- El reinicio limpia el estado y cancela las animaciones en curso, sin recargar los recursos.

## Organización del código

| Archivo | Responsabilidad |
|---|---|
| `GameSession.cs` | Reglas de colocación, conteo único y reinicio. |
| `GameManager.cs` | Coordina selección, encaje, ayuda, victoria y preferencias. |
| `DraggableBlock.cs` | Arrastre, selección y movimiento hacia el destino o la bandeja. |
| `DropTarget.cs` | Destinos interactivos, pistas y foco visible. |
| `GameHud.cs` | Interfaz adaptable, navegación y diálogos. |
| `AudioToggle.cs` | Música, efectos y silencio persistente. |
| `PrimeGameAssets.cs` | Referencias al arte y al audio original. |
| `GameText.cs` | Textos de interfaz cargados desde JSON. |
| `GamePalette.cs` | Colores compartidos y comprobables. |
| `FocusIndicator.cs` | Foco visible en los controles estándar. |

`requireBottomFirst` en el componente GameManager permite exigir la construcción de abajo hacia arriba. Está desactivado para que la experiencia inicial sea sencilla.

## Pruebas y compilación

En el menú **The PrimeGame → Validate game (EditMode and PlayMode)** se ejecutan las pruebas de reglas e interacción. Los resultados NUnit XML se guardan en `ThePrimeGame/TestResults`. También pueden ejecutarse desde **Window → General → Test Runner**.

Las pruebas cubren destinos incorrectos, eventos repetidos, orden opcional, reinicio, entrada de ratón y teclado, arrastre, cancelación del reinicio, victoria y persistencia de preferencias. Su existencia no sustituye revisar el resultado de cada ejecución.

Validación de v2.0 en Unity 6000.3.0f1: el resumen verificado está en [docs/Validation.md](docs/Validation.md). La comprobación en Play también completó una partida por clic, usó Ayuda, canceló un reinicio y volvió a jugar después de la victoria.

**The PrimeGame → Build for Windows** genera `ThePrimeGame/Builds/Windows/ThePrimeGame.exe`. La instalación del Editor debe incluir soporte de compilación para Windows. Para reconstruir la escena y sus referencias utiliza **The PrimeGame → Create or update playable level**; este comando reemplaza el contenido de Level1.

La entrega está orientada a Windows. La entrada táctil y la distribución vertical están implementadas, pero publicar en Android o iOS requiere sus módulos de compilación y pruebas en dispositivos reales. No se ha añadido integración con lectores de pantalla.


## Accesibilidad y organización

La interfaz combina números, nombres de colores, bordes visibles, mensajes y efectos sonoros. Las descripciones del audio permanecen disponibles al silenciarlo y dentro de los diálogos. La música comienza tras una interacción intencional. Hay foco de teclado visible y alternativa por dos clics para cada arrastre.

Se aplican criterios seleccionados de WCAG 2.2 y DUA. Las medidas y limitaciones están en [docs/Accessibility.md](docs/Accessibility.md); no constituye una certificación integral de conformidad.

Todo el código C# de la aplicación, herramientas del Editor y pruebas usa identificadores, nombres de objetos, comentarios y mensajes técnicos en inglés. Los textos del jugador están en `ThePrimeGame/Assets/Resources/Localization/Spanish.json`. Los nombres heredados de recursos se conservan.

```text
ThePrimeGame/
  Assets/Editor/         Preparación, pruebas y compilación
  Assets/Scripts/        Reglas, interacción e interfaz
  Assets/Resources/      Textos de la interfaz
  Assets/Scenes/         Level1
  Assets/Settings/       Referencias y renderizado
  Assets/Sprites/        Sprites originales
  Assets/audio/          Audios originales
  Assets/Tests/          EditMode y PlayMode
  Packages/             Dependencias
  ProjectSettings/      Configuración
  docs/                 Documentación incluida en el ZIP del proyecto
```

En el repositorio, `docs/` está en la raíz, junto a [CONTRIBUTORS.md](CONTRIBUTORS.md). Se conservan las carpetas históricas de integrantes. Cachés, compilaciones, resultados locales y el guion están excluidos de Git. [docs/Development.md](docs/Development.md) explica las decisiones y el alcance del uso de IA.

## Versionado

La entrega de esta implementación corresponde a la etiqueta anotada `v2.0`, con versión de producto `2.0`. Los commits identifican a Julian Tunjuelo como responsable con la identidad proporcionada por el usuario. Se conserva el historial de `selena_ortiz` sin atribuir aportes nuevos a otros integrantes.

Para otra versión, ejecuta pruebas y compilación, revisa los cambios, realiza un commit con tu identidad real y crea una etiqueta anotada sobre ese commit. Cada integrante debe registrar sus propios aportes reales.
