using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for AllReversalEntriesRecordList.
	/// </summary>
	public class AllReversalEntriesRecordList : RecordList
	{
		public AllReversalEntriesRecordList()
		{
		}

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			// <recordList owner="IReversalIndex" property="AllEntries" assemblyPath="RBRExtensions.dll" class="RBRExtensions.AllReversalEntriesRecordList"/>
			BaseInit(cache, mediator, recordListNode);
			//string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			m_flid = ReversalIndexTags.kflidEntries; //LT-12577 a record list needs a real flid.
			//LT-14722 Crash when clicking Reversal Indexes
			//Clerk is null when going to Reversal Indexes for the first time.
			if (Clerk != null)
			{
				Guid riGuid = GetReversalIndexGuid(mediator);
				if (riGuid != Guid.Empty)
				{
					IReversalIndex ri = cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
					m_owningObject = ri;
					m_fontName = cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem).DefaultFontName;
				}
			}
			m_oldLength = 0;
		}

		public override bool CanInsertClass(string className)
		{
			if (base.CanInsertClass(className))
				return true;
			return className == "ReversalIndexEntry";
		}

		public override bool CreateAndInsert(string className)
		{
			if (className != "ReversalIndexEntry")
				return base.CreateAndInsert(className);
			m_newItem = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
			var ri = (IReversalIndex)m_owningObject;
			ri.EntriesOC.Add(m_newItem);
			var extensions = m_cache.ActionHandlerAccessor as IActionHandlerExtensions;
			if (extensions != null)
				extensions.DoAtEndOfPropChanged(SelectNewItem);
			return true;
		}

		private IReversalIndexEntry m_newItem;

		void SelectNewItem()
		{
			Clerk.OnJumpToRecord(m_newItem.Hvo);
		}

		/// <summary>
		/// Get the current reversal index guid.  If there is none, create a new reversal index
		/// since there must not be any.  This fixes LT-6653.
		/// </summary>
		/// <param name="mediator"></param>
		/// <returns></returns>
		internal static Guid GetReversalIndexGuid(Mediator mediator)
		{
			var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(mediator, "ReversalIndexGuid");

			if (riGuid.Equals(Guid.Empty))
			{
				try
				{
					mediator.SendMessage("InsertReversalIndex_FORCE", null);
					riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(mediator, "ReversalIndexGuid");
				}
				catch
				{
					return Guid.Empty;
				}
			}
			return riGuid;
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			IReversalIndex ri = m_owningObject as IReversalIndex;
			Debug.Assert(ri != null && ri.IsValidObject, "The owning IReversalIndex object is invalid!?");
			// Review: is there a better to to convert from List<Subclass> to List<Class>???
			List<IReversalIndexEntry> rgrie = ri.AllEntries;
			var rgcmo = new List<int>(rgrie.Count);
			foreach (IReversalIndexEntry rie in rgrie)
				rgcmo.Add(rie.Hvo);
			return rgcmo;
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		/// <param name="state"></param>
		public override void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete)
		{
			CheckDisposed();

			base.DeleteCurrentObject(state, thingToDelete);

			ReloadList();
		}
	}

	public class BulkReversalEntryPosEditor : BulkPosEditorBase
	{
		public BulkReversalEntryPosEditor()
		{
		}

		protected override ICmPossibilityList List
		{
			get
			{
				ICmPossibilityList list = null;
				var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_mediator, "ReversalIndexGuid");
				if (!riGuid.Equals(Guid.Empty))
				{
					try
					{
						IReversalIndex ri = m_cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
						list = ri.PartsOfSpeechOA;
					}
					catch
					{
						// Can't get an index if we have a bad guid.
					}
				}
				return list;
			}
		}

		public override List<int> FieldPath
		{
			get
			{
				return new List<int>(new int[] { ReversalIndexEntryTags.kflidPartOfSpeech,
					CmPossibilityTags.kflidName});
			}
		}

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want the PartOfSpeech indicated by m_selectedHvo, even if 0,
		/// to become the POS of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an IMoStemMsa.
		/// (a) If the owning entry has an IMoStemMsa with the
		/// right POS, set the sense to use it.
		/// (b) If the sense already refers to an IMoStemMsa, and any other senses
		/// of that entry which point at it are also to be changed, change the POS
		/// of the MSA.
		/// (c) If the entry has an IMoStemMsa which is not used at all, change it to the
		/// required POS and use it.
		/// (d) Make a new IMoStemMsa in the ILexEntry with the required POS and point the sense at it.
		/// </summary>
		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			CheckDisposed();

			m_cache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksUndoBulkEditRevPOS,
				LexEdStrings.ksRedoBulkEditRevPOS);
			int i = 0;
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (int entryId in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / itemsToChange.Count() + 20;
					state.Breath();
				}
				IReversalIndexEntry entry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(entryId);
				if (m_selectedHvo == 0)
					entry.PartOfSpeechRA = null;
				else
					entry.PartOfSpeechRA = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(m_selectedHvo);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		protected override bool CanFakeIt(int hvo)
		{
			return true;
		}
	}
}
