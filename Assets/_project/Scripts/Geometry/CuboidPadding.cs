namespace KitchenDesign.Geometry
{
    using Melcher;
    /// <summary>
    ///     Represents a set of distances for each face of a <see
    ///     cref="Cuboid"/>. It can be applied to a cuboid to return a new
    ///     cuboid that is larger or smaller by the distances recorded in the
    ///     padding.
    /// </summary>
    [System.Serializable]
    public struct CuboidPadding
    {
        // =====================================================================
        // Static properties
        // =====================================================================

        public static CuboidPadding zero => new CuboidPadding();

        public static CuboidPadding one =>
            new CuboidPadding()
            {
                left = 1.0f,
                right = 1.0f,
                top = 1.0f,
                bottom = 1.0f,
                towardMinZ = 1.0f,
                towardMaxZ = 1.0f
            };
        // =====================================================================
        // Fields
        // =====================================================================

        /// <summary>
        ///     Amount to add to a cuboid's <see cref="Cuboid.xMin"/>.
        /// </summary>
        public float left;

        /// <summary>
        ///     Amount to subtract from a cuboid's <see cref="Cuboid.xMax"/>.
        /// </summary>
        public float right;

        /// <summary>
        ///     Amount to add to a cuboid's <see cref="Cuboid.yMin"/>.
        /// </summary>
        public float bottom;

        /// <summary>
        ///     Amount to subtract from a cuboid's <see cref="Cuboid.yMax"/>.
        /// </summary>
        public float top;

        /// <summary>
        ///     Amount to add to a cuboid's <see cref="Cuboid.zMax"/>.
        /// </summary>
        public float towardMaxZ;

        /// <summary>
        ///     Amount to subtract from a cuboid's <see cref="Cuboid.zMin"/>.
        /// </summary>
        public float towardMinZ;

        /// <summary>
        ///     Apply the padding as an outward magnitude to a cuboid. Expand
        ///     all faces of the cuboid out from its center by the amount in
        ///     the corresponding padding fields.
        /// </summary>
        public Cuboid Inflate(Cuboid toInflate)
        {
            toInflate.xMin -= left;
            toInflate.xMax += right;

            toInflate.yMin -= bottom;
            toInflate.yMax += top;

            toInflate.zMin -= towardMinZ;
            toInflate.zMax += towardMaxZ;

            return toInflate;
        }

        /// <summary>
        ///     Apply the padding as an inward magnitude to a cuboid. Shrink
        ///     all faces of the cuboid towards its center by the amount in
        ///     the corresponding padding fields.
        /// </summary>
        public Cuboid Deflate(Cuboid toDeflate)
        {
            toDeflate.xMin += left;
            toDeflate.xMax -= right;

            toDeflate.yMin += bottom;
            toDeflate.yMax -= top;

            toDeflate.zMin += towardMinZ;
            toDeflate.zMax -= towardMaxZ;

            return toDeflate;
        }

        public override string ToString()
        {
            return $"(l:{left}, r:{right}, b:{bottom}, t:{top}, f:{towardMinZ}, b:{towardMaxZ})";
        }

        // =====================================================================
        // Operators
        // =====================================================================

        public static CuboidPadding operator -(CuboidPadding value)
        {
            value.left      = -value.left;
            value.right     = -value.right;
            value.top       = -value.top;
            value.bottom    = -value.bottom;
            value.towardMinZ = -value.towardMinZ;
            value.towardMaxZ = -value.towardMaxZ;

            return value;
        }

        public static CuboidPadding operator *(CuboidPadding padding, float scale)
        {
            padding.top     *= scale;
            padding.bottom  *= scale;
            padding.left    *= scale;
            padding.right   *= scale;
            padding.towardMinZ   *= scale;
            padding.towardMaxZ    *= scale;
            return padding;
        }

        public static CuboidPadding operator +(CuboidPadding padding, float scale)
        {
            padding.top     += scale;
            padding.bottom  += scale;
            padding.left    += scale;
            padding.right   += scale;
            padding.towardMinZ   += scale;
            padding.towardMaxZ    += scale;
            return padding;
        }

    }   // struct CuboidPadding

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-06-22
/// ===========================================================================
