#if UNITY_EDITOR
namespace Melcher
{
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    // Editor-only implementation
    public partial class MelcherFindAssetAttribute : MelcherOnValidateAttribute
    {
        /// <summary>
        /// This validator works within a <see cref="ScriptableObject"/> and
        /// <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override bool IsCompatibleCallerType(System.Type callerType)
        {
            return typeof(Object).IsAssignableFrom(callerType);
        }

        protected override void DoValidate(Object caller, FieldInfo field)
        {
            // Ensure non-public fields have [SerializeField] attribute
            LogErrorIfFieldIsNotSerialized(caller, field);

            // When an absolute asset path isn't provided, attempt to find the
            // asset by its type and optional search keyword
            string searchFilter = m_AssetPathOrSearchKeyword;
            string assetPath = m_AssetPathOrSearchKeyword;
            if (string.IsNullOrWhiteSpace(assetPath) ||
                string.IsNullOrWhiteSpace(System.IO.Path.GetDirectoryName(assetPath)))
            {
                searchFilter = $"t:{field.FieldType.Name} {assetPath}";

                string[] guids = AssetDatabase.FindAssets(searchFilter);

                if (guids != null && guids.Length > 0)
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
                else
                {
                    assetPath = null;
                }
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                // No suitable asset path was found
                MelcherLog.LogError(
                    $"{GetFieldFullName(caller, field)} cannot find suitable asset of type " +
                    $"{MelcherFormat.ConsoleTypeName(field.FieldType)} using the search terms: " +
                    $"{MelcherFormat.ConsoleString(searchFilter)}",
                    context: caller
                );
                return;
            }

            Object asset = AssetDatabase.LoadAssetAtPath(assetPath, field.FieldType);
            if (!asset)
            {
                // Could not load asset
                MelcherLog.LogError(
                    $"Failed to load asset at path: {MelcherFormat.ConsoleString(assetPath)}"
                );
                return;
            }

            Object currentValue = field.GetValue(caller) as Object;
            if (currentValue != asset)
            {
                field.SetValue(caller, asset);
                EditorUtility.SetDirty(caller);
            }
        }

    }   // class MelcherFindAssetAttribute

}   // namespace Melcher

#endif  // if UNITY_EDITOR

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-12-19
/// ===========================================================================