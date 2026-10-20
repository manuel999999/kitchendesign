using UnityEngine;
using UnityEngine.UI;

namespace Doge.Common
{
    public class CameraRescaler : MonoBehaviour
    {
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] Camera targetCamera;
        [Tooltip("Try to experiement around different values, start with the default value 1 and try it in different phone resolutions.")]
        [SerializeField] float scaleMultiplier = 1f;

        private void Awake() {
            UpdateScale();
        }

        public void UpdateScale ()
        {
            var screenRatio = Screen.height / Screen.width;
            var canvasReferenceRation = canvasScaler.referenceResolution.y / canvasScaler.referenceResolution.x;

            var scaleFactor = screenRatio / canvasReferenceRation;
            targetCamera.orthographicSize = scaleMultiplier * Mathf.Max (1, scaleFactor);
        }
    }
}
