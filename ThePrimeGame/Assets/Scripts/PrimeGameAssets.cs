using UnityEngine;

namespace ThePrimeGame
{
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

        public static string PieceName(PieceId id)
        {
            switch (id)
            {
                case PieceId.Blue: return "Azul";
                case PieceId.Mint: return "Menta";
                case PieceId.Cream: return "Crema";
                case PieceId.Coral: return "Coral";
                default: return "Pieza";
            }
        }

        public static int Number(PieceId id) => (int)id + 1;
    }
}
