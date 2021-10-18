using System.Collections;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity
{
    public class ApiEntityCollection<T> : ApiEntity, IApiEntityCollection, ICollection<T> where T : ApiEntity
    {
        #region private fields

        private readonly List<T> _innerList = new List<T>();

        #endregion private fields

        public void Add(ApiEntity resource)
        {
            _innerList.Add((T)resource);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public void Add(T item)
        {
            _innerList.Add(item);
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(T item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _innerList.Remove(item);
        }

        public int Count => _innerList.Count;

        public bool IsReadOnly => false;
    }
}