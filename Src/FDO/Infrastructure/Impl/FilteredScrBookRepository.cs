// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilteredScrBookRepository.cs
// Responsibility: steenwykt
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class FilteredScrBookRepository : IFilteredScrBookRepository
	{
		private readonly FdoCache m_cache;
		private readonly Dictionary<int, FilteredScrBooks> m_filteredBooks =
			new Dictionary<int, FilteredScrBooks>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredScrBookRepository"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		internal FilteredScrBookRepository(FdoCache cache)
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all of the book filters from this repository
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			m_filteredBooks.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets virtual property handler corresponding to filter instance.
		/// </summary>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks GetFilterInstance(int filterInstance)
		{
			FilteredScrBooks bookFilter;
			// combination of cache hash code and filter instance is used as key so that
			// different filter will be used if either is different.
			if (m_filteredBooks.TryGetValue(filterInstance, out bookFilter))
			{
				// This can no longer be done. If it turns out this is needed, we need
				// to figure out a better way of doing it (possibly with an internal
				// method that doesn't take any parameters).
				//bookFilter.CheckListForDeletedBooks(ref bookFilter.m_filteredBooks);
				return bookFilter;
			}

			bookFilter = new FilteredScrBooks(m_cache);
			m_filteredBooks[filterInstance] = bookFilter;

			return bookFilter;
		}
	}
}
