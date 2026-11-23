using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThePrimeGame
{
    /// <summary>Represents a numbered destination that accepts drag, click and keyboard-submit interaction.</summary>
    public sealed class DropTarget : MonoBehaviour, IPointerClickHandler,
        ISubmitHandler, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public PieceId Id { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public GameManager Manager { get; private set; }
        private Image outline;
        private Button button;
        private BoxCollider2D snapTrigger;
        private DraggableBlock overlappingPiece;
        private bool hinted;
        private bool focused;
        private bool occupied;
        private bool hovered;

        /// <summary>Connects the stable piece identity and focus border to the shared game manager.</summary>
        public void Initialize(GameManager manager, PieceId id, Image focusOutline)
        {
            Manager = manager;
            Id = id;
            outline = focusOutline;
            button = GetComponent<Button>();
            snapTrigger = gameObject.AddComponent<BoxCollider2D>();
            snapTrigger.isTrigger = true;
            RefreshOutline();
        }

        internal void UpdateTriggerSize()
        {
            snapTrigger.size = Rect.rect.size;
        }

        internal bool Overlaps(DraggableBlock piece)
        {
            var distance = snapTrigger.Distance(piece.DragCollider);
            return distance.isValid && distance.distance <= 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (occupied || Manager.InputLocked || !other.TryGetComponent(out DraggableBlock piece)
                || !piece.IsDragging || piece.Id != Id) return;
            overlappingPiece = piece;
            RefreshOutline();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out DraggableBlock piece)) ClearDragPreview(piece);
        }

        internal void ClearDragPreview(DraggableBlock piece)
        {
            if (overlappingPiece != piece) return;
            overlappingPiece = null;
            RefreshOutline();
        }

        /// <summary>Routes a pointer click through the same placement path used by keyboard submission.</summary>
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !occupied)
                Manager.PlaceSelected(this);
        }

        /// <summary>Provides a keyboard alternative to the drag gesture without duplicating placement rules.</summary>
        public void OnSubmit(BaseEventData data)
        {
            if (!occupied) Manager.PlaceSelected(this);
        }

        public void OnSelect(BaseEventData data) { focused = true; RefreshOutline(); }
        public void OnDeselect(BaseEventData data) { focused = false; RefreshOutline(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; RefreshOutline(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; RefreshOutline(); }
        public void SetHint(bool value) { hinted = value; RefreshOutline(); }
        /// <summary>Removes completed targets from keyboard navigation and clears obsolete hints.</summary>
        public void SetOccupied(bool value)
        {
            occupied = value;
            button.interactable = !value;
            hinted = false;
            RefreshOutline();
        }

        /// <summary>Combines hint, hover and focus states while keeping completed targets visually settled.</summary>
        private void RefreshOutline()
        {
            if (outline != null) outline.enabled = !occupied && (hinted || focused || hovered || overlappingPiece != null);
        }
    }
}
