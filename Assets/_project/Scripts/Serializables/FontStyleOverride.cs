namespace KitchenDesign.Serializables
{
    using KitchenDesign.Shared.Enums;
    using TMPro;

    [System.Serializable]
    public struct FontStyleOverride
    {
        public FontFlagOverride bold;
        public FontFlagOverride italic;
        public FontFlagOverride underline;
        public FontFlagOverride strikethrough;
        public FontFlagOverride lowercase;
        public FontFlagOverride uppercase;
        public FontFlagOverride smallcaps;

        public FontStyles Apply(FontStyles style)
        {
            style = SetFlag(style, this.bold, FontStyles.Bold);
            style = SetFlag(style, this.italic, FontStyles.Italic);
            style = SetFlag(style, this.underline, FontStyles.Underline);
            style = SetFlag(style, this.strikethrough, FontStyles.Strikethrough);
            style = SetFlag(style, this.lowercase, FontStyles.LowerCase);
            style = SetFlag(style, this.uppercase, FontStyles.UpperCase);
            style = SetFlag(style, this.smallcaps, FontStyles.SmallCaps);

            return style;
        }

        private static FontStyles SetFlag(FontStyles current, FontFlagOverride overrideFlag, FontStyles bitToChange)
        {
            if (overrideFlag == FontFlagOverride.Enabled)
            {
                return current | bitToChange;
            }

            if (overrideFlag == FontFlagOverride.Disabled)
            {
                return current & ~bitToChange;
            }

            return current;
        }
    }
}
