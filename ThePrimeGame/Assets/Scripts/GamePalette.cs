using UnityEngine;

namespace ThePrimeGame
{
    /// <summary>Centralizes readable interface colors so contrast checks cover the colors used by the game.</summary>
    public static class GamePalette
    {
        public static readonly Color Ink = new Color32(39, 61, 73, 255);
        public static readonly Color MutedInk = new Color32(72, 91, 101, 255);
        public static readonly Color Accent = new Color32(36, 100, 111, 255);
        public static readonly Color Background = new Color32(250, 248, 243, 255);
        public static readonly Color Tray = new Color32(238, 243, 240, 255);
        public static readonly Color ButtonSurface = new Color32(222, 236, 231, 255);
        public static readonly Color SelectedTint = new Color32(185, 218, 215, 255);
        public static readonly Color PressedTint = new Color32(168, 205, 202, 255);
    }
}
