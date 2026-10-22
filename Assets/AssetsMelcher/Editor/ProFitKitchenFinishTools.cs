namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;

    public static class ProFitKitchenFinishTools
    {
        /// <summary>
        ///     Attempts to use the user's current selection as the base
        ///     material for subsequent, Auto-Create Finish Material
        ///     operations.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Pro-Fit/Finish Tools - Select parent material")]
        private static void SelectParentMaterial()
        {
            m_ParentMaterial = null;

            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject && selectedObject is Material baseMaterial)
                {
                    m_ParentMaterial = baseMaterial;
                    break;
                }
            }

            if (m_ParentMaterial)
            {
                MelcherLog.Log($"Using {m_ParentMaterial.name} as the parent material for subsequent finishes.", m_ParentMaterial);
            }
            else
            {
                MelcherLog.LogError("A material must be selected in the project window.");
                EditorUtility.DisplayDialog(
                    title: $"{nameof(SelectParentMaterial)}() Error",
                    message: "A material must be selected in the project window.",
                    ok: "OK"
                );
                return;
            }
        }

        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Pro-Fit/Finish Tools - Create materials for selected textures")]
        private static void CreateMaterialsForSelectedTextures()
        {
            if (!m_ParentMaterial)
            {
                EditorUtility.DisplayDialog(
                    title: $"{nameof(CreateMaterialsForSelectedTextures)}() Error",
                    message: "A parent material hasn't been selected yet.\n" +
                        "\n" +
                        "Select a material in the project window and then " +
                        "use the Tools menu (Melcher Studios > Pro-Fit) to " +
                        "mark it as selected.",
                    ok: "OK"
                );
                return;
            }

            string outputFolderPath = AssetDatabase.GetAssetPath(m_ParentMaterial);
            int trailingSlashIndex = outputFolderPath.LastIndexOf('/');
            if (trailingSlashIndex >= 0)
            {
                outputFolderPath = outputFolderPath.Remove(trailingSlashIndex + 1);
            }

            int createdMaterialCount = 0;

            foreach (Object selectedObject in Selection.objects)
            {
                Texture texture = selectedObject as Texture;

                if (!texture)
                {
                    continue;
                }

                Material variantMaterial = new Material(m_ParentMaterial.shader);
                variantMaterial.name = texture.name;
                variantMaterial.parent = m_ParentMaterial;

                if (m_ParentMaterial.shader.FindPropertyIndex("_AlbedoMap") >= 0)
                {
                    // Our Triplanar shader uses the name _AlbedoMap for the
                    // primary color texture. Too bad we didn't stick with
                    // _MainTex for simplicity.
                    variantMaterial.SetTexture("_AlbedoMap", texture);
                }
                else
                {
                    variantMaterial.mainTexture = texture;
                }

                string assetPath = outputFolderPath + texture.name + ".mat";

                AssetDatabase.CreateAsset(variantMaterial, assetPath);
                MelcherLog.Log($"Created {assetPath}", variantMaterial);
                ++createdMaterialCount;
            }

            MelcherLog.Log($"Done! Created {createdMaterialCount} materials.");
        }

        private static Material m_ParentMaterial;

    }   // class ProFitKitchenFinishTools

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-10-13
/// ===========================================================================
