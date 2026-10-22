namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using KitchenDesign.Cutouts;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class KitchenCutoutClipping
    {
        private readonly CutoutClipSet _set = new CutoutClipSet();
        private Kitchen _kitchen;
        private bool _warnedSlots;
        private bool _warnedBoxes;

        public int LastSlotCount { get; private set; }
        public int LastBoxCount => _set.BoxCount;
        public int LastDroppedBoxCount => _set.DroppedBoxCount;

        public void Attach(Kitchen kitchen)
        {
            _kitchen = kitchen;
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        }

        public void Detach()
        {
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            _kitchen = null;
            _set.Clear();
            _set.Apply();
        }

        public void Refresh()
        {
            _set.Clear();
            LastSlotCount = 0;

            if (_kitchen == null || _kitchen.layout == null)
            {
                _set.Apply();
                return;
            }

            int nextSlot = 1;
            foreach (ElementIndex<Cabinet> entry in _kitchen.layout.EnumerateCabinets())
            {
                Cabinet cabinet = entry.element;
                int slot = HasActiveCutOut(cabinet) ? nextSlot : 0;

                if (slot >= CutoutClipSet.MaxSlots)
                {
                    WarnOnce(ref _warnedSlots, $"More than {CutoutClipSet.MaxSlots - 1} cabinets have cutouts; the rest are drawn without them.");
                    slot = 0;
                }

                if (slot > 0)
                {
                    nextSlot++;
                    LastSlotCount++;
                }

                foreach (MeshRenderer renderer in cabinet.meshRenderers)
                {
                    SetSlot(renderer, slot);
                }

                foreach (Occupant occupant in cabinet.occupants)
                {
                    AddOccupant(occupant, slot);
                }
            }

            _set.Apply();

            if (_set.DroppedBoxCount > 0)
            {
                WarnOnce(ref _warnedBoxes, $"More than {CutoutClipSet.MaxBoxes} cutout boxes; {_set.DroppedBoxCount} are drawn without a cutout.");
            }
        }

        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            Refresh();
        }

        private void AddOccupant(Occupant occupant, int slot)
        {
            ProductVisualContent content = occupant.productVisualContentInstance;
            if (content == null)
            {
                return;
            }

            IReadOnlyList<Renderer> extraRenderers = content.additionalCabinetRenderMaskMeshes;
            if (extraRenderers != null)
            {
                for (int i = 0; i < extraRenderers.Count; ++i)
                {
                    SetSlot(extraRenderers[i] as MeshRenderer, slot);
                }
            }

            AddCutOut(content.cabinetCutOut, slot);
            AddCutOut(content.countertopCutOut, slot);

            if (occupant.TryGetComponent(out CabinetShelf shelf))
            {
                foreach (Occupant nested in shelf.occupants)
                {
                    AddOccupant(nested, slot);
                }
            }
        }

        private void AddCutOut(CabinetCutOut cutOut, int slot)
        {
            if (cutOut == null)
            {
                return;
            }

            var interiors = cutOut.interiors;
            while (interiors.MoveNext())
            {
                SetSlot(interiors.Current, slot);
            }

            if (slot > 0 && cutOut.TryGetClipBox(out Bounds localBounds, out CutoutFaces openFaces))
            {
                _set.AddBox(slot, cutOut.stencilRoot, localBounds, openFaces);
            }
        }

        private static bool HasActiveCutOut(Cabinet cabinet)
        {
            foreach (Occupant occupant in cabinet.occupants)
            {
                if (HasActiveCutOut(occupant))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActiveCutOut(Occupant occupant)
        {
            ProductVisualContent content = occupant.productVisualContentInstance;
            if (content == null)
            {
                return false;
            }

            if ((content.cabinetCutOut && content.cabinetCutOut.gameObject.activeInHierarchy) ||
                (content.countertopCutOut && content.countertopCutOut.gameObject.activeInHierarchy))
            {
                return true;
            }

            if (occupant.TryGetComponent(out CabinetShelf shelf))
            {
                foreach (Occupant nested in shelf.occupants)
                {
                    if (HasActiveCutOut(nested))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void SetSlot(MeshRenderer renderer, int slot)
        {
            if (renderer == null || renderer.GetShaderUserValue() == (uint)slot)
            {
                return;
            }

            CutoutClipSet.SetRendererSlot(renderer, slot);
        }

        private static void WarnOnce(ref bool warned, string message)
        {
            if (warned)
            {
                return;
            }

            warned = true;
#if UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }
    }
}
