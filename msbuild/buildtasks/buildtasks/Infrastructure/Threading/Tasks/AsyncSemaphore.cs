namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// An Asynchronous version of the Semaphore.
    /// </summary>
    /// <remarks>
    /// This code has been taken from the MsDev Blog:
    /// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-5-asyncsemaphore/
    /// <para>The advantage of this primitive is that it doesn't need to be disposed of.</para>
    /// </remarks>
    internal class AsyncSemaphore
    {
        private readonly Queue<TaskCompletionSource<bool>> m_Waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_CurrentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncSemaphore"/> class.
        /// </summary>
        /// <param name="initialCount">
        /// The initial count (number of instances immediately available that won't block).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Initial count must be zero or greater.</exception>
        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount), Resources.Infra_Tasks_InitialCountZero);
            m_CurrentCount = initialCount;
        }

        /// <summary>
        /// Waits for an instance to become available (via <see cref="Release"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that can be waited on for when an instance is available.</returns>
        public Task WaitAsync()
        {
            lock (m_Waiters) {
                if (m_CurrentCount > 0) {
                    m_CurrentCount--;
                    return Task.CompletedTask;
                }
                TaskCompletionSource<bool> waiter = new TaskCompletionSource<bool>();
                m_Waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        /// <summary>
        /// Releases an instance in the semaphore.
        /// </summary>
        /// <remarks>
        /// Releases an instance in the semaphore, so that the next thread in the queue waiting is unblocked.
        /// </remarks>
        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_Waiters) {
                if (m_Waiters.Count > 0) {
                    // We need to be able to wait for individual waiters, hence the queue, one per waiter.
                    toRelease = m_Waiters.Dequeue();
                } else {
                    m_CurrentCount++;
                }
            }
            if (toRelease != null) toRelease.SetResult(true);
        }
    }
}
