using Cysharp.Threading.Tasks;
using KitchenDesign.Cabinets;
using KitchenDesign.KitchenModel;
using KitchenDesign.Occupancy;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Shared.Interfaces;
using Melcher;
using UnityEngine;

namespace KitchenDesign
{
    [DefaultExecutionOrder(-10)]
    public sealed class KitchenSelectionClickControl : MonoBehaviour
    {
        private Kitchen _kitchen;
        private DesignerNavigationUI _navigation;
        private UniTask? _clickTask;

        public void Initialize(Kitchen kitchen, DesignerNavigationUI navigation)
        {
            this._kitchen = kitchen;
            this._navigation = navigation;
        }

        private void Update()
        {
            if (this._navigation == null)
            {
                return;
            }

            if (!this._clickTask.HasValue || this._clickTask.Value.Status.IsCompleted())
            {
                this._clickTask = this.HandleClick();
            }
        }

        private async UniTask HandleClick()
        {
            if (Input.GetMouseButtonDown(0) && !MelcherScreenEvents.IsAnyPointerOverGameObject())
            {
                this._navigation.CloseHamburgerMenu();

                if (this._kitchen && this._kitchen.layout != null)
                {
                    Ray pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float nearestCabinetDistance = float.PositiveInfinity;
                    Cabinet nearestCabinet = null;

                    float nearestOccupantDistance = float.PositiveInfinity;
                    Occupant nearestOccupant = null;

                    foreach (ElementIndex<Cabinet> i in this._kitchen.layout.EnumerateCabinets())
                    {
                        Cabinet cabinet = i.element;

                        foreach (Occupant occupant in cabinet.occupants)
                        {
                            if (!occupant.productVisual.info.requireManualSelection)
                            {
                                continue;
                            }

                            Bounds b = occupant.productVisualContentInstance.clickCollider.bounds;
                            MelcherLog.Log("size: " + occupant.productVisualContentInstance.clickCollider.bounds.size);
                            Debug.DrawLine(Vector3.zero, b.min, Color.yellow, 3.0f);
                            Debug.DrawLine(Vector3.zero, b.max, Color.magenta, 3.0f);

                            if (occupant.productVisualContentInstance.clickCollider.bounds.IntersectRay(pointerRay, out float occupantDistance) &&
                                occupantDistance < nearestOccupantDistance)
                            {
                                Debug.DrawLine(pointerRay.origin, pointerRay.GetPoint(occupantDistance), Color.blue, 5.0f);
                                nearestOccupantDistance = occupantDistance;
                                nearestOccupant = occupant;
                            }
                        }

                        if (cabinet.GetOrientedBounds().IntersectRay(pointerRay, out float cabinetDistance) &&
                            cabinetDistance < nearestCabinetDistance)
                        {
                            bool allowCabinet;

                            IBuildingMethodRules rules = this._kitchen.buildingRules;

                            if (rules.SelectsWholeKitchen)
                            {
                                allowCabinet = cabinet.cabinetType != CabinetType.Space;
                            }
                            else if (rules.HasBlindCorners)
                            {
                                if (cabinet.Is(CabinetType.Corner, CabinetModelType.Procedural))
                                {
                                    this._kitchen.layout.GetCabinetNeighbours(
                                        cabinet,
                                        out Cabinet leftCabinet,
                                        out Cabinet rightCabinet
                                    );

                                    if (leftCabinet && leftCabinet.Is(CabinetType.StraightOnLeftOfCorner))
                                    {
                                        cabinet = leftCabinet;
                                        allowCabinet = true;
                                    }
                                    else if (rightCabinet && rightCabinet.Is(CabinetType.StraightOnRightOfCorner))
                                    {
                                        cabinet = rightCabinet;
                                        allowCabinet = true;
                                    }
                                    else
                                    {
                                        allowCabinet = false;
                                    }
                                }
                                else
                                {
                                    allowCabinet =
                                        cabinet.canHaveProducts ||
                                        cabinet.Is(CabinetType.StraightOnLeftOfCorner) ||
                                        cabinet.Is(CabinetType.StraightOnRightOfCorner) ||
                                        cabinet.Is(CabinetType.Corner, CabinetModelType.Authored);
                                }
                            }
                            else if (rules.CabinetsAreProducts)
                            {
                                allowCabinet = cabinet.canHaveProducts;
                            }
                            else
                            {
                                throw new System.InvalidOperationException("Unhandled kitchen building method: " + this._kitchen.selectedBuildingMethod);
                            }

                            Debug.DrawLine(
                                pointerRay.origin,
                                pointerRay.GetPoint(cabinetDistance),
                                allowCabinet ? Color.green : Color.red,
                                5.0f
                            );

                            if (allowCabinet || cabinet.canOccludeSelectionClick)
                            {

                                nearestCabinetDistance = cabinetDistance;
                                nearestCabinet = allowCabinet ? cabinet : null;
                            }
                        }
                    }

                    if (nearestOccupant)
                    {
                        MelcherLog.Log("Found nearest occupant : " + nearestOccupant.name, nearestOccupant);
                    }
                    else
                    {
                        MelcherLog.Log("No manual occupant found");
                    }

                    if (nearestOccupant)
                    {
                        await this._kitchen.TryHighlightObject(nearestOccupant.gameObject);
                        return;
                    }

                    if (nearestCabinet)
                    {
                        GameObject objectToHighlight = nearestCabinet.gameObject;

                        if (this._kitchen.buildingRules.SelectsWholeKitchen)
                        {
                            objectToHighlight = this._kitchen.gameObject;
                        }

                        await this._kitchen.TryHighlightObject(objectToHighlight);
                        return;
                    }
                }

                if (this._kitchen)
                {
                    await this._kitchen.TryHighlightObject(null);
                }
            }
        }
    }
}
