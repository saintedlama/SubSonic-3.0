using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubSonic.Schema;
using SubSonic.Query;
using SubSonic.DataProviders;

namespace SubSonic.Repository
{
    public class AutoloadedList<T> : IList<T> where T : class, new()
    {
        private SimpleRepository _repo;
        private List<T> _delegatee;
        private IColumn _foreignKey;
        private IDataProvider _provider;
        private object _key;

        public AutoloadedList(SimpleRepository repo, IDataProvider provider, IColumn foreignKey, object key)
        {
            _repo = repo;
            _foreignKey = foreignKey;
            _provider = provider;
            _key = key;
        }

        public int IndexOf(T item)
        {
            return Delegatee.IndexOf(item);
        }

        private IList<T> Delegatee
        {
            get
            {
                if (_delegatee == null)
                {
                    _delegatee = new Select(_provider).From(_foreignKey.Table).Where(_foreignKey).IsEqualTo(_key).ExecuteTypedList<T>();
                }

                return _delegatee;
            }
        }

        public void Insert(int index, T item)
        {
            Delegatee.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Delegatee.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return Delegatee[index];
            }
            set
            {
                Delegatee[index] = value;
            }
        }

        public void Add(T item)
        {
            Delegatee.Add(item);
        }

        public void Clear()
        {
            Delegatee.Clear();
        }

        public bool Contains(T item)
        {
            return Delegatee.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Delegatee.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Delegatee.Count; }
        }

        public bool IsReadOnly
        {
            // TODO: Check back!
            get { return false; }
        }

        public bool Remove(T item)
        {
            return Delegatee.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Delegatee.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Delegatee.GetEnumerator();
        }
    }
}
