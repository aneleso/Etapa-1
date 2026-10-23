using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThePrimeGame
{
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
        }

        public void SelectPiece(DraggableBlock piece)
        {
            if (InputLocked || piece.IsPlaced || piece.IsReturning || Session.IsComplete) return;
            if (Selected != piece) Audio.Pickup();
            ClearSelection();
            Selected = piece;
            piece.SetSelected(true);
            Hud.Message($"Pieza {PrimeGameAssets.Number(piece.Id)}: elige el destino con el mismo número.");
        }

        public void ClearSelection()
        {
            if (Selected != null) Selected.SetSelected(false);
            Selected = null;
            foreach (var target in Hud.Targets) target.SetHint(false);
        }

        public void PlaceSelected(DropTarget target)
        {
            if (InputLocked || IsDragging) return;
            if (Selected == null)
            {
                Hud.Message("Primero elige una pieza de tu bandeja.");
                return;
            }
            TryPlace(Selected, target);
        }

        public bool TryPlace(DraggableBlock piece, DropTarget target)
        {
            if (InputLocked || piece == null || target == null || piece.IsReturning) return false;
            var result = Session.TryPlace(piece.Id, target.Id);
            if (result == PlacementResult.AlreadyPlaced) return piece.IsPlaced;
            if (result != PlacementResult.Placed)
            {
                Hud.Message(result == PlacementResult.RequiresLowerPiece
                    ? "Empieza por la pieza de abajo. Puedes usar Ayuda."
                    : $"Busca el destino {PrimeGameAssets.Number(piece.Id)} para esta pieza.");
                return false;
            }
            piece.Dock(target);
            target.SetOccupied(true);
            IsDragging = false;
            ClearSelection();
            Audio.Snap();
            Hud.UpdateProgress(Session.PlacedCount);
            Hud.Message(Session.IsComplete ? "¡Lo construiste!" : "¡Encaja! Elige otra pieza cuando quieras.");
            if (Session.IsComplete) celebration = StartCoroutine(Celebrate());
            return true;
        }

        public bool TrySnapNearby(DraggableBlock piece)
        {
            var target = Hud.Targets[(int)piece.Id];
            float distance = Vector2.Distance(piece.Rect.position, target.Rect.position) / Hud.Canvas.scaleFactor;
            return distance <= 72f && TryPlace(piece, target);
        }

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
            Hud.Message($"La pieza {PrimeGameAssets.Number(piece.Id)} va en el marco resaltado.");
        }

        public void ToggleSound()
        {
            Audio.Toggle();
            Hud.UpdateSound(Audio.IsMuted);
        }

        public void ToggleMotion()
        {
            ReducedMotion = !ReducedMotion;
            PlayerPrefs.SetInt(MotionKey, ReducedMotion ? 1 : 0);
            PlayerPrefs.Save();
            Hud.UpdateMotion(ReducedMotion);
        }

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
            Hud.Message("Elige una pieza. Aquí puedes jugar a tu ritmo.");
            EventSystem.current?.SetSelectedGameObject(Hud.Pieces[0].gameObject);
        }

        private IEnumerator Celebrate()
        {
            yield return new WaitForSecondsRealtime(ReducedMotion ? 0f : 0.3f);
            Audio.Success();
            Hud.ShowVictory();
            celebration = null;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused || Hud == null) return;
            foreach (var piece in Hud.Pieces)
                if (piece.IsDragging) piece.ReturnHome();
        }
    }
}
