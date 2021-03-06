// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: OnApplication.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
//using System.Diagnostics;
using System.Threading;
//using NUnit.Framework;
//using System.Windows.Forms;

namespace GuiTestDriver
{
	/// <summary>
	/// A OnStartup only exists in an OnApplication.
	/// There is only one in each.
	/// </summary>
	public class OnStartup : Context
	{
		public OnStartup()
		{
			m_tag = "on-startup";
		}

		/// <summary>
		/// Execute does nothing.
		/// The real work is invoked via ExecuteOnDemand,
		/// which is invoked from its OnApplication.
		/// </summary>
		public override void Execute()
		{
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// ExecuteOnDemand is a pass through for handling startup dialogs.
		/// It is invoked from its OnApplication.
		/// </summary>
		/// <param name="ts">The TestState object</param>
		public void ExecuteOnDemand()
		{
			base.Execute();
		}
	}
}
