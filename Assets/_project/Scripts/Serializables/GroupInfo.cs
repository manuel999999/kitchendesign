using System;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [Serializable]
    public struct GroupInfo
    {
        [Tooltip("A name shown in the inspector when this type is " +
            "serialized in a collection.")]
        public string comment;

        [Tooltip("The ID that links this group to the corresponding " +
            "product group or manufacturer on the server.")]
        public int id;

        [Tooltip("The image that displays in the designer scene " +
            "toolbar. This can be null to use the GroupCatalog's " +
            "default sprite.")]
        public Sprite sprite;

        public bool showCommentOnButton;

        [Tooltip("The comment will be used as the display name in the " +
            "toolbar, regardless of what is entered in the backend.")]
        public bool useLocalCommentAsOverride;
    }
}
