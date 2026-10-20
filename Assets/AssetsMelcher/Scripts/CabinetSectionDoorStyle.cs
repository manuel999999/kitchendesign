namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(fileName = "CabinetSectionDoor", menuName = "Melcher Studios/Pro-Fit/Cabinet Section Door")]
    public class CabinetSectionDoorStyle : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public CabinetSectionDoorData[] mapping => m_Doors;


        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] private TextAsset m_Mapping;
        [SerializeField] private CabinetSectionDoorData[] m_Doors;
        
        // =====================================================================
        // Editor
        // =====================================================================
#if UNITY_EDITOR
        [ContextMenu("Build Door Data From Mapping")]
        private void BuildDoorDataFromMapping()
        {
            if (m_Mapping == null)
            {
                Debug.LogError("Mapping is not assigned.", this);
                return;
            }

            string[] lines =
                m_Mapping.text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<CabinetSectionDoorData> doors = new();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split(',');
                if (parts.Length < 2)
                {
                    Debug.LogWarning($"Skipping malformed line {i + 1}: {line}", this);
                    continue;
                }

                string cabinetId = parts[0].Trim();
                string doorId = parts[1].Trim();

                doors.Add(
                    new CabinetSectionDoorData
                    {
                        key = cabinetId,
                        value = doorId
                    }
                );
            }

            m_Doors = doors.ToArray();
            EditorUtility.SetDirty(this);
        }
#endif
    }   // class CabinetSectionDoorStyle

}   // namespace Melcher

