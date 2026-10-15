using Ohabjizol.UI;
using UnityEngine;

namespace Ohabjizol.Interview
{
    // The controller stays outside this host so hiding a window cannot disable questions.
    public sealed class RadialQuestionWindow : InfoWindow
    {
        [SerializeField] RadialQuestionView _questions;

        public void ConfigureEditor(RadialQuestionView questions)
        {
            _questions = questions;
        }

        protected override void BuildInfo(RectTransform info) { }

        public override void Refresh()
        {
            if (_questions) _questions.ShowQuestions();
        }

        public override void Restore()
        {
            base.Restore();
            Refresh();
        }

        public override void Close()
        {
            if (_questions) _questions.CollapseContent();
            base.Close();
        }

        public override void Minimize()
        {
            if (_questions) _questions.CollapseContent();
            base.Minimize();
        }
    }
}
