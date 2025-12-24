using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools
{
    public abstract class SettingsContent
    {
        public int priority;

        public abstract void Setup(VisualElement container);

        public abstract void Reset();
    }
}