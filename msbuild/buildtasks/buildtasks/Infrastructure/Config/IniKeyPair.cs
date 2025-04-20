namespace RJCP.MSBuildTasks.Infrastructure.Config
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A base abstract class for handling key pairs where the key is case insensitive.
    /// </summary>
    /// <typeparam name="T">The object to store as the value.</typeparam>
    internal abstract class IniKeyPair<T> : IDictionary<string, T> where T : class
    {
        private readonly Dictionary<string, T> m_Database = new Dictionary<string, T>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Gets or sets the item with the specified key.
        /// </summary>
        /// <param name="key">The case insensitive key.</param>
        /// <returns>The object stored in the key/value dictionary.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> and <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public T this[string key]
        {
            get
            {
                if (key is null) throw new ArgumentNullException(nameof(key));
                return m_Database[key];
            }
            set
            {
                if (key is null) throw new ArgumentNullException(nameof(key));
                if (value is null) throw new ArgumentNullException(nameof(value));
                m_Database[key] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="IniKeyPair{T}" />.
        /// </summary>
        /// <value>The number of elements in the key/value pair dictionary.</value>
        public int Count
        {
            get
            {
                return m_Database.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IniKeyPair{T}" /> is read-only.
        /// </summary>
        /// <value><see langword="true"/> if this instance is read only; otherwise, <see langword="false"/>.</value>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{String}" /> containing the keys of the <see cref="IniKeyPair{T}" />.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<string> Keys
        {
            get
            {
                return m_Database.Keys;
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}" /> containing the values in the <see cref="IniKeyPair{T}" />.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<T> Values
        {
            get
            {
                return m_Database.Values;
            }
        }

        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item)
        {
            if (item.Key is null) throw new ArgumentException("Key is null", nameof(item));
            if (item.Value is null) throw new ArgumentException("Value is null", nameof(item));
            m_Database.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="IniKeyPair{T}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> are <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">An item with the same <paramref name="key"/> has already been added.</exception>
        public void Add(string key, T value)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (value is null) throw new ArgumentNullException(nameof(value));
            m_Database.Add(key, value);
        }

        /// <summary>
        /// Removes all items from the <see cref="IniKeyPair{T}" />.
        /// </summary>
        public void Clear()
        {
            m_Database.Clear();
        }

        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item)
        {
            return ((IDictionary<string, T>)m_Database).Contains(new KeyValuePair<string, T>(item.Key, item.Value));
        }

        /// <summary>
        /// Determines whether the <see cref="IniKeyPair{T}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="IniKeyPair{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IniKeyPair{T}"/> contains an element with the key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsKey(string key)
        {
            return m_Database.ContainsKey(key);
        }

        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            ((IDictionary<string, T>)m_Database).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}" /> that can be used to iterate through the collection.</returns>
        public Dictionary<string, T>.Enumerator GetEnumerator()
        {
            return m_Database.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            return ((IDictionary<string, T>)m_Database).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<string, T>)m_Database).GetEnumerator();
        }

        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item)
        {
            return m_Database.Remove(item.Key);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="IniKeyPair{T}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>. This
        /// method also returns <see langword="false"/> if <paramref name="key"/> was not found in the original <see cref="IniKeyPair{T}"/>.
        /// </returns>
        public bool Remove(string key)
        {
            return m_Database.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found; otherwise, the
        /// default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the object that implements <see cref="IniKeyPair{T}"/> contains an element with the
        /// specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(string key, out T value)
        {
            return m_Database.TryGetValue(key, out value);
        }
    }
}
