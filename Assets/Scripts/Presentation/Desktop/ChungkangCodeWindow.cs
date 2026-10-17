using System;
using System.Collections;
using Ohabjizol.Interview;
using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Presentation
{
    /// <summary>
    /// Discord-like launcher. The server column holds the four stages: cleared ones carry a badge,
    /// locked ones are dimmed, and <see cref="StageProgress.NextStage"/> is highlighted.
    /// <see cref="Connect"/> waits, shows the connecting line, then hands the next stage over.
    /// </summary>
    public sealed class ChungkangCodeWindow : InfoWindow
    {
        const float ServerColumn = 112f;
        const float ChannelColumn = 240f;
        const float ServerTile = 64f;
        const float ServerPitch = 104f;

        static readonly string[] Channels = { "공지", "면접실", "잡담" };
        const int InterviewChannel = 1;

        [SerializeField] ServerSlot[] _servers;
        [SerializeField] Text _serverName;
        [SerializeField] Text _heading;
        [SerializeField] Text _detail;
        [SerializeField] Text _status;
        StageProgress _progress;
        Coroutine _connect;
        bool _connected;

        public StageId NextStage
        {
            get { return CampaignRunStore.Load().NextStage; }
        }

        [SerializeField] Button _newGame;
        public event Action RunReset;
        public bool HasAuthoredContent => _servers != null && _servers.Length == StageCatalog.All.Count &&
            Array.TrueForAll(_servers, slot => slot != null && slot.HasReferences) &&
            _serverName != null && _heading != null && _detail != null && _status != null && _newGame != null;
        protected override void OnAuthoredInitialized()
        {
            if (!HasAuthoredContent) { Debug.LogError("ChungkangCodeWindow authored content missing; bake in Edit Mode.", this); return; }
            _newGame.onClick.RemoveListener(NewGame);
            _newGame.onClick.AddListener(NewGame);
        }
        void NewGame()
        {
            if (_connect != null) StopCoroutine(_connect);
            _connect = null;
            _connected = false;
            CampaignRunStore.StartNewRun();
            Refresh();
            RunReset?.Invoke();
        }
        protected override void BuildInfo(RectTransform info)
        {
            Image main = UiKit.Image(info, "Main", DesktopUi.DiscordMain);
            UiKit.Stretch(main.rectTransform, 0f);

            Image serverColumn = UiKit.Image(info, "Servers", DesktopUi.DiscordDark);
            RectTransform serverRect = serverColumn.rectTransform;
            serverRect.anchorMin = new Vector2(0f, 0f);
            serverRect.anchorMax = new Vector2(0f, 1f);
            serverRect.pivot = new Vector2(0f, 0.5f);
            serverRect.sizeDelta = new Vector2(ServerColumn, 0f);
            serverRect.anchoredPosition = Vector2.zero;

            int count = StageCatalog.All.Count;
            _servers = new ServerSlot[count];
            for (int i = 0; i < count; i++)
                _servers[i] = ServerSlot.Create(serverRect, StageCatalog.All[i], i);

            Image channelColumn = UiKit.Image(info, "Channels", DesktopUi.DiscordPanel);
            RectTransform channelRect = channelColumn.rectTransform;
            channelRect.anchorMin = new Vector2(0f, 0f);
            channelRect.anchorMax = new Vector2(0f, 1f);
            channelRect.pivot = new Vector2(0f, 0.5f);
            channelRect.sizeDelta = new Vector2(ChannelColumn, 0f);
            channelRect.anchoredPosition = new Vector2(ServerColumn, 0f);

            _serverName = DesktopUi.Label(channelRect, "ServerName", string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, DesktopUi.DiscordText);
            DesktopUi.PlaceTopLeft(_serverName.rectTransform, 16f, 10f, ChannelColumn - 24f, 44f);

            Image divider = UiKit.Image(channelRect, "Divider", DesktopUi.DiscordDark);
            DesktopUi.PlaceTopLeft(divider.rectTransform, 0f, 60f, ChannelColumn, 2f);

            for (int i = 0; i < Channels.Length; i++)
            {
                bool active = i == InterviewChannel;
                if (active)
                {
                    Image pick = UiKit.Image(channelRect, "ActiveChannel", new Color(1f, 1f, 1f, 0.08f));
                    DesktopUi.PlaceTopLeft(pick.rectTransform, 8f, 76f + i * 44f, ChannelColumn - 16f, 38f);
                }

                Text channel = DesktopUi.Label(
                    channelRect,
                    "Channel" + i,
                    "#  " + Channels[i],
                    20,
                    active ? FontStyle.Bold : FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    active ? DesktopUi.DiscordText : DesktopUi.DiscordMuted);
                DesktopUi.PlaceTopLeft(channel.rectTransform, 20f, 76f + i * 44f, ChannelColumn - 32f, 38f);
            }

            float left = ServerColumn + ChannelColumn + 28f;
            _heading = DesktopUi.Label(info, "Heading", string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleLeft, DesktopUi.DiscordText);
            RectTransform headingRect = _heading.rectTransform;
            headingRect.anchorMin = new Vector2(0f, 1f);
            headingRect.anchorMax = new Vector2(1f, 1f);
            headingRect.pivot = new Vector2(0.5f, 1f);
            headingRect.offsetMin = new Vector2(left, -64f);
            headingRect.offsetMax = new Vector2(-24f, -12f);

            Image rule = UiKit.Image(info, "HeadingRule", DesktopUi.DiscordDark);
            RectTransform ruleRect = rule.rectTransform;
            ruleRect.anchorMin = new Vector2(0f, 1f);
            ruleRect.anchorMax = new Vector2(1f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 1f);
            ruleRect.offsetMin = new Vector2(ServerColumn + ChannelColumn, -72f);
            ruleRect.offsetMax = new Vector2(0f, -70f);

            _detail = DesktopUi.Label(info, "Detail", string.Empty, 22, FontStyle.Normal, TextAnchor.UpperLeft, DesktopUi.DiscordText);
            _detail.verticalOverflow = VerticalWrapMode.Overflow;
            _detail.lineSpacing = 1.25f;
            RectTransform detailRect = _detail.rectTransform;
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.offsetMin = new Vector2(left, 110f);
            detailRect.offsetMax = new Vector2(-24f, -92f);

            Image statusPlate = UiKit.Image(info, "StatusPlate", DesktopUi.DiscordPanel);
            RectTransform plateRect = statusPlate.rectTransform;
            plateRect.anchorMin = new Vector2(0f, 0f);
            plateRect.anchorMax = new Vector2(1f, 0f);
            plateRect.pivot = new Vector2(0.5f, 0f);
            plateRect.offsetMin = new Vector2(left - 8f, 24f);
            plateRect.offsetMax = new Vector2(-20f, 84f);

            _status = DesktopUi.Label(plateRect, "Status", string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, DesktopUi.DiscordAccent);
            UiKit.Stretch(_status.rectTransform, 0f);
            _status.rectTransform.offsetMin = new Vector2(18f, 0f);
            _status.rectTransform.offsetMax = new Vector2(-180f, 0f);
            _newGame = UiKit.Button(plateRect, "NewGame", "새 게임", DesktopUi.DiscordAccent, 22);
            var newRect = _newGame.GetComponent<RectTransform>();
            newRect.anchorMin = new Vector2(1f, 0f);
            newRect.anchorMax = Vector2.one;
            newRect.pivot = new Vector2(1f, 0.5f);
            newRect.sizeDelta = new Vector2(160f, 0f);
            newRect.anchoredPosition = Vector2.zero;
        }

        public override void Refresh()
        {
            if (!HasAuthoredContent || _connect != null || _connected) return;
            _progress = StageProgressStore.Load();
            var run = CampaignRunStore.Load();
            StageId next = run.NextStage;
            for (int i = 0; i < _servers.Length; i++)
                _servers[i].Paint(_progress, next, run.IsCompleted(_servers[i].Id), run.IsComplete);
            if (run.IsComplete)
            {
                _serverName.text = "면접 완료";
                _heading.text = "# 엔딩";
                _detail.text = "모든 면접을 마쳤습니다.\n총 별점: " + (run.TotalHalfStars / 2f) + "\n청강코드 아이콘으로 엔딩 보기 · 새 게임으로 다시 시작";
                _status.text = "엔딩 준비 완료";
                return;
            }
            StageDefinition stage = StageCatalog.Get(next);
            _serverName.text = stage.DisplayName;
            _heading.text = "# 면접실";
            int best = _progress.BestAccuracy(next);
            _detail.text = stage.DisplayName + " 서버\n\n주제: " + stage.Theme.DisplayName +
                "\n지원자: " + stage.Applicants.Count + "명\n최고 기록: " + (best < 0 ? "기록 없음" : best + "%");
            _status.text = "서버 정보를 불러오는 중…";
        }
        /// <summary>
        /// After <paramref name="delay"/> seconds, shows the connecting line and passes
        /// <see cref="NextStage"/> to <paramref name="connected"/>. Closing the window first cancels it.
        /// </summary>
        public void Connect(float delay, Action<StageId> connected)
        {
            if (_connect != null || _connected || !isActiveAndEnabled)
                return;

            _connect = StartCoroutine(ConnectAfter(Mathf.Max(0f, delay), connected));
        }

        IEnumerator ConnectAfter(float delay, Action<StageId> connected)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            _connect = null;
            _connected = true;
            _status.text = "접속 중…";
            if (connected != null)
                connected(NextStage);
        }

        void OnDisable()
        {
            _connect = null;
        }

        [Serializable]
        sealed class ServerSlot
        {
            static readonly Color LockedTile = new Color32(0x26, 0x27, 0x2B, 0xFF);

            [SerializeField] Image _tile;
            [SerializeField] Text _glyph;
            [SerializeField] Image _pill;
            [SerializeField] Image _badge;
            [SerializeField] Text _caption;
            [SerializeField] StageId _id;

            public static ServerSlot Create(RectTransform column, StageDefinition stage, int index)
            {
                var slot = new ServerSlot();
                slot._id = stage.Id;
                float top = 18f + index * ServerPitch;

                slot._pill = UiKit.Image(column, "Pill" + index, Color.white);
                DesktopUi.PlaceTopLeft(slot._pill.rectTransform, 0f, top + 12f, 5f, ServerTile - 24f);

                slot._tile = UiKit.Image(column, "Server" + index, DesktopUi.DiscordMain);
                DesktopUi.PlaceTopLeft(slot._tile.rectTransform, (ServerColumn - ServerTile) * 0.5f, top, ServerTile, ServerTile);

                string name = stage.DisplayName;
                slot._glyph = DesktopUi.Label(slot._tile.transform, "Glyph", name.Substring(0, 1), 28, FontStyle.Bold, TextAnchor.MiddleCenter, DesktopUi.DiscordText);
                UiKit.Stretch(slot._glyph.rectTransform, 0f);

                slot._badge = UiKit.Image(slot._tile.transform, "Cleared", DesktopUi.DiscordGreen);
                RectTransform badgeRect = slot._badge.rectTransform;
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.sizeDelta = new Vector2(20f, 20f);
                badgeRect.anchoredPosition = new Vector2(4f, -4f);

                slot._caption = DesktopUi.Label(column, "Caption" + index, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperCenter, DesktopUi.DiscordMuted);
                DesktopUi.PlaceTopLeft(slot._caption.rectTransform, 4f, top + ServerTile + 4f, ServerColumn - 8f, 22f);
                return slot;
            }

            public StageId Id => _id;
            public bool HasReferences => _tile != null && _glyph != null && _pill != null && _badge != null && _caption != null;
            public void Paint(StageProgress progress, StageId next, bool completed, bool allComplete)
            {
                bool cleared = completed;
                bool playable = completed || (!allComplete && _id == next);
                bool current = !allComplete && _id == next;

                _tile.color = current ? DesktopUi.DiscordAccent : playable ? DesktopUi.DiscordMain : LockedTile;
                _glyph.color = playable ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                _pill.gameObject.SetActive(current);
                _badge.gameObject.SetActive(cleared);

                if (cleared)
                {
                    _caption.text = "진행 완료";
                    _caption.color = DesktopUi.DiscordGreen;
                }
                else if (current)
                {
                    _caption.text = "도전";
                    _caption.color = DesktopUi.DiscordText;
                }
                else
                {
                    _caption.text = playable ? "열림" : "잠김";
                    _caption.color = playable ? DesktopUi.DiscordMuted : new Color(0.58f, 0.61f, 0.64f, 0.5f);
                }
            }
        }
    }
}
