namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    public class ComponentCutoutHelper : MelcherMonoBehaviour
    {
        private const string BEGIN_NAME = "ModelCutOutBegin";
        private const string EXTENT_NAME = "ModelCutOutExtent";
        private const float CUTOUT_OFFSET = 0.0125f;

        [ContextMenu("Bring Cutouts")]
        private void ApplyCutouts()
        {
            BringBegin();
            BringExtent();
        }

        private void BringBegin()
        {
            Transform bring = GetSiblingByName(BEGIN_NAME);

            if(!bring)
            {
                Debug.LogError($"{BEGIN_NAME} NOT FOUND.");
                return;
            }

            bring.localPosition = new Vector3(0f, m_BeginMarker.localPosition.y, 0f);
        }

        private void BringExtent()
        {
            Transform bring = GetSiblingByName(EXTENT_NAME);

            if(!bring)
            {
                Debug.LogError($"{EXTENT_NAME} NOT FOUND.");
                return;
            }

            float extentX = m_ExtentEdgeMarker.localPosition.x - CUTOUT_OFFSET;
            float extentZ = m_ExtentBottomMarker.localPosition.z;

            bring.localPosition = new Vector3(extentX, 0f, extentZ);
        }

        private Transform GetSiblingByName(string name)
        {
            Transform root = gameObject.transform.parent;
            for(int i = 0; i < root.childCount; i++)
            {
                Transform item = root.GetChild(i);
                if(item.name == name)
                {
                    return item;
                }
            }

            return null;
        }

        [SerializeField] private Transform m_BeginMarker;
        [SerializeField] private Transform m_ExtentBottomMarker;
        [SerializeField] private Transform m_ExtentEdgeMarker;
    }   // class ComponentCutoutHelper

}   // namespace KitchenDesign

