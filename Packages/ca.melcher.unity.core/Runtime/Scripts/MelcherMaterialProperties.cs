namespace Melcher
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// <see cref="MelcherMaterialProperties"/> binds a Unity <see
    /// cref="MaterialPropertyBlock"/> to a list of subscribed <see
    /// cref="Renderer"/>s. This greatly simplifies making material changes
    /// and dispatching it to all concerned renderers.
    /// <br /><br />
    ///
    /// This class uses the function chaining pattern. After the properties
    /// are set, call <see cref="Apply"/> to update all its subscribed
    /// renderers.
    /// </summary>
    public class MelcherMaterialProperties
    {
        /// <summary>
        /// The renderers associated with this object.
        /// </summary>
        public readonly ReadOnlyCollection<Renderer> renderers;

        /// <summary>
        /// The underlying property block that this object modifies.
        /// </summary>
        public readonly MaterialPropertyBlock properties;

        /// <summary>
        /// The affected material index on the associated renderers.
        /// </summary>
        public readonly int materialIndex;

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        /// Create a <see cref="MelcherMaterialProperties"/> for one renderer.
        /// </summary>
        public MelcherMaterialProperties(Renderer renderer, int materialIndex = 0)
            : this(new[] { renderer }, materialIndex)
        {
        }

        /// <summary>
        /// Create a <see cref="MelcherMaterialProperties"/> for many renderers.
        ///
        /// The enumerable is cloned and any duplicate renderers will be removed.
        /// </summary>
        public MelcherMaterialProperties(IEnumerable<Renderer> renderers, int materialIndex = 0)
            : this(renderers.Distinct().ToArray(), materialIndex)
        {
        }

        /// <summary>
        /// A private constructor that expects <paramref name="internalRendererArray"/>
        /// to have been created internally so the object can assume it has sole
        /// ownership of the collection.
        /// </summary>
        private MelcherMaterialProperties(Renderer[] internalRendererArray, int materialIndex)
        {
            this.renderers = System.Array.AsReadOnly(internalRendererArray);
            this.materialIndex = materialIndex;
            properties = new MaterialPropertyBlock();
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Update the <see cref="PropertyBlock"/> of all renderers associated
        /// with this object.
        /// </summary>
        public MelcherMaterialProperties Apply()
        {
            foreach (Renderer renderer in renderers)
            {
                renderer?.SetPropertyBlock(properties, materialIndex);
            }
            return this;
        }

        // =====================================================================
        // Float
        // =====================================================================

        /// <summary>
        /// Get a float from the first subscribed renderer's material. This
        /// lookup ignores any float overrides in the property block. By
        /// default, the fetched value is reassigned to the property block
        /// as an override. Set <paramref name="setOverride"/> to <see
        /// langword="false"/> to disable this behaviour.
        /// </summary>
        public MelcherMaterialProperties GetFloatFromMaterial(int nameId, out float f, bool setOverride = true)
        {
            f = default;

            if (TryGetSharedMaterialFromFirstRenderer(out Material material))
            {
                f = material.GetFloat(nameId);

                if (setOverride)
                {
                    SetFloatOverride(nameId, f);
                }
            }

            return this;
        }

        public MelcherMaterialProperties GetFloatOverride(int nameId, out float f)
        {
            f = properties.GetFloat(nameId);
            return this;
        }

        public MelcherMaterialProperties SetFloatOverride(int nameId, float f)
        {
            properties.SetFloat(nameId, f);
            return this;
        }

        // =====================================================================
        // Vector
        // =====================================================================

        public MelcherMaterialProperties GetVectorFromMaterial(int nameId, out Vector4 v, bool setOverride = true)
        {
            v = default;

            if (TryGetSharedMaterialFromFirstRenderer(out Material material))
            {
                v = material.GetVector(nameId);

                if (setOverride)
                {
                    SetVectorOverride(nameId, v);
                }
            }

            return this;
        }

        public MelcherMaterialProperties GetVectorOverride(int nameId, out Vector4 v)
        {
            v = properties.GetVector(nameId);
            return this;
        }

        public MelcherMaterialProperties SetVectorOverride(int nameId, Vector4 v)
        {
            properties.SetVector(nameId, v);
            return this;
        }

        // =====================================================================
        // Color
        // =====================================================================

        /// <summary>
        /// Set the properties block color to match the material's.
        /// </summary>
        public MelcherMaterialProperties GetColorFromMaterial(int nameId, out Color color, bool setOverride = true)
        {
            color = default;

            if (TryGetSharedMaterialFromFirstRenderer(out Material material))
            {
                color = material.GetColor(nameId);

                if (setOverride)
                {
                    SetColorOverride(nameId, color);
                }
            }

            return this;
        }

        public MelcherMaterialProperties GetColorOverride(int nameId, out Color color)
        {
            color = properties.GetColor(nameId);
            return this;
        }

        public MelcherMaterialProperties SetColorOverride(int nameId, Color color)
        {
            properties.SetColor(nameId, color);
            return this;
        }

        // =====================================================================
        // Texture
        // =====================================================================

        public MelcherMaterialProperties GetTextureFromMaterial(int nameId, out Texture texture, bool setOverride = true)
        {
            texture = default;

            if (TryGetSharedMaterialFromFirstRenderer(out Material material))
            {
                texture = material.GetTexture(nameId);

                if (setOverride)
                {
                    SetTextureOverride(nameId, texture);
                }
            }

            return this;
        }

        public MelcherMaterialProperties GetTextureOverride(int nameId, out Texture texture)
        {
            texture = properties.GetTexture(nameId);
            return this;
        }

        public MelcherMaterialProperties SetTextureOverride(int nameId, Texture texture)
        {
            properties.SetTexture(nameId, texture);
            return this;
        }

        // ====================================================================
        // Internal methods
        // ====================================================================

        private bool TryGetSharedMaterialFromFirstRenderer(out Material material)
        {
            // There shouldn't be any references to shared materials at call time
            MelcherLog.Assert(s_SharedMaterialList.Count <= 0);

            material = null;

            if (renderers.Count > 0)
            {
                if (materialIndex == 0)
                {
                    material = renderers[0].sharedMaterial;
                }
                else
                {
                    // Search each renderer until a material is found
                    for (int rendererIndex = 0; rendererIndex < renderers.Count && !material; ++rendererIndex)
                    {
                        if (!renderers[rendererIndex])
                        {
                            // Skip null renderers (in case they were deleted
                            // during gameplay)
                            continue;
                        }

                        renderers[rendererIndex].GetSharedMaterials(s_SharedMaterialList);

                        try
                        {
                            material = s_SharedMaterialList[materialIndex];
                        }
                        finally
                        {
                            // Ensure references are not held longer than necessary
                            s_SharedMaterialList.Clear();
                        }
                    }

                }
            }

            // Do not hold shared material references beyond this function
            MelcherLog.Assert(s_SharedMaterialList.Count <= 0);

            return material != null;
        }

        // =====================================================================
        // Static private fields
        // =====================================================================

        /// <summary>
        ///     The list used when fetching shared materials from a renderer. A
        ///     list allows the materials to be fetched without the overhead of
        ///     allocating a new array each time. This list is shared by all
        ///     <see cref="MelcherMaterialProperties"/> and it is purposely kept
        ///     empty when not in use to avoid unnecessary reference counts to
        ///     the materials.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly List<Material> s_SharedMaterialList = new List<Material>();

    }   // class MelcherMaterialProperties

}   // namespace Melcher

