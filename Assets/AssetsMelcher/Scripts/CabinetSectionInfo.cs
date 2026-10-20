namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Represents a type of cabinet as defined by an admin in the app's
    ///     web dashboard. This references a <see cref="CabinetVisual"/> by
    ///     its visual ID to specify the cabinet's appearance.
    ///     <br /><br />
    ///
    ///     This class was created to mitigate the fact that <see
    ///     cref="ProductVisualContent"/> was becoming too heavy: trying to handle
    ///     both kitchen products as well as kitchen cabinets.
    /// </summary>
    public class CabinetSectionInfo
    {
        public CabinetSectionInfo()
        {
        }

        public CabinetSectionInfo(in CabinetSectionInfo toClone)
        {
            id                  = toClone.id;
            cabinetId           = toClone.cabinetId;
            manufacturerId      = toClone.manufacturerId;
            groupId             = toClone.groupId;
            name                = toClone.name;
            description         = toClone.description;
            categoryId          = toClone.categoryId;
            visualId            = toClone.visualId;
            defaultSizeInMeters = toClone.defaultSizeInMeters;
            minWidthInMeters    = toClone.minWidthInMeters;
            maxWidthInMeters    = toClone.maxWidthInMeters;
        }

        /// <summary>
        ///     The cabinet's primary key in the backend. This is not really
        ///     relevant to anything that happens in the Unity app. Prefer to
        ///     use <see cref="cabinetId"/>.
        /// </summary>
        public int id;

        /// <summary>
        ///     The ID the user entered in the dashboard when they registered
        ///     this product. This is intended to be used in the kitchen's save
        ///     files as it is more readable than just a number.
        /// </summary>
        public string cabinetId;

        public int categoryId;

        public int manufacturerId;

        /// <summary>
        ///     The ID of the group this cabinet belongs to. (Groups are
        ///     defined in the web dashboard.)
        ///     <br /><br />
        ///
        ///     This is 0 if the cabinet doesn't belong to a group.
        /// </summary>
        public int groupId;

        public string name;

        public string description;

        public string visualId;

        public Vector3 defaultSizeInMeters;

        public float? minWidthInMeters;

        public float? maxWidthInMeters;

#if UNITY_EDITOR

        static CabinetSectionInfo()
        {
            const int ExpectedFieldCount = 11;

            System.Type thisType =
                MelcherEditorUtility.GetTypeFromStackTrace();

            int actualFieldCount = thisType.GetFields().Length;

            if (actualFieldCount != ExpectedFieldCount)
            {
                MelcherLog.LogError(
                    $"Ensure {MelcherFormat.ConsoleTypeName(thisType)}'s " +
                    $"copy constructor copies all fields and then update " +
                    $"{MelcherFormat.ConsoleSymbolName(nameof(ExpectedFieldCount))} " +
                    $"with the correct field count. " +
                    $"Expected {MelcherFormat.ConsoleNumericValue(ExpectedFieldCount)} " +
                    $"but found {MelcherFormat.ConsoleNumericValue(actualFieldCount)}."
                );
            }
        }

#endif  // if UNITY_EDITOR

    }   // class CabinetSectionInfo

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-09-30
/// ===========================================================================
