using System;
using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;

namespace Ohabjizol.Interview
{
    public sealed class DocumentLibraryView : InfoWindow
    {
        [SerializeField] RectTransform content;
        [SerializeField] DocumentTaskbarItem rowPrefab;
        readonly Dictionary<DocumentKey, DocumentTaskbarItem> rows = new Dictionary<DocumentKey, DocumentTaskbarItem>();
        public RectTransform Content => content;
        public bool HasAuthoredUi => content != null && rowPrefab != null;
        public RectTransform GetDocumentTarget(DocumentKey key)
            => rows.TryGetValue(key, out var row) && row ? row.transform as RectTransform : null;
        public void ConfigureEditor(RectTransform authoredContent, DocumentTaskbarItem authoredRow)
        { content = authoredContent; rowPrefab = authoredRow; }
        public void Bind(IReadOnlyList<DownloadedDocument> documents, Action<DocumentKey> open)
        {
            if (!HasAuthoredUi) return;
            foreach (DownloadedDocument document in documents)
            {
                if (rows.ContainsKey(document.Key)) continue;
                DocumentKey key = document.Key;
                DocumentTaskbarItem row = Instantiate(rowPrefab, content, false);
                row.Bind(document, () => open(key));
                row.gameObject.SetActive(true);
                rows.Add(key, row);
            }
        }
        public void ClearItems()
        {
            foreach (DocumentTaskbarItem row in rows.Values)
            {
                if (row == null) continue;
                row.Unbind();
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            rows.Clear();
        }
        protected override void BuildInfo(RectTransform info) { }
        void OnDestroy() { ClearItems(); }
    }
}
