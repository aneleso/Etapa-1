using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThePrimeGame
{
    /// <summary>Shows a high-contrast border for pointer hover and keyboard focus on standard buttons.</summary>
    public sealed class FocusIndicator : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private Image border;
        private Selectable selectable;
        private bool focused;
        private bool hovered;
        public bool IsVisible => border != null && border.enabled;

        /// <summary>Connects the visual border without changing the control's hit area or layout.</summary>
        public void Initialize(Image image)
        {
            border = image;
            selectable = GetComponent<Selectable>();
            Refresh();
        }

        public void OnSelect(BaseEventData data) { focused = true; Refresh(); }
        public void OnDeselect(BaseEventData data) { focused = false; Refresh(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }

        private void OnDisable()
        {
            // Closing a dialog must not leave its previous hover or focus state active when it reopens.
            focused = hovered = false;
            Refresh();
        }

        private void Refresh()
        {
            if (border == null) return;
            border.enabled = selectable != null && selectable.IsInteractable() && (focused || hovered);
        }
    }
}
