// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2005' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotesEditingHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;
using XCore;
using SIL.FieldWorks.FDO.Application;
using System.Diagnostics;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NotesEditingHelper has methods needed by various notes views.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NotesEditingHelper : FwEditingHelper
	{
		private readonly IScripture m_scr;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private int m_indexNewNote;
		private int m_bookNewNote;

		#region Constructor
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The DB connection</param>
		/// <param name="callbacks">implementation of <see cref="IEditingCallbacks"/></param>
		/// <param name="helpTopicProvider">The Help topic provider.</param>
		/// --------------------------------------------------------------------------------
		public NotesEditingHelper(FdoCache cache, IEditingCallbacks callbacks,
			IHelpTopicProvider helpTopicProvider) : base(cache, callbacks)
		{
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_helpTopicProvider = helpTopicProvider;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor for the currently edited rootbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal TeNotesVc CurrentNotesVc
		{
			get
			{
				if (Callbacks != null)
				{
					IVwViewConstructor vc;
					int hvo, frag;
					IVwStylesheet styleSheet;
					Callbacks.EditedRootBox.GetRootObject(out hvo, out vc, out frag, out styleSheet);
					return vc as TeNotesVc;
				}
				return null;
			}
		}
		#endregion

		#region Insert Note
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a note referencing the currently selected paragraph.
		/// </summary>
		/// <param name="noteType">Type of note</param>
		/// <param name="startRef">reference at beginning of selection</param>
		/// <param name="endRef">reference at end of selection</param>
		/// <param name="topObj">The object where quoted text begins.</param>
		/// <param name="bottomObj">The object where quoted text ends.</param>
		/// <param name="startOffset">The starting character offset.</param>
		/// <param name="endOffset">The ending character offset.</param>
		/// <param name="tssQuote">The text of the quote.</param>
		/// <returns>The inserted note</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrScriptureNote InsertNote(ICmAnnotationDefn noteType, BCVRef startRef,
			BCVRef endRef, ICmObject topObj, ICmObject bottomObj,
			int startOffset, int endOffset, ITsString tssQuote)
		{
			CheckDisposed();

			int iPos;

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[startRef.Book - 1];

			StTxtParaBldr quoteParaBldr = new StTxtParaBldr(m_cache);
			quoteParaBldr.ParaStyleName = ScrStyleNames.Remark;
			quoteParaBldr.StringBuilder.ReplaceTsString(0, 0, tssQuote);
			IScrScriptureNote annotation = annotations.InsertNote(startRef, endRef, topObj, bottomObj,
				noteType.Guid, startOffset, endOffset, quoteParaBldr, null, null, null,
				out iPos);

			RegisterScrollCallBack(startRef.Book, iPos);

			// REVIEW: Do we need to create a synch record?
			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Register a call back with the action handler so that the selection can be set in the
		/// new note when the UOW is completed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RegisterScrollCallBack(int bookNewNote, int indexNewNote)
		{
			m_bookNewNote = bookNewNote;
			m_indexNewNote = indexNewNote;
			IActionHandlerExtensions handler = Cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if (handler != null)
				handler.DoAtEndOfPropChanged(ScrollAfterPropChangedCompleted);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll new note into view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ScrollAfterPropChangedCompleted()
		{
			// We are no longer able to cancel the request for this notification if we get disposed in the meantime.
			// In test teardown, cache may already have been cleared
			if (IsDisposed || Cache == null)
				return;
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[m_bookNewNote - 1];
			IScrScriptureNote annotation = annotations.NotesOS[m_indexNewNote];
			TeNotesVc notesVc = CurrentNotesVc;

			if (notesVc != null)
			{
				// tell the VC that the newly inserted item should be expanded. That will cause
				// the view to be updated to show the new note.
				notesVc.ExpandItem(annotation.Hvo, EditedRootBox);
				notesVc.ExpandItem(annotation.DiscussionOA.Hvo, EditedRootBox);
			}

			if (Control != null)
				Control.Focus();

			// Make a selection in the discussion so the user can start to type
			int iPos = m_indexNewNote;
			if (notesVc != null)
			{
				// Get the corresponding index in the virtual property.
				iPos = ((FilteredDomainDataByFlidDecorator)EditedRootBox.DataAccess).
					GetFilteredIndexOfItem(annotations.Hvo, iPos);
				// New note doesn't match current filter - need to turn it off
				if (iPos < 0)
				{
					// Turn off the filter so the new note will show
					((NotesDataEntryView)Control).OnChangeFilter(null);
					iPos = ((FilteredDomainDataByFlidDecorator)EditedRootBox.DataAccess).
						GetFilteredIndexOfItem(annotations.Hvo, m_indexNewNote);
					Debug.Assert(iPos >= 0);
				}
			}

			IVwRootSite rootSite = Control as IVwRootSite;
			MakeSelectionInNote(notesVc, m_bookNewNote - 1, iPos, rootSite, true);

		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the specified annotation (without scrolling the annotation in
		/// the view).
		/// </summary>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="iResponse">Index of the response (0 if setting the selection in one of
		/// the StJournalText fields rather than in a response.</param>
		/// <param name="noteTag">The tag indicating the field of the annotation where the
		/// selection is to be made.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNote(int bookIndex, int iAnnotation, int iResponse,
			int noteTag)
		{
			MakeSelectionInNote(CurrentNotesVc, true, bookIndex, iAnnotation, iResponse,
				noteTag, Control as IVwRootSite, true);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the discussion of an annotation after first scrolling the
		/// annotation to near the top of the view.
		/// </summary>
		/// <param name="vc">The notes view constructor</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="fNoteIsExpanded">if <c>true</c> make a selection at the start and end so
		/// that the whole annotation can be scrolled into view. if set to <c>false</c> only
		/// make a selection at the start of the annotation.</param>
		/// --------------------------------------------------------------------------------
		internal void MakeSelectionInNote(TeNotesVc vc, int bookIndex, int iAnnotation,
			IVwRootSite rootSite, bool fNoteIsExpanded)
		{
			MakeSelectionInNote(vc, true, bookIndex, iAnnotation, 0,
				ScrScriptureNoteTags.kflidDiscussion, rootSite, fNoteIsExpanded);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a selection in the specified annotation (without scrolling the annotation in
		/// the view).
		/// </summary>
		/// <param name="vc">The notes view constructor</param>
		/// <param name="fScrollNearTop">if set to <c>true</c> scrolls the specified note to a
		/// position near the top of the view.</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="iResponse">Index of the response (0 if setting the selection in one of
		/// the StJournalText fields rather than in a response.</param>
		/// <param name="noteTag">The tag indicating the field of the annotation where the
		/// selection is to be made.</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="fNoteIsExpanded">if <c>true</c> make a selection at the start and end so
		/// that the whole annotation can be scrolled into view. if set to <c>false</c> only
		/// make a selection at the start of the annotation.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNote(TeNotesVc vc, bool fScrollNearTop, int bookIndex,
			int iAnnotation, int iResponse, int noteTag,
			IVwRootSite rootSite, bool fNoteIsExpanded)
		{
			if (vc == null)
				return;

			SelectionHelper selHelper;
			if (fScrollNearTop)
			{
				// Make an un-installed selection at the top of the annotation in order to scroll the
				// annotation to the top of the view.
				selHelper = new SelectionHelper();
				selHelper.NumberOfLevels = 2;
				selHelper.LevelInfo[0].cpropPrevious = 0;
				selHelper.LevelInfo[0].ich = -1;
				selHelper.LevelInfo[0].ihvo = iAnnotation;
				selHelper.LevelInfo[0].tag = ScrBookAnnotationsTags.kflidNotes;
				selHelper.LevelInfo[0].ws = 0;
				selHelper.LevelInfo[1].cpropPrevious = 0;
				selHelper.LevelInfo[1].ich = -1;
				selHelper.LevelInfo[1].ihvo = bookIndex;
				selHelper.LevelInfo[1].tag = ScriptureTags.kflidBookAnnotations;
				selHelper.LevelInfo[1].ws = 0;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, -2);
				selHelper.IchAnchor = 0;
				selHelper.AssocPrev = false;
				selHelper.NumberOfPreviousProps = 2;
				if (fNoteIsExpanded)
				{
					selHelper.SetSelection(rootSite, true, true, VwScrollSelOpts.kssoNearTop);
				}
				else
				{
					// Annotation is collapsed. Only attempt a selection at the start of it.
					selHelper.SetSelection(rootSite, true, true);
					return;
				}
			}
			else
				EnsureNoteIsVisible(vc, bookIndex, iAnnotation, rootSite);

			// Now make the real (installed) selection in the desired field of the annotation.
			bool fIsResponse = (noteTag == ScrScriptureNoteTags.kflidResponses);
			selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = (int)noteTag;
			selHelper.LevelInfo[1].ihvo = iResponse;
			selHelper.LevelInfo[1].cpropPrevious = (fIsResponse ? 0 : 1);
			selHelper.LevelInfo[2].tag = ScrBookAnnotationsTags.kflidNotes;
			selHelper.LevelInfo[2].ihvo = iAnnotation;
			selHelper.LevelInfo[3].tag = ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[3].ihvo = bookIndex;
			selHelper.IchAnchor = 0;
			selHelper.AssocPrev = false;
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			selHelper.SetSelection(rootSite, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the selection in in the Scripture reference of the specified annotation.
		/// </summary>
		/// <param name="vc">The vc.</param>
		/// <param name="bookIndex">Index of the book.</param>
		/// <param name="iAnnotation">Index of the annotation.</param>
		/// <param name="notesDataEntryView">The notes data entry view.</param>
		/// ------------------------------------------------------------------------------------
		internal void MakeSelectionInNoteRef(TeNotesVc vc, int bookIndex, int iAnnotation,
			NotesDataEntryView notesDataEntryView)
		{
			EnsureNoteIsVisible(vc, bookIndex, iAnnotation, notesDataEntryView);

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 2;
			selHelper.LevelInfo[0].cpropPrevious = 0;
			selHelper.LevelInfo[0].ich = -1;
			selHelper.LevelInfo[0].ihvo = iAnnotation;
			selHelper.LevelInfo[0].tag = ScrBookAnnotationsTags.kflidNotes;
			selHelper.LevelInfo[0].ws = 0;
			selHelper.LevelInfo[1].cpropPrevious = 0;
			selHelper.LevelInfo[1].ich = -1;
			selHelper.LevelInfo[1].ihvo = bookIndex;
			selHelper.LevelInfo[1].tag = ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[1].ws = 0;
			selHelper.IchAnchor = 0;
			selHelper.AssocPrev = false;
			selHelper.TextPropId = -2;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
				CmBaseAnnotationTags.kflidBeginRef);

			selHelper.SetSelection(notesDataEntryView, true, true, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the annotation is mostly visible by making an uninstalled selection
		/// toward the end of the modified date.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void EnsureNoteIsVisible(TeNotesVc vc, int bookIndex, int iAnnotation,
			IVwRootSite notesDataEntryView)
		{
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 2;
			selHelper.LevelInfo[0].cpropPrevious = 0;
			selHelper.LevelInfo[0].ich = -1;
			selHelper.LevelInfo[0].ihvo = iAnnotation;
			selHelper.LevelInfo[0].tag = ScrBookAnnotationsTags.kflidNotes;
			selHelper.LevelInfo[0].ws = 0;
			selHelper.LevelInfo[1].cpropPrevious = 0;
			selHelper.LevelInfo[1].ich = -1;
			selHelper.LevelInfo[1].ihvo = bookIndex;
			selHelper.LevelInfo[1].tag = ScriptureTags.kflidBookAnnotations;
			selHelper.LevelInfo[1].ws = 0;
			selHelper.AssocPrev = false;

			// Put the selection at the end of the shortest possible date value. It doesn't
			// have to be right at the end, but the closer it is, the more reliable it will
			// be that it is fully scrolled into view.
			selHelper.IchAnchor = 8;

			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
				CmAnnotationTags.kflidDateModified);

			selHelper.SetSelection(notesDataEntryView, false, true, VwScrollSelOpts.kssoDefault);
		}
		#endregion

		#region Overrides of EditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if pasting is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanPaste()
		{
			if (base.CanPaste())
			{
				// Currently, can not paste if the current selection covers more than one paragraph.
				// The deletion of the range will cause the a new selection to be requested at the
				// end of the UOW which will destroy the current selection. This could be solved by
				// splitting the UOW inot 2 parts, but it was decided not to do this right now.
				SelLevInfo anchorInfo;
				SelLevInfo endInfo;
				if (CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs,
					SelectionHelper.SelLimitType.Anchor, out anchorInfo) &&
					CurrentSelection.GetLevelInfoForTag(StTextTags.kflidParagraphs,
					SelectionHelper.SelLimitType.End, out endInfo))
				{
					return anchorInfo.hvo == endInfo.hvo;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the base method to disable the creation of pictures and footnotes in an
		/// annotation.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sTextRep"></param>
		/// <param name="selDst"></param>
		/// <param name="kodt"></param>
		/// ------------------------------------------------------------------------------------
		public override Guid MakeObjFromText(FdoCache cache, string sTextRep, IVwSelection selDst, out int kodt)
		{
			kodt = 0;
			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a value determining if the new writing systems should be created as a side-effect
		/// of a paste operation.
		/// </summary>
		/// <param name="wsf">writing system factory containing the new writing systems</param>
		/// <param name="destWs">The destination writing system (writing system used at the
		/// selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			// Determine writing system at selection (destination for paste).
			destWs = 0;
			if (CurrentSelection != null)
				destWs = CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
			if (destWs <= 0)
				destWs = Cache.DefaultAnalWs; // set to default analysis, if 0.

			int cws = wsf.NumberOfWs;

			using (ArrayPtr ptr = MarshalEx.ArrayToNative<int>(cws))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = MarshalEx.NativeToArray<int>(ptr, cws);

				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] != 0 && wsf.get_EngineOrNull(vws[iws]) == null)
					{
						// found corrupt writing system--don't want to use any ws in this pasted string
						return PasteStatus.UseDestWs;
					}
				}
			}

			return PasteStatus.PreserveWs;
		}
		#endregion
	}
}
