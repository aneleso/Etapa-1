using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ThePrimeGame.Tests
{
    /// <summary>Checks the actual shared palette against WCAG relative-luminance contrast thresholds.</summary>
    public sealed class AccessibilityTests
    {
        private static IEnumerable<TestCaseData> TextPairs()
        {
            yield return new TestCaseData(GamePalette.Ink, GamePalette.Background).SetName("BodyTextOnBackground");
            yield return new TestCaseData(GamePalette.MutedInk, Color.white).SetName("TargetNumbersOnWhite");
            yield return new TestCaseData(GamePalette.Accent, GamePalette.Background).SetName("StatusTextOnBackground");
            yield return new TestCaseData(GamePalette.Ink, GamePalette.Tray).SetName("TrayTextOnSurface");
            yield return new TestCaseData(GamePalette.Ink, GamePalette.ButtonSurface).SetName("ButtonTextAtRest");
            yield return new TestCaseData(GamePalette.Ink, GamePalette.ButtonSurface * GamePalette.SelectedTint).SetName("ButtonTextWhenSelected");
            yield return new TestCaseData(GamePalette.Ink, GamePalette.ButtonSurface * GamePalette.PressedTint).SetName("ButtonTextWhenPressed");
        }

        [TestCaseSource(nameof(TextPairs))]
        public void NormalTextHasAtLeastFourPointFiveContrast(Color foreground, Color background)
        {
            Assert.That(Contrast(foreground, background), Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void FocusAndTargetBoundariesHaveAtLeastThreeContrast()
        {
            // Focus rings sit outside controls, against these surrounding interface surfaces.
            foreach (Color surface in new[] { Color.white, GamePalette.Background, GamePalette.Tray })
            {
                Assert.That(Contrast(GamePalette.Accent, surface), Is.GreaterThanOrEqualTo(3f));
                Assert.That(Contrast(GamePalette.MutedInk, surface), Is.GreaterThanOrEqualTo(3f));
            }
        }

        [Test]
        public void LocalizedInstructionsAndAudioDescriptionsAreAvailable()
        {
            Assert.That(GameText.Get("pieceSelected", 2), Does.Contain("2"));
            foreach (string key in new[] { "heading", "instruction", "audioReady", "audioPickup", "audioSnap", "audioSuccess" })
                Assert.That(GameText.Get(key), Is.Not.Empty);
            Assert.Throws<KeyNotFoundException>(() => GameText.Get("missing-test-key"));
        }

        /// <summary>Converts sRGB channels to linear light before calculating the WCAG contrast ratio.</summary>
        private static float Contrast(Color first, Color second)
        {
            float firstLuminance = Luminance(first);
            float secondLuminance = Luminance(second);
            return (Mathf.Max(firstLuminance, secondLuminance) + 0.05f)
                / (Mathf.Min(firstLuminance, secondLuminance) + 0.05f);
        }

        private static float Luminance(Color color) =>
            0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);

        private static float Linear(float channel) => channel <= 0.04045f
            ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
