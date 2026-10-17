using UnityEngine;
using UnityEngine.EventSystems;
namespace Ohabjizol.Presentation
{
    public sealed class MonitorClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] MainMenuController menu;
        public void Bind(MainMenuController owner) { menu = owner; }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && menu != null) menu.MonitorClicked();
        }
    }
}
