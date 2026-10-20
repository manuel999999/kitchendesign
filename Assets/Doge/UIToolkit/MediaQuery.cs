using UnityEngine;
using UnityEngine.UIElements;

namespace VideoPoker.UIToolkit
{
    [RequireComponent(typeof(UIDocument))]
    public class MediaQuery : MonoBehaviour
    {
        [SerializeField] private PanelSettings landscapeSettings;
        [SerializeField] private PanelSettings portraitSettings;

        private UIDocument _document;
        private VisualElement _root;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _root = GetComponent<UIDocument>().rootVisualElement;

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnDestroy()
        {
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void Start()
        {
            UpdateResolution();
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateResolution();
        }

        public void UpdateResolution()
        {
            bool landscape = Screen.width > Screen.height;

            if (landscape)
            {
                _document.panelSettings = this.landscapeSettings;
            }
            else
            {
                _document.panelSettings = this.portraitSettings;
            }
        }
    }
}
