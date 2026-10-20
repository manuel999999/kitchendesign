namespace Melcher
{
    using UnityEngine;
    
    [System.Serializable]
    public class CabinetSectionDoorHandleData
    {
        /// <summary>
        /// Cabinet Id that uses this type of handle
        /// </summary>
        public string id;

        public Transform prefab;

        /// <summary>
        /// Count of handles
        /// </summary>
        public int count;
    }
}
