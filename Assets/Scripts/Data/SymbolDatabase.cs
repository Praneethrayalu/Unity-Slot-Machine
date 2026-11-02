using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Registry of every symbol in the game, looked up by id at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolDatabase", menuName = "Slot Game/Symbol Database", order = 1)]
    public sealed class SymbolDatabase : ScriptableObject
    {
        [SerializeField] private List<SymbolDefinition> symbols = new List<SymbolDefinition>();

        private Dictionary<int, SymbolDefinition> _lookup;

        public IReadOnlyList<SymbolDefinition> Symbols => symbols;

        /// <summary>Returns the symbol with <paramref name="id"/>, or null (with an error) if unknown.</summary>
        public SymbolDefinition Get(int id)
        {
            if (TryGet(id, out SymbolDefinition symbol))
            {
                return symbol;
            }

            Debug.LogError($"[SymbolDatabase] Unknown symbol id {id}.", this);
            return null;
        }

        public bool TryGet(int id, out SymbolDefinition symbol)
        {
            EnsureLookup();
            return _lookup.TryGetValue(id, out symbol);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<int, SymbolDefinition>(symbols.Count);
            foreach (SymbolDefinition symbol in symbols)
            {
                if (symbol == null)
                {
                    continue;
                }

                if (_lookup.ContainsKey(symbol.Id))
                {
                    Debug.LogError($"[SymbolDatabase] Duplicate symbol id {symbol.Id} ({symbol.name}).", this);
                    continue;
                }

                _lookup.Add(symbol.Id, symbol);
            }
        }

        private void OnValidate()
        {
            // Rebuild the cache after edits in the inspector.
            _lookup = null;
        }

#if UNITY_EDITOR
        public void EditorSetSymbols(IEnumerable<SymbolDefinition> newSymbols)
        {
            symbols = new List<SymbolDefinition>(newSymbols);
            _lookup = null;
        }
#endif
    }
}
