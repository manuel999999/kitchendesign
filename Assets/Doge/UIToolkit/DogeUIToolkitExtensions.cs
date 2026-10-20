using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public static class DogeUIToolkitExtensions
    {
        public static void Show(this VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        public static void Hide(this VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        public static bool IsHidden(this VisualElement element)
        {
            return element.style.display == DisplayStyle.None;
        }

        public static bool IsShown(this VisualElement element)
        {
            return element.style.display == DisplayStyle.Flex;
        }

        public static void ReplaceClass(this VisualElement element, string replace, string insert)
        {
            element.RemoveFromClassList(replace);

            if (element.ClassListContains(insert) == false)
            {
                element.AddToClassList(insert);
            }
            
        }
    }
}
