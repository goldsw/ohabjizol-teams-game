using System;
using System.Collections.Generic;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public enum DocumentViewerState { Closed, Normal, Minimized }

    public sealed class DocumentWorkspace : MonoBehaviour, IDisposable
    {
        [SerializeField] RectTransform itemParent;
        [SerializeField] RectTransform windowParent;
        [SerializeField] DocumentTaskbarItem itemPrefab;
        [SerializeField] PdfDocumentViewer viewerPrefab;
        [SerializeField] PortfolioSpriteCatalog catalog;
        [SerializeField] DocumentLibraryView library;
        [SerializeField] Button libraryButton;
        [SerializeField] WindowTaskbarView taskbarView;
        readonly StageDocumentStore store = new StageDocumentStore();
        readonly Dictionary<DocumentKey, PdfDocumentViewer> viewers = new Dictionary<DocumentKey, PdfDocumentViewer>();
        ApplicantDocumentPacket packet;
        public StageDocumentStore Store => store;
        public WindowTaskbarView TaskbarView => taskbarView;
        public void ConfigureTaskbarEditor(WindowTaskbarView view) { taskbarView = view; }
        public DocumentLibraryView Library => library;
        public bool HasAuthoredLibrary => library != null && library.HasAuthoredUi && libraryButton != null;

        public void Configure(RectTransform itemParent, RectTransform windowParent,
            DocumentTaskbarItem itemPrefab, PdfDocumentViewer viewerPrefab, PortfolioSpriteCatalog catalog)
        {
            this.itemParent = itemParent;
            this.windowParent = windowParent;
            this.itemPrefab = itemPrefab;
            this.viewerPrefab = viewerPrefab;
            this.catalog = catalog;

        }
        public void ConfigureEditor(DocumentLibraryView authoredLibrary, Button authoredButton)
        {

            library = authoredLibrary;
            libraryButton = authoredButton;
        }
        public void BindApplicant(ApplicantDocumentPacket applicant)
        {
            bool same = packet != null && applicant != null &&
                string.Equals(packet.ApplicantKey, applicant.ApplicantKey, StringComparison.Ordinal);
            if (!same) ClearApplicant();
            packet = applicant;
        }
        public void Download(DocumentKind kind)
        {
            if (kind != DocumentKind.Application && kind != DocumentKind.Portfolio)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (packet == null) return;
            DocumentKey key = new DocumentKey(packet.ApplicantKey, kind);
            store.AddOrGet(packet, kind);
            RefreshLibrary();
            OpenDocument(key);
        }
        void RefreshLibrary() { if (library != null) library.Bind(store.Documents, OpenDocument); }
        public void OpenLibrary()
        {
            if (library == null) return;
            RefreshLibrary();
            library.Restore();
        }
        public void ToggleLibrary()
        {
            if (library == null) return;
            if (library.IsOpen) library.Minimize();
            else OpenLibrary();
        }
        public void OpenDocument(DocumentKey key)
        {
            if (taskbarView == null)
            { Debug.LogError("Document workspace requires an authored taskbar reference.", this); return; }
            if (taskbarView.Queue.TryGetWindow(key, out IManagedWindow existing)) { existing.Maximize(); return; }
            if (!store.TryGet(key, out DownloadedDocument document)) return;
            if (windowParent == null || viewerPrefab == null)
            { Debug.LogError("Document workspace requires authored viewer references.", this); return; }
            PdfDocumentViewer viewer = WindowFactory.Instantiate(viewerPrefab, windowParent);
            viewers[key] = viewer;
            viewer.Bind(document.PacketSnapshot, key.Kind, catalog);
            viewer.Closed += OnViewerClosed;
            viewer.Initialize(document, taskbarView.Queue);
            viewer.Maximize();
        }
        public bool TryGetViewer(DocumentKind kind, out PdfDocumentViewer viewer)
        {
            viewer = null;
            if (packet == null || (kind != DocumentKind.Application && kind != DocumentKind.Portfolio)) return false;
            var key = new DocumentKey(packet.ApplicantKey, kind);
            if (!viewers.TryGetValue(key, out PdfDocumentViewer candidate) || !candidate ||
                candidate.IsClosed || !candidate.IsOpen || candidate.IsMinimized || taskbarView == null ||
                !taskbarView.Queue.TryGetWindow(key, out IManagedWindow owned) || !ReferenceEquals(owned, candidate))
                return false;
            viewer = candidate;
            return true;
        }
        public DocumentViewerState GetState(DocumentKind kind) => packet == null ? DocumentViewerState.Closed :
            GetState(new DocumentKey(packet.ApplicantKey, kind));
        public DocumentViewerState GetState(DocumentKey key)
        {
            if (!viewers.TryGetValue(key, out PdfDocumentViewer viewer) || viewer == null || viewer.IsClosed)
                return DocumentViewerState.Closed;
            if (viewer.IsMinimized) return DocumentViewerState.Minimized;
            return viewer.IsOpen ? DocumentViewerState.Normal : DocumentViewerState.Closed;
        }
        void OnViewerClosed(IManagedWindow closed)
        {
            closed.Closed -= OnViewerClosed;
            DocumentKey? removed = null;
            foreach (var pair in viewers) if (ReferenceEquals(pair.Value, closed)) { removed = pair.Key; break; }
            if (removed.HasValue) viewers.Remove(removed.Value);
        }
        public void CloseAllWindows() { CloseWindows(); }
        public void CloseWindows()
        {
            foreach (PdfDocumentViewer viewer in new List<PdfDocumentViewer>(viewers.Values)) if (viewer != null) viewer.Close();
            viewers.Clear();
            if (taskbarView != null) taskbarView.Queue.ClearFocus();
            if (library != null) library.Close();
        }
        public void ClearApplicant() { packet = null; CloseWindows(); }
        public void ResetStage()
        {
            ClearApplicant();
            foreach (PdfDocumentViewer viewer in viewers.Values)
                if (viewer != null) Destroy(viewer.gameObject);
            viewers.Clear();
            if (library != null) library.ClearItems();
            store.Clear();
        }
        public void Dispose() { ResetStage(); }
        void OnDestroy()
        {

            Dispose();
        }
    }
}
