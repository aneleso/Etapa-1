using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThePrimeGame
{
    public sealed class DraggableBlock : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IPointerClickHandler, ISubmitHandler, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public PieceId Id { get; private set; }
        public bool IsPlaced { get; private set; }
        public bool IsReturning { get; private set; }
        public bool IsDragging { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public GameManager Manager { get; private set; }
        private RectTransform home;
        private Vector2 homeSize;
        private CanvasGroup group;
        private Button button;
        private Image outline;
        private Vector3 grabOffset;
        private Coroutine motion;
        private bool focused;
        private bool selected;
        private bool hovered;
        private int dragEndedFrame = -1;

        public void Initialize(GameManager manager, PieceId id, RectTransform homeSlot, Image focusOutline)
        {
            Manager = manager;
            Id = id;
            home = homeSlot;
            homeSize = Rect.sizeDelta;
            outline = focusOutline;
            group = gameObject.AddComponent<CanvasGroup>();
            button = GetComponent<Button>();
            RefreshOutline();
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && Time.frameCount != dragEndedFrame)
                Choose();
        }

        public void OnSubmit(BaseEventData data) => Choose();
        private void Choose()
        {
            if (!IsPlaced && !IsReturning && !Manager.InputLocked) Manager.SelectPiece(this);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || IsPlaced || IsReturning || Manager.InputLocked || Manager.IsDragging) return;
            StopMotion();
            Manager.SelectPiece(this);
            IsDragging = true;
            Manager.IsDragging = true;
            Rect.SetParent(Manager.Hud.DragLayer, true);
            Rect.SetAsLastSibling();
            grabOffset = Rect.position - ScreenWorld(data);
            group.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData data)
        {
            if (IsDragging && !IsPlaced) Rect.position = ScreenWorld(data) + grabOffset;
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!IsDragging && !IsPlaced) return;
            IsDragging = false;
            Manager.IsDragging = false;
            dragEndedFrame = Time.frameCount;
            if (IsPlaced) return;
            var hit = data.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<DropTarget>() : null;
            if (target != null && Manager.TryPlace(this, target)) return;
            if (target == null && Manager.TrySnapNearby(this)) return;
            ReturnHome();
        }

        public void Dock(DropTarget target)
        {
            StopMotion();
            IsPlaced = true;
            IsDragging = false;
            IsReturning = false;
            group.blocksRaycasts = false;
            button.interactable = false;
            SetSelected(false);
            Rect.SetParent(Manager.Hud.DragLayer, true);
            motion = StartCoroutine(MoveTo(target.Rect, true));
        }

        public void ReturnHome()
        {
            StopMotion();
            IsDragging = false;
            IsReturning = true;
            Manager.IsDragging = false;
            Manager.ClearSelection();
            group.blocksRaycasts = false;
            Rect.SetParent(Manager.Hud.DragLayer, true);
            motion = StartCoroutine(MoveTo(home, false));
        }

        public void ResetToHome()
        {
            StopMotion();
            IsPlaced = IsReturning = IsDragging = selected = focused = hovered = false;
            button.interactable = true;
            group.blocksRaycasts = true;
            Rect.SetParent(home, false);
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = Vector2.zero;
            Rect.sizeDelta = homeSize;
            Rect.localScale = Vector3.one;
            RefreshOutline();
        }

        public void SetHomeSize(Vector2 size)
        {
            homeSize = size;
            if (!IsPlaced && !IsDragging && !IsReturning) Rect.sizeDelta = size;
        }

        public void OnSelect(BaseEventData data) { focused = true; RefreshOutline(); }
        public void OnDeselect(BaseEventData data) { focused = false; RefreshOutline(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; RefreshOutline(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; RefreshOutline(); }
        public void SetSelected(bool value) { selected = value; RefreshOutline(); }
        private void RefreshOutline() { if (outline != null) outline.enabled = !IsPlaced && (focused || selected || hovered); }

        private Vector3 ScreenWorld(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Manager.Hud.DragLayer,
                data.position, data.pressEventCamera, out var local);
            return Manager.Hud.DragLayer.TransformPoint(local);
        }

        private IEnumerator MoveTo(RectTransform destination, bool placed)
        {
            Vector3 start = Rect.position;
            Vector2 startSize = Rect.sizeDelta;
            float duration = Manager.ReducedMotion ? 0f : (placed ? 0.22f : 0.3f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                Rect.position = Vector3.Lerp(start, destination.position, t);
                Rect.sizeDelta = Vector2.Lerp(startSize, placed ? destination.sizeDelta : homeSize, t);
                yield return null;
            }
            Rect.SetParent(destination, false);
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = Vector2.zero;
            Rect.sizeDelta = placed ? destination.sizeDelta : homeSize;
            Rect.localScale = Vector3.one;
            IsReturning = false;
            group.blocksRaycasts = !placed;
            motion = null;
        }

        private void StopMotion()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
        }
    }
}
