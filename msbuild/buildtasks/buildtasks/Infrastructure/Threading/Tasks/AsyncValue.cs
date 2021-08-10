namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Be able to set a value, and asynchronously wait that it is set from another thread.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <remarks>
    /// Used in multithreading environments, especially useful with tasks. If two threads enter at the same time, one
    /// thread could be performing a long running operation, for which the value needs to be obtained, and the second
    /// thread needs to wait until the value is present. With proper synchronization, only one thread needs to do the
    /// work and wait for the result if another thread is already doing the wait.
    /// </remarks>
    /// <example>
    /// <code lang="csharp"><![CDATA[
    /// private AsyncValue<string> m_Branch = new AsyncValue<string>();
    ///
    /// public async Task<string> GetBranchAsync(string path)
    /// {
    ///   // If this is the first time this function is called, it will set executing the async lambda. If a different
    ///   // thread has already called this method, this thread will now wait for the result to be set by that
    ///   // different thread and return when it's done, so the async lambda isn't called, but becomes asynchronous.
    ///   // Finally, if the value is already set by the time this method is called, the value is returned
    ///   // synchronously without calling the async lambda.
    ///
    ///   return await m_Branch.GetSetAsync(async () => {
    ///     return await GetBranchInternalAsync();
    ///   });
    /// }]]>
    /// </code>
    /// </example>
    internal class AsyncValue<TValue>
    {
        private Exception m_Exception;
        private TValue m_Value;

        private const int NotCompleted = 0;
        private const int IsRunning = 1;
        private const int IsCompleted = 2;

        private int m_Set;
        private volatile AsyncManualResetEvent m_Complete;

        /// <summary>
        /// An object that can be awaited on for when the value is set with a call to <see cref="Set(TValue)"/> or
        /// <see cref="GetSetAsync(Func{Task{TValue}})"/>
        /// </summary>
        /// <value>The object which can be awaited on.</value>
        /// <remarks>
        /// You should use the method <see cref="GetSetAsync(Func{Task{TValue}})"/> for setting and awaiting on the
        /// value instead of this property directly. Once the <see cref="Complete"/> is set, you can get the value from
        /// the <see cref="Get"/> method without blocking. If the value is set multiple times, it's not guaranteed which
        /// value is returned (only the current value at the time it is read).
        /// </remarks>
        public AsyncManualResetEvent Complete
        {
            get
            {
                if (m_Complete == null) {
                    int done = m_Set;

                    // Allocate a new structure, but in a thread safe manner. If we get here, we always create a new
                    // object, but it is only assigned if the original object m_Complete is not yet assigned. If it is
                    // assigned in another thread, we just throw it away.
                    AsyncManualResetEvent mre = new AsyncManualResetEvent();
                    if (Interlocked.CompareExchange(ref m_Complete, mre, null) == null) {
                        if (done != IsCompleted && m_Set == IsCompleted) {
                            m_Complete.Set();
                        }
                    }
                }
                return m_Complete;
            }
        }

        /// <summary>
        /// Gets the value, or sets if it isn't already set, and await on the value to be set.
        /// </summary>
        /// <param name="func">The function that should be awaited on to set the result.</param>
        /// <returns>The value that is set, either by this function, or another call in another thread.</returns>
        /// <remarks>
        /// This method ensures that only one thread is setting the value. If a second thread tries to set the value
        /// while it is in the process of being set, it will just await for the first thread to finish and return the
        /// result set by the first thread. If it is already set prior, it returns the result without awaiting at all.
        /// <para>
        /// If the value is set multiple times via <see cref="Set(TValue)"/>, the value returned here is undefined, it
        /// could be the first or any later that happens to be set before this method returns.
        /// </para>
        /// </remarks>
        public async Task<TValue> GetSetAsync(Func<Task<TValue>> func)
        {
            if (Interlocked.CompareExchange(ref m_Set, IsRunning, NotCompleted) == NotCompleted) {
                try {
                    TValue result = await func();
                    return Set(result);
                } catch (Exception ex) {
                    Set(ex);
                    throw;
                }
            }
            return await GetAsync();
        }

        /// <summary>
        /// Gets the value, or sets if it isn't already set, and block on the value to be set.
        /// </summary>
        /// <param name="func">The function that should be executed to set the result.</param>
        /// <returns>The value that is set, either by this function, or another call in another thread.</returns>
        /// <remarks>
        /// This method ensures that only one thread is setting the value. If a second thread tries to set the value
        /// while it is in the process of being set, it will just await for the first thread to finish and return the
        /// result set by the first thread. If it is already set prior, it returns the result without awaiting at all.
        /// <para>
        /// If the value is set multiple times via <see cref="Set(TValue)"/>, the value returned here is undefined, it
        /// could be the first or any later that happens to be set before this method returns.
        /// </para>
        /// </remarks>
        public TValue GetSet(Func<TValue> func)
        {
            if (Interlocked.CompareExchange(ref m_Set, IsRunning, NotCompleted) == NotCompleted) {
                try {
                    TValue result = func();
                    return Set(result);
                } catch (Exception ex) {
                    Set(ex);
                    throw;
                }
            }
            return Get();
        }

        /// <summary>
        /// Waits asynchronously for another thread to set the value, or if it is already set, return the value
        /// immediately.
        /// </summary>
        /// <returns>The value</returns>
        /// <remarks>
        /// If the value is set multiple times via <see cref="Set(TValue)"/>, the value returned here is undefined, it
        /// could be the first or any later that happens to be set before this method returns.
        /// </remarks>
        public async Task<TValue> GetAsync()
        {
            if (Thread.VolatileRead(ref m_Set) != IsCompleted) await Complete.WaitAsync();
            if (m_Exception != null) throw m_Exception;
            return m_Value;
        }

        /// <summary>
        /// Waits synchronously for another thread to set the value, or if it is already set, return the value
        /// immediately.
        /// </summary>
        /// <returns>The value</returns>
        /// <remarks>
        /// If the value is set multiple times via <see cref="Set(TValue)"/>, the value returned here is undefined, it
        /// could be the first or any later that happens to be set before this method returns.
        /// </remarks>
        public TValue Get()
        {
            if (Thread.VolatileRead(ref m_Set) != IsCompleted) Complete.Wait();
            if (m_Exception != null) throw m_Exception;
            return m_Value;
        }

        /// <summary>
        /// Sets the value and notify any waiting threads that it is set.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The value that was set.</returns>
        /// <remarks>
        /// Setting the value should not be done in parallel with other calls to <see cref="Set(TValue)"/> or
        /// <see cref="Set(Exception)"/>, which includes a call to <see cref="GetSetAsync(Func{Task{TValue}})"/>. The
        /// value is set immediately (whether or not the value was previously set), and a call to
        /// <see cref="GetAsync()"/> may return either value (the old, or the new). In general,use only the calls
        /// <see cref="GetSetAsync(Func{Task{TValue}})"/>, or use multiple calls to <see cref="GetAsync()"/> with a
        /// single call to <see cref="Set(TValue)"/>.
        /// </remarks>
        public TValue Set(TValue value)
        {
            m_Value = value;

            int prevState = Interlocked.Exchange(ref m_Set, IsCompleted);
            if (prevState != IsCompleted) {
                if (m_Complete != null) m_Complete.Set();
            }
            return m_Value;
        }

        /// <summary>
        /// Sets an exception that occurred, so that retrieving the value results in the exception.
        /// </summary>
        /// <param name="exception">The exception which occurred.</param>
        /// <remarks>
        /// Setting the exception should not be done in parallel with other calls to <see cref="Set(TValue)"/> or
        /// <see cref="Set(Exception)"/>, which includes a call to <see cref="GetSetAsync(Func{Task{TValue}})"/>. The
        /// value is set immediately (whether or not the value was previously set), and a call to
        /// <see cref="GetAsync()"/> may return either value (the old, or the new). In general,use only the calls
        /// <see cref="GetSetAsync(Func{Task{TValue}})"/>, or use multiple calls to <see cref="GetAsync()"/> with a
        /// single call to <see cref="Set(Exception)"/>.
        /// <para>Setting an exception will cause calls to <see cref="GetAsync()"/> to raise this exception.</para>
        /// </remarks>
        public void Set(Exception exception)
        {
            m_Exception = exception;

            int prevState = Interlocked.Exchange(ref m_Set, IsCompleted);
            if (prevState != IsCompleted) {
                if (m_Complete != null) m_Complete.Set();
            }
        }
    }
}
