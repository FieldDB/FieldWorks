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
// File: IxCoreColleague.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace XCore
{
	public enum ColleaguePriority
	{
		High = 0,
		Medium  = 0x000F000,
		Low = 0xFFFFFFF
	}

	public interface IxCoreColleague
	{
		void Init(Mediator mediator, XmlNode configurationParameters);

		/// <summary>
		/// In OnInvokeRecursive in the mediator this list will determine order.
		/// </summary>
		/// <returns></returns>
		IxCoreColleague[] GetMessageTargets();

		bool ShouldNotCall { get; }

		/// <summary>
		/// When Colleagues are added to the mediator this priority will determine the order that they are called
		/// in InvokeOnColleagues in the Mediator, and also in the Mediator Dispose method.
		///
		/// Where possible ColleaguePriority should be used, if two Colleagues conflict and both belong at the same
		/// ColleaguePriority level a custom priority may be necessary. Priority is determined by the natural sort order for
		/// int, so lower numbers are higher priority. Maximum integer would be the lowest possible priority.
		/// </summary>
		int Priority { get; }
	}

	/// <summary>
	/// Use to get accessability information.
	/// Implementors of this interface MUST derive, directly, or indirectly,
	/// from Control, as it will be cast to Control.
	/// </summary>
	/// <remarks>
	/// The only real reason this interface has been defined is so MultiPane is happy.
	/// It used
	/// </remarks>
	public interface IXCoreUserControl
	{
		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		string AccName { get; }
	}

	/// <summary>
	/// All Controls that can be used as main controls in an XWindow,
	/// must implement the IxCoreContentControl interface.
	/// It 'extends' other required interfaces, just to keep them all in one bundle.
	/// </summary>
	public interface IxCoreContentControl : IXCoreUserControl, IxCoreCtrlTabProvider, IxCoreColleague
	{
		bool PrepareToGoAway();
		string AreaName { get;}
	}

	/// <summary>
	/// Implement this interface on controls that can provide targets to Cntrl-(Shift-)Tab into.
	/// </summary>
	public interface IxCoreCtrlTabProvider
	{
		/// <summary>
		/// Gather up suitable targets to Cntrl-(Shift-)Tab into.
		/// </summary>
		/// <param name="targetCandidates">List of places to move to.</param>
		/// <returns>A suitable target for moving to in Ctrl(+Shit)+Tab.
		/// This returned value should also have been added to the main list.</returns>
		Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates);
	}

	/// <summary>
	/// A control implements this if it wants to snap the split position to particular points.
	/// </summary>
	public interface ISnapSplitPosition
	{
		/// <summary>
		/// An implementor answers true if it wants to take control of the split position.
		/// It may alter the position.
		/// If it answers true, the position will not be modified further by the MultiPane.
		/// Width is the width this pane will be after the splitter is positioned.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		bool SnapSplitPosition(ref int width);
	}

	/// <summary>
	/// This is an interface implemented by xWindow (and perhaps other main window classes?)
	/// that allows a few of their key functions to be called by things that don't reference xCore.
	/// </summary>
	public interface IxWindow
	{
		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		void SuspendIdleProcessing();

		/// <summary>
		/// See SuspentIdleProcessing.
		/// </summary>
		void ResumeIdleProcessing();

		/// <summary>
		/// this allows the calling application to talk to the window.
		/// Placement in the IxWindow interface lets FwApp call Mediator.BroadcastPendingItems (see FWNX-213).
		/// </summary>
		Mediator Mediator
		{
			get;
		}

		/// <summary>
		/// Call this for the duration of a block of code outside of xWindow that might update
		/// the size of the window (OnCreateHandle, for instance) without regard to the Mediator
		/// PropertyTable. Call ResumeWindowSizing when done.
		/// </summary>
		void SuspendWindowSizePersistence();

		/// <summary>
		/// See SuspendWindowSizing.
		/// </summary>
		void ResumeWindowSizePersistence();
	}
}
