using UnityEngine;
using UnityEngine.UI;
namespace Ohabjizol.Interview
{
    public sealed class InterviewQuestionView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Text _prompt;
        public Button Button => _button;
        public bool HasAuthoredUi => _button && _prompt;
        public void Configure(Button button, Text prompt) { _button = button; _prompt = prompt; }
        public void Bind(string prompt) { _prompt.text = prompt; }
    }
}