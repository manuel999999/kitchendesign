// Weaver // https://kybernetik.com.au/weaver // Copyright 2022 Kybernetik //

#if UNITY_EDITOR

#pragma warning disable IDE0051 // Remove unused private members.

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal] References to various textures that are used as icons.</summary>
    internal sealed class Icons : WeaverIcons
    {
        /************************************************************************************************************************/

        /// <summary>Singleton instance assigned using Asset Injection.</summary>
        [AssetReference(EditorOnly = true, Optional = true, Tooltip =
            "These icons are used to indicate that an asset in the Project Window is referenced by one of the Weaver systems")]
        [Window.ShowInPanel(typeof(Window.MiscPanel))]
        private static new Icons Instance { set => WeaverIcons.Instance = value; }

        /************************************************************************************************************************/
    }
}

#endif
