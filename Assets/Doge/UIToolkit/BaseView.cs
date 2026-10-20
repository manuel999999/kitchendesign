using System;
using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public abstract class BaseView : IDisposable
    {
        public VisualElement Root { get; }
        public bool IsDisplayed => HasElement && IsShown();
        public string ShowClass { get; protected set; }
        public string HideClass { get; protected set; }

        protected bool DisplayClassSet { get; set; }

        public BaseView(VisualElement root)
        {
            Root = root;
            HasElement = true;
            DisplayClassSet = false;
        }

        public BaseView(VisualElement source, string targetName)
        {
            Root = source.Query<VisualElement>(targetName);
            HasElement = Root is not null;
            DisplayClassSet = false;
        }

        public bool HasElement { get; }

        public virtual void Show()
        {
            
            if (DisplayClassSet)
            {
                Root.ReplaceClass(HideClass, ShowClass);
            }
            else
            {
                Root.Show();
            }
        }

        public virtual void Hide()
        {
            if (DisplayClassSet)
            {
                Root.ReplaceClass(ShowClass, HideClass);
            }
            else
            {
                Root.Hide();
            }
        }

        public virtual void SetDisplayClasses(string show, string hide)
        {
            ShowClass = show;
            HideClass = hide;
            DisplayClassSet = true;
        }

        protected bool IsShown()
        {
            return DisplayClassSet ? Root.ClassListContains(ShowClass) : Root.IsShown();
        }

        public virtual void Dispose() { }
    }
}
