using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ohabjizol.Interview
{
    public sealed class StageDocumentStore
    {
        readonly Dictionary<DocumentKey, DownloadedDocument> documents = new Dictionary<DocumentKey, DownloadedDocument>();
        readonly List<DownloadedDocument> ordered = new List<DownloadedDocument>();
        readonly ReadOnlyCollection<DownloadedDocument> readOnly;
        public StageDocumentStore() { readOnly = ordered.AsReadOnly(); }
        public IReadOnlyList<DownloadedDocument> Documents => readOnly;
        public DownloadedDocument AddOrGet(ApplicantDocumentPacket packet, DocumentKind kind)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            var key = new DocumentKey(packet.ApplicantKey, kind);
            if (documents.TryGetValue(key, out DownloadedDocument existing)) return existing;
            var document = new DownloadedDocument(key, packet, ordered.Count);
            documents.Add(key, document);
            ordered.Add(document);
            return document;
        }
        public bool TryGet(DocumentKey key, out DownloadedDocument document) => documents.TryGetValue(key, out document);
        public void Clear() { documents.Clear(); ordered.Clear(); }
    }
}
