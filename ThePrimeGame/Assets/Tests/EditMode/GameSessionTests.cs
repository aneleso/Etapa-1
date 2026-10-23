using System;
using NUnit.Framework;

namespace ThePrimeGame.Tests
{
    public sealed class GameSessionTests
    {
        [Test]
        public void WrongDestinationDoesNotChangeProgress()
        {
            var session = new GameSession();
            Assert.That(session.TryPlace(PieceId.Blue, PieceId.Cream), Is.EqualTo(PlacementResult.WrongTarget));
            Assert.That(session.PlacedCount, Is.Zero);
            Assert.That(session.IsPlaced(PieceId.Blue), Is.False);
        }

        [Test]
        public void RepeatedDropCountsOnlyOnce()
        {
            var session = new GameSession();
            Assert.That(session.TryPlace(PieceId.Blue, PieceId.Blue), Is.EqualTo(PlacementResult.Placed));
            Assert.That(session.TryPlace(PieceId.Blue, PieceId.Blue), Is.EqualTo(PlacementResult.AlreadyPlaced));
            Assert.That(session.PlacedCount, Is.EqualTo(1));
            Assert.That(session.IsComplete, Is.False);
        }

        [Test]
        public void FourDifferentPiecesCompleteTheLevelInAnyOrder()
        {
            var session = new GameSession();
            foreach (var id in new[] { PieceId.Coral, PieceId.Cream, PieceId.Blue, PieceId.Mint })
                Assert.That(session.TryPlace(id, id), Is.EqualTo(PlacementResult.Placed));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.PlacedCount, Is.EqualTo(4));
        }

        [Test]
        public void OptionalOrderRequiresTheLowerPiece()
        {
            var session = new GameSession { RequireBottomFirst = true };
            Assert.That(session.TryPlace(PieceId.Cream, PieceId.Cream), Is.EqualTo(PlacementResult.RequiresLowerPiece));
            Assert.That(session.TryPlace(PieceId.Mint, PieceId.Mint), Is.EqualTo(PlacementResult.RequiresLowerPiece));
            Assert.That(session.TryPlace(PieceId.Coral, PieceId.Coral), Is.EqualTo(PlacementResult.Placed));
            foreach (var id in new[] { PieceId.Blue, PieceId.Mint, PieceId.Cream })
                Assert.That(session.TryPlace(id, id), Is.EqualTo(PlacementResult.Placed));
            Assert.That(session.IsComplete, Is.True);
        }

        [Test]
        public void RestartClearsEveryPieceAndAllowsAnotherGame()
        {
            var session = new GameSession();
            foreach (PieceId id in Enum.GetValues(typeof(PieceId))) session.TryPlace(id, id);
            session.Reset();
            Assert.That(session.PlacedCount, Is.Zero);
            Assert.That(session.IsComplete, Is.False);
            foreach (PieceId id in Enum.GetValues(typeof(PieceId))) Assert.That(session.IsPlaced(id), Is.False);
            Assert.That(session.TryPlace(PieceId.Blue, PieceId.Blue), Is.EqualTo(PlacementResult.Placed));
        }

        [Test]
        public void InvalidIdsCannotCorruptTheSession()
        {
            var session = new GameSession();
            Assert.Throws<ArgumentOutOfRangeException>(() => session.TryPlace((PieceId)4, PieceId.Blue));
            Assert.That(session.PlacedCount, Is.Zero);
        }
    }
}
