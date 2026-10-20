using UnityEngine;
using UnityEditor;
using Doge.Common;

namespace Doge.EditorScripts
{
    [CustomEditor(typeof(CameraRescaler), true)]
    internal class CameraRescalerEditor : Editor
    {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            CameraRescaler classType = (CameraRescaler)target;
            if(GUILayout.Button("Update Camera Scale"))
            {
                classType.UpdateScale();
            }
        }
    }
}
