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
// File: Demo.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Demo.
	/// </summary>
	[TestFixture]
	public class Demo
	{
		RunTest m_rt = new RunTest("FS");

		public Demo()
		{
		}

		[Test]
		public void DemoA()
		{
			m_rt.fromFile("Demo");
		}

		[Test]
		public void DemoPartD()
		{
			m_rt.fromFile("DemoD");
		}

		[Test]
		public void DemoPartF()
		{
			m_rt.fromFile("DemoF");
		}

		[Test]
		public void DemoPartG()
		{
			m_rt.fromFile("DemoG");
		}

		[Test]
		public void DemoPartH()
		{
			m_rt.fromFile("DemoH");
		}

		[Test]
		public void DemoPartI()
		{
			m_rt.fromFile("DemoI");
		}

		[Test]
		public void DemoPartJ()
		{
			m_rt.fromFile("DemoJ");
		}

		[Test]
		public void DemoPartK()
		{
			m_rt.fromFile("DemoK");
		}

	}
}
