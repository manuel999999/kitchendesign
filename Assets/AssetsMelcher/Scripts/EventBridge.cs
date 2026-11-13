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

            // =====================================================================
            // Cabinet setup
            // =====================================================================

            /// <summary>
            ///     Invoked by the kitchen to notify listeners when the
            ///     kitchen's building mode has changed. This is called even if
            ///     the new building method is the same as the previous.
            /// </summary>
            public static Action<KitchenBuildingMethod> onBuildingMethodChanged;

            public static SequentiallyAwaitedDelegate<BrandCatalogSO> onBrandCatalogChanged = new ();

            /// <summary>
            ///     Used to inform systems when a kitchen is finished being
            ///     created/loaded. This is useful for features that should
            ///     happen on the final step of kitchen readying, which can
            ///     vary depending on the building method.
            /// </summary>
            public static SequentiallyAwaitedDelegate onKitchenReady = new ();

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

            /// <summary>
            ///     Called when the user adds a new cabinet.
            /// </summary>
            public static SequentiallyAwaitedDelegate<Cabinet> onCabinetAddedByUser = new ();

            // =====================================================================
            // Products
            // =====================================================================

            public static SequentiallyAwaitedDelegate<Occupant> onOccupantAddedToCabinet = new();

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
            /// Invoked when an occupant is successfully dragged by the user
            /// within its current volume or into a new volume.
            /// </summary>
            public static DragHandler onOccupantDragSuccess;

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
            /// Called when the highlighted object changes. Receives <see
            /// langword="null"/> when no object is highlighted.
            /// </summary>
            public static SequentiallyAwaitedDelegate<GameObject /* highlightObject */> onHighlightChanged = new ();

            // ----------------------------------------------------------------

            // ----------------------------------------------------------------

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
