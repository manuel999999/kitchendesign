# Melcher Core for Unity

This repo is an ongoing effort to implement solutions to recurring design problems in Unity.

Some Unity systems are replaced with wrappers to provide extra functionality.

# Features

## MelcherLog

A full replacement of `UnityEngine.Debug`. The functions and their signatures are intentionally identical to make it possible to swap one for the other by changing just the class name. It provides more automatic context logging than the default logger; logged messages implicitly contain the name of the file that logged them as a prefix.

`MelcherLog.Log("This is my log message");`
> [MyScript] This is my log message

Other features:

* Log verbosity levels. Messages can be filtered by log level.
* A `Breadcrumb` function to quickly log the file, function and line number. Useful when debugging.
* Editor-only versions of the Unity log functions. These only emit log messages in the editor or development builds.

## MelcherMonoBehaviour

`MelcherMonoBehaviour` supersedes Unity's `MonoBehaviour`. It adds support for the `MelcherFindComponent` system (described below) that is used extensively throughout the package.

## MelcherFindComponentAttribute

`MelcherFindComponent` is a field attribute for `Component`-type variables declared within a `MelcherMonoBehaviour`. It attempts to locate the requested component(s) automatically via a search strategy specified by the user. See below for the different search modes.

Example:

```csharp
public class Car
    : MelcherMonoBehaviour // must be a MelcherMonoBehaviour
{

    // Find a component on the game object this object is attached to
    [SerializeField]
    [MelcherFindComponent]
    private SteeringWheel m_SteeringWheel;


    // Search for a component anywhere in the scene (or prefab, if this is an asset)
    [SerializeField]
    [MelcherFindComponent(MelcherFindComponentMode.Scene)]
    private Driver m_Driver;


    // It also supports arrays and lists. Here, minDistance tells the
    // search to skip the calling game object and start searching its
    // children
    [SerializeField]
    [MelcherFindComponent(MelcherFindComponentMode.Down, minDistance: 1)]
    private Wheel[] m_Wheels;

}   // class Car
```

The values that are found are serialized into the field. This happens during the object's `OnValidate` callback, provided the object is a `MelcherMonoBehaviour`.

This system of finding references at build-time and caching them outperforms searches in Awake/Start. It also eliminates the error-prone job of manually assigning references in the inspector. It automatically and optionally reports errors when a search fails. It makes deterministic loading easier because all serialized fields are ready to be accessed immediately at Awake.

There are several parameters to alter the search:

* **searchMode** (`MelcherFindComponentMode`)

    This argument defines which objects to consider in the search.

    | Mode    | Description  |
    |---------|--------------|
    | `Self`  | Search the game object the script is attached to for the component. This is the default mode. |
    | `Down`  | Search down the hierarchy (pre-order traversal). Without arguments, the search will include the calling game object and exclude any disabled child game objects. |
    | `Up`    | Search up the hierarchy. Without arguments, the search will include the calling game object. Disabled game objects are always considered during upwards searches. |
    | `Scene` | Anywhere in the scene/prefab. The search starts at the root and searches similar to the `down` mode.  |

* **minDistance** (`int`)

    The minimum distance (in terms of hierarchy levels) from the calling game object that a component can be found. In other words, this indicates how many hierarchy levels will be ignored at the start of the search.

    The default value is 0. This means "no offset" or "start searching from and including the calling object".

    A value of 1 means skip one hierarchy level, i.e., ignore the calling object. The search will begin with the caller's children (`MelcherFindComponentMode.Down`) or its direct parent (`MelcherFindComponentMode.Up`).

* **count** (`int`)

    The max number of hierarchy levels the search will check. This is relative to the shifted starting point.

    I.e., if this is 0, nothing will be checked; and if it's 1, only the caller (when `minDistance == 0`) or objects that are `minDistance`-hierarchy levels away will be checked. At higher numbers, more hierarchy levels are searched.

    The default value is -1 which is interpreted to mean "no depth limit."

* **includeInactiveGameObjects** (`bool`)

   When `true`, inactive game objects are included in the search candidates. Otherwise, inactive game objects are ignored and will prevent the search from visiting the disabled objects children.

   For upwards searches, this value has no effect.

   The default is `false`.

* **includeInactiveComponents** (`bool`)

   When `true`, components that are disabled on their game objects can be found. Otherwise, only components that are enabled will be returned.

   For components that do not have an enabled property, they are always treated as being enabled.

   The default is `true`.

## MelcherScriptableObject

`MelcherScriptableObject` supersedes Unity's `ScriptableObject` and offers two extra features:

1. It has support for `MelcherFindAsset`. This is an attribute similar to `MelcherFindComponent` but is used to locate assets.
2. Instances are notified when the editor's play mode changes. This can be used to reset the state of a scriptable object when play mode exits.

    ```csharp
    public override void OnEditorPlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
    {
        if (mode == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            // Do something cool. E.g., reset our data to its defaults
        }
    }
    ```

## MelcherUnicodeDictionary

A `MelcherUnicodeDictionary` is useful when working with a graphical font like FontAwesome where it is more convenient to be able to type an alias like 'arrow-right' rather than copy and paste the actual glyph or memorize its unicode number. Unicode dictionaries are ScriptableObjects that associate a font with a mapping of aliases to unicode characters within that font.

To create a unicode dictionary, create its asset using the right-click menu in the project window or the Assets item on the menu bar: **Create** > **Melcher Studios** > **Unicode Dictionary**. Unicode dictionaries must be placed in the project's Resources folder where they can be loaded at runtime. The dictionary asset is best named after the font it represents.

Aliases are defined inside the dictionary. Each alias has a name (e.g., 'arrow-right') and a glyph (e.g., '\\uf061'). The glyph can be anything including regular, non-escaped text. If the output is recognized, it will appear as the label on its button. Clicking this button will copy the full translation phrase for the glyph into the clipboard which can be used in a `MelcherLocalizedText`.

Aliases are used/expressed inside a `MelcherLocalizedText` using `MelcherLocalizer`'s translation phrase syntax: e.g., '$(arrow-right)'. The glyph/text associated with the alias will be inserted into the TextMesh Pro Text object and wrapped in a rich text tag of the appropriate font.

The fonts used by unicode dictionaries must be placed in the Resources folder so they can be loaded dynamically. Be sure that the project's TMP Settings asset has the correct path to the fonts resources. We typically remove the "Fonts & Materials" part and load directly from the root of the Resources folder.

The **Rebuild** button (seen in the unicode dictionary's inspector) generates a text sub-asset that contains all of the output glyphs used by the dictionary. This text file can be used by TextMesh Pro's Font Asset Creator to generate a texture atlas. To do this, set the font's Character Set dropdown to "Characters from File" and choose the unicode dictionary's text asset before generating the font atlas. Sometimes the text asset does not appear. This may require the project to be re-saved or the editor to be restarted.

To refresh loaded dictionaries at editor time, start and stop play mode.

## MelcherSceneRequest

`MelcherSceneRequest` is a unified way to identify a scene. A scene request object is created using one of the class's factory methods (see below). A request will internally reference a scene using a build index, scene handle, scene name, or path. Using requests, we avoid the need to provide many different function overloads for scene-based APIs.

```csharp
public void StartSceneTransition(MelcherSceneRequest request) { /* ... */ }

// The type of scene request is arbitrary. Here, we use a scene name
var request = MelcherSceneRequest.FromName("ShopScene");

TransitionToScene(request);
```

Use one of these factory methods to create a scene request:

- `FromBuildIndex` - The index in project's scene build list
- `FromHandle` - A handle of a loaded or loading scene
- `FromName` - The scene's name
- `FromPath` - The scene's path (without the .unity file extension)
- `FromScene` - A valid scene object

## MelcherSceneManager

`MelcherSceneManager` supersedes `UnityEngine.SceneManagement.SceneManager`. Scenes must be loaded and unloaded via its functions in order for the new features to work properly.

```csharp
// Load a scene additively.
MelcherSceneManager.LoadSceneAsync(
    request:  MelcherSceneRequest.FromName("ShopScene"),
    loadMode: MelcherSceneLoadMode.Additive,
    onLoaded: (loadedScene, loadMode) =>
        MelcherLog.Log($"Shop scene finished loading!")
);
```

`sceneUnloading` is a new event that is notified whenever a scene is just about to unload.

This static class must be initialized by calling `MelcherSceneManager.Init()` once in the applications life-time.

## MelcherMaterialProperties

`MelcherMaterialProperties` binds a Unity `MaterialPropertyBlock` with a list of renderers. It's intended to make material property overrides easier to work with. It allows multiple renderers to be updated through one object. This class uses a function-chaining API.

Example of creating a `MelcherMaterialProperties` for a list of renderers:

```csharp
MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

MelcherMaterialProperties properties = new MelcherMaterialProperties(meshRenderers);
```

Example of fetching values from the material and setting property overrides:

```csharp
// Fetch an initial value from the source material
int mainColorPropertyId = Shader.PropertyToID("_Color");
properties.GetColorFromMaterial(mainColorPropertyId, out Color _);

// Set an override
int emissiveColorPropertyId = Shader.PropertyToID("_EmissiveColor");
properties.SetColorOverride(emissiveColorPropertyId, Color.white);

// Apply the changes to all the renderers that were bound
properties.Apply();
```

## States and state machines

### IMelcherState

States are used to manage program flow. They're useful to bookend some process with activation and deactivation logic. E.g., scene setup in the activation phase and tear-down in its deactivation.

`IMelcherState` has 5 abstract functions:

1. `OnStateAdded` - Called when a state is added to a state machine. This is where the state's start-up actions should go. E.g., starting some music, showing some UI, etc.

    This is always paired with an eventual `OnStateRemoved()` call.

2. `OnStateNowTopmost` - Called when a state becomes the topmost state in its state machine. I.e., when a state is pushed it becomes the topmost state; when it is popped, the previous state becomes the topmost.

    This can be useful to implement pause/resume behaviour between child states.

    This is always paired with an eventual `OnStateNowNotTopmost()` call.

3. `OnStateEvent` - Called when a state machine receives an update event. The event is processed by each state in the state machine in the reverse order they were added. This function can return `false` to prevent the event propagating any further.

    For example, `MelcherGlobalStateMachine` calls `OnStateEvent()` for every FixedUpdate, Update and LateUpdate tick (passing in a corresponding event object).

4. `OnStateNowNotTopmost` - Called when a new state is pushed onto a state machine, displacing this state from its topmost position.

5. `OnStateRemoved` - Called when a state is removed from a state machine. This is where the state's cleanup actions should go.


## MelcherResetStaticFieldsMethodAttribute

`MelcherResetStaticFieldsMethodAttribute` is a function attribute designed to make it easier to work in a project that has domain reloading disabled.

With domain reloading disabled, the editor switches to play mode faster but it also is more prone to bugs because it causes static fields to preserve their previous value between play sessions. Unity states that developers should clear all static fields and events in a script using a specially marked function.  Awkwardly, the type of attribute Unity says to use depends on whether the script is a runtime or editor script.

`MelcherResetStaticFieldsMethodAttribute`, on the other hand, can be used with any static function regardless of whether the script is for the editor or the runtime. Additionally, the attribute automatically double-checks that the function clears *all* static fields. This is helpful to catch a common error where someone declares a new static field but forgets to add it to the reset function.

Example use:

```csharp
public class StaticFieldResetExample : MelcherMonoBehaviour
{
    private static List<GameObject> s_Objects = new List<GameObject>();

    private static int s_NumberOfSomething;

    /// <summary>
    ///     Ensure static fields are reset between play sessions.
    /// </summary>
    [MelcherResetStaticFieldsMethod]
    private static int ResetStatics()
    {
        int fieldCount = 0;

        // You can do something like this to count each reset on its own line
        ++fieldCount;	s_Objects.Clear();
        ++fieldCount;	s_NumberOfSomething = 0;

        return fieldCount;
    }
}
```

For a longer write up about domain reloading, its benefits and side-effects, see [Domain Reloading](https://docs.unity3d.com/Manual/DomainReloading.html) in the Unity documentation.
