using KitchenDesign.Geometry;
using KitchenDesign.KitchenModel;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Pricing
{
    public sealed class StollPriceSheet : IPriceSheetExtras
    {
        private const float InchesToFeet = 1.0f / Units.InchesPerFoot;

        private readonly KitchenSelections _selections;

        public StollPriceSheet(KitchenSelections selections)
        {
            this._selections = selections;
        }

        public void AddTo(PriceSheetInputs inputs, CabinetLayout layout, float toeKickLinearMeters)
        {
            JsonList manualPriceSheetEntries = inputs.manualPriceSheetEntries;

            static JsonDict CreateManualPriceSheetEntry(string productSku, string productName, float price)
            {
                return PriceSheetInputs.CreateManualPriceSheetEntry(productSku, productName, price);
            }

            manualPriceSheetEntries.Add(
                CreateManualPriceSheetEntry(
                    productSku: "OKSEFE",
                    productName: "Custom Kitchen Setup Fee",
                    price: 325.00f
                )
            );

            if (this._selections.countertopFinish != null)
            {
                float summedCountertopAreaInMeters = 0.0f;
                CabinetSpan continguousPreviousSpan = null;

                foreach (CabinetSpan span in layout.spans)
                {
                    if (!span.IsSpace())
                    {
                        Cuboid outsideCuboid =
                            span.GetOutsideCuboidInSpanSpace(includeCountertop: true);

                        float countertopWidthInMeters = outsideCuboid.width;
                        float countertopDepthInMeters = outsideCuboid.depth;

                        if (continguousPreviousSpan != null)
                        {
                            countertopWidthInMeters -= this._selections.baseCabinetDepthInMeters;
                        }

                        if (span.backing && span.backing.hasRiserAndRiserCountertop)
                        {
                            Cuboid backingCountertopCuboid =
                                span.backing.GetCountertopCuboidInSpanSpace(default);

                            countertopDepthInMeters += backingCountertopCuboid.depth;
                        }

                        summedCountertopAreaInMeters += countertopWidthInMeters * countertopDepthInMeters;
                    }

                    continguousPreviousSpan = span.IsSpace() ? null : span;
                }

                float stollCountertopAreaInSquareFeet =
                    summedCountertopAreaInMeters * (Units.Meters2Inches * Units.Meters2Inches) * (InchesToFeet * InchesToFeet);

                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        this._selections.countertopFinish.sku,
                        this._selections.countertopFinish.name,
                        this._selections.countertopFinish.priceCustomer * Mathf.CeilToInt((float)stollCountertopAreaInSquareFeet)
                    )
                );
            }

            if (this._selections.cabinetFinish != null)
            {
                float summedOutsideWidthInMeters = 0.0f;

                foreach (CabinetSpan span in layout.spans)
                {
                    if (span.IsSpace())
                    {
                        continue;
                    }

                    Cuboid outsideCuboid =
                        span.GetOutsideCuboidInSpanSpace();

                    summedOutsideWidthInMeters += outsideCuboid.width;
                }

                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        this._selections.cabinetFinish.sku,
                        this._selections.cabinetFinish.name,
                        this._selections.cabinetFinish.priceCustomer * Mathf.CeilToInt((summedOutsideWidthInMeters * Units.Meters2Inches) / Units.InchesPerFoot)
                    )
                );
            }

            foreach (ElementIndex<Cabinet> i in layout.EnumerateCabinets())
            {
                ProductInfo cabinetProductInfo =
                    i.element.cabinetSectionProductInfo;

                if (cabinetProductInfo == null ||
                    cabinetProductInfo.priceCustomer <= 0.0)
                {
                    continue;
                }

                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        productSku: cabinetProductInfo.sku,
                        productName: cabinetProductInfo.name,
                        price: cabinetProductInfo.priceCustomer
                    )
                );
            }

            float backsplashLinearMeters = 0.0f;
            int highBarTopBackColumnHalvesCount = 0;
            bool lastSpanHasHighBarTop = false;

            foreach (CabinetSpan span in layout.spans)
            {
                bool includeFirstColumn = !lastSpanHasHighBarTop;
                lastSpanHasHighBarTop = false;

                if (!span.backing)
                {
                    continue;
                }

                if (span.backing.info.category == ProductCategory.Backsplash)
                {
                    backsplashLinearMeters += span.backing.riserCuboid.width;

                    continue;
                }

                if (span.backing.info.category != ProductCategory.BarTopHigh)
                {
                    continue;
                }

                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    highBarTopBackColumnHalvesCount += cabinet.CountVisibleBackColumnHalves();
                }

                if (!includeFirstColumn)
                {
                    --highBarTopBackColumnHalvesCount;
                }
            }

            int highBarTopColumnCount = highBarTopBackColumnHalvesCount / 2;

            for (int i = 0; i < highBarTopColumnCount; ++i)
            {
                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        productSku: "CORBEL",
                        productName: "Raised Bar Countertop Support Corbel",
                        price: 75.00f
                    )
                );
            }

            int backsplashFeet = Mathf.CeilToInt(backsplashLinearMeters * Units.Meters2Inches / Units.InchesPerFoot);
            for (int i = 0; i < backsplashFeet; ++i)
            {
                manualPriceSheetEntries.Add(
                    CreateManualPriceSheetEntry(
                        productSku: "OKBKSP",
                        productName: "Backsplash per Linear Foot",
                        price: 75.00f
                    )
                );
            }
        }
    }
}
