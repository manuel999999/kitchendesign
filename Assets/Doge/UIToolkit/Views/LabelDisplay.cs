using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public class LabelDisplay : BaseView
    {
        public LabelDisplay(VisualElement source, string targetName) : base(source, targetName)
        {
            Label = source.Query<Label>(targetName);
            HasLabel = Label is not null;
        }

        public Label Label { get; }
        public bool HasLabel { get; }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnTextUpdate();
            }
        }

        private void OnTextUpdate()
        {
            if (HasLabel == false)
            {
                return;
            }

            Label.text = _text;
        }
    }
}
