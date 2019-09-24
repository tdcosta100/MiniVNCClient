using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	public class RangeCollection<TRange, TItem> : IList<(TRange Minimum, TRange Maximum, TItem Item)> where TRange : struct
	{
		#region Fields
		private List<(TRange Minimum, TRange Maximum, TItem Item)> _InternalList;
		private Comparer<TRange> _Comparer = Comparer<TRange>.Default;
		#endregion

		#region Properties
		public (TRange Minimum, TRange Maximum, TItem Item) this[int index] { get => _InternalList[index]; set => _InternalList[index] = value; }

		public int Count => _InternalList.Count;

		public bool IsReadOnly => false;
		#endregion

		#region Constructors
		public RangeCollection()
		{
			_InternalList = new List<(TRange Minimum, TRange Maximum, TItem Item)>();
		}

		public RangeCollection(IEnumerable<(TRange Minimum, TRange Maximum, TItem Item)> source)
		{
			_InternalList = source
				.Select(t => (rangeValues: new[] { t.Minimum, t.Maximum }.OrderBy(r => r).ToArray(), item: t.Item))
				.Select(t => (Minimum: t.rangeValues[0], Maximum: t.rangeValues[1], Item: t.item))
				.ToList();
		}
		#endregion

		#region Private methods
		#endregion

		#region Public methods
		public void Add((TRange Minimum, TRange Maximum, TItem Item) item)
		{
			_InternalList.Add(item);
		}

		public void Clear()
		{
			_InternalList.Clear();
		}

		public bool Contains((TRange Minimum, TRange Maximum, TItem Item) item)
		{
			return _InternalList.Contains(item);
		}

		public void CopyTo((TRange Minimum, TRange Maximum, TItem Item)[] array, int arrayIndex)
		{
			_InternalList.CopyTo(array, arrayIndex);
		}

		public IEnumerator<(TRange Minimum, TRange Maximum, TItem Item)> GetEnumerator()
		{
			return _InternalList.GetEnumerator();
		}

		public int IndexOf((TRange Minimum, TRange Maximum, TItem Item) item)
		{
			return _InternalList.IndexOf(item);
		}

		public void Insert(int index, (TRange Minimum, TRange Maximum, TItem Item) item)
		{
			_InternalList.Insert(index, item);
		}

		public bool Remove((TRange Minimum, TRange Maximum, TItem Item) item)
		{
			return _InternalList.Remove(item);
		}

		public void RemoveAt(int index)
		{
			_InternalList.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _InternalList.GetEnumerator();
		}

		public TItem GetItemInRange(TRange value)
		{
			return _InternalList
				.Where(
					t =>
						_Comparer.Compare(value, t.Minimum) >= 0
						&&
						_Comparer.Compare(value, t.Maximum) <= 0
				)
				.Select(t => t.Item)
				.FirstOrDefault();
		}
		#endregion
	}
}
