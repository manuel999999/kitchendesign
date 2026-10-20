namespace Melcher
{
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;

    // This script defines delegate-like types that support `await`. Each
    // delegate subscriber is invoked sequentially (in the order of first
    // registered to last registered). It advances to the next listener only
    // when the current listener's `Task` completes.
    //
    // A standard C# delegate cannot be awaited. This poses a problem in this
    // project because all of the events were originally designed to be invoked
    // synchronously yet the Addressables feature requires asynchronous code
    // (primarily because of WebGL's inability to use synchronous functions
    // like `AsyncOperationHandle.WaitForCompleted()`)

    /// <summary>
    ///     A delegate whose subscribers are invoked and awaited in the order
    ///     they were subscribed. I.e., subsequent subscribers are not started
    ///     until the subscriber before it has completed.
    /// </summary>
    public class SequentiallyAwaitedDelegate
    {
        public delegate UniTask Handler();

        public event Handler OnInvoke
        {
            add => m_Subscribers.Add(value);
            remove => m_Subscribers.Remove(value);
        }

        public async UniTask InvokeIfNonEmpty()
        {
            if (m_Subscribers.Count <= 0)
            {
                return;
            }

            await Invoke();
        }

        public async UniTask Invoke()
        {
            // Use a copy of the subscriber list to safely allow reentry.
            List<Handler> subscribersSnapshot = new (m_Subscribers);

            foreach (Handler handler in subscribersSnapshot)
            {
                await handler.Invoke();
            }
        }

        private List<Handler> m_Subscribers = new ();
    }

    /// <summary>
    ///     <inheritdoc cref="SequentiallyAwaitedDelegate"/>
    ///     <br /><br />
    ///
    ///     This version is intended to handle an arbitrary number of arguments
    ///     by way of a tuple type. The advantage of using a tuple is it allows
    ///     the declaring field to provide names for the arguments.
    /// </summary>
    /// <typeparam name="TArg">
    ///     If more than one argument is needed, pass a tuple or custom type.
    /// </typeparam>
    public class SequentiallyAwaitedDelegate<TArg>
    {
        public delegate UniTask Handler(TArg arg);

        public event Handler OnInvoke
        {
            add => m_Subscribers.Add(value);
            remove => m_Subscribers.Remove(value);
        }

        public async UniTask InvokeIfNonEmpty(TArg arg)
        {
            if (m_Subscribers.Count <= 0)
            {
                return;
            }

            await Invoke(arg);
        }

        public async UniTask Invoke(TArg arg)
        {
            // Use a copy of the subscriber list to safely allow reentry.
            List<Handler> subscribersSnapshot = new (m_Subscribers);

            foreach (Handler handler in subscribersSnapshot)
            {
                await handler.Invoke(arg);
            }
        }

        private List<Handler> m_Subscribers = new ();
    }

}

