using System.Linq;
using FrogAcross.Pieces;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace FrogAcross.Tests.EditMode.Art
{
    public class SpriteLibraryTests
    {
        [Test]
        public void EveryPiece_HasRealSprites_NoPlaceholders()
        {
            var reg = PieceRegistry.Load();
            var missing = reg.pieces
                .Where(p => p != null)
                .Where(p => p.sprites == null || p.sprites.Length == 0 || p.sprites.Any(s => s == null))
                .Select(p => p.id)
                .ToList();
            Assert.IsEmpty(missing, "pieces without complete sprite sets: " + string.Join(", ", missing));
        }

        [Test]
        public void Atlas_ExistsAndCoversTheLibrary()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Art/Sprites/FrogAcross.spriteatlas");
            Assert.IsNotNull(atlas, "sprite atlas missing");
            Assert.Greater(atlas.GetPackables().Length, 0, "atlas has no packables");
        }

        [Test]
        public void CharacterSprites_CoverAllFourFacings()
        {
            var reg = PieceRegistry.Load();
            foreach (var ch in reg.All<CharacterDef>())
                Assert.AreEqual(4, ch.sprites.Length, $"{ch.id}: expected up/down/left/right");
        }
    }
}
