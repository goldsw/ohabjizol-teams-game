using UnityEngine;
using UnityEngine.EventSystems;

namespace Ohabjizol.Interview
{
    public sealed class DocumentLibraryButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] DocumentWorkspace workspace;
        public void ConfigureEditor(DocumentWorkspace authoredWorkspace) { workspace = authoredWorkspace; }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                workspace?.ToggleLibrary();
        }
    }
}
