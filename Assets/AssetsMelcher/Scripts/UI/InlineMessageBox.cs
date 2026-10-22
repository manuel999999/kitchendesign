namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///     A UI control that shows a message/error or is otherwise completely
    ///     hidden. It is intended to display error messages when something in
    ///     a UI errors. E.g., the user is logging in but doesn't enter a
    ///     password.
    /// </summary>
    public class InlineMessageBox : MelcherMonoBehaviour
    {
        public void Info(string message)
        {
            m_Circle.phrase = "$(circle)";
            m_Circle.textMesh.color =
                ProFitGlobals.appOptions.appSkin.primaryColor;
            m_Icon.phrase = "$(info)";
            m_Message.phrase = message;
        }

        public void Error(string message)
        {
            MelcherLog.LogError(message);
            m_Circle.phrase = "$(circle)";
            m_Circle.textMesh.color =
                ProFitGlobals.appOptions.appSkin.dangerColor;
            m_Icon.phrase = "$(exclamation)";
            m_Message.phrase = message;
        }

        public void ErrorFromWebRequest(MelcherWebRequestBuilder builder, System.Exception error, MelcherJsonValue jsonValue)
        {
            List<string> errors = new List<string>();

            if (jsonValue.isObject)
            {
                MelcherJsonObjectParser parser = new MelcherJsonObjectParser(jsonValue.jsonObject);

                if (parser.TryGetErrorMap("errors", out Dictionary<string, List<string>> errorMap))
                {
                    foreach (var issue in errorMap)
                    {
                        foreach (string e in issue.Value)
                        {
                            errors.Add(e);
                        }
                    }
                }
            }

            if (error != null)
            {
                errors.Add(error.Message);
            }

            string errorMessage = string.Join("\n", errors);

            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = "Unknown error. Response code " + builder.request.responseCode;
            }

            Error(errorMessage);
        }

        public void Clear()
        {
            m_Circle.phrase = string.Empty;
            m_Icon.phrase = string.Empty;
            m_Message.phrase = string.Empty;
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected MelcherLocalizedText m_Circle = null;
        [SerializeField] protected MelcherLocalizedText m_Icon = null;
        [SerializeField] protected MelcherLocalizedText m_Message = null;

    }   // class InlineMessageBox

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-25
/// ===========================================================================
