namespace KitchenDesign.Save
{
    using System.Collections.Generic;
    using UnityEngine;
    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    public static class JsonVectors
    {
        public static JsonDict Vector3ToJson(Vector3 vec)
        {
            return new Dictionary<string, object>()
            {
                { "x", vec.x },
                { "y", vec.y },
                { "z", vec.z }
            };
        }
    }
}
