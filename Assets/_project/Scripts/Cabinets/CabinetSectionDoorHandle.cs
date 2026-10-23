namespace KitchenDesign.Cabinets
{
    using Melcher;
    using System;
    using UnityEngine;
    using KitchenDesign.Serializables;
    using KitchenDesign.Scriptables;

    public class CabinetSectionDoorHandle : MonoBehaviour
    {
#region Static methods
        // ====================================================================
        // Static methods
        // ====================================================================

        public static void InstantiateHandle(
            CabinetSectionDoorHandleStyleSO   doorHandleStyle,
            string                          cabinetBaseVisualId,
            string                          selectedDoorHandleStyle,
            Transform                       cabinet,
            Transform                       parent)
        {
            if(doorHandleStyle == default)
            {
                return;
            }

            string usedCabinetVisualId;

            if(cabinetBaseVisualId.IndexOf('@') > 0)
            {
                usedCabinetVisualId = cabinetBaseVisualId.Split('@')[0];
            }
            else
            {
                usedCabinetVisualId = cabinetBaseVisualId;
            }      

            // TODO: DOOR HANDLES: Try requesting a door handle using the cabinet door id instead of cabinet base id
            string customUsedId = ProFitGlobals.catalogData.DoorStyles.FindMappedDoorId(usedCabinetVisualId);

            if(customUsedId != string.Empty)
            {
                usedCabinetVisualId = customUsedId;
            }

            CabinetSectionDoorHandleData handleData = GetDoorHandleDataByCabinetBaseId(doorHandleStyle, usedCabinetVisualId);

            if(handleData == null || handleData.prefab == null)
            {
                MelcherLog.LogError($"Cannot find Door Handle Prefab for {usedCabinetVisualId}.");
                return;
            }

            Transform doorHandleInstance = Instantiate(handleData.prefab, parent);

            doorHandleInstance.localScale = cabinet.localScale;

            doorHandleInstance.gameObject.AddComponent<CabinetSectionDoorHandle>();
            CabinetSectionDoorHandle handle = doorHandleInstance.GetComponent<CabinetSectionDoorHandle>();
            handle.hanndleCount = handleData.count;

            handle.ActivateDoorHandleStyle(selectedDoorHandleStyle);
        }

        protected static CabinetSectionDoorHandleData GetDoorHandleDataByCabinetBaseId(CabinetSectionDoorHandleStyleSO doorHandleStyle, string cabinetBaseVisualId)
        {
            if (doorHandleStyle == default)
            {
                return null;
            }

            CabinetSectionDoorHandleData[] handles = doorHandleStyle.prefabs;
            if (handles != null)
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    CabinetSectionDoorHandleData handleData = handles[i];
                    if (handleData == null || string.IsNullOrWhiteSpace(handleData.id))
                    {
                        continue;
                    }

                    if (string.Equals(handleData.id, cabinetBaseVisualId, StringComparison.OrdinalIgnoreCase))
                    {
                        MelcherLog.Log($"Found Door Handle Prefab for {cabinetBaseVisualId} : {handleData.id}", handleData.prefab);
                        return handleData;
                    }
                }
            }

            MelcherLog.LogError($"Cannot find Door Handle Prefab for {cabinetBaseVisualId}.");
            return null;
        }

#if UNITY_EDITOR
        public static Transform InstantiateHandleEditor(
            CabinetSectionDoorHandleStyleSO   doorHandleStyle,
            string                          cabinetBaseVisualId,
            string                          selectedDoorHandleStyle,
            Transform                       parent)
        {
            if(doorHandleStyle == default)
            {
                return default;
            }

            string usedCabinetVisualId;

            if(cabinetBaseVisualId.IndexOf('@') > 0)
            {
                usedCabinetVisualId = cabinetBaseVisualId.Split('@')[0];
            }
            else
            {
                usedCabinetVisualId = cabinetBaseVisualId;
            }         

            CabinetSectionDoorHandleData handleData = GetDoorHandleDataByCabinetBaseId(doorHandleStyle, usedCabinetVisualId);

            if(handleData == null || handleData.prefab == null)
            {
                MelcherLog.LogError($"Cannot find Door Handle Prefab for {usedCabinetVisualId}.");
                return default;
            }

            Transform doorHandleInstance = (Transform)UnityEditor.PrefabUtility.InstantiatePrefab(handleData.prefab, parent);

            doorHandleInstance.gameObject.AddComponent<CabinetSectionDoorHandle>();
            CabinetSectionDoorHandle handle = doorHandleInstance.GetComponent<CabinetSectionDoorHandle>();

            handle.ActivateDoorHandleStyle(selectedDoorHandleStyle);

            MelcherLog.Log($"Creating Handle for thumbnail: {handle.name}", handleData.prefab);

            return doorHandleInstance;
        }
#endif

#endregion /Static Methods

#region Methods
        // ====================================================================
        // Methods
        // ====================================================================
        private void Awake()
        {
            EventBridge.Designer.onDoorHandleStyleNameUpdate += ActivateDoorHandleStyle;
        }

        private void OnDestroy()
        {
            EventBridge.Designer.onDoorHandleStyleNameUpdate -= ActivateDoorHandleStyle;
        }

        public void ActivateDoorHandleStyle(string doorHandleStyle)
        {
            if(doorHandleStyle == "Default")
            {
                return;
            }

            TryInitializeHandleStyles();

            for(int i = 0; i < m_Handles.Length;i ++)
            {
                GameObject handle = m_Handles[i];

                if(handle.name.ToLower().EndsWith(doorHandleStyle.ToLower()))
                {
                    handle.SetActive(true);
                }
                else
                {
                    handle.SetActive(false);
                }
            }
        }

        private void TryInitializeHandleStyles()
        {
            if(m_IsInitialized)
            {
                return;
            }

            int childCount = transform.childCount;
            m_Handles = new GameObject[childCount];

            for(int i = 0; i < childCount; i++)
            {
                GameObject handle = transform.GetChild(i).gameObject;   
                m_Handles[i] = handle;
                
                handle.SetActive(false);
            }

            m_IsInitialized = true;
        }


#endregion /Methods

#region Fields
        // =====================================================================
        // Fields
        // =====================================================================
        public int hanndleCount = 0;

        protected GameObject[] m_Handles;
        protected bool m_IsInitialized = false;

#endregion /Fields

    }   // class CabinetSectionDoorHandle
}   // namespace KitchenDesign
