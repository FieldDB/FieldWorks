// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XWindowTests.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------


using System;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

using NUnit.Framework;

namespace XCore
{
	public abstract class XWindowTestsBase : BaseTest
	{
		//protected XCoreApp m_testXCoreApp;
		protected XWindow m_window;
		abstract protected string TestFile{	get;}
		protected string m_settingsPath;

		//--------------------------------------------------------------------------------------
		/// <summary>
		/// Get a temporary path. We add the username for machines where multiple users run
		/// tests (e.g. build machine).
		/// </summary>
		//--------------------------------------------------------------------------------------
		private static string TempPath
		{
			get { return Path.Combine(Path.GetTempPath(), Environment.UserName); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TestXCoreApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// load a persisted version of the property table.
			m_settingsPath = Path.Combine(TempPath, "settingsBackup");
			if (!Directory.Exists(m_settingsPath))
				Directory.CreateDirectory(m_settingsPath);

			m_window = new XWindow();
			m_window.PropertyTable.UserSettingDirectory = m_settingsPath;
			// delete any existing property table settings.
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();
			//m_window.PropertyTable.SetProperty("PreferredUILibrary", "FlexUIAdapter.dll");
			m_window.LoadUI(ConfigurationFilePath);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the TestXCoreApp object is destroyed.
		/// Especially since the splash screen it puts up needs to be closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			//m_window.Close();
			m_window.PropertyTable.RemoveLocalAndGlobalSettings();
			m_window.Dispose();

			try
			{
				Directory.Delete(m_settingsPath);
			}
			catch { }

			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			Application.DoEvents();
		}

		protected string ConfigurationFilePath
		{
			get
			{
				//NB: the NANT file must copy the configuration file into the assembly's directory.
				//e.g. 	<includes name="${fwroot}\Src\XCore\xCoreTests\basicTest.xml"/>

				string asmPathname = Assembly.GetExecutingAssembly().CodeBase;
				asmPathname = FileUtils.StripFilePrefix(asmPathname);
				string asmPath = asmPathname.Substring(0, asmPathname.LastIndexOf("/"));
				return Path.Combine(asmPath, TestFile);
			}
		}

		protected void ReopenWindow()
		{
			m_window.Close();
			m_window.Dispose();
			m_window = new XWindow();
			m_window.PropertyTable.UserSettingDirectory = m_settingsPath;
			//m_window.PropertyTable.SetProperty("PreferredUILibrary", "FlexUIAdapter.dll");
			m_window.LoadUI(ConfigurationFilePath);
			m_window.SuspendWindowSizePersistence();
			m_window.Show();
			m_window.ResumeWindowSizePersistence();
		}

		protected ChoiceGroup FindControlGroupById(string type,string id)
		{
			Assert.AreEqual("menu", type, "only looking up menus has been implemented");
			ChoiceGroupCollection groupCollection = m_window.MenusChoiceGroupCollection;
			Assert.IsNotNull(groupCollection, "could not get the ChoiceGroupCollection for '"+type+"'.");
			//will throw an exception if it is not found.
			return groupCollection.FindById(id);
		}

		protected ChoiceBase FindControlById(string type, string id)
		{
			ChoiceGroup group = FindControlGroupById(type,id);
			Assert.IsNotNull(group, "could not get the ControlGroup");
			// note that this could either be what we think of as a menu, or as an item.  They have the same class in windows.forms
			return (ChoiceBase)group.FindById(id);
		}

		protected object FindWidgetForItem(string id)
		{
			foreach(ChoiceGroup group in m_window.MenusChoiceGroupCollection)
			{
				ChoiceBase info =(ChoiceBase)group.FindById(id);
				if (info != null)
					return info.ReferenceWidget;
			}
			return null;
		}

		/// <summary>
		/// perform a click on the first MenuItem with this id (or label).
		/// </summary>
		/// <remarks> this will perform the clicked even if the menu item is disabled!
		/// so it is not a test of the display properties polling system.</remarks>
		/// <param name="id"></param>
		protected void ClickMenuItem (string groupId, string itemId)
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			adapter.ClickItem(groupId, itemId);
		}

		[Test]
		public void VisitAllMenus()
		{
			if (m_window.Mediator == null)
				ReopenWindow(); // A previously run test had closed it, so the mediator was gone.
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			adapter.ClickOnEverything();
		}
	}
}
