namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Find and print every <see cref="Component"/> class.
    /// </summary>
    public static class MelcherClassHierarchyDump
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Dump component class hierarchy")]
        private static void DumpComponentClassHierarchy()
        {
            DumpClassHierarchy<Component>();
        }

        private static void DumpClassHierarchy<RootType>()
            where RootType : class
        {
            List<TypeAndPathNode> allDerivedTypes = new List<TypeAndPathNode>();

            Assembly[] loadedAssemblies =
                System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in loadedAssemblies)
            {
                //MelcherLog.Log("Analyzing assembly: " + assembly.FullName);
                FindDerivedClassesInAssembly<RootType>(assembly, allDerivedTypes);
            }

            allDerivedTypes.Sort((a, b) => a.hierarchyPath.CompareTo(b.hierarchyPath));

            Regex countPathDepth = new Regex("/");

            string output = "\n";

            foreach (TypeAndPathNode node in allDerivedTypes)
            {
                string name = System.IO.Path.GetFileName(node.hierarchyPath);
                int depth = countPathDepth.Matches(node.hierarchyPath).Count;

                // Indent by depth
                for (int i = 0 ; i < depth; ++i)
                {
                    output += "|----";
                }

                output += name + '\n';
            }

            MelcherLog.Log(output);
        }

        private static void FindDerivedClassesInAssembly<RootType>(Assembly assembly, List<TypeAndPathNode> results)
            where RootType : class
        {
            Type rootType = typeof(RootType);

            IEnumerable<Type> derivedTypes = 
                assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(RootType)) || t == rootType);

            foreach (Type derivedType in derivedTypes)
            {
                string path = "";

                Type cursor = derivedType;
                while (cursor != null)
                {
                    if (path.Length == 0)
                    {
                        path = cursor.FullName;
                    }
                    else
                    {
                        path = cursor.FullName + "/" + path;
                    }

                    if (cursor == rootType)
                    {
                        break;
                    }

                    cursor = cursor.BaseType;
                }

                TypeAndPathNode node = new TypeAndPathNode()
                {
                    derivedType = derivedType,
                    hierarchyPath = path
                };

                results.Add(node);
            }
        }

        struct TypeAndPathNode
        {
            public Type derivedType;
            public string hierarchyPath;
        }

    }   // class MelcherClassHierarchyDump

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-23
/// ===========================================================================
