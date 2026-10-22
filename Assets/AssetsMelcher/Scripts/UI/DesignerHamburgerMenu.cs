namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    using JsonDict = System.Collections.Generic.IDictionary<string, object>;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Handles the top-left menu button in the design scene. When clicked,
    /// a sub-menu opens. Clicking any of the items or clicking anywhere outside
    /// of the sub-menu will close it. See <see cref="DesignerScene"/>.
    /// </summary>
    public class DesignerHamburgerMenu : MelcherMonoBehaviour
    {
        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        /// Close the expandable panel. This does a tween.
        /// </summary>
        public void ClosePanel()
        {
            if (!m_MenuButton.gameObject.activeSelf)
            {
                // Do not close the panel when the hamburger menu button is not
                // enabled because otherwise there's no way to re-show it. The
                // menu button is disabled on PC and WebGL builds due to
                // feedback about it being difficult for some users.
                return;
            }

            SetAccordionOpen(false);
        }

        protected void Start()
        {
            ProFitGlobals.ExecuteWhenReady(SetupUI);
        }

        protected void OnEnable()
        {
            m_MenuButton.onClick    += ToggleMenu;

            m_ExportJpgButton.onClick += ExportJpg;
            m_EditSizeButton.onClick += ShowEditSizePrompt;
            m_SaveButton.onClick    += ShowSavePrompt;
            m_RestartButton.onClick += ShowRestartPrompt;
            m_QuitButton.onClick    += ShowQuitPrompt;
        }

        protected void OnDisable()
        {
            m_MenuButton.onClick    -= ToggleMenu;

            m_ExportJpgButton.onClick -= ExportJpg;
            m_EditSizeButton.onClick -= ShowEditSizePrompt;
            m_SaveButton.onClick    -= ShowSavePrompt;
            m_RestartButton.onClick -= ShowRestartPrompt;
            m_QuitButton.onClick    -= ShowQuitPrompt;
        }

        protected void ToggleMenu(MelcherButton caller)
        {
            Vector4 offscreenPadding = m_MenuPanelPaddingWhenVisible;
            offscreenPadding.z += 200;
            offscreenPadding.w -= 200;

            SetAccordionOpen(!m_MenuAccordion.isExpanded);
        }

        protected void SetAccordionOpen(bool isExpanded)
        {
            const float TweenSeconds = 0.25f;
            m_MenuAccordion.TweenUsingDuration(isExpanded, TweenSeconds);
        }

        protected void ExportJpg(MelcherButton caller)
        {
            Resolution screenResolution = Screen.currentResolution;

            Vector2Int imageResolution = new Vector2Int(screenResolution.width, screenResolution.height);

            byte[] screenshotBytes =
                KitchenScreenshotManager.TakeScreenshotFromCamera(Camera.main, imageResolution);

            string imageTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmssfff");

            string fileName =
                $"{imageTime}_{ProFitGlobals.userProfile.contactLastName}_{ProFitGlobals.userProfile.contactFirstName}.jpg";

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                MelcherLog.LogVerbose($"Trying to download: {fileName}");

                EventBridge.Designer.showToast?.Invoke($"Saving image...");

                MelcherWebGL.TryDownloadingFile(screenshotBytes, fileName);
            }
            else if (Application.isMobilePlatform)
            {
                NativeGallery.SaveImageToGallery(
                    screenshotBytes,
                    ProFitGlobals.appOptions.fileSystemName,
                    fileName,
                    callback: static (didSave, filePath) =>
                    {
                        if (didSave)
                        {
                            EventBridge.Designer.showToast?.Invoke($"Saved image to gallery.");
                        }
                    }
                );

            }
            else
            {
                string outputPath =
                    System.IO.Path.Combine(
                        Application.persistentDataPath,
                        fileName
                    );

                try
                {
                    System.IO.File.WriteAllBytes(
                        outputPath,
                        screenshotBytes
                    );

                    EventBridge.Designer.showToast?.Invoke($"Saved image to persistent data.");
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }

            }
        }

        protected void ShowEditSizePrompt(MelcherButton caller)
        {
            ClosePanel();

            EventBridge.Designer.showCabinetDetailsUi?.Invoke(CabinetOtherSizesUI.Mode.ModifyExistingCabinet);
        }

        protected void ShowSavePrompt(MelcherButton caller)
        {
            ShowSavePromptAsync().Forget();

            async UniTask ShowSavePromptAsync()
            {
                ClosePanel();

                string saveName = null;
                string saveNotes = null;

                if (ProFitGlobals.activeQuoteId.HasValue)
                {
                    int clientId = ProFitGlobals.activeClientId.Value;
                    int quoteId = ProFitGlobals.activeQuoteId.Value;

                    if (ProFitGlobals.TryGetClientAndQuoteIndex(clientId, quoteId, out int clientIndex, out int quoteIndex))
                    {
                        UserClientQuote quote =
                            ProFitGlobals.userClients[clientIndex].quotes[quoteIndex];

                        saveName = quote.saveName;
                        saveNotes = quote.saveNotes;
                    }
                }

                if (saveName == null)
                {
                    // Get a default name for the save
                    string clientName = "Guest";
                    if (ProFitGlobals.activeClientId.HasValue &&
                        ProFitGlobals.TryGetClientIndex(ProFitGlobals.activeClientId.Value, out int clientIndex))
                    {
                        UserClient client = ProFitGlobals.userClients[clientIndex];

                        // Combine the client's first and last name together with a
                        // space. If either is blank, leave it off
                        IEnumerable<string> names = new []{ client.firstName, client.lastName };
                        names = names.Where(x => !string.IsNullOrWhiteSpace(x));

                        clientName = string.Join(" ", names);
                    }

                    string layoutName = null;
                    CabinetLayout cabinetLayout = null;
                    EventBridge.Designer.onWantCabinetLayout?.Invoke(ref cabinetLayout);
                    layoutName = cabinetLayout?.displayName;

                    IEnumerable<string> parts = new[] { clientName, layoutName };
                    string defaultSaveName = string.Join(" - ", parts);

                    saveName = defaultSaveName;
                }

                // Clear the highlight to restore the original materials and
                // hide the resize widgets from the upcoming screenshots
                await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);

                EventBridge.Designer.showSavePrompt.Invoke(saveName, saveNotes, SaveDesign, error: null);
            }
        }

        protected async UniTask SaveDesign(string saveName, string saveNotes)
        {
            MelcherLog.LogEditorOnly(
                $"Saving with name {MelcherFormat.ConsoleString(saveName)} " +
                $"and notes {MelcherFormat.ConsoleString(saveNotes)}"
            );

            // Show the loading UI for better user feedback
            EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Save);

            using var loadingUiHider =
                MelcherDeferred.Defer(
                    onDispose: static () => EventBridge.Designer.showLoadingUi?.Invoke(EventBridge.Designer.LoadingUIMode.Hide)
                );

            // Clear the user's selection so our screenshots are not tinted.
            await EventBridge.Designer.onTryHighlightObject.InvokeIfNonEmpty(null);

            // TODO: Better way to get the kitchen
            Kitchen kitchen = FindObjectOfType<Kitchen>();

            KitchenScreenshotManager.ScreenshotGallery screenshots =
                await m_KitchenScreenshotManager.TakeScreenshots(kitchen);

            JsonDict saveData = kitchen.SaveToJson();

            System.DateTime currentTime = System.DateTime.Now;

            saveData.Add("fileName", saveName);
            saveData.Add("timeStamp", currentTime.ToString());

            // This is commented out because the client requested that saving
            // always generate a new save file. A better fix would be to prompt
            // the player if they want to overwrite their old save or create
            // a new one. TODO: Add a prompt to overwrite old save.
            //if (ProFitGlobals.activeQuoteId.HasValue)
            //{
            //    ProFitGlobals.server.Quotes.UpdateQuote(
            //        ProFitGlobals.user,
            //        ProFitGlobals.activeClientId.GetValueOrDefault(),
            //        ProFitGlobals.activeQuoteId.Value,
            //        saveName,
            //        saveData,
            //        cover,
            //        topDownPhoto,
            //        frontPhotos,
            //        onSuccess: (newUserClientQuote) =>
            //        {
            //            int clientId = ProFitGlobals.activeClientId.Value;
            //            int quoteId = newUserClientQuote.id;

            //            if (!ProFitGlobals.TryGetClientAndQuoteIndex(clientId, quoteId, out int clientIndex, out int quoteIndex))
            //            {
            //                return;
            //            }

            //            ProFitGlobals.userClients[clientIndex].quotes[quoteIndex] = newUserClientQuote;
            //            ProFitGlobals.activeQuoteId = quoteId;

            //            MelcherLog.ConditionalLog($"Updated save for client ID {clientId}, quote ID {quoteId}");
            //        },
            //        onError: (request, error, jsonResult, callbackData) =>
            //        {
            //            MelcherLog.LogError(error);
            //        }
            //    );
            //}
            //else
            {
                ProFitGlobals.server.Quotes.CreateQuote(
                    ProFitGlobals.user,
                    ProFitGlobals.activeClientId.GetValueOrDefault(),
                    saveName,
                    saveNotes,
                    saveData,
                    screenshots.cover,
                    screenshots.topDown,
                    screenshots.front,
                    screenshots.topDownPlan,
                    screenshots.frontPlan,
                    onSuccess: (newUserClientQuote) =>
                    {
                        int quoteId = newUserClientQuote.id;

                        int clientId = ProFitGlobals.activeClientId.Value;
                        if (!ProFitGlobals.TryGetClientIndex(clientId, out int clientIndex))
                        {
                            return;
                        }

                        ProFitGlobals.userClients[clientIndex].quotes.Add(newUserClientQuote);
                        ProFitGlobals.activeQuoteId = quoteId;

                        // Pull the analytics token from the save data (the
                        // server may have modified it). Then, use this token
                        // (if it exists) to record the user's current product
                        // selection for analytics.

                        newUserClientQuote.saveData.TryGetValueFromJson("analytics_key", out string analyticsKey);

                        if (!string.IsNullOrEmpty(analyticsKey))
                        {
                            // Ensure our local data uses the server-assigned
                            // analytics token.
                            kitchen.analyticsKey = analyticsKey;
                            saveData["analytics_key"] = analyticsKey;

                            List<int> productIds = new ();

                            // These can be null and RecordAnalytics will
                            // safely ignore them.
                            if (kitchen.selectedCabinetFinish != null)
                            {
                                productIds.Add(kitchen.selectedCabinetFinish.id);
                            }

                            if (kitchen.selectedCountertopFinish != null)
                            {
                                productIds.Add(kitchen.selectedCountertopFinish.id);
                            }

                            if (kitchen.selectedFlooringFinish != null)
                            {
                                productIds.Add(kitchen.selectedFlooringFinish.id);
                            }

                            foreach (ElementIndex<Cabinet> i in kitchen.layout.EnumerateCabinets())
                            {
                                Cabinet cabinet = i.element;

                                foreach (Occupant occupant in cabinet.occupants)
                                {
                                    productIds.Add(occupant.productInfo.id);

                                    if (occupant.TryGetComponent(out CabinetShelf cabinetShelf))
                                    {
                                        foreach (Occupant nestedOccupant in cabinetShelf.occupants)
                                        {
                                            productIds.Add(nestedOccupant.productInfo.id);
                                        }
                                    }
                                }
                            }

                            ProFitGlobals.server.Analytics.RecordAnalytics(
                                ProFitGlobals.user,
                                analyticsKey,
                                productIds,
                                System.DateTime.UtcNow - kitchen.sessionStartTime,
                                onComplete: wasSuccessful =>
                                {
                                    if (wasSuccessful)
                                    {
                                        kitchen.sessionStartTime = System.DateTime.UtcNow;
                                    }
                                }
                            );
                        }

                        // Show a confirmation screen for the completed save.

                        if (ProFitGlobals.session.ShouldUseWhiteLabelMode())
                        {
                            string notificationMessage =
                                "Thank you for using our 3D Outdoor Kitchen Design Tool. Your design and quote have been emailed to you.";

                            if (ProFitGlobals.activeClientWantsToBeContacted.GetValueOrDefault(true))
                            {
                                string companyName =
                                    ProFitGlobals.userProfile.businessName;

                                if (string.IsNullOrWhiteSpace(companyName))
                                {
                                    companyName = "our company";
                                }

                                notificationMessage +=
                                    $" A representative of {companyName} will reach out to you directly to discuss your outdoor kitchen project.";
                            }

                            EventBridge.Designer.showNotification?.Invoke(
                                title: "Request Sent",
                                message: notificationMessage,
                                onDismiss: () =>
                                {
                                    string url = ProFitGlobals.userProfile.whiteLabelReturnUrl;

                                    if (string.IsNullOrWhiteSpace(url))
                                    {
                                        MelcherLog.Log("No redirect URL to return to.", context: this, severity: MelcherLogSeverity.Verbose);
                                        return;
                                    }

                                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                                    {
                                        MelcherLog.Log($"Prepending \"https://\" to the redirect URL: {url}", context: this, severity: MelcherLogSeverity.Verbose);
                                        url = "https://" + url;
                                    }

                                    // Allow the URL to receive the quote ID
                                    url = url.Replace("{$quoteId}", quoteId.ToString());

                                    MelcherWebGL.ChangeUrl(url);
                                }
                            );
                        }
                        else
                        {
                            EventBridge.Designer.showNotification?.Invoke(
                                title: "Thank You",
                                message: "Your design is now saved.",
                                onDismiss: null
                            );
                        }

                        MelcherLog.LogEditorOnly($"Created save for client ID {clientId} with quote ID {quoteId}");
                    },
                    onError: (builder, error, jsonValue) =>
                    {
                        MelcherLog.LogError(error.Message);

                        EventBridge.Designer.showSavePrompt.Invoke(
                            saveName,
                            saveNotes,
                            SaveDesign,
                            error.Message
                        );
                    }
                );
            }
        }

        protected void ShowRestartPrompt(MelcherButton caller)
        {
            ClosePanel();

            EventBridge.Designer.showConfirmPrompt.Invoke(
                title: "Restart?",
                message: "Erase the current build and start again from scratch?\n" +
                    "Any unsaved progress will be lost.",
                confirmLabel: "Restart",
                onConfirm: () =>
                {
                    // Don't reload the previous save
                    ProFitGlobals.activeQuoteId = null;

                    EventBridge.Designer.restart.Invoke();
                }
            );
        }

        protected void ShowQuitPrompt(MelcherButton button)
        {
            ClosePanel();

            EventBridge.Designer.showConfirmPrompt.Invoke(
                title: "Quit?",
                message: "Please remember to save your design before you quit.",
                confirmLabel: "Quit",
                onConfirm: () => EventBridge.Designer.loadMenuScene.Invoke()
            );
        }

        /// <summary>
        ///     There are some UI differences the client has requested for
        ///     White Label mode:
        ///     <br /><br />
        ///
        ///     - The Quit button in the hamburger menu is not to be shown.<br />
        ///     - The hamburger menu should always be open.<br />
        /// </summary>
        protected void SetupUI()
        {
            bool isWhiteLabelLayout = ProFitGlobals.session.ShouldUseWhiteLabelMode();

            bool showQuitButton = !isWhiteLabelLayout;
            m_QuitButton.gameObject.SetActive(showQuitButton);

            int quitSeparatorIndex =
                m_QuitButton.transform.GetSiblingIndex() - 1;

            if (quitSeparatorIndex >= 0)
            {
                // Hide the UI separator that appears directly above the quit
                // button.

                m_ButtonList.GetChild(quitSeparatorIndex).gameObject.SetActive(showQuitButton);
            }

            // The hamburger menu is kept permanently open in non-mobile
            // builds. This is to make the app easier to use for less
            // tech-savvy users. On mobile, screen space is a premium and
            // therefore the hamburger menu is still used.
            bool showHamburgerButton = MelcherWebGL.IsMobilePlatform();

            // TODO: This doesn't work on the first frame after loading
            //       the scene from PlatformSetupScene. Therefore, we use a
            //       coroutine.
            MEC.Timing.CallDelayed(delay: 1.0f, () => m_MenuAccordion.Snap(expand: !showHamburgerButton));

            // Hide the hamburger menu button if the accordion is permanently
            // open
            m_MenuButton.gameObject.SetActive(showHamburgerButton);

            // When white label mode is active, the accordion is shifted
            // more vertical (-padding) to avoid a large gap.
            LayoutGroup group = GetComponent<LayoutGroup>();
            RectOffset padding = group.padding;
            padding.top = Mathf.Abs(padding.top);
            if (isWhiteLabelLayout)
            {
                padding.top = -padding.top;
            }
            // Assign the padding back via the layout's property to force a
            // refresh.
            group.padding = padding;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected Accordion m_MenuAccordion = null;

        [Tooltip("The parent of the buttons")]
        [SerializeField] protected Transform m_ButtonList = null;

        [SerializeField] protected MelcherButton m_ExportJpgButton = null;
        [SerializeField] protected MelcherButton m_EditSizeButton = null;
        [SerializeField] protected MelcherButton m_SaveButton = null;
        [SerializeField] protected MelcherButton m_RestartButton = null;
        [SerializeField] protected MelcherButton m_QuitButton = null;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected KitchenScreenshotManager m_KitchenScreenshotManager = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected MelcherButton m_MenuButton = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected Vector4 m_MenuPanelPaddingWhenVisible;
        protected RecyclableTween<Tween> m_MenuSlideTween = new RecyclableTween<Tween>();

    }   // class DesignerHamburgerMenu

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-02
/// ===========================================================================
