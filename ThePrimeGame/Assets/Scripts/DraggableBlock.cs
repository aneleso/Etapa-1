using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThePrimeGame
{
    /// <summary>Provides pointer dragging and keyboard selection while keeping each piece anchored to a known home.</summary>
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
        private Rigidbody2D dragBody;
        private BoxCollider2D dragCollider;
        internal BoxCollider2D DragCollider => dragCollider;
        private Vector3 grabOffset;
        private Coroutine motion;
        private bool focused;
        private bool selected;
        private bool hovered;
        private int dragEndedFrame = -1;

        /// <summary>Caches the home slot, input components and outline used by later animation and reset operations.</summary>
        public void Initialize(GameManager manager, PieceId id, RectTransform homeSlot, Image focusOutline)
        {
            Manager = manager;
            Id = id;
            home = homeSlot;
            homeSize = Rect.sizeDelta;
            outline = focusOutline;
            group = gameObject.AddComponent<CanvasGroup>();
            button = GetComponent<Button>();
            dragBody = gameObject.AddComponent<Rigidbody2D>();
            dragBody.bodyType = RigidbodyType2D.Kinematic;
            dragBody.useFullKinematicContacts = true;
            dragBody.simulated = false;
            dragCollider = gameObject.AddComponent<BoxCollider2D>();
            dragCollider.enabled = false;
            RefreshOutline();
        }

        /// <summary>Suppresses the click generated at the end of a drag to avoid selecting a piece twice.</summary>
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && Time.frameCount != dragEndedFrame)
                Choose();
        }

        public void OnSubmit(BaseEventData data) => Choose();
        /// <summary>Rejects locked, moving or modal-blocked pieces before forwarding selection to the manager.</summary>
        private void Choose()
        {
            if (!IsPlaced && !IsReturning && !Manager.InputLocked) Manager.SelectPiece(this);
        }

        /// <summary>Starts one drag at a time and lets raycasts pass through the piece to the destination below it.</summary>
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
            dragCollider.size = Rect.rect.size;
            dragCollider.enabled = true;
            dragBody.simulated = true;
        }

        /// <summary>Preserves the initial grab offset so the block does not jump to its center under the pointer.</summary>
        public void OnDrag(PointerEventData data)
        {
            if (IsDragging && !IsPlaced) Rect.position = ScreenWorld(data) + grabOffset;
        }

        /// <summary>Synchronizes the final drag position, then validates the overlap before committing the drop.</summary>
        public void OnEndDrag(PointerEventData data)
        {
            if (!IsDragging) return;
            OnDrag(data);
            IsDragging = false;
            Manager.IsDragging = false;
            dragEndedFrame = Time.frameCount;
            // Release can happen before the next fixed update, so trigger events alone are not enough.
            Physics2D.SyncTransforms();
            var hit = data.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<DropTarget>() : null;
            if (target != null ? target.Overlaps(this) && Manager.TryPlace(this, target)
                : Manager.TrySnapNearby(this)) return;
            ReturnHome();
        }

        /// <summary>Locks the piece before animation so repeated drop or click events cannot place it again.</summary>
        public void Dock(DropTarget target, System.Action onSettled)
        {
            StopMotion();
            StopDragCollisions();
            IsPlaced = true;
            IsDragging = false;
            IsReturning = false;
            group.blocksRaycasts = false;
            button.interactable = false;
            SetSelected(false);
            Rect.SetParent(Manager.Hud.DragLayer, true);
            motion = StartCoroutine(MoveTo(target.Rect, true, onSettled));
        }

        /// <summary>Returns an unsuccessful drop without an error sound or a progress penalty.</summary>
        public void ReturnHome()
        {
            StopMotion();
            StopDragCollisions();
            IsDragging = false;
            IsReturning = true;
            Manager.IsDragging = false;
            Manager.ClearSelection();
            group.blocksRaycasts = false;
            Rect.SetParent(Manager.Hud.DragLayer, true);
            motion = StartCoroutine(MoveTo(home, false));
        }

        /// <summary>Cancels in-flight movement before restoring every input and transform state for replay.</summary>
        public void ResetToHome()
        {
            StopMotion();
            StopDragCollisions();
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

        /// <summary>Updates the future home size during window resizing without disrupting an active drag.</summary>
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

        /// <summary>Converts screen coordinates into the overlay canvas coordinate system for mouse or touch.</summary>
        private Vector3 ScreenWorld(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Manager.Hud.DragLayer,
                data.position, data.pressEventCamera, out var local);
            return Manager.Hud.DragLayer.TransformPoint(local);
        }

        /// <summary>Animates with unscaled time and recalculates sizes while resizing; reduced motion completes instantly.</summary>
        private IEnumerator MoveTo(RectTransform destination, bool placed, System.Action onSettled = null)
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
            onSettled?.Invoke();
        }

        /// <summary>Stops old coroutines before another placement or restart can change the same transform.</summary>
        private void StopMotion()
        {
            if (motion != null) StopCoroutine(motion);
            motion = null;
        }

        private void StopDragCollisions()
        {
            dragCollider.enabled = false;
            // Let the canvas own the transform again during the return or docking animation.
            dragBody.simulated = false;
            // Disabling a collider can end a contact without delivering an exit to its counterpart.
            foreach (var target in Manager.Hud.Targets) target.ClearDragPreview(this);
        }
    }
}
