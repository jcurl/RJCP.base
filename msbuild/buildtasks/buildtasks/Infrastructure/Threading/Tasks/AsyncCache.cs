namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements a dictionary which can be used to manage key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of the Key.</typeparam>
    /// <typeparam name="TValue">The type of the Value.</typeparam>
    public class AsyncCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, AsyncValue<TValue>> m_Dict = new Dictionary<TKey, AsyncValue<TValue>>();

        /// <summary>
        /// sets a value in the dictionary as an asynchronous operation.
        /// </summary>
        /// <param name="key">
        /// The key to get the value for, if it is already set, or to execute the function if not set.
        /// </param>
        /// <param name="func">
        /// The function that if the key is not set, which should be used to get the result to set.
        /// </param>
        /// <returns>The value that is set, either by entry, or after the function is executed</returns>
        /// <remarks>
        /// Use this method for getting data that is cached in a dictionary, sorted by key. If the value isn't in the
        /// dictionary, the function <paramref name="func"/> is called, which can take some time. If in parallel, a
        /// second call to this method is made while <paramref name="func"/> is processing, it will also await for the
        /// result, but the <paramref name="func"/> will not be called twice.
        /// </remarks>
        public async Task<TValue> GetSetAsync(TKey key, Func<Task<TValue>> func)
        {
            if (TryGetValue(key, out AsyncValue<TValue> value)) {
                return await value.GetAsync();
            }

            try {
                TValue result = await func();
                return value.Set(result);
            } catch (Exception ex) {
                value.Set(ex);
                throw;
            }
        }

        /// <summary>
        /// sets a value in the dictionary as an asynchronous operation.
        /// </summary>
        /// <param name="key">
        /// The key to get the value for, if it is already set, or to execute the function if not set.
        /// </param>
        /// <param name="func">
        /// The function that if the key is not set, which should be used to get the result to set.
        /// </param>
        /// <returns>The value that is set, either by entry, or after the function is executed</returns>
        /// <remarks>
        /// Use this method for getting data that is cached in a dictionary, sorted by key. If the value isn't in the
        /// dictionary, the function <paramref name="func"/> is called, which can take some time. If in parallel, a
        /// second call to this method is made while <paramref name="func"/> is processing, it will also await for the
        /// result, but the <paramref name="func"/> will not be called twice.
        /// </remarks>
        public async Task<TValue> GetSetAsync(TKey key, Func<TValue> func)
        {
            if (TryGetValue(key, out AsyncValue<TValue> value)) {
                return await value.GetAsync();
            }

            try {
                TValue result = func();
                return value.Set(result);
            } catch (Exception ex) {
                value.Set(ex);
                throw;
            }
        }

        private bool TryGetValue(TKey key, out AsyncValue<TValue> value)
        {
            bool found;
            lock (m_Dict) {
                found = m_Dict.TryGetValue(key, out value);
                if (!found) {
                    value = new AsyncValue<TValue>();
                    m_Dict.Add(key, value);
                }
            }
            return found;
        }

        /// <summary>
        /// Removes the specified key from the cached values.
        /// </summary>
        /// <param name="key">The key that should be removed.</param>
        /// <returns>
        /// <see langword="true"/> if the entry was cached and is now removed, <see langword="false"/> otherwise.
        /// </returns>
        public bool Remove(TKey key)
        {
            lock (m_Dict) {
                return m_Dict.Remove(key);
            }
        }

        /// <summary>
        /// Enumerates the collection of values, calling an enumeration function on each
        /// </summary>
        /// <param name="checkFunction">The check function.</param>
        /// <returns><see langword="true"/> if any entries were processed, <see langword="false"/> otherwise.</returns>
        public bool Enumerate(Func<TKey, bool> checkFunction)
        {
            bool found = false;
            lock (m_Dict) {
                foreach (var entry in m_Dict) {
                    if (checkFunction(entry.Key)) {
                        found = true;
                    }
                }
            }
            return found;
        }
    }
}
