// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SyncedDraftViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for synchronized DraftViews. These tests use mock objects and
	/// so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SyncedDraftViewTests : DraftViewTestBase
	{
		private RootSiteGroup m_group;

		#region Setup and Teardown

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_group = m_draftForm.CreateSyncDraftView(Cache);
		}

		/// <summary/>
		public override void TestTearDown()
		{
			m_group.Dispose();
			m_group = null;
			base.TestTearDown();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests laying out a view when there is a border on the bottom of titles. (TE-4141)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This is a test for TE-4141, which we hope JohnT can help us fix.")]
		public void WithBorderOnBottomOfTitle()
		{
			IStStyle titleMain = m_scr.FindStyle(ScrStyleNames.MainBookTitle);
			Assert.IsNotNull(titleMain);
			ITsPropsBldr propBldr = titleMain.Rules.GetBldr();
			propBldr.SetIntPropValues((int)FwTextPropType.ktptBorderBottom,
				(int)FwTextPropVar.ktpvMilliPoint, 20);
			titleMain.Rules = propBldr.GetTextProps();
			m_draftForm.StyleSheet.Init(Cache, m_scr.Hvo,
				ScriptureTags.kflidStyles);

			m_group.RefreshDisplay();
			m_draftView.PerformLayout();
		}
	}
}
