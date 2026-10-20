namespace Melcher
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A <see cref="MelcherResetStaticFieldsMethodAttribute"/> is used to mark
    /// static functions that are responsible for resetting a class's static
    /// fields between play sessions.
    /// <br /><br />
    ///
    /// The functions must be <see langword="static"/> and should ideally return
    /// an <see cref="int"/> indicating how many static fields were reset by
    /// the function. The return value of the function is used to double-check
    /// that all static fields were reset. (This can be very helpful to catch
    /// bugs caused when a new field is added to a class but isn't reset.)
    /// <br /><br />
    ///
    /// When the Unity project is using play-mode optimizations (i.e., avoiding
    /// reloading the Assembly domains), Unity recommends that developers
    /// manually reset their scripts' static fields during the
    /// <see cref="UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration"/>
    /// initialization phase. This decorated function will execute during this
    /// phase.
    /// <br /><br />
    ///
    /// A key feature of this attribute is it works even within generic types.
    /// Unity's built-in <see cref="UnityEngine.RuntimeInitializeOnLoadMethodAttribute"/>
    /// does not work on generic types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [Conditional("UNITY_EDITOR")]
    public class MelcherResetStaticFieldsMethodAttribute : Attribute
    {
    }   // class MelcherResetStaticFieldsMethodAttribute

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-07
/// ===========================================================================
