using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	public class RangeCollection<TRange, TItem> : IList<Range<TRange, TItem>> where TRange : struct
	{
		#region Fields
		private List<Range<TRange, TItem>> _InternalList;
		private Comparer<TRange> _Comparer = Comparer<TRange>.Default;
		#endregion

		#region Properties
		public Range<TRange, TItem> this[int index] { get => _InternalList[index]; set => _InternalList[index] = value; }

		public int Count => _InternalList.Count;

		public bool IsReadOnly => false;
		#endregion

		#region Constructors
		public RangeCollection()
		{
			_InternalList = new List<Range<TRange, TItem>>();
		}

		public RangeCollection(IEnumerable<Range<TRange, TItem>> source)
		{
			_InternalList = source
				.Select(t => new { rangeValues = new[] { t.Minimum, t.Maximum }.OrderBy(r => r).ToArray(), item = t.Item })
				.Select(t => new Range<TRange, TItem>() { Minimum = t.rangeValues[0], Maximum = t.rangeValues[1], Item = t.item })
				.ToList();
		}
		#endregion

		#region Private methods
		#endregion

		#region Public methods
		public void Add(Range<TRange, TItem> item)
		{
			_InternalList.Add(item);
		}

		public void Clear()
		{
			_InternalList.Clear();
		}

		public bool Contains(Range<TRange, TItem> item)
		{
			return _InternalList.Contains(item);
		}

		public void CopyTo(Range<TRange, TItem>[] array, int arrayIndex)
		{
			_InternalList.CopyTo(array, arrayIndex);
		}

		public IEnumerator<Range<TRange, TItem>> GetEnumerator()
		{
			return _InternalList.GetEnumerator();
		}

		public int IndexOf(Range<TRange, TItem> item)
		{
			return _InternalList.IndexOf(item);
		}

		public void Insert(int index, Range<TRange, TItem> item)
		{
			_InternalList.Insert(index, item);
		}

		public bool Remove(Range<TRange, TItem> item)
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
