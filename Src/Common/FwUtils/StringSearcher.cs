﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Palaso.WritingSystems.Collation;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Type of string searching.
	/// </summary>
	public enum SearchType
	{
		/// <summary>
		/// Matches the entire string
		/// </summary>
		Exact,
		/// <summary>
		/// Matches at the beginning of a string
		/// </summary>
		Prefix,
		/// <summary>
		/// Matches any words in a string.
		/// </summary>
		FullText
	}

	/// <summary>
	/// This class is used to do fast searching of strings. Searching is case-insensitive.
	/// </summary>
	public class StringSearcher<T>
	{
		private const int SortKeyFactor = 5;

		#region SortKeyComparer class

		private class SortKeyComparer : IComparer<byte[]>
		{
			public int Compare(byte[] x, byte[] y)
			{
				// this code mimics the strcmp function in C
				if (x.Length == 0)
					return -y.Length; // zero if equal, neg if b is longer (considered larger)

				if (y.Length == 0)
					return 1; // ka is longer and considered larger.

				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; x[ib] == y[ib] && x[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				return x[ib] - y[ib];
			}
		}

		#endregion SortKeyComparer class

		#region SortKeyIndex class

		/// <summary>
		/// SortKeyIndex associates one or more items (of class T) with a key.
		/// It is optimized for the common case of only ONE item per key.
		/// </summary>
		private class SortKeyIndex
		{
			// The value may be either a single T, if only one is associated with the key, or a HashSet<T> if more than
			// one Add call has been received for the specified key.
			private readonly TreeDictionary<byte[], object > m_index = new TreeDictionary<byte[], object>(new SortKeyComparer());

			public void Add(byte[] sortKey, T item)
			{
				object oldVal;
				if (m_index.TryGetValue(sortKey, out oldVal))
				{
					// Seen this item before. Have we already changed to storing a set?
					var items = oldVal as HashSet<T>;
					if (items != null)
						items.Add(item); // already called twice or more with this key; just add to set.
					else
					{
						// second call with this key: make a set and store in the dictionary.
						items = new HashSet<T>();
						m_index[sortKey] = items;
						items.Add((T)oldVal);
						items.Add(item);
					}
				}
				else // first item for this key, store the item itself as a singleton.
					m_index.Add(sortKey, item);
			}

			public IEnumerable<T> GetItems(byte[] lower, byte[] upper)
			{
				foreach (var pair in m_index.GetRange(lower, upper))
				{
					var items = pair.Value as HashSet<T>;
					if (items != null)
					{
						foreach (T item in items)
							yield return item;
					}
					else
					{
						yield return (T) pair.Value;
					}
				}
			}
		}

		#endregion SortKeyIndex class

		private readonly Dictionary<Tuple<int, int>, SortKeyIndex> m_indices = new Dictionary<Tuple<int, int>, SortKeyIndex>();
		private readonly SearchType m_type;
		private readonly IWritingSystemManager m_wsManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="StringSearcher&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="wsManager">The writing system store.</param>
		public StringSearcher(SearchType type, IWritingSystemManager wsManager)
		{
			m_type = type;
			m_wsManager = wsManager;
		}

		/// <summary>
		/// Adds the specified item to an index using the specified string.
		/// </summary>
		public void Add(T item, int indexId, ITsString tss)
		{
			if (tss.RunCount == 1) // VERY common special case
			{
				Add(indexId, tss.get_WritingSystemAt(0), tss.Text, item);
				return;
			}

			foreach (Tuple<int, string> wsStr in GetWsStrings(tss))
			{
				var wsId = wsStr.Item1;
				var text = wsStr.Item2;
				Add(indexId, wsId, text, item);
			}
		}

		private void Add(int indexId, int wsId, string text, T item)
		{
			SortKeyIndex index = GetIndex(indexId, wsId);
			IWritingSystem ws = m_wsManager.Get(wsId);
			ICollator collator = ws.Collator;
			switch (m_type)
			{
				case SearchType.Exact:
				case SearchType.Prefix:
					index.Add(collator.GetSortKey(text).KeyData, item);
					break;

				case SearchType.FullText:
					foreach (string token in Icu.Split(Icu.UBreakIteratorType.UBRK_WORD, ws.IcuLocale, text))
						index.Add(collator.GetSortKey(token).KeyData, item);
					break;
			}
		}

		/// <summary>
		/// Searches an index for the specified string.
		/// </summary>
		/// <param name="indexId">The index ID.</param>
		/// <param name="tss">The string.</param>
		/// <returns>The search results.</returns>
		public IEnumerable<T> Search(int indexId, ITsString tss)
		{
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return Enumerable.Empty<T>();

			HashSet<T> results = null;
			foreach (Tuple<int, string> wsStr in GetWsStrings(tss))
			{
				SortKeyIndex index = GetIndex(indexId, wsStr.Item1);
				ICollator collator = m_wsManager.Get(wsStr.Item1).Collator;
				switch (m_type)
				{
					case SearchType.Exact:
					case SearchType.Prefix:
						{
							byte[] sortKey = collator.GetSortKey(wsStr.Item2).KeyData;
							var lower = new byte[wsStr.Item2.Length * SortKeyFactor];
							Icu.GetSortKeyBound(sortKey, Icu.UColBoundMode.UCOL_BOUND_LOWER, ref lower);
							var upper = new byte[wsStr.Item2.Length * SortKeyFactor];
							Icu.GetSortKeyBound(sortKey,
												m_type == SearchType.Exact
													? Icu.UColBoundMode.UCOL_BOUND_UPPER
													: Icu.UColBoundMode.UCOL_BOUND_UPPER_LONG, ref upper);
							IEnumerable<T> items = index.GetItems(lower, upper);
							if (results == null)
								results = new HashSet<T>(items);
							else
								results.IntersectWith(items);
							break;
						}

					case SearchType.FullText:
						string locale = m_wsManager.GetStrFromWs(wsStr.Item1);
						string[] tokens = Icu.Split(Icu.UBreakIteratorType.UBRK_WORD, locale, wsStr.Item2).ToArray();
						for (int i = 0; i < tokens.Length; i++)
						{
							byte[] sortKey = collator.GetSortKey(tokens[i]).KeyData;
							var lower = new byte[tokens[i].Length*SortKeyFactor];
							Icu.GetSortKeyBound(sortKey, Icu.UColBoundMode.UCOL_BOUND_LOWER, ref lower);
							var upper = new byte[tokens[i].Length*SortKeyFactor];
							Icu.GetSortKeyBound(sortKey,
												i < tokens.Length - 1
													? Icu.UColBoundMode.UCOL_BOUND_UPPER
													: Icu.UColBoundMode.UCOL_BOUND_UPPER_LONG, ref upper);
							IEnumerable<T> items = index.GetItems(lower, upper);
							if (results == null)
								results = new HashSet<T>(items);
							else
								results.IntersectWith(items);
						}
						break;
				}
			}
			return results ?? Enumerable.Empty<T>();
		}

		/// <summary>
		/// Clears all of the indices.
		/// </summary>
		public void Clear()
		{
			m_indices.Clear();
		}

		private SortKeyIndex GetIndex(int indexId, int ws)
		{
			var key = Tuple.Create(indexId, ws);
			SortKeyIndex index;
			if (!m_indices.TryGetValue(key, out index))
			{
				index = new SortKeyIndex();
				m_indices[key] = index;
				return index;
			}
			return index;
		}

		private static IEnumerable<Tuple<int, string>> GetWsStrings(ITsString tss)
		{
			var sb = new StringBuilder();
			int curWs = -1;
			for (int i = 0; i < tss.RunCount; i++)
			{
				int var;
				int ws = tss.get_Properties(i).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (curWs == -1)
				{
					curWs = ws;
				}
				else if (ws != curWs)
				{
					yield return Tuple.Create(curWs, sb.ToString());
					sb = new StringBuilder();
					curWs = ws;
				}
				sb.Append(tss.get_RunText(i));
			}
			yield return Tuple.Create(curWs, sb.ToString());
		}
	}
}
