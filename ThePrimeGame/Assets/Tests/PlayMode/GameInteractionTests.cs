using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ThePrimeGame.Tests
{
    /// <summary>Verifies scene behavior with both interface events and frame-by-frame Input System devices.</summary>
    public sealed class GameInteractionTests
    {
        private GameManager game;
        private Mouse virtualMouse;
        private Keyboard virtualKeyboard;
        private int previousMute, previousMotion;
        private bool hadMute, hadMotion;

        /// <summary>Isolates every scenario in a fresh level while preserving the real player preferences for teardown.</summary>
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            hadMute = PlayerPrefs.HasKey("ThePrimeGame.Muted");
            hadMotion = PlayerPrefs.HasKey("ThePrimeGame.ReducedMotion");
            previousMute = PlayerPrefs.GetInt("ThePrimeGame.Muted");
            previousMotion = PlayerPrefs.GetInt("ThePrimeGame.ReducedMotion");
            PlayerPrefs.SetInt("ThePrimeGame.Muted", 0);
            PlayerPrefs.SetInt("ThePrimeGame.ReducedMotion", 0);
            yield return SceneManager.LoadSceneAsync("Level1");
            yield return null;
            game = Object.FindFirstObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        /// <summary>Removes virtual devices and restores preferences so tests do not change a player's saved choices.</summary>
        [TearDown]
        public void RestorePreferences()
        {
            if (virtualMouse != null && virtualMouse.added) InputSystem.RemoveDevice(virtualMouse);
            if (virtualKeyboard != null && virtualKeyboard.added) InputSystem.RemoveDevice(virtualKeyboard);
            if (hadMute) PlayerPrefs.SetInt("ThePrimeGame.Muted", previousMute);
            else PlayerPrefs.DeleteKey("ThePrimeGame.Muted");
            if (hadMotion) PlayerPrefs.SetInt("ThePrimeGame.ReducedMotion", previousMotion);
            else PlayerPrefs.DeleteKey("ThePrimeGame.ReducedMotion");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator SceneHasOriginalArtFourPiecesAndSilentStartup()
        {
            Assert.That(game.Hud.Pieces.Length, Is.EqualTo(4));
            Assert.That(game.Hud.Targets.Length, Is.EqualTo(4));
            Assert.That(game.Audio.MusicStarted, Is.False);
            Assert.That(game.Session.PlacedCount, Is.Zero);
            foreach (var piece in game.Hud.Pieces) Assert.That(piece.IsPlaced, Is.False);
            yield return Capture("01-inicio.png");
        }

        [UnityTest]
        public IEnumerator ClickThenClickWorksWithoutDragging()
        {
            Click(game.Hud.Pieces[0].gameObject);
            Assert.That(game.Selected, Is.EqualTo(game.Hud.Pieces[0]));
            Click(game.Hud.Targets[2].gameObject);
            Assert.That(game.Session.PlacedCount, Is.Zero);
            Click(game.Hud.Targets[0].gameObject);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(game.Session.PlacedCount, Is.EqualTo(1));
            Assert.That(game.Hud.Pieces[0].Rect.parent, Is.EqualTo(game.Hud.Targets[0].transform));
            Assert.That(game.Hud.Pieces[0].Rect.sizeDelta, Is.EqualTo(game.Hud.Targets[0].Rect.sizeDelta));
        }

        [UnityTest]
        public IEnumerator KeyboardSubmitPlacesASelectedPiece()
        {
            var data = new BaseEventData(EventSystem.current);
            ExecuteEvents.Execute(game.Hud.Pieces[3].gameObject, data, ExecuteEvents.submitHandler);
            ExecuteEvents.Execute(game.Hud.Targets[3].gameObject, data, ExecuteEvents.submitHandler);
            yield return null;
            Assert.That(game.Session.IsPlaced(PieceId.Coral), Is.True);
        }

        [UnityTest]
        public IEnumerator RealInputSystemMouseRoutesClicksThroughTheCanvas()
        {
            virtualMouse = InputSystem.AddDevice<Mouse>();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, game.Hud.Pieces[0].Rect.position);
            yield return MouseClick(start);
            Assert.That(game.Selected, Is.EqualTo(game.Hud.Pieces[0]));
            Vector2 destination = RectTransformUtility.WorldToScreenPoint(null, game.Hud.Targets[0].Rect.position);
            yield return MouseClick(destination);
            Assert.That(game.Session.IsPlaced(PieceId.Blue), Is.True);
        }

        [UnityTest]
        public IEnumerator RealKeyboardSpaceSelectsAndPlacesAFocusedPiece()
        {
            virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            yield return KeyPress(Key.Digit4);
            Assert.That(game.Selected, Is.EqualTo(game.Hud.Pieces[3]));
            game.ClearSelection();
            EventSystem.current.SetSelectedGameObject(game.Hud.Pieces[3].gameObject);
            yield return KeyPress(Key.Space);
            Assert.That(game.Selected, Is.EqualTo(game.Hud.Pieces[3]));
            EventSystem.current.SetSelectedGameObject(game.Hud.Targets[3].gameObject);
            yield return KeyPress(Key.Space);
            Assert.That(game.Session.IsPlaced(PieceId.Coral), Is.True);
        }

        [UnityTest]
        public IEnumerator RealInputSystemDragReturnsFromWrongTargetThenSnapsCorrectly()
        {
            virtualMouse = InputSystem.AddDevice<Mouse>();
            var piece = game.Hud.Pieces[1];
            Transform origin = piece.transform.parent;
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, piece.Rect.position);
            Vector2 wrong = RectTransformUtility.WorldToScreenPoint(null, game.Hud.Targets[2].Rect.position);
            yield return MouseDrag(start, wrong);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(game.Session.PlacedCount, Is.Zero);
            Assert.That(piece.transform.parent, Is.EqualTo(origin));
            Assert.That(piece.IsReturning, Is.False);
            Vector2 correct = RectTransformUtility.WorldToScreenPoint(null, game.Hud.Targets[1].Rect.position);
            yield return MouseDrag(start, correct);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(game.Session.PlacedCount, Is.EqualTo(1));
            Assert.That(piece.transform.parent, Is.EqualTo(game.Hud.Targets[1].transform));
            Assert.That(game.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator CancellingRestartPreservesProgressAndBlocksBoardInput()
        {
            game.SelectPiece(game.Hud.Pieces[0]);
            game.PlaceSelected(game.Hud.Targets[0]);
            game.RequestRestart();
            Assert.That(game.InputLocked, Is.True);
            game.SelectPiece(game.Hud.Pieces[1]);
            Assert.That(game.Selected, Is.Null);
            Assert.That(game.TryPlace(game.Hud.Pieces[1], game.Hud.Targets[1]), Is.False);
            game.Hud.HideModal();
            Assert.That(game.InputLocked, Is.False);
            Assert.That(game.Session.PlacedCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingToTheCorrectDestinationCountsOnePiece()
        {
            var piece = game.Hud.Pieces[1];
            var target = game.Hud.Targets[1];
            var data = new PointerEventData(EventSystem.current)
            {
                pointerDrag = piece.gameObject,
                position = RectTransformUtility.WorldToScreenPoint(null, piece.Rect.position),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(piece.gameObject, data, ExecuteEvents.beginDragHandler);
            data.position = RectTransformUtility.WorldToScreenPoint(null, target.Rect.position);
            data.pointerCurrentRaycast = new RaycastResult { gameObject = target.gameObject };
            ExecuteEvents.Execute(piece.gameObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(piece.gameObject, data, ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(game.Session.PlacedCount, Is.EqualTo(1));
            Assert.That(piece.IsPlaced, Is.True);
            Assert.That(game.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator FailedDragReturnsHomeAndRestartCancelsItsAnimation()
        {
            var piece = game.Hud.Pieces[0];
            Transform origin = piece.transform.parent;
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, piece.Rect.position),
                button = PointerEventData.InputButton.Left
            };
            piece.OnBeginDrag(data);
            data.position = new Vector2(10, 10);
            piece.OnDrag(data);
            piece.OnEndDrag(data);
            Assert.That(game.Session.PlacedCount, Is.Zero);
            game.Restart();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(piece.IsReturning, Is.False);
            Assert.That(piece.transform.parent, Is.EqualTo(origin));
            Assert.That(piece.Rect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(game.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator VictoryAndReplayAreFunctional()
        {
            for (int i = 0; i < 4; i++)
            {
                game.SelectPiece(game.Hud.Pieces[i]);
                game.PlaceSelected(game.Hud.Targets[i]);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(game.Session.IsComplete, Is.True);
            Assert.That(game.Hud.VictoryVisible, Is.True);
            yield return Capture("02-victoria.png");
            game.Restart();
            yield return null;
            Assert.That(game.Session.PlacedCount, Is.Zero);
            Assert.That(game.Hud.VictoryVisible, Is.False);
            foreach (var piece in game.Hud.Pieces) Assert.That(piece.IsPlaced, Is.False);
        }

        [UnityTest]
        public IEnumerator MuteAndReducedMotionPersistAcrossSceneReload()
        {
            game.ToggleSound();
            game.ToggleMotion();
            Assert.That(game.Audio.IsMuted, Is.True);
            foreach (var source in game.GetComponents<AudioSource>()) Assert.That(source.mute, Is.True);
            yield return SceneManager.LoadSceneAsync("Level1");
            yield return null;
            game = Object.FindFirstObjectByType<GameManager>();
            Assert.That(game.Audio.IsMuted, Is.True);
            Assert.That(game.ReducedMotion, Is.True);
            game.SelectPiece(game.Hud.Pieces[0]);
            game.PlaceSelected(game.Hud.Targets[0]);
            Assert.That(game.Hud.Pieces[0].Rect.parent, Is.EqualTo(game.Hud.Targets[0].transform));
        }

        [UnityTest]
        public IEnumerator AudioDescriptionsRemainAvailableWhenMutedAndAtVictory()
        {
            game.ToggleSound();
            Assert.That(game.Hud.AudioDescription, Is.EqualTo(GameText.Get("audioMuted")));
            game.SelectPiece(game.Hud.Pieces[0]);
            Assert.That(game.Hud.AudioDescription, Is.EqualTo(GameText.Get("audioMutedCue", GameText.Get("audioPickup"))));
            game.PlaceSelected(game.Hud.Targets[0]);
            Assert.That(game.Hud.AudioDescription, Is.EqualTo(GameText.Get("audioMutedCue", GameText.Get("audioSnap"))));
            for (int i = 1; i < 4; i++)
            {
                game.SelectPiece(game.Hud.Pieces[i]);
                game.PlaceSelected(game.Hud.Targets[i]);
            }
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(game.Hud.VictoryVisible, Is.True);
            string description = GameText.Get("audioMutedCue", GameText.Get("audioSuccess"));
            Assert.That(game.Hud.AudioDescription, Is.EqualTo(description));
            var panel = game.Hud.Canvas.transform.Find("Victory Overlay/Victory Panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(System.Array.Exists(panel.GetComponentsInChildren<Text>(), label => label.text == description), Is.True);
            foreach (var source in game.GetComponents<AudioSource>()) Assert.That(source.mute, Is.True);
        }

        [UnityTest]
        public IEnumerator StandardControlsExposeVisibleKeyboardFocus()
        {
            foreach (var button in game.Hud.Canvas.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<DraggableBlock>() != null || button.GetComponent<DropTarget>() != null) continue;
                Assert.That(button.GetComponent<FocusIndicator>(), Is.Not.Null, button.name);
            }
            var help = game.Hud.Canvas.transform.Find("Help Button");
            var motion = game.Hud.Canvas.transform.Find("Motion Button");
            EventSystem.current.SetSelectedGameObject(help.gameObject);
            yield return null;
            Assert.That(help.GetComponent<FocusIndicator>().IsVisible, Is.True);
            EventSystem.current.SetSelectedGameObject(motion.gameObject);
            yield return null;
            Assert.That(motion.GetComponent<FocusIndicator>().IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator EnglishObjectNamesAreIndependentOfLocalizedPlayerText()
        {
            Assert.That(game.Hud.Pieces[0].name, Is.EqualTo("Piece 1 Blue"));
            Assert.That(game.Hud.Targets[0].name, Is.EqualTo("Target 1"));
            foreach (var item in game.Hud.Canvas.GetComponentsInChildren<Transform>(true))
                foreach (char character in item.name) Assert.That((int)character, Is.LessThan(128), item.name);
            Assert.That(System.Array.Exists(game.Hud.Canvas.GetComponentsInChildren<Text>(),
                label => label.text == GameText.Get("heading")), Is.True);
            yield return null;
        }

        private static void Click(GameObject target)
        {
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>Distributes pointer states over frames so the UI module processes a real press and release sequence.</summary>
        private IEnumerator MouseClick(Vector2 position)
        {
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = position });
            yield return null;
            yield return null;
        }

        /// <summary>Feeds a virtual keyboard through the Input System instead of bypassing the input module.</summary>
        private IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(virtualKeyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(virtualKeyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        /// <summary>Keeps the button held over several movement frames to exercise drag thresholds and target raycasts.</summary>
        private IEnumerator MouseDrag(Vector2 from, Vector2 to)
        {
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = from });
            yield return null;
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = from }.WithButton(MouseButton.Left));
            yield return null;
            for (int i = 1; i <= 8; i++)
            {
                InputSystem.QueueStateEvent(virtualMouse,
                    new MouseState { position = Vector2.Lerp(from, to, i / 8f) }.WithButton(MouseButton.Left));
                yield return null;
            }
            InputSystem.QueueStateEvent(virtualMouse, new MouseState { position = to });
            yield return null;
            yield return null;
        }

        /// <summary>Exports the rendered game as test evidence without capturing desktop or account information.</summary>
        private static IEnumerator Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults"));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name));
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }
}
