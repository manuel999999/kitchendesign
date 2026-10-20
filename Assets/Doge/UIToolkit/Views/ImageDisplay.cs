using UnityEngine;
using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public class ImageDisplay : BaseView
    {
        public ImageDisplay(VisualElement source, string targetName) : base(source, targetName)
        {
            Image = source.Query<VisualElement>(targetName);
            HasImage = Image is not null;
        }

        public VisualElement Image;
        public bool HasImage { get; }

        protected Sprite _sprite;
        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
                OnSpriteChanged();
            }
        }

        protected Color _backgroundColor;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnBackgroundColorChanged(value);
            }
        }

        protected Color _tint;
        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                OnTintChanged(value);
            }
        }

        protected void OnSpriteChanged()
        {
            if (HasElement == false)
            {
                return;
            }

            Root.style.backgroundImage = new StyleBackground(_sprite);
        }

        protected void OnBackgroundColorChanged(Color color)
        {
            if (HasElement == false)
            {
                return;
            }

            Root.style.backgroundColor = color;
        }

        protected void OnTintChanged(Color color)
        {
            if (HasElement == false)
            {
                return;
            }

            Root.style.unityBackgroundImageTintColor = color;
        } 
    }
}
