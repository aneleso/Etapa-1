using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ThePrimeGame
{
    // All interaction uses uGUI and the new Input System, including touch and keyboard.
    public sealed class GameHud : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public DraggableBlock[] Pieces { get; } = new DraggableBlock[4];
        public DropTarget[] Targets { get; } = new DropTarget[4];
        public bool ModalOpen => restartOverlay.gameObject.activeSelf || victoryOverlay.gameObject.activeSelf;
        public bool VictoryVisible => victoryOverlay.gameObject.activeSelf;
        public string StatusMessage => status.text;
        private GameManager manager;
        private PrimeGameAssets assets;
        private Font font;
        private Sprite rounded;
        private Sprite border;
        private Texture2D roundedTexture;
        private Texture2D borderTexture;
        private RectTransform root, logo, heading, subtitle, tray, board;
        private RectTransform instruction, statusRect, keyboardLegend;
        private readonly RectTransform[] cards = new RectTransform[4];
        private readonly RectTransform[] homes = new RectTransform[4];
        private readonly Text[] captions = new Text[4];
        private readonly List<Selectable> navigation = new List<Selectable>();
        private RectTransform trayTitle, trayNote, boardTitle, progressRect;
        private Text status, progress, motionText, soundText;
        private Image soundImage;
        private Button soundButton, restartButton, helpButton, motionButton;
        private RectTransform restartOverlay, restartPanel, victoryOverlay, victoryPanel;
        private Button keepPlaying, confirmRestart, replay, viewBuild;
        private RectTransform starRect;
        private int lastWidth, lastHeight;
        private float scale;
        private readonly Color ink = new Color32(39, 61, 73, 255);
        private readonly Color mutedInk = new Color32(72, 91, 101, 255);
        private readonly Color accent = new Color32(36, 100, 111, 255);

        public void Initialize(GameManager game, PrimeGameAssets resources)
        {
            manager = game;
            assets = resources;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = RoundedSprite();
            border = RoundedSprite(true);
            var canvasObject = new GameObject("PrimeGame Interface", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            root = (RectTransform)canvasObject.transform;
            Canvas = canvasObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule))
                    .GetComponent<EventSystem>();
                var module = eventSystem.GetComponent<InputSystemUIInputModule>();
                module.enabled = false;
                module.AssignDefaultActions();
                module.actionsAsset.Disable();
                module.submit.action.AddBinding("<Keyboard>/space");
                module.enabled = true;
            }
            Panel("Background", root, new Color32(250, 248, 243, 255), false, false);
            logo = Picture("The PrimeGame", root, assets.logo);
            heading = Label("Construye a tu ritmo", root, 32, TextAnchor.MiddleLeft).rectTransform;
            subtitle = Label("Cuatro piezas, una pequeña construcción.", root, 21, TextAnchor.MiddleLeft).rectTransform;
            instruction = Label("Arrastra una pieza o selecciónala y toca su destino.", root, 22, TextAnchor.MiddleCenter).rectTransform;
            tray = Panel("Tus piezas", root, new Color32(238, 243, 240, 255));
            board = Panel("Construcción", root, Color.white);
            trayTitle = Label("Tus piezas", tray, 25, TextAnchor.MiddleLeft).rectTransform;
            trayNote = Label("Los números te ayudan a encontrar su lugar.", tray, 18, TextAnchor.MiddleLeft).rectTransform;
            boardTitle = Label("Tu construcción", board, 25, TextAnchor.MiddleLeft).rectTransform;
            progress = Label("0 de 4 piezas", board, 22, TextAnchor.MiddleRight);
            progressRect = progress.rectTransform;

            for (int i = 0; i < 4; i++)
            {
                var id = (PieceId)i;
                cards[i] = Panel("Pieza " + (i + 1), tray, Color.white);
                homes[i] = Empty("Origen", cards[i]);
                var pieceRoot = Panel("Pieza " + (i + 1) + " " + PrimeGameAssets.PieceName(id), homes[i], Color.clear, true, false);
                var pieceButton = MakeSelectable(pieceRoot);
                var pieceOutline = FocusRing(pieceRoot);
                var face = Picture("Bloque original", pieceRoot, assets.Piece(id));
                Fill(face);
                var number = Label((i + 1).ToString(), pieceRoot, 30, TextAnchor.MiddleCenter);
                Fill(number.rectTransform, 0, 8, 0, 0);
                Pieces[i] = pieceRoot.gameObject.AddComponent<DraggableBlock>();
                Pieces[i].Initialize(manager, id, homes[i], pieceOutline);
                navigation.Add(pieceButton);
                captions[i] = Label($"{i + 1} · {PrimeGameAssets.PieceName(id)}", cards[i], 22, TextAnchor.MiddleCenter);

                var targetRoot = Panel("Destino " + (i + 1), board, Color.clear, true, false);
                var targetButton = MakeSelectable(targetRoot);
                var targetOutline = FocusRing(targetRoot);
                var silhouette = Picture("Silueta original", targetRoot,
                    id == PieceId.Coral ? assets.silhouetteSmall : assets.silhouetteWide);
                Fill(silhouette);
                var targetNumber = Label((i + 1).ToString(), targetRoot, 30, TextAnchor.MiddleCenter);
                targetNumber.color = mutedInk;
                Fill(targetNumber.rectTransform, 0, 8, 0, 0);
                Targets[i] = targetRoot.gameObject.AddComponent<DropTarget>();
                Targets[i].Initialize(manager, id, targetOutline);
            }
            foreach (var target in Targets) navigation.Add(target.GetComponent<Button>());

            status = Label("Elige una pieza. Aquí puedes jugar a tu ritmo.", root, 22, TextAnchor.MiddleCenter);
            status.color = accent;
            statusRect = status.rectTransform;
            helpButton = TextButton("Ayuda", root, manager.Hint);
            motionButton = TextButton("Animaciones: sí", root, manager.ToggleMotion);
            motionText = motionButton.GetComponentInChildren<Text>();
            keyboardLegend = Label("Teclado: Tab y flechas · Enter para elegir · H ayuda · M sonido · R reiniciar", root,
                16, TextAnchor.MiddleCenter).rectTransform;
            DragLayer = Empty("Piezas en movimiento", root);
            Fill(DragLayer);
            BuildOverlays();
            // The sound control remains above modal overlays and is always reachable.
            soundButton = IconButton("Sonido", root, assets.soundOn, manager.ToggleSound, out soundImage, out soundText);
            restartButton = IconButton("Reiniciar", root, assets.restart, manager.RequestRestart, out _, out _);
            navigation.Add(soundButton);
            navigation.Add(restartButton);
            navigation.Add(helpButton);
            navigation.Add(motionButton);
            UpdateSound(manager.Audio.IsMuted);
            UpdateMotion(manager.ReducedMotion);
            Layout();
            RefreshNavigation();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Layout();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame)
                MoveFocus(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1);
            if (keyboard.mKey.wasPressedThisFrame) manager.ToggleSound();
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (restartOverlay.gameObject.activeSelf) HideModal();
                else if (VictoryVisible) HideVictory();
                else { manager.ClearSelection(); Message("Elige otra pieza cuando quieras."); }
            }
            if (ModalOpen || manager.IsDragging) return;
            if (keyboard.hKey.wasPressedThisFrame) manager.Hint();
            if (keyboard.rKey.wasPressedThisFrame) manager.RequestRestart();
            var digits = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key };
            for (int i = 0; i < digits.Length; i++)
                if (digits[i].wasPressedThisFrame) manager.SelectPiece(Pieces[i]);
        }

        public void Message(string text) => status.text = text;
        public void UpdateProgress(int count)
        {
            progress.text = $"{count} de 4 piezas";
            RefreshNavigation();
        }
        public void UpdateSound(bool muted)
        {
            soundImage.sprite = muted ? assets.soundOff : assets.soundOn;
            soundText.text = muted ? "Sonido: no" : "Sonido: sí";
        }
        public void UpdateMotion(bool reduced) => motionText.text = reduced ? "Animaciones: no" : "Animaciones: sí";

        public void ShowRestartConfirmation()
        {
            restartOverlay.gameObject.SetActive(true);
            RefreshNavigation();
            EventSystem.current.SetSelectedGameObject(keepPlaying.gameObject);
        }

        public void HideModal()
        {
            restartOverlay.gameObject.SetActive(false);
            RefreshNavigation();
            EventSystem.current?.SetSelectedGameObject(manager.Selected != null
                ? manager.Selected.gameObject : helpButton.gameObject);
        }

        public void ShowVictory()
        {
            victoryOverlay.gameObject.SetActive(true);
            RefreshNavigation();
            EventSystem.current.SetSelectedGameObject(replay.gameObject);
            StartCoroutine(AnimateStar());
        }

        public void HideVictory()
        {
            victoryOverlay.gameObject.SetActive(false);
            starRect.localScale = Vector3.one;
            RefreshNavigation();
            EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
        }

        private IEnumerator AnimateStar()
        {
            float elapsed = 0f;
            while (!manager.ReducedMotion && elapsed < 0.65f && VictoryVisible)
            {
                elapsed += Time.unscaledDeltaTime;
                starRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0, 1, elapsed / 0.65f));
                yield return null;
            }
            starRect.localScale = Vector3.one;
        }

        private void BuildOverlays()
        {
            restartOverlay = Panel("Confirmar reinicio", root, new Color32(39, 61, 73, 145), true, false);
            restartPanel = Panel("Volver a empezar", restartOverlay, new Color32(250, 248, 243, 255));
            var title = Label("¿Empezar de nuevo?", restartPanel, 30, TextAnchor.MiddleCenter);
            var note = Label("Las cuatro piezas volverán a tu bandeja.", restartPanel, 22, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 20, 25, 480, 50);
            Place(note.rectTransform, 20, 83, 480, 55);
            keepPlaying = TextButton("Seguir jugando", restartPanel, HideModal);
            confirmRestart = TextButton("Volver a empezar", restartPanel, manager.Restart);
            Place((RectTransform)keepPlaying.transform, 30, 168, 220, 62);
            Place((RectTransform)confirmRestart.transform, 270, 168, 220, 62);
            restartOverlay.gameObject.SetActive(false);

            victoryOverlay = Panel("Construcción terminada", root, new Color32(250, 248, 243, 230), true, false);
            victoryPanel = Panel("Celebración", victoryOverlay, Color.white);
            starRect = Picture("Estrella de logro", victoryPanel, assets.star);
            Place(starRect, 185, 22, 150, 150);
            var victoryTitle = Label("¡Lo construiste!", victoryPanel, 34, TextAnchor.MiddleCenter);
            var victoryNote = Label("Una pequeña construcción, un gran logro.", victoryPanel, 22, TextAnchor.MiddleCenter);
            Place(victoryTitle.rectTransform, 20, 176, 480, 52);
            Place(victoryNote.rectTransform, 20, 230, 480, 50);
            replay = TextButton("Jugar otra vez", victoryPanel, manager.Restart);
            viewBuild = TextButton("Ver mi construcción", victoryPanel, HideVictory);
            Place((RectTransform)replay.transform, 30, 302, 220, 62);
            Place((RectTransform)viewBuild.transform, 270, 302, 220, 62);
            victoryOverlay.gameObject.SetActive(false);
        }

        private void MoveFocus(int direction)
        {
            var options = FocusOptions();
            if (options.Count == 0) return;
            var current = EventSystem.current.currentSelectedGameObject;
            int index = options.FindIndex(s => s.gameObject == current);
            index = index < 0 ? (direction > 0 ? 0 : options.Count - 1)
                : (index + direction + options.Count) % options.Count;
            EventSystem.current.SetSelectedGameObject(options[index].gameObject);
        }

        private List<Selectable> FocusOptions()
        {
            var options = new List<Selectable>();
            if (restartOverlay.gameObject.activeSelf) options.AddRange(new Selectable[] { keepPlaying, confirmRestart, soundButton });
            else if (VictoryVisible) options.AddRange(new Selectable[] { replay, viewBuild, soundButton });
            else options.AddRange(navigation);
            options.RemoveAll(s => !s.gameObject.activeInHierarchy || !s.IsInteractable());
            return options;
        }

        // Arrow navigation follows the same modal boundary as Tab navigation.
        private void RefreshNavigation()
        {
            if (soundButton == null) return;
            foreach (var selectable in navigation)
                selectable.navigation = new Navigation { mode = Navigation.Mode.None };
            var options = FocusOptions();
            for (int i = 0; i < options.Count; i++)
            {
                var previous = options[(i + options.Count - 1) % options.Count];
                var next = options[(i + 1) % options.Count];
                options[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = previous, selectOnUp = previous,
                    selectOnRight = next, selectOnDown = next
                };
            }
        }

        private void Layout()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            scale = Mathf.Clamp(Screen.width / 1280f, 0.6f, 1.5f);
            Canvas.scaleFactor = scale;
            UnityEngine.Canvas.ForceUpdateCanvases();
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            bool portrait = h > w * 1.08f;
            float header = portrait ? 200 : 122;
            float bottom = portrait ? 150 : 112;
            float contentHeight = h - header - bottom;
            Place(logo, 24, 14, portrait ? 110 : 108, 84);
            Place(heading, portrait ? 146 : 154, 23, w - (portrait ? 170 : 430), 48);
            Place(subtitle, portrait ? 146 : 154, 70, w - (portrait ? 170 : 430), 30);
            subtitle.gameObject.SetActive(!portrait);
            Place((RectTransform)soundButton.transform, w - 232, portrait ? 100 : 18, 100, 80);
            Place((RectTransform)restartButton.transform, w - 116, portrait ? 100 : 18, 100, 80);
            Place(instruction, 24, portrait ? 116 : 92, portrait ? w - 280 : w - 48, portrait ? 64 : 30);
            float trayWidth = portrait ? w - 48 : Mathf.Min(490, w * 0.43f);
            float boardWidth = portrait ? w - 48 : w - trayWidth - 68;
            float boardHeight = portrait ? Mathf.Min(500, contentHeight * 0.54f) : contentHeight;
            float trayHeight = portrait ? contentHeight - boardHeight - 18 : contentHeight;
            Place(tray, 24, portrait ? header + boardHeight + 18 : header, trayWidth, trayHeight);
            Place(board, portrait ? 24 : trayWidth + 44, header, boardWidth, boardHeight);
            Place(trayTitle, 20, 12, trayWidth - 40, 38);
            Place(trayNote, 20, 48, trayWidth - 40, 32);
            Place(boardTitle, 20, 12, boardWidth - 200, 38);
            Place(progressRect, boardWidth - 178, 15, 158, 32);
            float cardWidth = (trayWidth - 52) / 2;
            float cardHeight = (trayHeight - 100) / 2;
            float pieceWidth = Mathf.Min(212, cardWidth - 24, (cardHeight - 48) * 1.6f);
            for (int i = 0; i < 4; i++)
            {
                float x = 18 + i % 2 * (cardWidth + 16);
                float y = 86 + i / 2 * (cardHeight + 12);
                Place(cards[i], x, y, cardWidth, cardHeight);
                Place(homes[i], cardWidth / 2, (cardHeight - 34) / 2, 0, 0);
                Place(captions[i].rectTransform, 4, cardHeight - 42, cardWidth - 8, 34);
                var piece = Pieces[i];
                piece.SetHomeSize(new Vector2(i == 3 ? pieceWidth / 2 : pieceWidth, pieceWidth / 1.6f));
            }
            float targetHeight = Mathf.Min(140, (boardHeight - 100) / 2.72f);
            float targetWidth = targetHeight * 1.6f;
            float towerLeft = (boardWidth - targetWidth * 1.5f - 18) / 2;
            float baseTop = boardHeight - 32 - targetHeight;
            for (int i = 0; i < 3; i++)
                Place(Targets[i].Rect, towerLeft, baseTop - i * targetHeight * 0.86f, targetWidth, targetHeight);
            Place(Targets[3].Rect, towerLeft + targetWidth + 18, baseTop, targetWidth / 2, targetHeight);
            foreach (var piece in Pieces)
                if (piece.IsPlaced) piece.Rect.sizeDelta = Targets[(int)piece.Id].Rect.sizeDelta;
            Place(statusRect, 24, h - bottom + 4, w - 48, portrait ? 58 : 38);
            Place((RectTransform)helpButton.transform, (w - 410) / 2, h - 74, 180, 52);
            Place((RectTransform)motionButton.transform, (w - 410) / 2 + 194, h - 74, 216, 52);
            Place(keyboardLegend, 24, h - 22, w - 48, 20);
            keyboardLegend.gameObject.SetActive(!portrait);
            LayoutOverlay(restartOverlay, restartPanel, header, w, h, 266);
            LayoutOverlay(victoryOverlay, victoryPanel, header, w, h, 390);
        }

        private static void LayoutOverlay(RectTransform overlay, RectTransform panel, float top, float w, float h, float panelHeight)
        {
            Place(overlay, 0, top, w, h - top);
            float panelScale = Mathf.Min(1, (w - 40) / 520f);
            panel.localScale = Vector3.one * panelScale;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520, panelHeight);
        }

        private RectTransform Empty(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private RectTransform Panel(string name, Transform parent, Color color, bool raycast = false, bool round = true)
        {
            var rect = Empty(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (round) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            if (name == "Background") Fill(rect);
            return rect;
        }

        private RectTransform Picture(string name, Transform parent, Sprite sprite)
        {
            var rect = Empty(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        private Text Label(string text, Transform parent, int size, TextAnchor alignment)
        {
            var rect = Empty(text, parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.color = ink;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Image FocusRing(RectTransform parent)
        {
            var ring = Panel("Foco visible", parent, accent);
            Fill(ring, -6, -6, -6, -6);
            var image = ring.GetComponent<Image>();
            image.sprite = border;
            image.enabled = false;
            return image;
        }

        private Button MakeSelectable(RectTransform rect)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(217, 235, 233, 255);
            colors.selectedColor = new Color32(185, 218, 215, 255);
            colors.pressedColor = new Color32(168, 205, 202, 255);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            return button;
        }

        private Button TextButton(string text, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(text, parent, new Color32(222, 236, 231, 255), true);
            var button = MakeSelectable(rect);
            button.onClick.AddListener(action);
            var label = Label(text, rect, 22, TextAnchor.MiddleCenter);
            Fill(label.rectTransform, 10, 4, 10, 4);
            return button;
        }

        private Button IconButton(string name, Transform parent, Sprite sprite, UnityEngine.Events.UnityAction action,
            out Image icon, out Text caption)
        {
            var rect = Panel(name, parent, Color.clear, true, false);
            var button = MakeSelectable(rect);
            button.onClick.AddListener(action);
            var picture = Picture(name + " icono", rect, sprite);
            Place(picture, 21, 0, 58, 58);
            icon = picture.GetComponent<Image>();
            caption = Label(name, rect, 18, TextAnchor.MiddleCenter);
            Place(caption.rectTransform, 0, 58, 100, 24);
            return button;
        }

        private static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(left + width / 2, -top - height / 2);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Fill(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private Sprite RoundedSprite(bool outline = false)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = outline ? "Visible focus border" : "Rounded UI surface";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(15 - x, x - 48, 0);
                    float dy = Mathf.Max(15 - y, y - 48, 0);
                    float alpha = Mathf.Clamp01(16 - Mathf.Sqrt(dx * dx + dy * dy));
                    if (outline)
                    {
                        float innerX = Mathf.Max(15 - x, x - 48, 0);
                        float innerY = Mathf.Max(15 - y, y - 48, 0);
                        float inner = Mathf.Clamp01(13 - Mathf.Sqrt(innerX * innerX + innerY * innerY));
                        if (x < 3 || x > 60 || y < 3 || y > 60) inner = 0;
                        alpha *= 1 - inner;
                    }
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            if (outline) borderTexture = texture;
            else roundedTexture = texture;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
        }

        private void OnDestroy()
        {
            if (rounded != null) Destroy(rounded);
            if (roundedTexture != null) Destroy(roundedTexture);
            if (border != null) Destroy(border);
            if (borderTexture != null) Destroy(borderTexture);
        }
    }
}
