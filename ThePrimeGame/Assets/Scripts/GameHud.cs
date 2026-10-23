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
        public string AudioDescription => audioDescription.text;
        private GameManager manager;
        private PrimeGameAssets assets;
        private Font font;
        private Sprite rounded;
        private Sprite border;
        private Texture2D roundedTexture;
        private Texture2D borderTexture;
        private RectTransform root, logo, heading, subtitle, tray, board;
        private RectTransform instruction, statusRect, audioDescriptionRect, keyboardLegend;
        private readonly RectTransform[] cards = new RectTransform[4];
        private readonly RectTransform[] homes = new RectTransform[4];
        private readonly Text[] captions = new Text[4];
        private readonly List<Selectable> navigation = new List<Selectable>();
        private RectTransform trayTitle, trayNote, boardTitle, progressRect;
        private Text status, progress, motionText, soundText;
        private Text audioDescription, restartAudioDescription, victoryAudioDescription;
        private Image soundImage;
        private Button soundButton, restartButton, helpButton, motionButton;
        private RectTransform restartOverlay, restartPanel, victoryOverlay, victoryPanel;
        private Button keepPlaying, confirmRestart, replay, viewBuild;
        private RectTransform starRect;
        private int lastWidth, lastHeight;
        private float scale;
        private readonly Color ink = GamePalette.Ink;
        private readonly Color mutedInk = GamePalette.MutedInk;
        private readonly Color accent = GamePalette.Accent;

        /// <summary>Builds English-named objects and localized labels, then connects a single accessible input module.</summary>
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
            Panel("Background", root, GamePalette.Background, false, false);
            logo = Picture("The PrimeGame", root, assets.logo);
            heading = Label(GameText.Get("heading"), root, 32, TextAnchor.MiddleLeft).rectTransform;
            subtitle = Label(GameText.Get("subtitle"), root, 21, TextAnchor.MiddleLeft).rectTransform;
            instruction = Label(GameText.Get("instruction"), root, 22, TextAnchor.MiddleCenter).rectTransform;
            tray = Panel("Piece Tray", root, GamePalette.Tray);
            board = Panel("Construction Board", root, Color.white);
            trayTitle = Label(GameText.Get("trayTitle"), tray, 25, TextAnchor.MiddleLeft).rectTransform;
            trayNote = Label(GameText.Get("trayNote"), tray, 18, TextAnchor.MiddleLeft).rectTransform;
            boardTitle = Label(GameText.Get("boardTitle"), board, 25, TextAnchor.MiddleLeft).rectTransform;
            progress = Label(GameText.Get("progress", 0), board, 22, TextAnchor.MiddleRight);
            progressRect = progress.rectTransform;

            for (int i = 0; i < 4; i++)
            {
                var id = (PieceId)i;
                cards[i] = Panel("Piece Card " + (i + 1), tray, Color.white);
                homes[i] = Empty("Home Slot", cards[i]);
                var pieceRoot = Panel("Piece " + (i + 1) + " " + id, homes[i], Color.clear, true, false);
                var pieceButton = MakeSelectable(pieceRoot);
                var pieceOutline = FocusRing(pieceRoot);
                var face = Picture("Original Block", pieceRoot, assets.Piece(id));
                Fill(face);
                var number = Label((i + 1).ToString(), pieceRoot, 30, TextAnchor.MiddleCenter);
                Fill(number.rectTransform, 0, 8, 0, 0);
                Pieces[i] = pieceRoot.gameObject.AddComponent<DraggableBlock>();
                Pieces[i].Initialize(manager, id, homes[i], pieceOutline);
                navigation.Add(pieceButton);
                captions[i] = Label(GameText.Get("pieceCaption", i + 1, PrimeGameAssets.PieceName(id)), cards[i], 22, TextAnchor.MiddleCenter);

                var targetRoot = Panel("Target " + (i + 1), board, Color.clear, true, false);
                var targetButton = MakeSelectable(targetRoot);
                // The original pastel silhouette is decorative; this extra boundary keeps the destination perceivable.
                var boundary = Panel("Target Boundary", targetRoot, mutedInk);
                boundary.GetComponent<Image>().sprite = border;
                Fill(boundary);
                var targetOutline = FocusRing(targetRoot);
                var silhouette = Picture("Original Silhouette", targetRoot,
                    id == PieceId.Coral ? assets.silhouetteSmall : assets.silhouetteWide);
                Fill(silhouette);
                var targetNumber = Label((i + 1).ToString(), targetRoot, 30, TextAnchor.MiddleCenter);
                targetNumber.color = mutedInk;
                Fill(targetNumber.rectTransform, 0, 8, 0, 0);
                Targets[i] = targetRoot.gameObject.AddComponent<DropTarget>();
                Targets[i].Initialize(manager, id, targetOutline);
            }
            foreach (var target in Targets) navigation.Add(target.GetComponent<Button>());

            status = Label(GameText.Get("initialStatus"), root, 22, TextAnchor.MiddleCenter);
            status.color = accent;
            statusRect = status.rectTransform;
            audioDescription = Label(GameText.Get("audioReady"), root, 18, TextAnchor.MiddleCenter);
            audioDescriptionRect = audioDescription.rectTransform;
            helpButton = TextButton("Help Button", GameText.Get("helpButton"), root, manager.Hint);
            motionButton = TextButton("Motion Button", GameText.Get("motionOn"), root, manager.ToggleMotion);
            motionText = motionButton.GetComponentInChildren<Text>();
            keyboardLegend = Label(GameText.Get("keyboardLegend"), root,
                16, TextAnchor.MiddleCenter).rectTransform;
            DragLayer = Empty("Moving Pieces", root);
            Fill(DragLayer);
            BuildOverlays();
            // The sound control remains above modal overlays and is always reachable.
            soundButton = IconButton("Sound Button", GameText.Get("soundCaption"), root, assets.soundOn, manager.ToggleSound, out soundImage, out soundText);
            restartButton = IconButton("Restart Button", GameText.Get("restartButton"), root, assets.restart, manager.RequestRestart, out _, out _);
            navigation.Add(soundButton);
            navigation.Add(restartButton);
            navigation.Add(helpButton);
            navigation.Add(motionButton);
            UpdateSound(manager.Audio.IsMuted);
            UpdateMotion(manager.ReducedMotion);
            Layout();
            RefreshNavigation();
        }

        /// <summary>Handles display changes and shortcut keys while respecting drag and modal input boundaries.</summary>
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
                else { manager.ClearSelection(); Message(GameText.Get("selectionCancelled")); }
            }
            if (ModalOpen || manager.IsDragging) return;
            if (keyboard.hKey.wasPressedThisFrame) manager.Hint();
            if (keyboard.rKey.wasPressedThisFrame) manager.RequestRestart();
            var digits = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key };
            for (int i = 0; i < digits.Length; i++)
                if (digits[i].wasPressedThisFrame) manager.SelectPiece(Pieces[i]);
        }

        public void Message(string text) => status.text = text;

        /// <summary>Mirrors sound events in text and keeps their descriptions readable inside either dialog.</summary>
        public void DescribeAudio(AudioCue cue, bool muted)
        {
            string key;
            switch (cue)
            {
                case AudioCue.Music: key = "audioMusic"; break;
                case AudioCue.Pickup: key = "audioPickup"; break;
                case AudioCue.Snap: key = "audioSnap"; break;
                case AudioCue.Success: key = "audioSuccess"; break;
                case AudioCue.Muted: key = "audioMuted"; break;
                case AudioCue.Enabled: key = "audioEnabled"; break;
                default: key = "audioReady"; break;
            }
            string description = GameText.Get(key);
            if (muted && cue != AudioCue.Muted) description = GameText.Get("audioMutedCue", description);
            audioDescription.text = description;
            restartAudioDescription.text = description;
            victoryAudioDescription.text = description;
        }
        /// <summary>Refreshes both the count and navigation after a piece becomes unavailable.</summary>
        public void UpdateProgress(int count)
        {
            progress.text = GameText.Get("progress", count);
            RefreshNavigation();
        }
        public void UpdateSound(bool muted)
        {
            soundImage.sprite = muted ? assets.soundOff : assets.soundOn;
            soundText.text = muted ? GameText.Get("soundOff") : GameText.Get("soundOn");
        }
        public void UpdateMotion(bool reduced) => motionText.text = reduced ? GameText.Get("motionOff") : GameText.Get("motionOn");

        /// <summary>Moves focus into the confirmation dialog to keep underlying controls out of the keyboard path.</summary>
        public void ShowRestartConfirmation()
        {
            restartOverlay.gameObject.SetActive(true);
            RefreshNavigation();
            EventSystem.current.SetSelectedGameObject(keepPlaying.gameObject);
        }

        /// <summary>Closes restart confirmation and restores focus to a usable game control.</summary>
        public void HideModal()
        {
            restartOverlay.gameObject.SetActive(false);
            RefreshNavigation();
            EventSystem.current?.SetSelectedGameObject(manager.Selected != null
                ? manager.Selected.gameObject : helpButton.gameObject);
        }

        /// <summary>Shows the achievement and starts focus on the action that begins a new game.</summary>
        public void ShowVictory()
        {
            victoryOverlay.gameObject.SetActive(true);
            RefreshNavigation();
            EventSystem.current.SetSelectedGameObject(replay.gameObject);
            StartCoroutine(AnimateStar());
        }

        /// <summary>Restores the construction view and puts keyboard focus on restart.</summary>
        public void HideVictory()
        {
            victoryOverlay.gameObject.SetActive(false);
            starRect.localScale = Vector3.one;
            RefreshNavigation();
            EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
        }

        /// <summary>Uses one short, non-flashing animation and exits immediately when reduced motion is enabled.</summary>
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

        /// <summary>Creates progress-protecting and success dialogs with their own visible audio descriptions.</summary>
        private void BuildOverlays()
        {
            restartOverlay = Panel("Restart Confirmation Overlay", root, new Color32(39, 61, 73, 145), true, false);
            restartPanel = Panel("Restart Confirmation Panel", restartOverlay, new Color32(250, 248, 243, 255));
            var title = Label(GameText.Get("restartTitle"), restartPanel, 30, TextAnchor.MiddleCenter);
            var note = Label(GameText.Get("restartNote"), restartPanel, 22, TextAnchor.MiddleCenter);
            restartAudioDescription = Label(GameText.Get("audioReady"), restartPanel, 18, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 20, 25, 480, 50);
            Place(note.rectTransform, 20, 83, 480, 55);
            Place(restartAudioDescription.rectTransform, 20, 139, 480, 25);
            keepPlaying = TextButton("Continue Button", GameText.Get("continueButton"), restartPanel, HideModal);
            confirmRestart = TextButton("Confirm Restart Button", GameText.Get("confirmRestartButton"), restartPanel, manager.Restart);
            Place((RectTransform)keepPlaying.transform, 30, 168, 220, 62);
            Place((RectTransform)confirmRestart.transform, 270, 168, 220, 62);
            restartOverlay.gameObject.SetActive(false);

            victoryOverlay = Panel("Victory Overlay", root, new Color32(250, 248, 243, 230), true, false);
            victoryPanel = Panel("Victory Panel", victoryOverlay, Color.white);
            starRect = Picture("Achievement Star", victoryPanel, assets.star);
            Place(starRect, 185, 22, 150, 150);
            var victoryTitle = Label(GameText.Get("victoryTitle"), victoryPanel, 34, TextAnchor.MiddleCenter);
            var victoryNote = Label(GameText.Get("victoryNote"), victoryPanel, 22, TextAnchor.MiddleCenter);
            victoryAudioDescription = Label(GameText.Get("audioSuccess"), victoryPanel, 18, TextAnchor.MiddleCenter);
            Place(victoryTitle.rectTransform, 20, 176, 480, 52);
            Place(victoryNote.rectTransform, 20, 230, 480, 40);
            Place(victoryAudioDescription.rectTransform, 20, 272, 480, 25);
            replay = TextButton("Replay Button", GameText.Get("replayButton"), victoryPanel, manager.Restart);
            viewBuild = TextButton("View Construction Button", GameText.Get("viewBuildButton"), victoryPanel, HideVictory);
            Place((RectTransform)replay.transform, 30, 302, 220, 62);
            Place((RectTransform)viewBuild.transform, 270, 302, 220, 62);
            victoryOverlay.gameObject.SetActive(false);
        }

        /// <summary>Wraps Tab navigation across currently usable controls, including reverse navigation with Shift.</summary>
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

        /// <summary>Limits focus to the open dialog and sound control, or to the remaining gameplay controls.</summary>
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

        /// <summary>Computes layout in canvas units and reserves separate space for instructions, captions and controls.</summary>
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
            float bottom = portrait ? 176 : 152;
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
            Place(audioDescriptionRect, 24, h - 109, w - 48, 27);
            Place((RectTransform)helpButton.transform, (w - 410) / 2, h - 74, 180, 52);
            Place((RectTransform)motionButton.transform, (w - 410) / 2 + 194, h - 74, 216, 52);
            Place(keyboardLegend, 24, h - 22, w - 48, 20);
            keyboardLegend.gameObject.SetActive(!portrait);
            LayoutOverlay(restartOverlay, restartPanel, header, w, h, 266);
            LayoutOverlay(victoryOverlay, victoryPanel, header, w, h, 390);
        }

        /// <summary>Centers each dialog and scales it to fit narrow windows without cropping its buttons.</summary>
        private static void LayoutOverlay(RectTransform overlay, RectTransform panel, float top, float w, float h, float panelHeight)
        {
            Place(overlay, 0, top, w, h - top);
            float panelScale = Mathf.Min(1, (w - 40) / 520f);
            panel.localScale = Vector3.one * panelScale;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(520, panelHeight);
        }

        /// <summary>Creates a named transform with stable parenting before other interface components are added.</summary>
        private RectTransform Empty(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Creates a decorative or interactive surface with explicit raycast behavior.</summary>
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

        /// <summary>Preserves original sprite proportions and lets input reach the parent control.</summary>
        private RectTransform Picture(string name, Transform parent, Sprite sprite)
        {
            var rect = Empty(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>Keeps the object name in English while allowing its visible content to come from localization.</summary>
        private Text Label(string text, Transform parent, int size, TextAnchor alignment)
        {
            var rect = Empty("Text", parent);
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

        /// <summary>Creates a transparent-centered border that never covers the information inside a focused control.</summary>
        private Image FocusRing(RectTransform parent)
        {
            var ring = Panel("Focus Indicator", parent, accent);
            Fill(ring, -6, -6, -6, -6);
            var image = ring.GetComponent<Image>();
            image.sprite = border;
            image.enabled = false;
            return image;
        }

        /// <summary>Applies shared interaction colors; explicit focus paths are assigned after all controls exist.</summary>
        private Button MakeSelectable(RectTransform rect)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(217, 235, 233, 255);
            colors.selectedColor = GamePalette.SelectedTint;
            colors.pressedColor = GamePalette.PressedTint;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            return button;
        }

        /// <summary>Combines a readable caption with a visible focus indicator and a stable pointer hit area.</summary>
        private Button TextButton(string name, string text, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(name, parent, GamePalette.ButtonSurface, true);
            var button = MakeSelectable(rect);
            rect.gameObject.AddComponent<FocusIndicator>().Initialize(FocusRing(rect));
            button.onClick.AddListener(action);
            var label = Label(text, rect, 22, TextAnchor.MiddleCenter);
            Fill(label.rectTransform, 10, 4, 10, 4);
            return button;
        }

        /// <summary>Pairs each supplied icon with a written label so its meaning does not depend on recognizing the art.</summary>
        private Button IconButton(string name, string text, Transform parent, Sprite sprite, UnityEngine.Events.UnityAction action,
            out Image icon, out Text caption)
        {
            var rect = Panel(name, parent, Color.clear, true, false);
            var button = MakeSelectable(rect);
            rect.gameObject.AddComponent<FocusIndicator>().Initialize(FocusRing(rect));
            button.onClick.AddListener(action);
            var picture = Picture(name + " Icon", rect, sprite);
            Place(picture, 21, 0, 58, 58);
            icon = picture.GetComponent<Image>();
            caption = Label(text, rect, 18, TextAnchor.MiddleCenter);
            Place(caption.rectTransform, 0, 58, 100, 24);
            return button;
        }

        /// <summary>Expresses rectangles from the top-left so layout calculations use consistent coordinates.</summary>
        private static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(left + width / 2, -top - height / 2);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Stretches a child inside its parent using explicit margins.</summary>
        private static void Fill(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Draws small interface surfaces procedurally; this creates no generative-AI image asset.</summary>
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

        /// <summary>Releases generated textures and sprites when the level is unloaded or Play stops.</summary>
        private void OnDestroy()
        {
            if (rounded != null) Destroy(rounded);
            if (roundedTexture != null) Destroy(roundedTexture);
            if (border != null) Destroy(border);
            if (borderTexture != null) Destroy(borderTexture);
        }
    }
}
