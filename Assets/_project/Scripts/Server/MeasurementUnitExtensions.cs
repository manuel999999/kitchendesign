using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Server
{
    public static class MeasurementUnitExtensions
    {
        public static string ToDisplayName(this MeasurementUnit unit)
        {
            return
                unit switch
                {
                    MeasurementUnit.FeetAndInches => "Feet and Inches",
                    _ => unit.ToString()
                };
        }
    }
}
