namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Serializables;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "GroupCatalog", menuName = "Melcher Studios/Pro-Fit/Group Database")]
    public class GroupDatabaseSO : MelcherScriptableObject
    {
        public Sprite defaultSprite => m_DefaultSprite;

        public IReadOnlyList<GroupInfo> entries => m_Entries;

        public int GetSortOrder(int id)
        {
            int index =
                MelcherCommon.FindIndex(
                    m_Entries,
                    x => x.id == id
                );

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

#if UNITY_EDITOR
        [SerializeField] protected string m_EditorFindComment;
        [SerializeField] protected int m_EditorFindId;
#endif

        [Tooltip("The fallback sprite to show when a group entry does not" +
            "provide one.")]
        [SerializeField] protected Sprite m_DefaultSprite = null;
        [SerializeField] protected List<GroupInfo> m_Entries = null;
    }
}

