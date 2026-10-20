namespace Melcher
{
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// <see cref="MelcherLocalizedText"/> wraps and updates a <see
    /// cref="TMP_Text"/> so it displays text dependent on the player's chosen
    /// language.
    /// <br /><br />
    ///
    /// Users of this class should assign a string to <see cref="phrase"/>.
    /// Phrase can contain zero or more translation keys (text with a special
    /// syntax so it can be used as a key in the translation tables). The
    /// underlying text object will be updated to reflect the translated
    /// phrase.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class MelcherLocalizedText : MelcherMonoBehaviour
    {
        // =====================================================================
        // Events
        // =====================================================================

        /// <summary>
        /// Allow text to be modified post-translate. This is useful for adding
        /// richtext tags to list items, etc.
        /// </summary>
        public delegate void TextProcessorHandler(MelcherLocalizedText caller, ref string translatedText);
        public event TextProcessorHandler onPostProcess;

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        /// The underlying <see cref="TextMeshProUGUI"/>
        /// </summary>
        public TMP_Text textMesh => m_TextMesh;

        /// <summary>
        /// A string containing zero or more translation keys. The text will be
        /// the phrase with all of the keys replaced with their translations
        /// </summary>
        public string phrase
        {
            get => m_Phrase;
            set
            {
                m_Phrase = value;
                RefreshText();
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        /// Refresh the displayed text.
        /// </summary>
        public void RefreshText()
        {
            string text = OnRefreshText(m_Phrase);
            onPostProcess?.Invoke(this, ref text);
            m_TextMesh.text = text;
        }

        protected virtual void OnEnable()
        {
            MelcherLocalizer.onLanguageChanged += UpdateTextOnLanguageChange;
            RefreshText();
        }

        protected virtual void OnDisable()
        {
            MelcherLocalizer.onLanguageChanged -= UpdateTextOnLanguageChange;
        }

        /// <summary>
        ///     This method is virtual so it possible to implement custom
        ///     logic, such as forcing as specific language code during
        ///     localization.
        /// </summary>
        protected virtual string OnRefreshText(string text)
        {
            MelcherLocalizer.Localize(ref text);
            return text;
        }

        protected virtual void UpdateTextOnLanguageChange(string languageCode)
        {
            RefreshText();
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        /// <summary>
        /// The untranslated phrase to show here.
        /// </summary>
        [SerializeField]
        [TextArea(minLines: 5, maxLines: 8)]
        protected string m_Phrase = null;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected TMP_Text m_TextMesh = null;

#if UNITY_EDITOR
        // =====================================================================
        // Editor only
        // =====================================================================

        [UnityEditor.MenuItem("CONTEXT/MelcherLocalizedText/Refresh TextMesh")]
        protected static void RefreshTextMesh(UnityEditor.MenuCommand command)
        {
            if (command.context is MelcherLocalizedText localizedText)
            {
                localizedText.RefreshText();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RefreshText();
            }
        }

        /// <summary>
        ///     Called when a <see cref="MelcherLocalizedText"/> is first added
        ///     to a game object. This function moves the added component above
        ///     the (required) <see cref="TMP_Text"/> component, which makes it
        ///     less likely for developers to miss and mistakenly edit the
        ///     original text mesh's text field.
        /// </summary>
        protected virtual void Reset()
        {
            // Initialize the localized text with the existing TMP text.
            TMP_Text textMesh = GetComponent<TMP_Text>();
            m_Phrase = textMesh.text;

            const int MaxComponentMoveUpIterations = 32;

            // Move the localized component to the top.
            MelcherSafeLoop safeLoop = MelcherSafeLoop.WithMaxIterations(MaxComponentMoveUpIterations);
            while (safeLoop.While(UnityEditorInternal.ComponentUtility.MoveComponentUp(this)))
            {
                // Condition does all the work.
            }

            if (safeLoop.isRunaway)
            {
                MelcherLog.LogError($"Could not make {typeof(MelcherLocalizedText).Name} the first component.", this);
            }

            // Move the canvas renderer to the top to preserve normal component
            // ordering (canvas text mesh).
            CanvasRenderer canvasRenderer = textMesh.canvasRenderer;
            if (canvasRenderer != null)
            {
                safeLoop = MelcherSafeLoop.WithMaxIterations(MaxComponentMoveUpIterations);
                while (safeLoop.While(UnityEditorInternal.ComponentUtility.MoveComponentUp(canvasRenderer)))
                {
                    // Condition does all the work.
                }

                if (safeLoop.isRunaway)
                {
                    MelcherLog.LogError($"Could not make {typeof(CanvasRenderer).Name} the first component.", this);
                }
            }

            // Move the canvas renderer to the top to preserve normal component
            // ordering (3D text mesh).
            if (textMesh.TryGetComponent(out MeshRenderer meshRenderer))
            {
                safeLoop = MelcherSafeLoop.WithMaxIterations(MaxComponentMoveUpIterations);
                while (safeLoop.While(UnityEditorInternal.ComponentUtility.MoveComponentUp(meshRenderer)))
                {
                    // Condition does all the work.
                }

                if (safeLoop.isRunaway)
                {
                    MelcherLog.LogError($"Could not make {typeof(MeshRenderer).Name} the first component.", this);
                }
            }
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherLocalizedText

}   // namespace Melcher

