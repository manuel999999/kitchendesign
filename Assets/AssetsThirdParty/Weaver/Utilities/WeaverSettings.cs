// Weaver // https://kybernetik.com.au/weaver // Copyright 2022 Kybernetik //

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Weaver.Editor
{
    /// <summary>[Editor-Only, Internal]
    /// A <see cref="ScriptableObject"/> which stores various data used by <see cref="Weaver"/>.
    /// </summary>
    internal sealed class WeaverSettings : WeaverSettingsBase { }
}

#endif
