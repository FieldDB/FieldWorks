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
// File: FieldWorksTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FieldWorksTests : BaseTest
	{
		#region GetProjectMatchStatus tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks with a matching project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectMatchStatus_Match()
		{
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fSingleProcessMode", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fWaitingForUserOrOtherFw", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_projectId",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null));

			Assert.AreEqual(ProjectMatch.ItsMyProject, ReflectionHelper.GetResult(
				typeof(FieldWorks), "GetProjectMatchStatus",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks with a different project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectMatchStatus_NotMatch()
		{
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fSingleProcessMode", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fWaitingForUserOrOtherFw", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_projectId",
				new ProjectId(FDOBackendProviderType.kXML, "primate", null));

			Assert.AreEqual(ProjectMatch.ItsNotMyProject, ReflectionHelper.GetResult(
				typeof(FieldWorks), "GetProjectMatchStatus",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when the project has yet to
		/// be determined
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectMatchStatus_DontKnow()
		{
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fSingleProcessMode", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fWaitingForUserOrOtherFw", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_projectId", null);

			Assert.AreEqual(ProjectMatch.DontKnowYet, ReflectionHelper.GetResult(
				typeof(FieldWorks), "GetProjectMatchStatus",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when waiting on another
		/// FieldWorks process
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectMatchStatus_WaitingForFw()
		{
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fSingleProcessMode", false);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fWaitingForUserOrOtherFw", true);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_projectId",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null));

			Assert.AreEqual(ProjectMatch.WaitingForUserOrOtherFw, ReflectionHelper.GetResult(
				typeof(FieldWorks), "GetProjectMatchStatus",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when in "single process mode"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectMatchStatus_SingleProcessMode()
		{
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fSingleProcessMode", true);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_fWaitingForUserOrOtherFw", true);
			ReflectionHelper.SetField(typeof(FieldWorks), "s_projectId",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null));

			Assert.AreEqual(ProjectMatch.SingleProcessMode, ReflectionHelper.GetResult(
				typeof(FieldWorks), "GetProjectMatchStatus",
				new ProjectId(FDOBackendProviderType.kXML, "monkey", null)));
		}

		#endregion

	}
}
