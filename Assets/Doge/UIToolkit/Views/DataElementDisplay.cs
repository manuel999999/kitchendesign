using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    public class DataElementDisplay<T> : BaseView
    {
        public DataElementDisplay(VisualElement root, T storedData) : base(root)
        {
            StoredData = storedData;
        }

        public DataElementDisplay(VisualElement source, string targetName, T storedData) : base(source, targetName)
        {
            StoredData = storedData;
        }

        public T StoredData { get; }
    }
}
