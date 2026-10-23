using System;

namespace ThePrimeGame
{
    public enum PieceId { Blue, Mint, Cream, Coral }
    public enum PlacementResult { Placed, WrongTarget, AlreadyPlaced, RequiresLowerPiece }

    // The session owns progress so repeated pointer events cannot count a piece twice.
    public sealed class GameSession
    {
        private readonly bool[] placed = new bool[4];
        public int PlacedCount { get; private set; }
        public bool IsComplete => PlacedCount == placed.Length;
        public bool RequireBottomFirst { get; set; }

        public bool IsPlaced(PieceId id) => placed[CheckedIndex(id)];

        public PlacementResult TryPlace(PieceId piece, PieceId target)
        {
            int index = CheckedIndex(piece);
            CheckedIndex(target);
            if (placed[index]) return PlacementResult.AlreadyPlaced;
            if (piece != target) return PlacementResult.WrongTarget;
            if (RequireBottomFirst && piece == PieceId.Mint && !IsPlaced(PieceId.Blue))
                return PlacementResult.RequiresLowerPiece;
            if (RequireBottomFirst && piece == PieceId.Cream && !IsPlaced(PieceId.Mint))
                return PlacementResult.RequiresLowerPiece;
            placed[index] = true;
            PlacedCount++;
            return PlacementResult.Placed;
        }

        public void Reset()
        {
            Array.Clear(placed, 0, placed.Length);
            PlacedCount = 0;
        }

        private static int CheckedIndex(PieceId id)
        {
            int index = (int)id;
            if (index < 0 || index >= 4) throw new ArgumentOutOfRangeException(nameof(id));
            return index;
        }
    }
}
