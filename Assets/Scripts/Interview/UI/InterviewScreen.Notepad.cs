using UnityEngine;
using UnityEngine.UI;

namespace Ohabjizol.Interview
{
    public sealed partial class InterviewScreen
    {
        [Header("Stage notepad")]
        [SerializeField] InterviewNotepadWindow _notepadWindow;
        [SerializeField] Button _notepadButton;
        [SerializeField] InterviewNotepadLauncher _notepadLauncher;

        public InterviewNotepadWindow NotepadWindow => _notepadWindow;
    }
}
