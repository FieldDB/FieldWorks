// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExcludeCheckingErrorsFilter.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region class ExcludeCheckingErrorsFilter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Filter for including only checking errors that are ignored with a comment
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExcludeCheckingErrorsFilter : IFilter
	{
		private FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ExcludeCheckingErrorsFilter"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public ExcludeCheckingErrorsFilter(FdoCache cache)
		{
			m_cache = cache;
		}

		#region IFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="M:SIL.FieldWorks.FDO.IFilter.MatchesCriteria(System.Int32)"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitCriteria()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns>
		/// 	<c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c>
		/// </returns>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			IScrScriptureNote note =
				m_cache.ServiceLocator.GetObject(hvoObj) as IScrScriptureNote;
			if (note == null)
				return false;
			if (note.AnnotationType != NoteType.CheckingError)
				return true; // Annotation is a Translator or Consultant note

			foreach (IStTxtPara para in note.ResolutionOA.ParagraphsOS)
			{
				if (para.Contents.Length > 0)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string FilterName
		{
			get { return string.Empty; }
		}
		#endregion
	}
	#endregion

	#region class NotesViewFilter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to allow encapsulation of a CmFilter in a ExcludeCheckingErrorsFilter while
	/// still allowing the filter to be cast as an CmFilter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class NotesViewFilter : AndIFilter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesViewFilter"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="userFilter">The user filter.</param>
		/// ------------------------------------------------------------------------------------
		internal NotesViewFilter(FdoCache cache, ICmFilter userFilter)
			: base(new ExcludeCheckingErrorsFilter(cache), userFilter)
		{
		}
	}
	#endregion
}
