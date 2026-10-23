using UnityEngine;

namespace ThePrimeGame
{
    /// <summary>Keeps supplied art and audio references separate from rules and localized display text.</summary>
    [CreateAssetMenu(menuName = "The PrimeGame/Art and audio")]
    public sealed class PrimeGameAssets : ScriptableObject
    {
        public Sprite blue;
        public Sprite mint;
        public Sprite cream;
        public Sprite coral;
        public Sprite silhouetteWide;
        public Sprite silhouetteSmall;
        public Sprite logo;
        public Sprite soundOn;
        public Sprite soundOff;
        public Sprite restart;
        public Sprite star;
        public AudioClip pickup;
        public AudioClip snap;
        public AudioClip success;
        public AudioClip ambient;

        /// <summary>Returns the original sprite for a stable piece identity rather than relying on hierarchy names.</summary>
        public Sprite Piece(PieceId id)
        {
            switch (id)
            {
                case PieceId.Blue: return blue;
                case PieceId.Mint: return mint;
                case PieceId.Cream: return cream;
                case PieceId.Coral: return coral;
                default: return null;
            }
        }

        /// <summary>Resolves a player-facing color label from localization data instead of embedding it in source.</summary>
        public static string PieceName(PieceId id)
        {
            switch (id)
            {
                case PieceId.Blue: return GameText.Get("blueName");
                case PieceId.Mint: return GameText.Get("mintName");
                case PieceId.Cream: return GameText.Get("creamName");
                case PieceId.Coral: return GameText.Get("coralName");
                default: return GameText.Get("fallbackPieceName");
            }
        }

        public static int Number(PieceId id) => (int)id + 1;
    }
}
