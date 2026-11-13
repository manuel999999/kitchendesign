namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using KitchenDesign.Geometry;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Occupancy;

    /// <summary>
    ///     A static group of events which allow scripts to communicate with
    ///     one another without having a direct reference to each other.
    ///     <br /><br />
    ///
    ///     - Decouples systems. <br />
    ///     - Lives in code; no worries about serialized references breaking in
    ///       the editor. <br />
    ///     - Easy to add new event signatures. <br />
    ///     - Compile-time type-checking. <br />
    ///     - Traceable code. <br />
    ///     - Super simple. <br />
    ///     <br />
    ///
    ///     For example, if the main menu needs a loading spinner to be
    ///     displayed, it will request it by invoking <see
    ///     cref="Menu.showFullScreenSpinner"/>. This way, the menu state does not need
    ///     a reference to the object implementing the spinner which enables
    ///     the request to be ignored or changed without breaking serialized
    ///     references.
    /// </summary>
    // REVIEW: In the future, prefer MelcherSceneLifetimeValue's or static
    //         methods in the relevant script rather than global-globals. -Ryan
    public static class EventBridge
    {

        /// <summary>
        /// Events for the Designer scene
        /// </summary>
        public static class Designer
        {

            /// <summary>
            /// Restart the design phase; clear the kitchen and go back to the
            /// initial setup questions (layout, sizes, etc.)
            /// </summary>
            public static Action restart;

            public delegate void GetCabinetLayoutHandler(ref CabinetLayout cabinetLayout);
            public static GetCabinetLayoutHandler onWantCabinetLayout;

            // =====================================================================
            // Designer UI
            // =====================================================================

            /// <summary>
            ///     Door Handle Style name is updated.
            /// </summary>
            public static Action<string> onDoorHandleStyleNameUpdate;

            public delegate void ShowSetCabinetWidthPromptHandler(OccupiableVolume targetVolume);

            /// <summary>
            ///     Show a dialog containing a textbox to allow the user to
            ///     input a new cabinet width using imperial units.
            /// </summary>
            public static ShowSetCabinetWidthPromptHandler showSetOccupiableVolumeWidthPrompt;

            // --

            // --

            /// <summary>
            ///     Attempt to enable or disable toe kicks in the kitchen. Toe
            ///     kicks are used in the Gemini version of the app.
            /// </summary>
            public static Action tryToggleToeKicks;

            /// <summary>
            ///     Invoked after the user enables or disables toe kicks.
            /// </summary>
            public static Action<CabinetToeKickType> onToeKickTypeChanged;

            // =====================================================================
            // Cabinet setup
            // =====================================================================

            /// <summary>
            ///     Used to inform the kitchen scene which building method the
            ///     player selected.
            /// </summary>
            public static Action<KitchenBuildingMethod> setBuildingMethod;

            /// <summary>
            ///     Invoked by the kitchen to notify listeners when the
            ///     kitchen's building mode has changed. This is called even if
            ///     the new building method is the same as the previous.
            /// </summary>
            public static Action<KitchenBuildingMethod> onBuildingMethodChanged;

            /// <summary>
            ///     Used to inform the kitchen scene which <see
            ///     cref="BrandCatalog"/> the user selected. This is called
            ///     by <see cref="BrandCatalogUI"/>.
            /// </summary>
            public static SequentiallyAwaitedDelegate<BrandCatalogSO> setBrandCatalog = new ();

            public static SequentiallyAwaitedDelegate<BrandCatalogSO> onBrandCatalogChanged = new ();

            /// <summary>
            ///     Used to inform systems when a kitchen is finished being
            ///     created/loaded. This is useful for features that should
            ///     happen on the final step of kitchen readying, which can
            ///     vary depending on the building method.
            /// </summary>
            public static SequentiallyAwaitedDelegate onKitchenReady = new ();

            public static Action<CabinetLayout> setCabinetLayout;

            /// <summary>
            ///     This is used in Catalog building method to specify a save
            ///     file the kitchen will use from the start of a new design.
            ///     This differs from <see cref="ProFitGlobals.activeQuoteId"/>
            ///     because this is treated as a new kitchen and works even
            ///     during edit mode when launching the Designer scene alone.
            /// </summary>
            public static Action<KitchenTemplateSO> setKitchenTemplate;

            public static Action<CountertopType> setCountertopType;

            public delegate void SetSizeInMetersHandler(float sizeInMeters);
            public static SetSizeInMetersHandler setBaseCabinetDepth;
            public static SetSizeInMetersHandler setCountertopThickness;

            public delegate void SetCuboidPaddingInMetersHandler(CuboidPadding paddingInMeters);
            public static SetCuboidPaddingInMetersHandler setCountertopOverhang;

            /// <summary>
            /// Create the kitchen as it has been chosen.
            /// </summary>
            public static SequentiallyAwaitedDelegate createKitchen = new ();

            public delegate void KitchenCabinetLayoutChangedHandler(CabinetLayout layoutPrefab, CabinetLayout layoutInstance);

            /// <summary>
            /// Called after the kitchen's cabinet layout is changed and it has
            /// been created (or removed, if no layout was selected).
            /// </summary>
            // TODO: Rename to `onKitchenLayoutCreated`. "Changed" makes it
            //       sound like it is called when cabinets are moved or
            //       resized, which it is not.
            public static KitchenCabinetLayoutChangedHandler onKitchenLayoutChanged;

            // TODO: Clean up the events. Something like this maybe:

            // public static Action<GameObject>        onHighlightChanged;

            public delegate void OnTryAddProductHandler(ProductInfo productInfo, Action<Occupant> onComplete);

            /// <summary>
            /// Called by the UI when a product wants to be added anywhere to
            /// the kitchen.
            /// </summary>
            public static OnTryAddProductHandler                onTryAddProduct;

            public delegate void OnTryAddSectionHandler(ProductInfo cabinetSectionProductInfo, Cabinet canReplaceCabinet);

            /// <summary>
            ///     Placeholder event that creates a new cabinet section. This
            ///     needs to be replaced with a system that lets the user
            ///     choose where to place the new section.
            /// </summary>
            public static OnTryAddSectionHandler                onTryAddSection;

            public static Action<Cabinet>                       onTryDestroySection;

            /// <summary>
            /// Called by the UI when the user wants to clone a product.
            /// </summary>
            public static OnTryAddProductHandler                onTryCloneProduct;

            /// <summary>
            /// Called by the UI when the user wants to clone a cabinet
            /// (Sections building method only).
            /// </summary>
            public static Action<Cabinet>                       onTryCloneSection;

            /// <summary>
            /// Called by the UI when the user wants to remove a product from
            /// the kitchen. The handler should destroy the product.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Occupant> onTryDestroyOccupant = new ();

            /// <summary>
            ///     Called by the UI when the user wants to remove a cabinet
            ///     backing. The attempt may fail if it would result in
            ///     cabinet occupants overlapping.
            /// </summary>
            public static SequentiallyAwaitedDelegate<CabinetBacking> onTryDestroyCabinetBacking = new ();

            /// <summary>
            ///     Called when the user adds a new cabinet.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Cabinet> onCabinetAddedByUser = new ();

            /// <summary>
            ///     Called anytime a <see cref="Occupant"/> is spawned for any
            ///     reason (e.g, player clicking add product button or during
            ///     load).
            /// </summary>
            public static SequentiallyAwaitedDelegate<(Occupant instantiatedOccupant, bool userInitiated)> onOccupantInstantiated = new ();

            /// <summary>
            ///     Reposition an occupant to a new location within the same
            ///     volume.
            /// </summary>
            /// <param name="occupantToMove">
            ///     The occupant to move. The product must already be placed on
            ///     a cabinet.
            /// </param>
            /// <param name="newPosition">
            ///     The new position to try moving the product. The position is
            ///     relative to the product's current cabinet's local space.
            /// </param>
            /// <param name="snapMode">
            ///     Controls how the reposition behaves if the requested
            ///     position is blocked. By default, the occupant will remain
            ///     where it was. Other values from <see
            ///     cref="RepositionSnapMode" /> will try to snap the occupant
            ///     to the nearest opening to the requested location.
            /// </param>
            /// <param name="didMove">
            ///     This is an output argument. The caller should ensure its
            ///     value is <see langword="false"/> prior to calling. If the
            ///     occupant is moved successfully, it will be set to <see
            ///     langword="true" />; otherwise, the value is set to <see
            ///     langword="false"/>. However, you should not rely on the
            ///     value being set to <see langword="false" /> because the
            ///     event may not be hooked.
            /// </param>
            public delegate void TryRepositionOccupantHandler(Occupant occupantToMove, Vector3 newPosition, RepositionSnapMode snapMode, ref bool didMove);

            /// <inheritdoc cref="TryRepositionOccupantHandler" />
            public static TryRepositionOccupantHandler onTryRepositionOccupant;

            // =====================================================================
            // Products
            // =====================================================================

            public static SequentiallyAwaitedDelegate<Occupant> onOccupantAddedToCabinet = new();
            /// <summary>
            /// Called when a product is removed from a cabinet.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Occupant> onOccupantRemovedFromCabinet = new ();

            /// <summary>
            ///     Called when an occupant is resized. E.g., a cabinet shelf.
            ///     This is intended to be listened to by <see
            ///     cref="MeasurementOverlayManager"/> to update the left-right
            ///     measurement lines when a product's size changes.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Occupant> onOccupantResized = new ();

            //

            public delegate void DragHandler(Occupant draggedOccupant, PointerEventData eventData);

            /// <summary>
            /// Invoked when the user begins dragging a occupant.
            /// </summary>
            public static DragHandler onOccupantDragBegin;

            /// <summary>
            /// Invoked every frame while the user is dragging an occupant and
            /// the drag's position has changed.
            /// </summary>
            public static DragHandler onOccupantDrag;

            /// <summary>
            /// Invoked when an occupant is successfully dragged by the user
            /// within its current volume or into a new volume.
            /// </summary>
            public static DragHandler onOccupantDragSuccess;

            /// <summary>
            /// Invoked when the user releases the product drag. This is
            /// intended to release the product so it stays at its current
            /// location.
            /// </summary>
            public static SequentiallyAwaitedDelegate<(Occupant draggedOccupant, PointerEventData eventData)> onOccupantDragEnd = new ();

            //

            public delegate void OccupantNudgedHandler(Occupant nudgedOccupant, Vector3 cabinetLocalNudgeDistanceInMeters);

            /// <summary>
            /// Called when an occupant is nudged by the user. E.g. shifted
            /// up or down slightly. We use this to update any measurement UI
            /// for the selected object.
            /// </summary>
            public static OccupantNudgedHandler onOccupantNudged;

            // ----------------------------------------------------------------

            /// <summary>
            /// Try to reorder a cabinet so it will either:
            /// <br /><br />
            ///
            ///     1.  Appear before the cabinet that is before it. I.e.,
            ///         shift the cabinet to the left. This happens when it is
            ///         passed <see cref="Extent.Min"/>.
            ///         <br /><br />
            ///
            ///     2.  Appear after the cabinet that is after it. I.e., shift
            ///         the cabinet to the right. This happens when it is passed
            ///         <see cref="Extent.Max"/>.
            ///         <br /><br />
            /// </summary>
            public static SequentiallyAwaitedDelegate<(Cabinet cabinetToReorder, Extent moveTowardsExtent)> onTryReorderCabinet = new ();

            /// <summary>
            ///     Invoked when a cabinet's order within the kitchen layout
            ///     changed. This happens when the user selects a cabinet in
            ///     Sections building method and clicks the shift left or shift
            ///     right buttons.
            /// </summary>
            ///
            public static SequentiallyAwaitedDelegate<(Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans)> onCabinetReordered = new ();

            // Swap cabinet ---------------------------------------------------

            public static Action<Cabinet> onCabinetSwapStart;
            public static Action onCabinetSwapCanceled;

            // Destroy cabinet ------------------------------------------------

            /// <summary>
            ///     Invoked when a cabinet is destroyed from the kitchen, but
            ///     immediately before its actual destruction. This gives
            ///     listeners a chance to process it if necessary.
            /// </summary>
            public static Action<Cabinet> onPreCabinetDestroyedByUser;

            /// <summary>
            ///     Invoked when a cabinet is truly destroyed from the kitchen.
            ///     At this point, the cabinet no longer exists, but some of its
            ///     data is forwarded to the listener.
            /// </summary>
            public static SequentiallyAwaitedDelegate<int /* cabinetSpanIndex */> onCabinetDestroyedByUser = new ();

            // Highlight product ----------------------------------------------

            /// <summary>
            /// Try to highlight an object. The highlight request will fail if
            /// the user is dragging a product or resizing the cabinet. Nothing
            /// happens trying to highlight the same product twice.
            /// </summary>
            public static SequentiallyAwaitedDelegate<GameObject /* highlightObject */> onTryHighlightObject = new ();

            /// <summary>
            /// Called when the highlighted object changes. Receives <see
            /// langword="null"/> when no object is highlighted.
            /// </summary>
            public static SequentiallyAwaitedDelegate<GameObject /* highlightObject */> onHighlightChanged = new ();

            // ----------------------------------------------------------------

            /// <summary>
            ///     Tell the kitchen to center the passed product within its
            ///     current occupiable volume.
            /// </summary>
            public static Action<Occupant> centerOccupantInVolume;

            // ----------------------------------------------------------------

            public delegate void FinishMaterialChangedHandler(ProductInfo productInfo, ProductCategory fallbackFinishCategory);

            /// <summary>
            ///     Invoked when the user selects a new finish product to apply
            ///     to the kitchen.
            /// </summary>
            public static FinishMaterialChangedHandler onFinishMaterialChanged;

            // =====================================================================
            // Cabinet Length Resize
            // =====================================================================

            /// <summary>
            ///     Called when the player actively changes the size of a cabinet
            ///     by dragging one of the resizer widgets.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Cabinet> onCabinetResized = new ();

        }   // class Designer

    }   // class EventBridge

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-10
/// ===========================================================================
