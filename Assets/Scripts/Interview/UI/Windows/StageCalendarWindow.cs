using Ohabjizol.UI;
using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class StageCalendarWindow : InfoWindow
    {
        [SerializeField] Image[] _dayCells;
        [SerializeField] Text[] _dayLabels;
        [SerializeField] Text _stageLabel;
        [SerializeField] Color _productionColor = new Color32(210, 65, 65, 255);
        [SerializeField] Color _neutralColor = new Color32(230, 234, 237, 255);
        StageCalendarState _state;
        string _stageName;
        public void Bind(StageCalendarState state, string stageName) {
            _state = state; _stageName = stageName ?? string.Empty; Refresh();
        }
        public override void Refresh()
        {
            if (_stageLabel != null) _stageLabel.text = _stageName;
            for (int i = 0; i < 30; i++) {
                if (_dayLabels != null && i < _dayLabels.Length && _dayLabels[i] != null) _dayLabels[i].text = (i + 1).ToString();
                if (_dayCells != null && i < _dayCells.Length && _dayCells[i] != null)
                    _dayCells[i].color = _state != null && _state.IsProductionDay(i + 1) ? _productionColor : _neutralColor;
            }
        }
        protected override void BuildInfo(RectTransform info)
        {
            _stageLabel = UiKit.Text(info, "StageName", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            _stageLabel.color = Color.black;
            UiKit.Place(_stageLabel.rectTransform, .04f, .84f, .96f, .98f);
            var grid = new GameObject("Days", typeof(RectTransform), typeof(GridLayoutGroup));
            var rect = grid.GetComponent<RectTransform>();
            rect.SetParent(info, false);
            UiKit.Place(rect, .04f, .04f, .96f, .80f);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 6;
            layout.cellSize = new Vector2(64, 48);
            layout.spacing = new Vector2(6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            _dayCells = new Image[30]; _dayLabels = new Text[30];
            for (int i = 0; i < 30; i++) {
                _dayCells[i] = UiKit.Image(rect, "Day" + (i + 1), _neutralColor);
                _dayLabels[i] = UiKit.Text(_dayCells[i].transform, "Label", 20, FontStyle.Normal, TextAnchor.MiddleCenter);
                _dayLabels[i].color = Color.black;
                UiKit.Stretch(_dayLabels[i].rectTransform, 0);
                _dayLabels[i].text = (i + 1).ToString();
            }
        }
    }
}