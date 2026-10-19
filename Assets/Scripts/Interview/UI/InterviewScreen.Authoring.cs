using System;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
#if UNITY_EDITOR
        public void BakeAuthoredUi()
        {
            if (Application.isPlaying) throw new InvalidOperationException("BakeAuthoredUi requires EditMode.");
            if (!_questionCatalog)
            {
                _questionCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<AuthoredQuestionCatalog>("Assets/Art/InterviewRefresh/QuestionCatalog.asset");
                if (_questionCatalog) UnityEditor.EditorUtility.SetDirty(this);
            }
            if (!_questionCatalog)
                throw new InvalidOperationException("Bake prerequisite: parent-created and seeded QuestionCatalog.asset.");
            if (HasAuthoredUi)
            {
                BakeTeamWindow();
                BakeTeamProfilesEditor();
                BakeNotepadWindowEditor();
                return;
            }
            if (_discordViewRoot) throw new InvalidOperationException("Existing authored view is incomplete. Assign missing references before baking again.");
            var itemAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InterviewRefresh/Documents/PdfTaskbarItem.prefab");
            _itemPrefab = itemAsset ? itemAsset.GetComponent<DocumentTaskbarItem>() : null;
            var viewerAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InterviewRefresh/Documents/PdfViewer.prefab");
            _viewerPrefab = viewerAsset ? viewerAsset.GetComponent<PdfDocumentViewer>() : null;
            _portfolioCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<PortfolioSpriteCatalog>("Assets/Art/InterviewRefresh/PortfolioCatalog.asset");
            if (!_itemPrefab || !_viewerPrefab || !_portfolioCatalog)
                throw new InvalidOperationException("Bake prerequisite: DocumentsUiBake.BuildAssets() and parent-created PortfolioCatalog.asset.");
            var canvasObject = new GameObject("InterviewAuthoredUi", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            _discordViewRoot = Rect(root, "DiscordViewRoot", 0, .06f, 1, 1);
            var background = UiKit.Image(_discordViewRoot, "Background", Color.white);
            background.sprite = Resources.Load<Sprite>("Interview/Discord_total");
            if (!background.sprite) background.color = new Color(.12f, .13f, .16f);
            background.raycastTarget = false;
            UiKit.Stretch(background.rectTransform, 0);
            _titleLabel = Label(_discordViewRoot, "Title", "면접", .02f, .93f, .8f, .99f, 26);
            var team = UiKit.Image(_discordViewRoot, "TeamSummary", new Color(.07f, .08f, .1f, .94f));
            UiKit.Place(team.rectTransform, .02f, .16f, .26f, .91f);
            Label(team.transform, "TeamTitle", "팀", .06f, .88f, .94f, .98f, 26);
            _slotLabels = new Text[6];
            for (int i = 0; i < _slotLabels.Length; i++) _slotLabels[i] = Label(team.transform, "Slot" + i, "빈 자리", .06f, .76f - i * .11f, .94f, .86f - i * .11f, 18);
            _currentLabel = Label(team.transform, "Current", "현재", .06f, .1f, .94f, .2f, 16);
            _requiredLabel = Label(team.transform, "Required", "요구", .06f, .01f, .94f, .1f, 16);
            var main = Rect(_discordViewRoot, "MainVideo", .29f, .39f, .75f, .89f);
            _call = main.gameObject.AddComponent<InterviewCallView>();
            var portrait = UiKit.Image(main, "Portrait", new Color(.21f, .26f, .34f));
            UiKit.Place(portrait.rectTransform, .05f, .12f, .95f, .97f);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            var nameLabel = Label(main, "Applicant", "지원자", .03f, 0, .97f, .12f, 24);
            var dots = Label(main, "AnsweringBubble", "", .76f, .8f, .97f, .96f, 32);
            _call.Configure(portrait, nameLabel, dots);
            _questions = new InterviewQuestionView[5];
            for (int i = 0; i < 5; i++)
            {
                var button = UiKit.Button(_discordViewRoot, "Question" + i, "질문 " + (i + 1), new Color(.22f, .25f, .32f), 19);
                UiKit.Place(button.GetComponent<RectTransform>(), .77f, .75f - i * .12f, .97f, .86f - i * .12f);
                _questions[i] = button.gameObject.AddComponent<InterviewQuestionView>();
                _questions[i].Configure(button, button.GetComponentInChildren<Text>());
            }
            string[] names = { "동료 면접관", "플레이어", "동료 면접관" };
            for (int i = 0; i < 3; i++)
            {
                var tile = UiKit.Image(_discordViewRoot, "FixedParticipant" + i, new Color(.19f + .04f * i, .23f, .3f));
                UiKit.Place(tile.rectTransform, .3f + i * .15f, .2f, .44f + i * .15f, .37f);
                Label(tile.transform, "Icon", i == 1 ? "나" : "면접관", .12f, .3f, .88f, .9f, 26);
                Label(tile.transform, "Name", names[i], .04f, 0, .96f, .27f, 16);
                if (i == 2) _notification = Label(tile.transform, "NewApplicantBubble", "", -.1f, 1.02f, 1.5f, 1.4f, 18);
            }
            _response = Label(_discordViewRoot, "FixedBottomResponse", "", .29f, .09f, .97f, .18f, 23);
            _acceptButton = UiKit.Button(_discordViewRoot, "Accept", "합격", new Color(.16f, .58f, .28f), 28);
            UiKit.Place(_acceptButton.GetComponent<RectTransform>(), .78f, .23f, .87f, .34f);
            _rejectButton = UiKit.Button(_discordViewRoot, "Reject", "불합격", new Color(.62f, .16f, .2f), 28);
            UiKit.Place(_rejectButton.GetComponent<RectTransform>(), .88f, .23f, .97f, .34f);
            _finishedLabel = Label(_discordViewRoot, "Finished", "면접 종료", .68f, .92f, .84f, .99f, 28);
            _finishedLabel.gameObject.SetActive(false);
            var panel = UiKit.Image(_discordViewRoot, "DownloadPanel", new Color(.12f, .14f, .2f, .98f));
            panel.rectTransform.anchorMin = new Vector2(1, 0);
            panel.rectTransform.anchorMax = new Vector2(1, 1);
            panel.rectTransform.pivot = new Vector2(1, .5f);
            panel.rectTransform.sizeDelta = new Vector2(1400, 0);
            _downloadPanel = panel.gameObject.AddComponent<DownloadPanelView>();
            Label(panel.transform, "Heading", "면접자 서류", .06f, .8f, .9f, .92f, 32);
            var application = UiKit.Button(panel.transform, "DownloadApplication", "지원서 다운로드", new Color(.24f, .33f, .48f), 26);
            UiKit.Place(application.GetComponent<RectTransform>(), .1f, .57f, .9f, .72f);
            var portfolio = UiKit.Button(panel.transform, "DownloadPortfolio", "포트폴리오 다운로드", new Color(.24f, .33f, .48f), 26);
            UiKit.Place(portfolio.GetComponent<RectTransform>(), .1f, .36f, .9f, .51f);
            var close = UiKit.Button(panel.transform, "ClosePanel", "닫기", new Color(.3f, .3f, .35f), 22);
            UiKit.Place(close.GetComponent<RectTransform>(), .7f, .08f, .9f, .2f);
            _downloadPanel.Configure(panel.rectTransform, application, portfolio, close, new Vector2(1400, 0), Vector2.zero, .25f);
            _downloadPanel.CloseImmediate();
            _chatButton = UiKit.Button(_discordViewRoot, "Chat", "채팅", new Color(.25f, .3f, .42f), 22);
            UiKit.Place(_chatButton.GetComponent<RectTransform>(), .86f, .93f, .97f, .99f);
            _unreadMark = Label(_chatButton.transform, "Unread", "!", .78f, .15f, .98f, .95f, 26).gameObject;
            _windowParent = Rect(root, "WindowLayer", 0, .06f, 1, 1);
            _workspace = _windowParent.gameObject.AddComponent<DocumentWorkspace>();
            var taskbar = UiKit.Image(root, "Taskbar", new Color(.04f, .045f, .06f, .98f));
            UiKit.Place(taskbar.rectTransform, 0, 0, 1, .06f);
            var logo = UiKit.Image(taskbar.transform, "TemporaryCheonggangLogo", new Color(1f, .43f, .12f));
            UiKit.Place(logo.rectTransform, .005f, .1f, .08f, .9f);
            Label(logo.transform, "LogoLabel", "청강OS", 0, 0, 1, 1, 18);
            Label(taskbar.transform, "GameLabel", "면접 게임", .09f, .1f, .18f, .9f, 18);
            _discordButton = UiKit.Button(taskbar.transform, "DiscordToggle", "Discord", new Color(.18f, .36f, .78f), 18);
            UiKit.Place(_discordButton.GetComponent<RectTransform>(), .19f, .1f, .28f, .9f);
            _itemParent = Rect(taskbar.transform, "DownloadedItems", .29f, .1f, .79f, .9f);
            _stageLabel = Label(taskbar.transform, "Stage", "Stage", .8f, .1f, .99f, .9f, 18);
            BakeTeamWindow();
            BakeTeamProfilesEditor();
            BakeNotepadWindowEditor();
            SetDiscordVisible(discordVisible);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float right, float top)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            UiKit.Place(rect, x, y, right, top);
            return rect;
        }
        static Text Label(Transform parent, string name, string text, float x, float y, float right, float top, int size)
        {
            var label = UiKit.Text(parent, name, size, FontStyle.Normal, TextAnchor.MiddleLeft);
            label.text = text;
            label.raycastTarget = false;
            UiKit.Place(label.rectTransform, x, y, right, top);
            return label;
        }
#endif
    }
}
