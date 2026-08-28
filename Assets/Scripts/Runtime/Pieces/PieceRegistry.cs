using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrogAcross.Pieces
{
    /// <summary>
    /// The single lookup from string id (what level JSON references) to piece
    /// definition. Lives at Resources/PieceRegistry so both runtime and tests
    /// load the same asset. Pieces not listed here are invisible to levels.
    /// </summary>
    public sealed class PieceRegistry : ScriptableObject
    {
        public List<PieceDef> pieces = new();
        public CharacterDef defaultCharacter;

        private Dictionary<string, PieceDef> _byId;

        public static PieceRegistry Load()
        {
            var reg = Resources.Load<PieceRegistry>("PieceRegistry");
            if (reg == null) throw new KeyNotFoundException("Resources/PieceRegistry asset missing");
            return reg;
        }

        public bool TryGet<T>(string id, out T def) where T : PieceDef
        {
            _byId ??= pieces.Where(p => p != null).ToDictionary(p => p.id);
            if (_byId.TryGetValue(id, out var found) && found is T typed)
            {
                def = typed;
                return true;
            }
            def = null;
            return false;
        }

        public T Get<T>(string id) where T : PieceDef
        {
            if (!TryGet<T>(id, out var def))
                throw new KeyNotFoundException($"Piece '{id}' ({typeof(T).Name}) not in PieceRegistry");
            return def;
        }

        public IEnumerable<T> All<T>() where T : PieceDef => pieces.OfType<T>();

        public void InvalidateCache() => _byId = null;
    }
}
