using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Shared.Common
{
    public static class ProductAnchorExtensions
    {
        public static bool IsOnTop(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnTopAtFront ||
                productAnchor == ProductAnchor.OnTopAtBack;
        }

        public static bool IsOnFront(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnFrontAtBottom ||
                productAnchor == ProductAnchor.OnFrontAtTop;
        }

        public static bool IsOnOrAtFront(this ProductAnchor productAnchor)
        {
            return productAnchor.IsOnFront() || productAnchor == ProductAnchor.OnTopAtFront;
        }

        public static bool IsAtBottom(this ProductAnchor productAnchor)
        {
            return productAnchor == ProductAnchor.OnFrontAtBottom ||
                productAnchor == ProductAnchor.OnBackAtBottom;
        }
    }
}
