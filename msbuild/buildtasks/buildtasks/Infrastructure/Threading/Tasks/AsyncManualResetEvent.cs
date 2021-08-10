namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An Asynchronous version of the ManualResetEvent.
    /// </summary>
    /// <remarks>
    /// This code has been taken from the MsDev Blog:
    /// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/
    /// <para>The advantage of this primitive is that it doesn't need to be disposed of.</para>
    /// </remarks>
    internal class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> m_EventTask = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncManualResetEvent"/> class.
        /// </summary>
        /// <remarks>
        /// The default state of the <see cref="AsyncManualResetEvent"/> is not set.
        /// </remarks>
        public AsyncManualResetEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncManualResetEvent"/> class.
        /// </summary>
        /// <param name="initialState">
        /// Allows to set the initial state of the <see cref="AsyncManualResetEvent"/>. If <see langword="true"/> then
        /// it is initially set.
        /// </param>
        public AsyncManualResetEvent(bool initialState)
        {
            if (initialState) m_EventTask.SetResult(true);
        }

        /// <summary>
        /// Waits asynchronously for the event to be set.
        /// </summary>
        /// <returns>An awaitable task that is set when the <see cref="Set"/> method is called.</returns>
        public Task WaitAsync()
        {
            return m_EventTask.Task;
        }

        /// <summary>
        /// Waits synchronously for the event to be set.
        /// </summary>
        /// <returns>An awaitable task that is set when the <see cref="Set"/> method is called.</returns>
        public void Wait()
        {
            m_EventTask.Task.Wait();
        }

        /// <summary>
        /// Sets this instance, so that waiting tasks are unblocked.
        /// </summary>
        /// <remarks>
        /// Sets the <see cref="AsyncManualResetEvent"/>, so that other tasks can continue. The behavior of this class
        /// is to allow other threads to continue on the thread that is calling this method (task continuation will be
        /// done on this thread as part of this <see cref="Set"/> if the runtime so chooses).
        /// </remarks>
        public void Set()
        {
            m_EventTask.TrySetResult(true);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        /// <remarks>
        /// Subsequent calls to <see cref="WaitAsync"/> will be blocking waiting for a call to Set.
        /// </remarks>
        public void Reset()
        {
            while (true) {
                // Only bring in a new uncompleted task in the case this one is completed, so no other thread can get
                // a task that is orphaned and can never be set.
                TaskCompletionSource<bool> eventTask = m_EventTask;
                if (!eventTask.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref m_EventTask, new TaskCompletionSource<bool>(), eventTask) == eventTask)
                    return;
            }
        }
    }
}
