using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThePrimeGame
{
    public sealed class DropTarget : MonoBehaviour, IDropHandler, IPointerClickHandler,
        ISubmitHandler, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public PieceId Id { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public GameManager Manager { get; private set; }
        private Image outline;
        private Button button;
        private bool hinted;
        private bool focused;
        private bool occupied;
        private bool hovered;

        public void Initialize(GameManager manager, PieceId id, Image focusOutline)
        {
            Manager = manager;
            Id = id;
            outline = focusOutline;
            button = GetComponent<Button>();
            RefreshOutline();
        }

        public void OnDrop(PointerEventData data)
        {
            if (Manager.InputLocked || occupied) return;
            var piece = data.pointerDrag != null ? data.pointerDrag.GetComponent<DraggableBlock>() : null;
            if (piece != null) Manager.TryPlace(piece, this);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !occupied)
                Manager.PlaceSelected(this);
        }

        public void OnSubmit(BaseEventData data)
        {
            if (!occupied) Manager.PlaceSelected(this);
        }

        public void OnSelect(BaseEventData data) { focused = true; RefreshOutline(); }
        public void OnDeselect(BaseEventData data) { focused = false; RefreshOutline(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; RefreshOutline(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; RefreshOutline(); }
        public void SetHint(bool value) { hinted = value; RefreshOutline(); }
        public void SetOccupied(bool value)
        {
            occupied = value;
            button.interactable = !value;
            hinted = false;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (outline != null) outline.enabled = !occupied && (hinted || focused || hovered);
        }
    }
}
