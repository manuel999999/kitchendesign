using KitchenDesign.KitchenModel;
using KitchenDesign.Cabinets;

namespace KitchenDesign.Pricing
{
    public interface IPriceSheetExtras
    {
        void AddTo(PriceSheetInputs inputs, CabinetLayout layout, float toeKickLinearMeters);
    }
}
