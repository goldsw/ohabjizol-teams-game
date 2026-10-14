using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ohabjizol.Interview
{
    [CreateAssetMenu(menuName = "Interview/Portfolio Catalog")]
    public sealed class PortfolioSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public JobRole Role;
            public PortfolioDisplayState State;
            public Sprite Sprite;
            public RectTransform CardPrefab;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => entries;

        public Sprite Get(JobRole role, PortfolioDisplayState state)
        {
            Entry entry = Find(role, state);
            return entry == null ? null : entry.Sprite;
        }

        public RectTransform GetCard(JobRole role, PortfolioDisplayState state)
        {
            Entry entry = Find(role, state);
            return entry == null ? null : entry.CardPrefab;
        }

        public void SetCard(JobRole role, PortfolioDisplayState state, RectTransform prefab)
        {
            Entry entry = Find(role, state);
            if (entry == null)
            {
                entry = new Entry { Role = role, State = state };
                entries.Add(entry);
            }
            entry.CardPrefab = prefab;
        }

        public void ConfigureEditor(IEnumerable<Entry> values)
        {
            entries = values == null ? new List<Entry>() : new List<Entry>(values);
        }

        Entry Find(JobRole role, PortfolioDisplayState state)
        {
            foreach (Entry entry in entries)
                if (entry != null && entry.Role.Equals(role) && entry.State == state)
                    return entry;
            return null;
        }
    }
}