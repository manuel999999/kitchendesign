namespace Melcher
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// <br>
    ///     Attribute to show or hide fields based on the value of one other field, set in the inspector.
    /// </br>
    /// <para>
    ///     Use attribute on any public or SerializeField marked field. Pass in reference to the current
    ///     class type, the name of property we want to check against, and an object with the value that
    ///     is required to match with the property we're checking against.
    /// </para>
    /// <para>
    ///     Example Usage:
    ///     public enum TestEnum { Value, AnotherValue, ValueThree }
    ///     [SerializeField]
    ///     private TestEnum m_MyTestEnum;
    ///
    ///     [MelcherShowOnValue(typeof(MyClass), nameof(m_MyTestEnum), TestEnum.ValueThree)]
    ///     [SerializeField]
    ///     private string m_MyString;
    /// </para>
    /// <para>
    ///     In this example the m_MyString field will only show in inspector if the user sets the
    ///     m_MyTestEnum value to TestEnum.ValueThree in the inspector.
    /// </para>
    ///     Current drawbacks: Addressable Asset Reference types are not supported
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class MelcherShowOnValueAttribute : PropertyAttribute
    {
        public readonly string variable;
        public readonly bool isToShow;
        public readonly object showOnValue;
        public readonly object fieldInfoObject;


        /// <param name="type">Requires the current class we are working in to be passed in. Example: typeof(MyClass)</param>
        /// <param name="variable">Requires the name of the field we want to check against for showing/hiding property. 
        /// Example: nameof(m_MyBool) (NOTE: Arrays, Lists, and Dictionaries are not supported at this time.)</param>
        /// <param name="value">Requires the value to trigger a positive comparison with the incoming value.
        /// Example: true, or TestEnum.ValueThree</param>
        /// <param name="callerFilePath"></param>
        public MelcherShowOnValueAttribute(
                Type type,
                string variable,
                object value,
                [CallerFilePath] string callerFilePath = null)
        {
#if UNITY_EDITOR
            this.variable = variable;
            this.showOnValue = value;
            string scriptType = type.AssemblyQualifiedName;
            Type caller = (Type)Type.GetType(scriptType);

            FieldInfo fieldInfo = caller.GetField(variable,
                BindingFlags.NonPublic |
                BindingFlags.IgnoreCase |
                BindingFlags.Public |
                BindingFlags.Instance);

            if (fieldInfo == null)
            {
                DisplayError(variable);
                return;
            }

            this.fieldInfoObject = fieldInfo;
#endif
        }

        public void DisplayError(string value)
        {
            MelcherLog.LogError($"Type or field {value} is incorrect or missing. \n" +
            "Please check your attribute parameters in the calling class.");
        }
    }   // class MelcherHeaderAttribute

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2022-08-16
/// ===========================================================================
