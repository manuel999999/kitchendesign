namespace Melcher
{
    using UnityEngine;

    public class StorageCutoutHelper : MelcherMonoBehaviour
    {
        public enum CutoutDirection
        {
            Auto,
            Up,
            Down
        }

        private const string BEGIN_NAME = "ModelCutOutBegin";
        private const string EXTENT_NAME = "ModelCutOutExtent";
        private const float CUTOUT_OFFSET = 0.0125f;

        [ContextMenu("Bring Begin Here")]
        private void BringBegin()
        {
            Bring(BEGIN_NAME);
        }

        [ContextMenu("Bring Extent Here")]
        private void BringExtent()
        {
            Bring(EXTENT_NAME);
        }

        private void Bring(string modelCutoutName)
        {
            Transform bring = GetSiblingByName(modelCutoutName);

            if(!bring)
            {
                Debug.LogError($"{modelCutoutName} NOT FOUND.");
                return;
            }


            Vector3 offset = GetOffset();
            Vector3 position = GetPosition(bring);
            CutoutDirection usedDirection = GetUsedCutoutDirection();

            if(usedDirection == CutoutDirection.Down)
            {
                bring.localPosition = position - offset;
            }
            else
            {
                bring.localPosition = position + offset;
            }
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

        private Vector3 GetPosition(Transform bring)
        {
            float posX = m_CopyX ? transform.localPosition.x : bring.localPosition.x;
            float posY = m_CopyY ? transform.localPosition.y : bring.localPosition.y;
            float posZ = m_CopyZ ? transform.localPosition.z : bring.localPosition.z;

            return new Vector3(posX, posY, posZ);
        }

        private Vector3 GetOffset()
        {
            float posX = m_CopyX ? CUTOUT_OFFSET : 0f;
            float posY = m_CopyY ? CUTOUT_OFFSET : 0f;
            float posZ = m_CopyZ ? transform.localPosition.z : 0f;

            return new Vector3(posX, posY, posZ);
        }

        private CutoutDirection GetUsedCutoutDirection()
        {
            if(m_CutoutDirection == CutoutDirection.Auto)
            {
                if(transform.localPosition.y > 0)
                {
                    return CutoutDirection.Down;
                }
                else
                {
                    return CutoutDirection.Up;
                }
            }
            else
            {
                return m_CutoutDirection;
            }
        }

        [SerializeField] private CutoutDirection m_CutoutDirection;
        [SerializeField] private bool m_CopyX = true;
        [SerializeField] private bool m_CopyY = true;
        [SerializeField] private bool m_CopyZ;
    }

}

