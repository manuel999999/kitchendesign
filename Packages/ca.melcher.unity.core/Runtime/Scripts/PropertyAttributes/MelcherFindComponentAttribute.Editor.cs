#if UNITY_EDITOR
namespace Melcher
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    // Editor-only implementation
    public partial class MelcherFindComponentAttribute : MelcherOnValidateAttribute
    {
        /// <summary>
        /// This validator works only within a <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override bool IsCompatibleCallerType(System.Type callerType)
        {
            return typeof(MonoBehaviour).IsAssignableFrom(callerType);
        }

        protected override void DoValidate(Object caller, FieldInfo field)
        {
            // Ensure non-public fields have [SerializeField] attribute
            LogErrorIfFieldIsNotSerialized(caller, field);

            Component callingComponent = caller as Component;

            // Make sure the scene is loaded before calling PrefabUtility.IsPartOfPrefabInstance()
            // as, if it's not, Unity will log a bunch of warnings whenever the
            // loaded scenes change in the editor.
            if (!callingComponent || !callingComponent.gameObject.scene.isLoaded)
            {
                return;
            }

            // If the component belongs to any type of prefab, enforce the
            // attribute's special prefab rules
            if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(callingComponent))
            {
                switch (m_SearchFromPrefabPermission)
                {
                    default:
                        throw new System.NotImplementedException($"Unhandled 'search from prefab' permission: {m_SearchFromPrefabPermission}");

                    case MelcherFindComponentSearchFromPrefabPermission.OnlyWithinRootAsset:
                        // The default and most strict behaviour. Only prefab
                        // assets will search for a value
                        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(callingComponent))
                        {
                            return;
                        }
                        break;

                    case MelcherFindComponentSearchFromPrefabPermission.OnlyWithinAsset:
                        // Allow prefab instances within prefab assets to
                        // search
                        if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(callingComponent))
                        {
                            return;
                        }
                        break;

                    case MelcherFindComponentSearchFromPrefabPermission.Anywhere:
                        // Anything goes.
                        break;
                }
            }

            bool wasValueChanged = false;

            int errorIfFewerThan = m_ErrorIfFewerThan;
            int errorIfMoreThan = m_ErrorIfMoreThan;

            if (IsListType(field.FieldType))
            {
                // Field is an array or IList.

                // If the min amount is unspecified, allow any amount
                if (errorIfFewerThan < 0)
                {
                    errorIfFewerThan = 0;
                }

                // If the max amount is unspecified, allow any amount
                if (errorIfMoreThan < 0)
                {
                    errorIfMoreThan = int.MaxValue;
                }

                string errorWithList = TryGetListElementType(field.FieldType, out System.Type elementType);

                if (errorWithList != null)
                {
                    // Something about the list's element type was bad
                    MelcherLog.LogError(
                        $"{GetFieldFullName(caller, field)} {errorWithList}",
                        context: callingComponent
                    );
                }
                else
                {
                    Component[] values =
                        SearchForAll(
                            searchFrom:     callingComponent,
                            searchMode:     m_SearchMode,
                            typeToFind:     elementType,
                            minDistance:    m_MinDistance,
                            count:          m_Count,
                            includeInactiveGameObjects: m_IncludeInactiveGameObjects,
                            includeInactiveComponents:  m_IncludeInactiveComponents
                        );

                    if (values == null || values.Length < errorIfFewerThan)
                    {
                        if (ShouldSearchLogComponentNotFoundError(m_SearchMode, m_SearchFromPrefabPermission, callingComponent))
                        {
                            MelcherLog.LogError(
                                $"{GetFieldFullName(caller, field)} too few values found using {MelcherFormat.ConsoleSymbolName(m_SearchMode)} search. Expected at least {errorIfFewerThan} but found {values.Length}.",
                                context: callingComponent
                            );
                        }
                    }
                    else if (values.Length > errorIfMoreThan)
                    {
                        if (ShouldSearchLogComponentNotFoundError(m_SearchMode, m_SearchFromPrefabPermission, callingComponent))
                        {
                            MelcherLog.LogError(
                                $"{GetFieldFullName(caller, field)} too many values found using {MelcherFormat.ConsoleSymbolName(m_SearchMode)} search. Expected at most {errorIfMoreThan} but found {values.Length}.",
                                context: callingComponent
                            );
                        }
                    }
                    else
                    {
                        wasValueChanged = UpdateListValues(caller, field, elementType, values);
                    }
                }
            }
            else if (typeof(Component).IsAssignableFrom(field.FieldType))
            {
                if (errorIfMoreThan != -1)
                {
                    MelcherLog.LogWarning($"{nameof(m_ErrorIfMoreThan)} is ignored during a single component search.", callingComponent);
                }

                // If the min amount is unspecified, error if nothing is found
                if (errorIfFewerThan < 0)
                {
                    errorIfFewerThan = 1;
                }

                if (errorIfFewerThan > 1)
                {
                    MelcherLog.LogWarning($"{nameof(m_ErrorIfFewerThan)} is reduced to 1 during a single component search.", callingComponent);
                }

                Component value =
                    SearchForOne(
                        searchFrom:     callingComponent,
                        searchMode:     m_SearchMode,
                        typeToFind:     field.FieldType,
                        minDistance:    m_MinDistance,
                        count:          m_Count,
                        includeInactiveGameObjects: m_IncludeInactiveGameObjects,
                        includeInactiveComponents:  m_IncludeInactiveComponents
                    );

                if (value == null && errorIfFewerThan > 0)
                {
                    if (ShouldSearchLogComponentNotFoundError(m_SearchMode, m_SearchFromPrefabPermission, callingComponent))
                    {
                        MelcherLog.LogError(
                            $"{GetFieldFullName(caller, field)} no suitable value found using {MelcherFormat.ConsoleSymbolName(m_SearchMode)} search",
                            context: callingComponent
                        );
                    }
                }
                else if (!Equals(value, field.GetValue(caller)))
                {
                    wasValueChanged = true;
                    field.SetValue(caller, value);
                }
            }
            else
            {
                // Cannot find non-Components
                MelcherLog.LogError(
                    $"{GetFieldFullName(caller, field)} must be a type that derives from {MelcherFormat.ConsoleTypeName<Component>(useFullName: true)}",
                    context: callingComponent
                );
            }

            if (wasValueChanged)
            {
                // Mark the object as dirty so the change sticks
                UnityEditor.EditorUtility.SetDirty(callingComponent);
            }
        }

        /// <summary>
        ///     This method determines whether an error for a search that
        ///     fails to find the requested component should be logged to the
        ///     console.
        /// </summary>
        private static bool ShouldSearchLogComponentNotFoundError(
            MelcherFindComponentMode searchMode,
            MelcherFindComponentSearchFromPrefabPermission allowablePrefabsToSearchFrom,
            Component theSearchingComponent)
        {
            bool isSceneSearch = searchMode == MelcherFindComponentMode.Scene;
            bool isSearchAllowedFromAnyPrefab = allowablePrefabsToSearchFrom == MelcherFindComponentSearchFromPrefabPermission.Anywhere;
            bool isPrefabAsset =
                UnityEditor.PrefabUtility.IsPartOfPrefabAsset(theSearchingComponent) ||
                MelcherEditorUtility.IsPartOfCurrentPrefabStage(theSearchingComponent.gameObject);

            // If the search is coming from a prefab asset but the search is
            // also allowed to occur in a prefab instance, we will not log an
            // error if the component isn't found. It is assumed the user
            // intends to find the value outside of the prefab and thus logging
            // an error only adds log clutter.
            return !(isSceneSearch && isSearchAllowedFromAnyPrefab && isPrefabAsset);
        }

        private static Component SearchForOne(
            Component                   searchFrom,
            MelcherFindComponentMode    searchMode,
            System.Type                 typeToFind,
            int                         minDistance,
            int                         count,
            bool                        includeInactiveGameObjects,
            bool                        includeInactiveComponents)
        {
            Component foundComponent = null;

            switch (searchMode)
            {
                default:
                    throw new System.NotImplementedException("Unhandled search mode: " + searchMode);

                case MelcherFindComponentMode.Self:
                    // Warn if unused values differ from their default.
                    if (minDistance != 0)
                    {
                        MelcherLog.LogWarning($"{nameof(minDistance)} is ignored by {nameof(MelcherFindComponentMode.Self)}.\n\nResponsible type: {searchFrom.GetType().FullName}", searchFrom);
                    }
                    if (count != -1)
                    {
                        MelcherLog.LogWarning($"{nameof(count)} is ignored by {nameof(MelcherFindComponentMode.Self)}.\n\nResponsible type: {searchFrom.GetType().FullName}", searchFrom);
                    }
                    if (includeInactiveGameObjects != false)
                    {
                        MelcherLog.LogWarning(
                            $"{nameof(includeInactiveGameObjects)} is ignored by {nameof(MelcherFindComponentMode.Self)}.\n\nResponsible type: {searchFrom.GetType().FullName}",
                            searchFrom
                        );
                    }

                    foundComponent = MelcherSearch.FirstFromTransformDownward(
                        componentType:  typeToFind,
                        searchFrom:     searchFrom.transform,
                        count:          1,
                        includeInactiveGameObjects: true,
                        includeInactiveComponents:  includeInactiveComponents
                    );

                    break;

                case MelcherFindComponentMode.Down:
                    foundComponent = MelcherSearch.FirstFromTransformDownward(
                        componentType:  typeToFind,
                        searchFrom:     searchFrom.transform,
                        minDistance:    minDistance,
                        count:          count,
                        includeInactiveGameObjects: includeInactiveGameObjects,
                        includeInactiveComponents:  includeInactiveComponents
                    );
                    break;

                case MelcherFindComponentMode.Up:
                    foundComponent = MelcherSearch.FirstFromTransformUpward(
                        componentType:  typeToFind,
                        searchFrom:     searchFrom.transform,
                        minDistance:    minDistance,
                        count:          count,
                        includeInactiveGameObjects: includeInactiveGameObjects,
                        includeInactiveComponents:  includeInactiveComponents
                    );

                    break;

                case MelcherFindComponentMode.Scene:
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(searchFrom))
                    {
                        // Handle prefabs assets. Prefab assets don't have a
                        // true "scene", but we can use everything in the
                        // prefab asset's hierarchy.

                        Transform prefabRoot = GetPrefabRoot(searchFrom);

                        foundComponent = MelcherSearch.FirstFromTransformDownward(
                            componentType:  typeToFind,
                            searchFrom:     prefabRoot,
                            minDistance:    minDistance,
                            count:          count,
                            includeInactiveGameObjects: includeInactiveGameObjects,
                            includeInactiveComponents:  includeInactiveComponents
                        );
                    }
                    else
                    {
                        // Handle prefab instances and normal game objects.

                        if (!searchFrom.gameObject.scene.IsValid() || !searchFrom.gameObject.scene.isLoaded)
                        {
                            // Not sure if this will ever happen, but it's a
                            // nice safety check when dealing with prefabs
                            // because they don't have scenes. -Ryan

                            // This code is duplicated in `SearchForAll`.

                            throw new System.ArgumentException(
                                $"Can't perform {MelcherFormat.ConsoleSymbolName(searchMode)} search because {MelcherUnityUtility.GetHierarchyPath(searchFrom.transform)} is not in a valid scene.",
                                nameof(searchFrom)
                            );
                        }

                        foundComponent = MelcherSearch.FirstFromScene(
                            componentType:  typeToFind,
                            sceneToSearch:  searchFrom.gameObject.scene,
                            minDistance:    minDistance,
                            count:          count,
                            includeInactiveGameObjects: includeInactiveGameObjects,
                            includeInactiveComponents:  includeInactiveComponents
                        );
                    }
                    break;
            }

            return foundComponent;
        }

        private static Component[] SearchForAll(
            Component                   searchFrom,
            MelcherFindComponentMode    searchMode,
            System.Type                 typeToFind,
            int                         minDistance,
            int                         count,
            bool                        includeInactiveGameObjects,
            bool                        includeInactiveComponents)
        {
            s_SharedComponentResults.Clear();

            switch (searchMode)
            {
                default:
                    throw new System.NotImplementedException("Unhandled search mode: " + searchMode);

                case MelcherFindComponentMode.Self:
                    // Warn if unused values differ from their default.
                    if (minDistance != 0)
                    {
                        MelcherLog.LogWarning($"{nameof(minDistance)} is ignored by {nameof(MelcherFindComponentMode.Self)}.", searchFrom);
                    }
                    if (count != -1)
                    {
                        MelcherLog.LogWarning($"{nameof(count)} is ignored by {nameof(MelcherFindComponentMode.Self)}.", searchFrom);
                    }
                    if (includeInactiveGameObjects != false)
                    {
                        MelcherLog.LogWarning($"{nameof(includeInactiveGameObjects)} is ignored by {nameof(MelcherFindComponentMode.Self)}.", searchFrom);
                    }

                    MelcherSearch.AllFromTransformDownward(
                        componentType:  typeToFind,
                        results:        s_SharedComponentResults,
                        searchFrom:     searchFrom.transform,
                        count:          1,
                        includeInactiveGameObjects: true,
                        includeInactiveComponents:  includeInactiveComponents
                    );

                    break;

                case MelcherFindComponentMode.Down:
                    MelcherSearch.AllFromTransformDownward(
                        componentType:  typeToFind,
                        results:        s_SharedComponentResults,
                        searchFrom:     searchFrom.transform,
                        minDistance:    minDistance,
                        count:          count,
                        includeInactiveGameObjects: includeInactiveGameObjects,
                        includeInactiveComponents:  includeInactiveComponents
                    );
                    break;

                case MelcherFindComponentMode.Up:
                    MelcherSearch.AllFromTransformUpward(
                        componentType:  typeToFind,
                        results:        s_SharedComponentResults,
                        searchFrom:     searchFrom.transform,
                        minDistance:    minDistance,
                        count:          count,
                        includeInactiveGameObjects: includeInactiveGameObjects,
                        includeInactiveComponents: includeInactiveComponents
                    );

                    break;

                case MelcherFindComponentMode.Scene:
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(searchFrom))
                    {
                        // Handle prefabs assets. Prefab assets don't have a
                        // true "scene", but we can use everything in the
                        // prefab asset's hierarchy.

                        Transform prefabRoot = GetPrefabRoot(searchFrom);

                        MelcherSearch.AllFromTransformDownward(
                            componentType:  typeToFind,
                            results:        s_SharedComponentResults,
                            searchFrom:     prefabRoot,
                            minDistance:    minDistance,
                            count:          count,
                            includeInactiveGameObjects: includeInactiveGameObjects,
                            includeInactiveComponents
                        );
                    }
                    else
                    {
                        // Handle prefab instances and normal game objects.

                        if (!searchFrom.gameObject.scene.IsValid() || !searchFrom.gameObject.scene.isLoaded)
                        {
                            // Not sure if this will ever happen, but it's a
                            // nice safety check when dealing with prefabs
                            // because they don't have scenes. -Ryan

                            // This code is duplicated in `SearchForAll`.

                            throw new System.ArgumentException(
                                $"Can't perform {MelcherFormat.ConsoleSymbolName(searchMode)} search because {MelcherUnityUtility.GetHierarchyPath(searchFrom.transform)} is not in a valid scene.",
                                nameof(searchFrom)
                            );
                        }

                        MelcherSearch.AllFromScene(
                            componentType:  typeToFind,
                            results:        s_SharedComponentResults,
                            sceneToSearch:  searchFrom.gameObject.scene,
                            minDistance:    minDistance,
                            count:          count,
                            includeInactiveGameObjects: includeInactiveGameObjects,
                            includeInactiveComponents:  includeInactiveComponents
                        );
                    }
                    break;
            }

            return s_SharedComponentResults.ToArray();
        }

        private static bool IsListType(System.Type type)
        {
            return type.IsArray || typeof(IList).IsAssignableFrom(type);
        }

        private static string TryGetListElementType(System.Type type, out System.Type elementType)
        {
            string error = null;
            elementType = null;

            if (type.IsArray)
            {
                // Plain old array

                if (type.GetArrayRank() != 1)
                {
                    error = $"must be a one-dimension array";
                }
                else
                {
                    elementType = type.GetElementType();
                }
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                // If it takes generic arguments, it must be a List<>

                System.Type[] genericTypeArguments = type.GenericTypeArguments;
                if (genericTypeArguments.Length != 1)
                {
                    error = $"must have single template type argument";
                }
                else
                {
                    elementType = genericTypeArguments[0];
                }
            }
            else
            {
                throw new System.NotImplementedException();
            }

            if (elementType != null)
            {
                // Ensure the extracted element type is valid. It shouldn't be
                // an array or to a non-Component type

                if (elementType.IsArray)
                {
                    error = "cannot be a collection of arrays";
                }
                else if (!typeof(Component).IsAssignableFrom(elementType))
                {
                    error = $"must use an element type that derives from {MelcherFormat.ConsoleTypeName<Component>(useFullName: true)}";
                }
            }

            if (error != null)
            {
                // Don't emit a type if an error was occurred
                elementType = null;
            }

            return error;
        }

        /// <summary>
        ///     Get the root <see cref="Transform"/> from a prefab object.
        /// </summary>
        private static Transform GetPrefabRoot(Component prefabComponent)
        {
            if (prefabComponent == null)
            {
                throw new System.ArgumentNullException(nameof(prefabComponent));
            }

            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(prefabComponent))
            {
                throw new System.ArgumentException("Not part of a prefab.", nameof(prefabComponent));
            }

            // To retrieve a a prefab's root, PrefabUtility.FindPrefabRoot()
            // documents to use PrefabUtility.GetOutermostPrefabInstanceRoot()
            // if the source is source is a Prefab instance; otherwise, it
            // says to use `source.transform.root.gameObject` if the source is
            // a Prefab Asset object.

            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefabComponent))
            {
                return prefabComponent.transform.root;
            }

            MelcherLog.Assert(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(prefabComponent));

            return
                UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(prefabComponent).transform;

        }

        private bool UpdateListValues(Object caller, FieldInfo field, System.Type elementType, Component[] newValues)
        {
            // Determine if the new list differs from the original
            IList originalValues = (IList)field.GetValue(caller);

            bool isDifferent = originalValues == null || newValues.Length != originalValues.Count;

            if (!isDifferent)
            {
                // The sizes match so we must compare the elements
                int arrayLength = originalValues.Count;
                for (int i = 0; i < arrayLength && !isDifferent; ++i)
                {
                    isDifferent = !object.ReferenceEquals(originalValues[i], newValues[i]);
                }
            }

            if (isDifferent)
            {
                if (field.FieldType.IsArray)
                {
                    // For arrays, we must cast the input Component array to the
                    // field's expected array type

                    System.Array castedValues =
                        System.Array.CreateInstance(elementType, newValues.Length);

                    System.Array.Copy(
                        sourceArray: newValues,
                        destinationArray: castedValues,
                        length: newValues.Length
                    );

                    field.SetValue(caller, castedValues);
                }
                else
                {
                    // Rebuild list
                    IList values = (IList)field.GetValue(caller);

                    values.Clear();

                    foreach (Component value in newValues)
                    {
                        values.Add(value);
                    }
                }
            }

            return isDifferent;
        }

        // =====================================================================
        // Static members
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static List<Component> s_SharedComponentResults = new List<Component>();

    }   // class MelcherFindComponentAttribute

}   // namespace Melcher

#endif  // if UNITY_EDITOR

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-19
/// ===========================================================================
