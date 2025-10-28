namespace MiniVNCClient.Util
{
    /// <summary>
    /// Represents a range collection that returns the same value for a range of keys
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RangeCollection{TItem}"/> class that contains elements copied from the specified <see cref="IEnumerable{T}"/>.
    /// </remarks>
    /// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="RangeCollection{TItem}"/>.</param>
    public class RangeCollection<TItem>(IEnumerable<KeyValuePair<Range, TItem>> collection)
    {
        #region Fields
        private readonly Dictionary<Range, TItem> _InternalDictionary = collection.ToDictionary();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the value associated with range of the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a
        /// <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <c>key</c> does not exist in the collection.</exception>
        public TItem this[int key]
        {
            get
            {
                var result = _InternalDictionary.Keys.Where(k => k.Start.Value <= key && key <= k.End.Value);

                if (result.Any())
                {
                    return _InternalDictionary[result.First()];
                }

                throw new KeyNotFoundException();
            }
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Determines whether the <see cref="RangeCollection{TItem}"/> contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="RangeCollection{TItem}"/>.</param>
        /// <returns></returns>
        public bool ContainsKey(int key)
        {
            return _InternalDictionary.Keys.Any(k => k.Start.Value <= key && key <= k.End.Value);
        }
        #endregion
    }
}
