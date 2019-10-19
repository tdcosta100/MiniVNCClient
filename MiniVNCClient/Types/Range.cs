using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniVNCClient.Types
{
	public class Range<TRange, TItem> where TRange: struct
	{
		#region Properties
		public TRange Minimum { get; set; }

		public TRange Maximum { get; set; }

		public TItem Item { get; set; }
		#endregion

		#region Constructors
		public Range()
		{

		}

		public Range(TRange minimum, TRange maximum, TItem item)
		{
			var orderedMinimumMaximum =
				new[] { minimum, maximum }
				.OrderBy(i => i)
				.ToArray();

			Minimum = orderedMinimumMaximum[0];
			Maximum = orderedMinimumMaximum[1];
			Item = item;
		}
		#endregion

		#region Public methods
		public override bool Equals(object obj)
		{
			var range = obj as Range<TRange, TItem>;

			if (range != null)
			{
				return
					range.Minimum.Equals(Minimum)
					&&
					range.Maximum.Equals(Maximum)
					&&
					range.Item.Equals(Item);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Minimum.GetHashCode() ^ Maximum.GetHashCode() ^ ((Item != null) ? Item.GetHashCode() : 0);
		}
		#endregion
	}
}
