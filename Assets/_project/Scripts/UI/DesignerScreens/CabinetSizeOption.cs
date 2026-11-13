using KitchenDesign.Utility;
namespace KitchenDesign.UI.DesignerScreens
{
    public readonly struct CabinetSizeOption
    {
        public CabinetSizeOption(float sizeInInches)
        {
            ImperialUnit unit = ImperialUnit.FromTotalInches(sizeInInches);
            displayLabel = unit.ToString();
            sizeInMeters = (float)unit.totalMeters;
        }

        public readonly string displayLabel;
        public readonly float sizeInMeters;
    }
}
