using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThePrimeGame
{
    /// <summary>Draws optional guidance and placement effects above the pieces without intercepting input.</summary>
    internal sealed class BoardFeedback : MonoBehaviour
    {
        private const string ResourceFolder = "Feedback/";
        private const int FramesPerSecond = 12;
        private const float ApproachDuration = 0.24f;
        private const float TapDuration = 4f / FramesPerSecond;
        private const float RestDuration = 0.20f;

        private Image guide;
        private Image[] snapEffects;
        private Sprite idlePose;
        private Sprite tapPose;
        private Sprite[] snapFrames;
        private Coroutine guideMotion;

        public void Initialize(int targetCount)
        {
            idlePose = Resources.Load<Sprite>(ResourceFolder + "hand_guide");
            tapPose = Resources.Load<Sprite>(ResourceFolder + "hand_guide_tap");
            snapFrames = Resources.LoadAll<Sprite>(ResourceFolder + "vfx_snap_sheet");
            // Resource enumeration order is unspecified; the slice names follow the strip from left to right.
            Array.Sort(snapFrames, (first, second) => string.CompareOrdinal(first.name, second.name));

            snapEffects = new Image[targetCount];
            for (int i = 0; i < snapEffects.Length; i++)
                snapEffects[i] = CreateImage("Snap Effect " + (i + 1));

            guide = CreateImage("Guide Hand");
            if (idlePose != null)
            {
                // The two poses use the same fingertip pivot and dimensions, so swapping them keeps the tip fixed.
                guide.rectTransform.pivot = idlePose.pivot / idlePose.rect.size;
            }
        }

        public void ShowGuide(DropTarget target)
        {
            ClearGuide();
            if (idlePose != null && tapPose != null)
                guideMotion = StartCoroutine(AnimateGuide(target.Rect));
        }

        public void ClearGuide()
        {
            if (guideMotion != null) StopCoroutine(guideMotion);
            guideMotion = null;
            guide.enabled = false;
        }

        public void PlaySnap(DropTarget target)
        {
            if (snapFrames.Length > 0)
                StartCoroutine(AnimateSnap(snapEffects[(int)target.Id], target.Rect));
        }

        public bool IsSnapPlaying(DropTarget target) => snapEffects[(int)target.Id].enabled;

        public void Clear()
        {
            StopAllCoroutines();
            ClearGuide();
            foreach (var effect in snapEffects) effect.enabled = false;
        }

        private IEnumerator AnimateGuide(RectTransform target)
        {
            double start = Time.unscaledTimeAsDouble;
            guide.enabled = true;
            while (Time.unscaledTimeAsDouble - start < ApproachDuration + TapDuration + RestDuration)
            {
                float elapsed = (float)(Time.unscaledTimeAsDouble - start);
                bool tapping = elapsed >= ApproachDuration && elapsed < ApproachDuration + TapDuration;
                guide.sprite = tapping ? tapPose : idlePose;
                float approach = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(elapsed / ApproachDuration));
                var rect = target.rect;
                // Point just inside the upper-left edge, leaving the destination number readable.
                var tip = new Vector3(rect.xMin + rect.width * 0.22f, rect.yMax - rect.height * 0.18f + 14f * approach);
                Position(guide, target, tip, Mathf.Clamp(rect.height * 0.9f, 60f, 108f));
                yield return null;
            }
            guide.enabled = false;
            guideMotion = null;
        }

        private IEnumerator AnimateSnap(Image effect, RectTransform target)
        {
            double start = Time.unscaledTimeAsDouble;
            effect.enabled = true;
            // Derive the frame from real elapsed time so the sequence keeps its rate on slower displays.
            int frame = 0;
            while (frame < snapFrames.Length)
            {
                effect.sprite = snapFrames[frame];
                Position(effect, target, target.rect.center, Mathf.Max(target.rect.width, target.rect.height) * 1.16f);
                yield return null;
                frame = (int)((Time.unscaledTimeAsDouble - start) * FramesPerSecond);
            }
            effect.enabled = false;
        }

        private Image CreateImage(string objectName)
        {
            var image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(transform, false);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void Position(Image image, RectTransform target, Vector3 localPoint, float size)
        {
            image.rectTransform.sizeDelta = Vector2.one * size;
            image.rectTransform.position = target.TransformPoint(localPoint);
        }
    }
}
