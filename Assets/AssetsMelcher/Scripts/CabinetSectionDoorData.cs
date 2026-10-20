namespace Melcher
{
    [System.Serializable]
    public class CabinetSectionDoorData
    {
        /// <summary>
        /// Cabinet Id that uses this type of door
        /// </summary>
        public string key;

        /// <summary>
        /// Prefab name of the door model to use for the cabinet section
        /// </summary>
        public string value;
    }
}
