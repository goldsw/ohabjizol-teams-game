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
        [SerializeField] ApplicationFormView applicationForm;
        [SerializeField] Image portfolioBackground;
        [SerializeField] Sprite portfolioBackgroundSprite;
        [SerializeField] Text windowTitle;
        [SerializeField] RectTransform documentViewport;
        [SerializeField] Vector2 applicationWindowSize = new Vector2(540f, 763.31f);
        [SerializeField] Vector2 portfolioWindowSize = new Vector2(640f, 740f);
        public bool HasDocumentAppearance => portfolioBackground && windowTitle && documentViewport;
        ApplicantDocumentPacket packet;
        DocumentKind kind;
        PortfolioSpriteCatalog catalog;
        RectTransform portfolioCard;
        bool portfolioHeightCaptured;
        float standardPortfolioHeight;

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
                {
                    portfolioCard = UnityEngine.Object.Instantiate(prefab, portfolioHost, false);
                    var illustrated = portfolioCard.GetComponent<IllustratedPortfolioCard>();
                    if (illustrated != null)
                    {
                        illustrated.FitToHost();
                        illustrated.ApplyState(packet.DisplayState);
                    }
                }
            }
        }

        protected override void BuildInfo(RectTransform info) { }

        public override void Refresh()
        {
            bool portfolio = packet != null && kind == DocumentKind.Portfolio;
            bool application = kind == DocumentKind.Application;
            bool useForm = applicationForm != null && applicationForm.HasAuthoredUi;
            RectTransform cardPrefab = portfolio && catalog != null
                ? catalog.GetCard(packet.Role, packet.DisplayState) : null;
            bool illustrated = cardPrefab != null && cardPrefab.GetComponent<IllustratedPortfolioCard>() != null;
            if (portfolioHost && portfolioHost.TryGetComponent<LayoutElement>(out var cardLayout))
            {
                if (!portfolioHeightCaptured)
                {
                    standardPortfolioHeight = cardLayout.preferredHeight;
                    portfolioHeightCaptured = true;
                }
                cardLayout.preferredHeight = illustrated
                    ? IllustratedPortfolioCard.PreferredWindowSize.y - ToolbarHeight - 24f
                    : standardPortfolioHeight;
            }
            if (HasDocumentAppearance) ApplyDocumentAppearance(application && useForm);
            if (illustrated) ApplyIllustratedAppearance();
            if (applicationForm != null)
            {
                applicationForm.gameObject.SetActive(application && useForm);
                if (application && useForm) applicationForm.Bind(packet);
            }
            if (applicantStats != null)
            {
                applicantStats.gameObject.SetActive(application);
                applicantStats.Bind(application && packet != null ? packet.DisplayedSkills : null);
            }
            string heading = kind == DocumentKind.Application ? "지원서" : "포트폴리오";
            Title = heading;
            if (documentTitle != null)
                documentTitle.text = packet == null || (application && useForm)
                    ? heading : packet.DisplayName + " · " + heading;
            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(!illustrated && !(application && useForm));
                bodyText.text = packet == null ? string.Empty :
                    kind == DocumentKind.Application ? packet.ApplicationText : packet.PortfolioText;
            }
            Sprite sprite = portfolio && catalog != null ? catalog.Get(packet.Role, packet.DisplayState) : null;
            bool hasCard = portfolio && catalog != null && catalog.GetCard(packet.Role, packet.DisplayState) != null;
            if (bodyImage != null)
            {
                bodyImage.sprite = hasCard ? null : sprite;
                bodyImage.gameObject.SetActive(!hasCard && sprite != null);
            }
            if (portfolioHost != null) portfolioHost.gameObject.SetActive(hasCard);
        }

        void ApplyDocumentAppearance(bool application)
        {
            // The application paper supplies its own sprite. Portfolio artwork is independent.
            portfolioBackground.enabled = !application;
            portfolioBackground.sprite = portfolioBackgroundSprite;
            portfolioBackground.color = portfolioBackgroundSprite
                ? Color.white : new Color32(255, 238, 215, 255);
            windowTitle.gameObject.SetActive(!application);
            if (documentTitle) documentTitle.gameObject.SetActive(!application);
            var root = (RectTransform)transform;
            Vector2 size = application ? applicationWindowSize : portfolioWindowSize;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            documentViewport.anchorMin = Vector2.zero;
            documentViewport.anchorMax = Vector2.one;
            documentViewport.offsetMin = application ? Vector2.zero : new Vector2(16f, 16f);
            documentViewport.offsetMax = application ? Vector2.zero : new Vector2(-16f, -68f);
        }

        void ApplyIllustratedAppearance()
        {
            if (portfolioBackground) portfolioBackground.enabled = false;
            if (documentTitle) documentTitle.gameObject.SetActive(false);
            if (windowTitle) windowTitle.gameObject.SetActive(false);
            var root = (RectTransform)transform;
            Vector2 size = IllustratedPortfolioCard.PreferredWindowSize;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            if (documentViewport)
            {
                documentViewport.anchorMin = Vector2.zero;
                documentViewport.anchorMax = Vector2.one;
                documentViewport.offsetMin = new Vector2(12f, 12f);
                documentViewport.offsetMax = new Vector2(-12f, -12f);
            }
        }

#if UNITY_EDITOR
        public void ConfigureDocumentAppearanceEditor(Image background, Text title,
            RectTransform viewport, Vector2 applicationSize)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Document authoring requires EditMode.");
            portfolioBackground = background;
            windowTitle = title;
            documentViewport = viewport;
            applicationWindowSize = applicationSize;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ConfigureApplicationFormEditor(ApplicationFormView form)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Document authoring requires EditMode.");
            applicationForm = form;
            UnityEditor.EditorUtility.SetDirty(this);
        }

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
