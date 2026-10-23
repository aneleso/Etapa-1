using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThePrimeGame
{
    /// <summary>Coordinates the rules, interface, input feedback, sound and replay for a single level.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private PrimeGameAssets assets;
        [SerializeField] private bool requireBottomFirst;
        public GameSession Session { get; } = new GameSession();
        public GameHud Hud { get; private set; }
        public AudioToggle Audio { get; private set; }
        public DraggableBlock Selected { get; private set; }
        public bool IsDragging { get; set; }
        public bool InputLocked => Hud != null && Hud.ModalOpen;
        public bool ReducedMotion { get; private set; }
        private Coroutine celebration;
        private const string MotionKey = "ThePrimeGame.ReducedMotion";

        public void SetAssets(PrimeGameAssets value) => assets = value;

        /// <summary>Initializes saved preferences and builds the interface only when its resource bundle is assigned.</summary>
        private void Awake()
        {
            if (assets == null)
            {
                Debug.LogError("The PrimeGame art and audio asset is not assigned.", this);
                enabled = false;
                return;
            }
            Application.targetFrameRate = 60;
            ReducedMotion = PlayerPrefs.GetInt(MotionKey, 0) == 1;
            Session.RequireBottomFirst = requireBottomFirst;
            Audio = gameObject.AddComponent<AudioToggle>();
            Audio.Initialize(assets);
            Hud = gameObject.AddComponent<GameHud>();
            Hud.Initialize(this, assets);
            Audio.CueChanged += HandleAudioCue;
            HandleAudioCue(AudioCue.Ready);
        }

        /// <summary>Selects an available piece and gives the same instruction to pointer and keyboard players.</summary>
        public void SelectPiece(DraggableBlock piece)
        {
            if (InputLocked || piece.IsPlaced || piece.IsReturning || Session.IsComplete) return;
            if (Selected != piece) Audio.Pickup();
            ClearSelection();
            Selected = piece;
            piece.SetSelected(true);
            Hud.Message(GameText.Get("pieceSelected", PrimeGameAssets.Number(piece.Id)));
        }

        /// <summary>Removes selection and hints together so stale destinations cannot remain highlighted.</summary>
        public void ClearSelection()
        {
            if (Selected != null) Selected.SetSelected(false);
            Selected = null;
            foreach (var target in Hud.Targets) target.SetHint(false);
        }

        /// <summary>Offers a click-or-submit alternative to dragging, with a prompt when no piece is selected.</summary>
        public void PlaceSelected(DropTarget target)
        {
            if (InputLocked || IsDragging) return;
            if (Selected == null)
            {
                Hud.Message(GameText.Get("selectFirst"));
                return;
            }
            TryPlace(Selected, target);
        }

        /// <summary>Uses the session as the authority, then synchronizes the visual lock, progress and feedback.</summary>
        public bool TryPlace(DraggableBlock piece, DropTarget target)
        {
            if (InputLocked || piece == null || target == null || piece.IsReturning) return false;
            var result = Session.TryPlace(piece.Id, target.Id);
            if (result == PlacementResult.AlreadyPlaced) return piece.IsPlaced;
            if (result != PlacementResult.Placed)
            {
                Hud.Message(result == PlacementResult.RequiresLowerPiece
                    ? GameText.Get("lowerPieceRequired")
                    : GameText.Get("wrongTarget", PrimeGameAssets.Number(piece.Id)));
                return false;
            }
            piece.Dock(target);
            target.SetOccupied(true);
            IsDragging = false;
            ClearSelection();
            Audio.Snap();
            Hud.UpdateProgress(Session.PlacedCount);
            Hud.Message(Session.IsComplete ? GameText.Get("victoryTitle") : GameText.Get("piecePlaced"));
            if (Session.IsComplete) celebration = StartCoroutine(Celebrate());
            return true;
        }

        /// <summary>Measures tolerance in canvas units so display scaling does not change the difficulty of a drop.</summary>
        public bool TrySnapNearby(DraggableBlock piece)
        {
            var target = Hud.Targets[(int)piece.Id];
            float distance = Vector2.Distance(piece.Rect.position, target.Rect.position) / Hud.Canvas.scaleFactor;
            return distance <= 72f && TryPlace(piece, target);
        }

        /// <summary>Finds a remaining piece and highlights its matching destination without penalties.</summary>
        public void Hint()
        {
            if (InputLocked || IsDragging || Session.IsComplete) return;
            var piece = Selected;
            if (piece == null)
                foreach (var candidate in Hud.Pieces)
                    if (!candidate.IsPlaced && !candidate.IsReturning) { piece = candidate; break; }
            if (piece == null) return;
            SelectPiece(piece);
            Hud.Targets[(int)piece.Id].SetHint(true);
            Hud.Message(GameText.Get("hint", PrimeGameAssets.Number(piece.Id)));
        }

        /// <summary>Applies persistent source muting and updates the visible sound control.</summary>
        public void ToggleSound()
        {
            Audio.Toggle();
            Hud.UpdateSound(Audio.IsMuted);
        }

        /// <summary>Persists the reduced-motion choice without changing game rules or progress.</summary>
        public void ToggleMotion()
        {
            ReducedMotion = !ReducedMotion;
            PlayerPrefs.SetInt(MotionKey, ReducedMotion ? 1 : 0);
            PlayerPrefs.Save();
            Hud.UpdateMotion(ReducedMotion);
        }

        /// <summary>Protects partial progress with a confirmation dialog and ignores restart during a drag.</summary>
        public void RequestRestart()
        {
            if (IsDragging) return;
            if (Session.PlacedCount == 0 || Session.IsComplete) Restart();
            else Hud.ShowRestartConfirmation();
        }

        // Reset in place so restarting does not interrupt audio preferences or reload art.
        public void Restart()
        {
            if (celebration != null) StopCoroutine(celebration);
            celebration = null;
            ClearSelection();
            IsDragging = false;
            Session.Reset();
            Hud.HideModal();
            Hud.HideVictory();
            foreach (var piece in Hud.Pieces) piece.ResetToHome();
            foreach (var target in Hud.Targets) target.SetOccupied(false);
            Hud.UpdateProgress(0);
            Hud.Message(GameText.Get("initialStatus"));
            EventSystem.current?.SetSelectedGameObject(Hud.Pieces[0].gameObject);
        }

        /// <summary>Waits for the final snap before showing success; reduced motion removes that delay.</summary>
        private IEnumerator Celebrate()
        {
            yield return new WaitForSecondsRealtime(ReducedMotion ? 0f : 0.3f);
            Audio.Success();
            Hud.ShowVictory();
            celebration = null;
        }

        /// <summary>Returns an interrupted drag safely when the player switches away from the application.</summary>
        private void OnApplicationFocus(bool focused)
        {
            if (focused || Hud == null) return;
            foreach (var piece in Hud.Pieces)
                if (piece.IsDragging) piece.ReturnHome();
        }

        /// <summary>Routes each sound cue to text, including cues emitted while sound is muted.</summary>
        private void HandleAudioCue(AudioCue cue) => Hud.DescribeAudio(cue, Audio.IsMuted);

        private void OnDestroy()
        {
            if (Audio != null) Audio.CueChanged -= HandleAudioCue;
        }
    }
}
