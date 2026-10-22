namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    ///     Helper object used to pass data "by-ref".
    ///     <br /><br />
    ///
    ///     For example, this can be used to return a value from a coroutine or
    ///     to capture a string variable by-ref for use in <see
    ///     cref="MelcherDeferred"/>.
    ///     <br /><br />
    ///
    ///     <code>
    ///     // A Ref&lt;string&gt; allows an error string to be updated even
    ///     // after the defer is created.
    ///
    ///     Ref&lt;string&gt; error = new (null);
    ///
    ///     using var deferredErrorHandler =
    ///         MelcherDeferred.Defer(
    ///             toCapture: error,
    ///             onDispose: static capturedError =>
    ///             {
    ///                 if (!string.IsNullOrEmpty(capturedError.value))
    ///                 {
    ///                     MelcherLog.LogError(capturedError.value);
    ///                 }
    ///             }
    ///         );
    ///
    ///     // Possibly changing the value of `error`. If it were simply a
    ///     // string, its new value would not be visible in the defer.
    ///
    ///     error.value = AssetDatabase.RenameAsset(/* */);
    ///     </code>
    /// </summary>
    public class Ref<T>
    {
        public Ref(T value)
        {
            this.value = value;
        }

        public T value;

    }   // class Ref

}   // namespace KitchenDesign

