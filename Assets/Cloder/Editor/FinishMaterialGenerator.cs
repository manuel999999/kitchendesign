namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.ProductsEditor;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UIElements;

    public class FinishMaterialGenerator : EditorWindow
    {
        private const string WINDOW_TITLE = "Finish Material Generator";
        private const string MENU_PATH = "Tools/Melcher Studios/Pro-Fit/Finish Material Generator";
        private const string FINISHES_FOLDER = "Assets/AssetsMelcher/Finishes";
        private const string TILE_PROPERTY = "Tile";
        private const float PREVIEW_HEIGHT = 220.0f;

        [MenuItem(MENU_PATH)]
        private static void ShowWindow()
        {
            FinishMaterialGenerator window = GetWindow<FinishMaterialGenerator>(WINDOW_TITLE);
            window.minSize = new Vector2(420.0f, 520.0f);
            window.Show();
        }

        private void OnFocus()
        {
            if (m_ImagesView != null)
            {
                RefreshImages();
            }
        }

        private void OnDisable()
        {
            DestroyPreviewEditor();
        }

        private void CreateGUI()
        {
            if (m_Template == null && Selection.activeObject is Material selectedMaterial)
            {
                m_Template = selectedMaterial;
            }

            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 4.0f;
            root.style.paddingRight = 4.0f;
            root.style.paddingTop = 4.0f;
            root.style.paddingBottom = 4.0f;

            m_TemplateField = new ObjectField("Template material");
            m_TemplateField.objectType = typeof(Material);
            m_TemplateField.allowSceneObjects = false;
            m_TemplateField.tooltip = "Each new material is a copy of this one, with its image in the image slot and Tile set to 1.";
            m_TemplateField.SetValueWithoutNotify(m_Template);
            m_TemplateField.RegisterValueChangedCallback(OnTemplateChanged);
            m_TemplateField.style.flexShrink = 0.0f;
            root.Add(m_TemplateField);

            m_SlotField = new DropdownField("Image slot");
            m_SlotField.tooltip = "The template's texture slot that gets each image.";
            m_SlotField.RegisterValueChangedCallback(changeEvent =>
            {
                m_TextureSlot = changeEvent.newValue;
                RefreshCreateButton();
            });
            m_SlotField.style.flexShrink = 0.0f;
            root.Add(m_SlotField);

            m_PreviewContainer = new IMGUIContainer(DrawPreview);
            m_PreviewContainer.style.height = PREVIEW_HEIGHT;
            m_PreviewContainer.style.flexShrink = 0.0f;
            m_PreviewContainer.style.marginTop = 4.0f;
            root.Add(m_PreviewContainer);

            VisualElement buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.flexWrap = Wrap.Wrap;
            buttonRow.style.flexShrink = 0.0f;
            buttonRow.style.marginTop = 4.0f;
            root.Add(buttonRow);

            Button pickButton = new Button(PickImages) { text = "Pick Images..." };
            pickButton.tooltip = $"Pick images in {FINISHES_FOLDER}.";
            buttonRow.Add(pickButton);

            Button addSelectionButton = new Button(AddProjectSelection) { text = "Add Project Selection" };
            addSelectionButton.tooltip = $"Add the images selected in the Project window. Only images in {FINISHES_FOLDER} are added.";
            buttonRow.Add(addSelectionButton);

            Button removeButton = new Button(RemoveSelectedImages) { text = "Remove Selected" };
            buttonRow.Add(removeButton);

            Button clearButton = new Button(() =>
            {
                m_Images.Clear();
                RefreshImages();
            }) { text = "Clear" };
            buttonRow.Add(clearButton);

            m_ImagesLabel = MakeLabel();
            root.Add(m_ImagesLabel);

            m_ImagesView = new ListView(m_Images, 20.0f, MakeImageRow, BindImageRow);
            m_ImagesView.selectionType = SelectionType.Multiple;
            m_ImagesView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_ImagesView.itemsChosen += chosen =>
            {
                if (chosen.FirstOrDefault() is Texture2D image)
                {
                    EditorGUIUtility.PingObject(image);
                }
            };
            m_ImagesView.style.flexGrow = 1.0f;
            root.Add(m_ImagesView);

            m_CreateButton = new Button(CreateMaterials) { text = "Create Materials" };
            m_CreateButton.style.flexShrink = 0.0f;
            m_CreateButton.style.height = 26.0f;
            m_CreateButton.style.marginTop = 4.0f;
            root.Add(m_CreateButton);

            RefreshTemplate();
            RefreshImages();
        }

        private static Label MakeLabel()
        {
            Label label = new Label();
            label.style.marginLeft = 4.0f;
            label.style.marginTop = 4.0f;
            label.style.marginBottom = 4.0f;

            return label;
        }

        private void OnTemplateChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            m_Template = changeEvent.newValue as Material;
            m_TextureSlot = FindDefaultTextureSlot(m_Template);
            RefreshTemplate();
        }

        private void RefreshTemplate()
        {
            DestroyPreviewEditor();

            if (m_Template != null)
            {
                m_PreviewEditor = Editor.CreateEditor(m_Template);
            }

            List<string> slots = GetTextureSlots(m_Template);

            if (!slots.Contains(m_TextureSlot))
            {
                m_TextureSlot = FindDefaultTextureSlot(m_Template);
            }

            m_SlotField.choices = slots;
            m_SlotField.SetValueWithoutNotify(m_TextureSlot);
            m_SlotField.SetEnabled(slots.Count > 0);

            m_PreviewContainer.style.display =
                m_PreviewEditor != null ? DisplayStyle.Flex : DisplayStyle.None;

            RefreshCreateButton();
        }

        private void DrawPreview()
        {
            if (m_PreviewEditor == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                m_PreviewEditor.OnPreviewSettings();
            }

            Rect previewRect = GUILayoutUtility.GetRect(
                0.0f,
                0.0f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            m_PreviewEditor.OnInteractivePreviewGUI(previewRect, GUIStyle.none);
        }

        private void DestroyPreviewEditor()
        {
            if (m_PreviewEditor != null)
            {
                DestroyImmediate(m_PreviewEditor);
                m_PreviewEditor = null;
            }
        }

        private static List<string> GetTextureSlots(Material material)
        {
            List<string> slots = new ();

            if (material == null || material.shader == null)
            {
                return slots;
            }

            Shader shader = material.shader;

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                {
                    continue;
                }

                string slot = shader.GetPropertyName(i);
                bool isHidden = (shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0;

                if (!isHidden || material.GetTexture(slot) != null)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static string FindDefaultTextureSlot(Material material)
        {
            List<string> slots = GetTextureSlots(material);

            string slot =
                slots.FirstOrDefault(name => material.GetTexture(name) is Texture texture && texture.name == material.name)
                ?? slots.FirstOrDefault(name => material.GetTexture(name) != null)
                ?? slots.FirstOrDefault();

            return slot ?? "";
        }

        private static bool IsInFinishesFolder(string assetPath)
        {
            return assetPath.StartsWith(FINISHES_FOLDER + "/", StringComparison.Ordinal);
        }

        private static List<Texture2D> FindFinishImages()
        {
            List<Texture2D> images = new ();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FINISHES_FOLDER }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileName(assetPath).Contains("__"))
                {
                    continue;
                }

                Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (image != null)
                {
                    images.Add(image);
                }
            }

            images.Sort(static (a, b) => EditorUtility.NaturalCompare(a.name, b.name));

            return images;
        }

        private static Dictionary<string, string> FindExistingMaterialPaths()
        {
            Dictionary<string, string> paths = new (StringComparer.OrdinalIgnoreCase);

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { FINISHES_FOLDER }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                paths.TryAdd(Path.GetFileNameWithoutExtension(assetPath), assetPath);
            }

            return paths;
        }

        private void PickImages()
        {
            List<Texture2D> candidates = FindFinishImages();
            Dictionary<string, string> existingMaterials = FindExistingMaterialPaths();

            FilteredObjectSelector.ShowPicker(
                "Pick Finish Images",
                candidates,
                getRowName: index => candidates[index].name,
                getRowStatus: null,
                selection =>
                {
                    if (selection != null && selection.Count > 0)
                    {
                        AddImages(selection.Select(index => candidates[index]));
                    }
                },
                extraFilter: index => !existingMaterials.ContainsKey(candidates[index].name),
                extraFilterText: "Hide images that already have a material",
                extraFilterTooltip: $"Hide images that already have a material of the same name in {FINISHES_FOLDER}",
                extraFilterDefaultOn: true,
                allowMultipleSelection: true
            );
        }

        private void AddProjectSelection()
        {
            List<Texture2D> outsideFolder = new ();
            List<Texture2D> toAdd = new ();

            foreach (Texture2D image in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                if (IsInFinishesFolder(AssetDatabase.GetAssetPath(image)))
                {
                    toAdd.Add(image);
                }
                else
                {
                    outsideFolder.Add(image);
                }
            }

            if (outsideFolder.Count > 0)
            {
                Debug.LogWarning(
                    $"Finish Material Generator: ignored {outsideFolder.Count} selected images that are not in {FINISHES_FOLDER}:\n"
                    + string.Join("\n", outsideFolder.Select(image => AssetDatabase.GetAssetPath(image)))
                );
            }

            AddImages(toAdd);
        }

        private void AddImages(IEnumerable<Texture2D> images)
        {
            foreach (Texture2D image in images)
            {
                if (image != null && !m_Images.Contains(image))
                {
                    m_Images.Add(image);
                }
            }

            RefreshImages();
        }

        private void RemoveSelectedImages()
        {
            HashSet<Texture2D> selected = m_ImagesView.selectedItems.OfType<Texture2D>().ToHashSet();
            m_Images.RemoveAll(image => selected.Contains(image));
            m_ImagesView.ClearSelection();
            RefreshImages();
        }

        private VisualElement MakeImageRow()
        {
            Label label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 4.0f;

            return label;
        }

        private void BindImageRow(VisualElement element, int index)
        {
            Label label = (Label)element;
            Texture2D image = m_Images[index];

            if (m_ExistingMaterialPaths.ContainsKey(image.name))
            {
                label.text = $"{image.name}    (material exists, will be skipped)";
                label.style.color = new Color(0.6f, 0.6f, 0.6f);
            }
            else
            {
                label.text = image.name;
                label.style.color = StyleKeyword.Null;
            }
        }

        private void RefreshImages()
        {
            m_Images.RemoveAll(image => image == null);
            m_ExistingMaterialPaths = FindExistingMaterialPaths();

            int skipCount = m_Images.Count(image => m_ExistingMaterialPaths.ContainsKey(image.name));

            m_ImagesLabel.text = skipCount > 0
                ? $"{m_Images.Count} images. {skipCount} already have a material and will be skipped."
                : $"{m_Images.Count} images.";

            m_ImagesView.RefreshItems();
            RefreshCreateButton();
        }

        private void RefreshCreateButton()
        {
            if (m_CreateButton == null)
            {
                return;
            }

            bool canCreate =
                m_Template != null
                && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m_Template))
                && !string.IsNullOrEmpty(m_TextureSlot)
                && m_Images.Count > 0;

            m_CreateButton.SetEnabled(canCreate);
        }

        private void CreateMaterials()
        {
            const string ProgressTitle = "Creating finish materials";

            string templatePath = AssetDatabase.GetAssetPath(m_Template);
            Dictionary<string, string> existingMaterials = FindExistingMaterialPaths();

            List<Texture2D> processed = new ();
            List<Material> created = new ();
            List<string> skipped = new ();
            List<string> failed = new ();

            int imageCount = m_Images.Count;

            try
            {
                for (int i = 0; i < imageCount; i++)
                {
                    Texture2D image = m_Images[i];

                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        ProgressTitle,
                        $"({i + 1}/{imageCount}) {image.name}",
                        progress: (i + 1) / (float)imageCount
                    );

                    if (cancel)
                    {
                        break;
                    }

                    processed.Add(image);

                    if (existingMaterials.TryGetValue(image.name, out string existingPath))
                    {
                        skipped.Add(image.name);
                        Debug.Log(
                            $"Finish Material Generator: skipped {image.name}, {existingPath} already exists",
                            AssetDatabase.LoadAssetAtPath<Material>(existingPath)
                        );
                        continue;
                    }

                    string materialPath = $"{FINISHES_FOLDER}/{image.name}.mat";

                    if (!AssetDatabase.CopyAsset(templatePath, materialPath))
                    {
                        failed.Add(image.name);
                        Debug.LogError($"Finish Material Generator: could not copy {templatePath} to {materialPath}");
                        continue;
                    }

                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    material.SetTexture(m_TextureSlot, image);

                    if (material.HasProperty(TILE_PROPERTY))
                    {
                        material.SetFloat(TILE_PROPERTY, 1.0f);
                    }

                    EditorUtility.SetDirty(material);

                    existingMaterials[image.name] = materialPath;
                    created.Add(material);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            string summary = $"Finish Material Generator: created {created.Count} of {imageCount} materials from {m_Template.name}";

            if (skipped.Count > 0)
            {
                summary += $", skipped {skipped.Count} that already exist ({string.Join(", ", skipped)})";
            }

            if (failed.Count > 0)
            {
                summary += $", {failed.Count} failed ({string.Join(", ", failed)})";
            }

            if (processed.Count < imageCount)
            {
                summary += $", canceled before {imageCount - processed.Count}";
            }

            Debug.Log(summary);

            m_Images.RemoveAll(image => processed.Contains(image) && !failed.Contains(image.name));
            RefreshImages();

            if (created.Count > 0)
            {
                Selection.objects = created.ToArray<UnityEngine.Object>();
                EditorGUIUtility.PingObject(created[^1]);
            }
        }

        [SerializeField] private Material m_Template;
        [SerializeField] private string m_TextureSlot = "";
        [SerializeField] private List<Texture2D> m_Images = new ();

        private Dictionary<string, string> m_ExistingMaterialPaths = new (StringComparer.OrdinalIgnoreCase);
        private Editor m_PreviewEditor;

        private ObjectField m_TemplateField;
        private DropdownField m_SlotField;
        private IMGUIContainer m_PreviewContainer;
        private Label m_ImagesLabel;
        private ListView m_ImagesView;
        private Button m_CreateButton;
    }
}
