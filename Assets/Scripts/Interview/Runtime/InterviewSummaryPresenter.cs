using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed class InterviewSummaryPresenter
    {
        readonly StageManager _stage;
        readonly Text _title, _stageLabel, _finished, _current, _required;
        readonly Text[] _slots;
        readonly Button _accept, _reject, _chat;
        readonly string _finishedText;

        public InterviewSummaryPresenter(StageManager stage, Text title, Text stageLabel, Text finished,
            Text current, Text required, Text[] slots, Button accept, Button reject, Button chat)
        {
            _stage = stage; _title = title; _stageLabel = stageLabel; _finished = finished;
            _current = current; _required = required; _slots = slots;
            _accept = accept; _reject = reject; _chat = chat; _finishedText = finished.text;
        }

        public void Render()
        {
            var session = _stage.Session;
            var definition = _stage.Context.StageDefinition;
            var theme = session.Theme;
            _title.text = definition.DisplayName + (theme == null ? "" : " · 주제 " + theme.DisplayName);
            _stageLabel.text = definition.DisplayName;
            var members = session.Team == null ? null : session.Team.Members;
            for (int i = 0; i < _slots.Length; i++)
            {
                var person = members != null && i < members.Count && members[i] != null ? members[i].Applicant : null;
                _slots[i].text = person == null ? (i + 1) + "  빈 자리"
                    : (i + 1) + "  " + person.Name + "  " + JobRoleText.Korean(person.PrimaryRole);
            }
            _current.text = SkillLine("현재", session.Team == null ? null : session.Team.DisplayedSum);
            _required.text = SkillLine("요구", theme == null ? null : theme.RequiredSkills);
            bool open = !session.IsFinished && session.Current != null && !_stage.IsDeciding;
            bool retry = _stage.CompletionPending && _stage.CompletionError != null;
            _accept.interactable = open || retry;
            _reject.interactable = open || retry;
            _chat.interactable = open;
            _finished.gameObject.SetActive(session.IsFinished);
            _finished.text = retry ? "저장 대기 · 합격/불합격 버튼으로 재시도" : _finishedText;
        }

        static string SkillLine(string title, SkillSet skills) => title + "  기획 " + (skills == null ? 0 : skills.Planning)
            + "  프로그래밍 " + (skills == null ? 0 : skills.Programming) + "  그래픽 " + (skills == null ? 0 : skills.Graphics);
    }
}