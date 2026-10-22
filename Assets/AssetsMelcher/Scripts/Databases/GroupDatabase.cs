namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// A client-side collection of "groups". Each group has an ID, a sprite
    /// and a debug name. Groups currently are used to sort products into
    /// subcategories of manufacturers and products.
    /// <br /><br />
    ///
    /// The sprite is used in the designer scene's toolbar to show a
    /// manufacturer's or product group's icon.
    /// </summary>
    [CreateAssetMenu(fileName = "GroupCatalog", menuName = "Melcher Studios/Pro-Fit/Group Database")]
    public class GroupDatabase : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public Sprite defaultSprite => m_DefaultSprite;

        public IReadOnlyList<GroupInfo> entries => m_Entries;

        // =====================================================================
        // Methods
        // =====================================================================

        public int GetSortOrder(int id)
        {
            int index =
                MelcherCommon.FindIndex(
                    m_Entries,
                    x => x.id == id
                );

            // Sort by index in catalog. If not found, sort it to the end
            return index >= 0 ? index : int.MaxValue;
        }

        public Sprite GetSprite(int id)
        {
            foreach (GroupInfo i in m_Entries)
            {
                if (i.id == id && i.sprite)
                {
                    return i.sprite;
                }
            }

            return m_DefaultSprite;
        }

        public bool Find(int idToFind, out GroupInfo groupInfo)
        {
            foreach (GroupInfo i in m_Entries)
            {
                if (i.id == idToFind)
                {
                    groupInfo = i;
                    if (groupInfo.sprite == null)
                    {
                        groupInfo.sprite = m_DefaultSprite;
                    }
                    return true;
                }
            }

            groupInfo = default;
            groupInfo.sprite = m_DefaultSprite;
            groupInfo.showCommentOnButton = true;
            groupInfo.useLocalCommentAsOverride = false;
            return false;
        }

#if UNITY_EDITOR
        [ContextMenu("Find index of ID")]
        protected void IndexOfId()
        {
           for(int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].id == m_EditorFindId)
                {
                    Debug.Log($"ID {m_EditorFindId} found at index {i} with label {m_Entries[i].comment}.");
                    return;
                }
            }

            Debug.Log($"ID {m_EditorFindId} not found in catalog.");
        }

        [ContextMenu("Find index of Comment contains")]
        protected void IndexOfComment()
        {
            bool found = false;
            for(int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].comment.Contains(m_EditorFindComment, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"Comment '{m_EditorFindComment}' found at index {i} with ID {m_Entries[i].id}, Comment: {m_Entries[i].comment}.");
                    found = true;
                }
            }
            
            if(!found)
            {                
                Debug.Log($"Comment '{m_EditorFindComment}' not found in catalog.");
            }
        }
#endif

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================
#if UNITY_EDITOR
        [SerializeField] protected string m_EditorFindComment;
        [SerializeField] protected int m_EditorFindId;
#endif

        [Tooltip("The fallback sprite to show when a group entry does not" +
            "provide one.")]
        [SerializeField] protected Sprite m_DefaultSprite = null;
        [SerializeField] protected List<GroupInfo> m_Entries = null;

        // =====================================================================
        // Nested types
        // =====================================================================

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

        }   // struct GroupInfo

    }   // class GroupDatabase

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-22
/// ===========================================================================
