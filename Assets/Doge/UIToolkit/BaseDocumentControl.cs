using UnityEngine;
using UnityEngine.UIElements;

namespace Doge.UIToolkit
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class BaseDocumentControl : MonoBehaviour
    {
        protected UIDocument sourceDocument;

        public bool IsInitialized { get; protected set; }

        protected virtual void Awake()
        {
            IsInitialized = TryGetComponent(out sourceDocument);

            if (IsInitialized)
            {
                GetDisplays(sourceDocument.rootVisualElement);
                AddClicks();
            }
        }

        protected virtual void OnDestroy()
        {
            if (IsInitialized)
            {
                RemoveClicks();
            }
        }

        protected abstract void GetDisplays(VisualElement root);

        protected abstract void AddClicks();

        protected abstract void RemoveClicks();
    }
}
