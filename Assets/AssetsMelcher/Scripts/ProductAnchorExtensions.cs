namespace KitchenDesign
{
    using Melcher;
    public static class ProductAnchorExtensions
    {
        /// <summary>
        /// Return <see langword="true"/> if the anchor is directly on top
        /// of the cabinet. This will return <see langword="false"/> if the
        /// product is at the top but is on the front or back.
        /// </summary>
        public static bool IsOnTop(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnTopAtFront ||
                productAnchor == ProductAnchor.OnTopAtBack;
        }

        /// <summary>
        /// Return <see langword="true"/> if the anchor is directly on the
        /// front of the cabinet. This will return <see langword="false"/> if
        /// the product is on the top or back of the cabinet.
        /// </summary>
        public static bool IsOnFront(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnFrontAtBottom ||
                productAnchor == ProductAnchor.OnFrontAtTop;
        }

        /// <summary>
        /// Return <see langword="true"/> if the anchor is on the front of the
        /// cabinet or on top of the cabinet and at the front. Otherwise, this
        /// returns <see langword="false"/>.
        /// </summary>
        public static bool IsOnOrAtFront(this ProductAnchor productAnchor)
        {
            return productAnchor.IsOnFront() || productAnchor == ProductAnchor.OnTopAtFront;
        }

        /// <summary>
        /// Return <see langword="true"/> if the anchor is at the bottom of the
        /// cabinet. This returns <see langword="false"/> if the anchor is
        /// anywhere else.
        /// </summary>
        public static bool IsAtBottom(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnFrontAtBottom ||
                productAnchor == ProductAnchor.OnBackAtBottom;
        }

    }   // class ProductAnchorExtensions

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-10-18
/// ===========================================================================
