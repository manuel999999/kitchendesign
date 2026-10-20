using System.Collections.Generic;
using KitchenDesign.Shared.Enums;
using UnityEngine;

namespace KitchenDesign.Cutouts
{
    public sealed class CutoutClipSet
    {
        public const int MaxSlots = 32;
        public const int MaxBoxes = 32;
        public const float FaceMarginMeters = 0.001f;

        private static readonly int SlotRangesId = Shader.PropertyToID("_KitchenCutoutSlotRanges");
        private static readonly int BoxRowsId = Shader.PropertyToID("_KitchenCutoutBoxRows");

        private readonly List<int> _boxSlots = new List<int>(MaxBoxes);
        private readonly List<Matrix4x4> _worldToBox = new List<Matrix4x4>(MaxBoxes);
        private readonly Vector4[] _slotRanges = new Vector4[MaxSlots];
        private readonly Vector4[] _boxRows = new Vector4[MaxBoxes * 3];

        public int BoxCount => this._boxSlots.Count;
        public int DroppedBoxCount { get; private set; }

        public void Clear()
        {
            this._boxSlots.Clear();
            this._worldToBox.Clear();
            this.DroppedBoxCount = 0;
        }

        public bool AddBox(int slot, Transform space, Bounds localBounds, CutoutFaces openFaces)
        {
            if (slot <= 0 || slot >= MaxSlots)
            {
                this.DroppedBoxCount++;
                return false;
            }

            if (this._boxSlots.Count >= MaxBoxes)
            {
                this.DroppedBoxCount++;
                return false;
            }

            Vector3 scale = space.lossyScale;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            max.x += FaceOffset(openFaces, CutoutFaces.XMax, scale.x);
            min.x -= FaceOffset(openFaces, CutoutFaces.XMin, scale.x);
            max.y += FaceOffset(openFaces, CutoutFaces.YMax, scale.y);
            min.y -= FaceOffset(openFaces, CutoutFaces.YMin, scale.y);
            max.z += FaceOffset(openFaces, CutoutFaces.ZMax, scale.z);
            min.z -= FaceOffset(openFaces, CutoutFaces.ZMin, scale.z);

            Vector3 size = max - min;
            if (size.x <= 0.0f || size.y <= 0.0f || size.z <= 0.0f)
            {
                this.DroppedBoxCount++;
                return false;
            }

            Matrix4x4 boxToWorld = space.localToWorldMatrix * Matrix4x4.TRS((min + max) * 0.5f, Quaternion.identity, size);
            this._boxSlots.Add(slot);
            this._worldToBox.Add(boxToWorld.inverse);
            return true;
        }

        public void Apply()
        {
            System.Array.Clear(this._slotRanges, 0, this._slotRanges.Length);
            System.Array.Clear(this._boxRows, 0, this._boxRows.Length);

            int written = 0;
            for (int slot = 1; slot < MaxSlots; slot++)
            {
                int start = written;
                for (int i = 0; i < this._boxSlots.Count; i++)
                {
                    if (this._boxSlots[i] != slot)
                    {
                        continue;
                    }

                    Matrix4x4 m = this._worldToBox[i];
                    this._boxRows[written * 3] = m.GetRow(0);
                    this._boxRows[written * 3 + 1] = m.GetRow(1);
                    this._boxRows[written * 3 + 2] = m.GetRow(2);
                    written++;
                }

                this._slotRanges[slot] = new Vector4(start, written - start, 0.0f, 0.0f);
            }

            Shader.SetGlobalVectorArray(SlotRangesId, this._slotRanges);
            Shader.SetGlobalVectorArray(BoxRowsId, this._boxRows);
        }

        public static void SetRendererSlot(MeshRenderer renderer, int slot)
        {
            renderer.SetShaderUserValue((uint)Mathf.Clamp(slot, 0, MaxSlots - 1));
        }

        private static float FaceOffset(CutoutFaces openFaces, CutoutFaces face, float axisScale)
        {
            float margin = FaceMarginMeters / Mathf.Max(Mathf.Abs(axisScale), 0.0001f);
            return (openFaces & face) != 0 ? margin : -margin;
        }
    }
}
