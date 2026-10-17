using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class PdfDocumentViewer : InfoWindow, IManagedWindow
    {
        [SerializeField] Text bodyText;
        [SerializeField] Image bodyImage;
        [SerializeField] Text documentTitle;
        [SerializeField] RectTransform portfolioHost;
        [SerializeField] ApplicantStatsView applicantStats;
        ApplicantDocumentPacket packet;
        DocumentKind kind;
        PortfolioSpriteCatalog catalog;
        RectTransform portfolioCard;

        TaskbarQueue queue;
        bool closed;
        bool closedPublished;
        public DownloadedDocument Document { get; private set; }
        public DocumentKey DocumentKey { get; private set; }
        public WindowTaskbarDescriptor TaskbarInfo { get; private set; }
        public bool IsVisible => IsOpen && !IsMinimized;
        public bool IsClosed => closed;
        public bool IsActive => !closed && IsVisible && queue != null &&
            ReferenceEquals(queue.ActiveWindow, this);
        public event Action<IManagedWindow> StateChanged;
        public new event Action<IManagedWindow> Closed;
        public event Action<IManagedWindow> FocusRequested;

        public void Initialize(DownloadedDocument document, TaskbarQueue taskbarQueue)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (taskbarQueue == null) throw new ArgumentNullException(nameof(taskbarQueue));
            if (closed) throw new InvalidOperationException("Cannot initialize a closed PDF window.");
            if (taskbarQueue.TryGetWindow(document.Key, out var existing) &&
                !ReferenceEquals(existing, this))
                throw new InvalidOperationException("A window for this document is already registered.");
            queue?.Unregister(this);
            Document = document;
            DocumentKey = document.Key;
            TaskbarInfo = document.TaskbarInfo;
            queue = taskbarQueue;
            queue.Register(this);
            if (IsVisible) BringToFront();
        }

        public override void Minimize()
        {
            if (closed) return;
            base.Minimize();
            StateChanged?.Invoke(this);
        }

        public override void Restore()
        {
            if (closed) return;
            base.Restore();
            StateChanged?.Invoke(this);
        }

        public override void Maximize()
        {
            if (closed) return;
            base.Restore();
            StateChanged?.Invoke(this);
        }

        public override void BringToFront()
        {
            if (closed) return;
            base.BringToFront();
            if (IsVisible) FocusRequested?.Invoke(this);
        }

        public override void Close()
        {
            if (closed) return;
            closed = true;
            try { base.Close(); }
            finally
            {
                try { PublishClosed(); }
                finally { Destroy(gameObject); }
            }
        }

        void PublishClosed()
        {
            if (closedPublished) return;
            closedPublished = true;
            try { Closed?.Invoke(this); }
            finally { queue?.Unregister(this); }
        }

        void OnDisable()
        {
            if (!closed) StateChanged?.Invoke(this);
        }

        public void ConfigureEditor(Text text, Image image, Text title, RectTransform host)
        {
            bodyText = text;
            bodyImage = image;
            documentTitle = title;
            portfolioHost = host;
        }

        public void Bind(ApplicantDocumentPacket applicant, DocumentKind documentKind,
            PortfolioSpriteCatalog portfolioCatalog)
        {
            packet = applicant;
            kind = documentKind;
            catalog = portfolioCatalog;
            ClearCard();
            Refresh();
            if (packet != null && kind == DocumentKind.Portfolio &&
                catalog != null && portfolioHost != null)
            {
                RectTransform prefab = catalog.GetCard(packet.Role, packet.DisplayState);
                if (prefab != null)
                    portfolioCard = UnityEngine.Object.Instantiate(prefab, portfolioHost, false);
            }
        }

        protected override void BuildInfo(RectTransform info) { }

        public override void Refresh()
        {
            bool portfolio = packet != null && kind == DocumentKind.Portfolio;
            if (applicantStats != null)
            {
                bool application = kind == DocumentKind.Application;
                applicantStats.gameObject.SetActive(application);
                applicantStats.Bind(application && packet != null ? packet.DisplayedSkills : null);
            }
            string heading = kind == DocumentKind.Application ? "지원서" : "포트폴리오";
            Title = heading;
            if (documentTitle != null)
                documentTitle.text = packet == null ? heading : packet.DisplayName + " · " + heading;
            if (bodyText != null)
                bodyText.text = packet == null ? string.Empty :
                    kind == DocumentKind.Application ? packet.ApplicationText : packet.PortfolioText;
            Sprite sprite = portfolio && catalog != null ? catalog.Get(packet.Role, packet.DisplayState) : null;
            bool hasCard = portfolio && catalog != null && catalog.GetCard(packet.Role, packet.DisplayState) != null;
            if (bodyImage != null)
            {
                bodyImage.sprite = hasCard ? null : sprite;
                bodyImage.gameObject.SetActive(!hasCard && sprite != null);
            }
            if (portfolioHost != null) portfolioHost.gameObject.SetActive(hasCard);
        }

#if UNITY_EDITOR
        /// <summary>Adds only a stats row block to the existing document scroll layout.</summary>
        public void BakeApplicantStatsUi(Font font)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stats authoring requires EditMode.");
            if (!bodyText || !font) throw new InvalidOperationException("Authored document body and persistent font required.");
            if (applicantStats)
            {
                if (!applicantStats.HasAuthoredUi) throw new InvalidOperationException("PDF stats references are incomplete.");
                return;
            }
            var content = bodyText.transform.parent as RectTransform;
            if (!content || !content.GetComponent<VerticalLayoutGroup>())
                throw new InvalidOperationException("The authored PDF body must be inside its vertical scroll layout.");
            Transform existing = content.Find("ApplicantStats");
            if (existing)
            {
                applicantStats = existing.GetComponent<ApplicantStatsView>();
                if (!applicantStats || !applicantStats.HasAuthoredUi)
                    throw new InvalidOperationException("Existing PDF stats require authored references.");
            }
            else
            {
                var host = new GameObject("ApplicantStats", typeof(RectTransform), typeof(LayoutElement));
                host.transform.SetParent(content, false);
                host.transform.SetAsFirstSibling();
                var size = host.GetComponent<LayoutElement>();
                size.minHeight = size.preferredHeight = 160f;
                size.flexibleHeight = 0f;
                applicantStats = host.AddComponent<ApplicantStatsView>();
                applicantStats.BakeAuthoredUi(font, bodyText.color, 20);
                applicantStats.gameObject.SetActive(false);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        void ClearCard()
        {
            if (portfolioCard == null) return;
            portfolioCard.gameObject.SetActive(false);
            Destroy(portfolioCard.gameObject);
            portfolioCard = null;
        }

        void OnDestroy()
        {
            closed = true;
            try { PublishClosed(); }
            finally { ClearCard(); }
        }
    }
}