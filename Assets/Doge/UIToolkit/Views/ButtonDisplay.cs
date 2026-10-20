using UnityEngine;
using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public class ButtonDisplay : BaseView
    {
        public ButtonDisplay(VisualElement source, string buttonName) : base(source, buttonName)
        {
            Button = source.Query<Button>(buttonName);
            HasButton = Button is not null;
        }

        public Button Button { get; }
        public bool HasButton { get; }

        protected ElementDisplay _highlightElement;
        protected bool _interactable;
        public virtual bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                OnInteractableSet();
            }
        }

        protected void OnInteractableSet()
        {
            if (HasButton == false)
            {
                return;
            }
            
            Button.SetEnabled(_interactable);
        }

        public void SetText(string text)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.text = text;
        }

        public void SetImage(Sprite sprite)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.style.backgroundImage = new StyleBackground(sprite);
        }

        public void AddClick(EventCallback<ClickEvent> callback)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.RegisterCallback(callback);
        }

        public void AddClick<TUserArgsType>(EventCallback<ClickEvent, TUserArgsType> callback, TUserArgsType args)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.RegisterCallback(callback, args);
        }

        public void RemoveClick(EventCallback<ClickEvent> callback)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.UnregisterCallback(callback);
        }

        public void RemoveClick<TUserArgsType>(EventCallback<ClickEvent, TUserArgsType> callback)
        {
            if (HasButton == false)
            {
                return;
            }

            Button.UnregisterCallback(callback);
        }
    }
}
