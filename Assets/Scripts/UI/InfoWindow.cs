using UnityEngine;

namespace Ohabjizol.UI
{
    /// <summary>
    /// Window that fills <see cref="WindowToolbar.Info"/> from <see cref="BuildInfo"/> once.
    /// Opening calls <see cref="Refresh"/>.
    /// </summary>
    public abstract class InfoWindow : WindowToolbar
    {
        protected override void OnBuilt()
        {
            BuildInfo(Info);
        }

        protected override void OnOpened()
        {
            Refresh();
        }

        protected abstract void BuildInfo(RectTransform info);

        public virtual void Refresh()
        {
        }
    }
}
