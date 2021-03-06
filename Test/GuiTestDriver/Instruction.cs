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
// File: Instruction.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// This is the abstract class for all FW GUI test driver instructions. Together the instances
// of derived classes form an instruction tree. Specialized instructions called "context"
// and "flow" instructions maintain a collection of their child instructions.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using System.Windows.Forms;
using System.Xml;

namespace GuiTestDriver
{
	/// <summary>
	/// Each instruction knows how to execute its own operation, how to access its context and the
	/// application it is issued for.
	/// </summary>
	public abstract class Instruction
	{
		//GuiTestDriver.
		Instruction m_parent;
		private int m_number;
		protected string m_tag; // the xml tag associated with this ins.
		protected Logger m_log;
		protected string m_id = "NONE";
		protected int m_Rest = 0; // number of milliseconds to wait before execution
		protected bool m_wait = true; // wait for before exec, false = don't wait
		protected bool m_finished = false;	// true if this instruction has completed
		protected bool m_success = false;	// true if completed successfully
		protected int m_ExecuteTickCount = 0;
		private bool m_deferAssert = false;
		protected int m_logLevel = 0; // 0 = default, 1 = everything, 2= timing
		//private static DebugWindow m_dWin = null;
		protected static long m_memory = 0;
		protected XmlElement m_elt = null;

		protected string m_path;
		protected string m_select;
		protected string m_selectPath;
		protected bool m_doneOnce; // used only with m_select in a do-once context so m_path is not duplicated

		/// <summary>
		/// Initialize an Instruction.
		/// </summary>
		public Instruction()
		{
			m_number = -1;
			m_parent = null;
			m_log = Logger.getOnly();
			m_id = "NONE";
			m_Rest = 0; // default is not to wait for execution
			m_tag = "NONE";
			m_doneOnce = false;
			m_path = null;
			m_select = null;
			m_selectPath = null;
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public virtual bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			// nothing special
			return true;
		}

		/// <summary>
		/// Most Instructions are code-behind classes for elements in
		/// XML test scripts. This is the corresponding element.
		/// </summary>
		public XmlElement Element
		{
			get { return m_elt; }
			set { m_elt = value; }
		}

		/// <summary>
		/// Default implementation that simply waits for the idle state.
		/// This should be called from each overrided Execute method.
		/// </summary>
		public virtual void Execute()
		{
			if (m_number == -1) m_number = TestState.getOnly().IncInstructionCount;
			WaitMsec(); // a script specified time
			CheckForErrorDialogs(true);
			m_log.mark(this);
			m_ExecuteTickCount = System.Environment.TickCount;
		}

		/// <summary>
		/// Checks for an open error window and closes it.
		/// This may close the application.
		/// Also, set doAssert to true if this test should terminate on finding one.
		/// </summary>
		/// <param name="doAssert">false when the test should not fail as when another test caused the error window.</param>
		protected void CheckForErrorDialogs(bool doAssert)
		{   // check for specific error dialogs
			// Try to get the ah for "An error has occurred" window
			// Log its info
			// Close the error window
			// Assert if directed to
			AccessibilityHelper ah = new AccessibilityHelper("An error has occurred");
			// This constructor returns the top window if it can't find the
			// one with the title
			// Even though the ah is not null, sometimes it can't get the name
			string name = "";
			if (ah != null && ah.Name == "An error has occurred")
			{ // this is really bad!
				string color = "green";
				m_log.paragraph("Window: " + name);
				GuiPath path = new GuiPath("button:Exit the application");
				AccessibilityHelper ahExit = ah.SearchPath(path, null);
				if (ahExit == null)
				{
					color = "yellow"; // can continue
					path = new GuiPath("button:Ok");
					ahExit = ah.SearchPath(path, null);
				}
				if (ahExit == null) color = "unknown";
				if (color == "green")   m_log.paragraph("Found a green error window!");
				if (color == "yellow")  m_log.paragraph("Found a yellow error window! Continuing...");
				if (color == "unknown") m_log.paragraph("Found an unknown error window!");
				// write the error text to the log
				path = new GuiPath("window:NAMELESS[2]");
				AccessibilityHelper ahTextWin = ah.SearchPath(path, null);
				AccessibilityHelper ahText = null;
				if (ahTextWin != null)
				{
					path = new GuiPath("text:NAMELESS[2]");
					ahText = ah.SearchPath(path, null);
				}
				if (ahText != null)
					m_log.paragraph(ahText.Value);
				else m_log.paragraph(@"Don't know where to get the message text from.");
				if (color == "green" || color == "yellow")
					ahExit.SimulateClickRelative(10,10);
				if (doAssert && color == "green")
					m_log.fail("Got an error window!");
				if (color == "unknown" && Application != null)
					Application.SendKeys(@"{ESC}"); // exits error window but maybe not the app
				if (doAssert && color == "unknown")
					m_log.fail("Closed the unknown error window.");
				Thread.Sleep(20000); // if still alive, wait for things to calm down
			}
		}

		/// <summary>
		/// If this instruction in a do-once instruction, then this
		/// method provides a way to know not to assert or terminate early.
		/// </summary>
		/// <returns>true if this instruction is in a do-once context.</returns>
		public bool DoingOnce()
		{
			return (DoOnce)Ancestor(typeof(DoOnce))!= null;
		}

		/// <summary>
		/// This attribute is used to defer the assert processing. This is needed for the 'do-once'
		/// context so that the instructions are allowed to retry upto the allowed time limit, and
		/// then assert if needed.
		/// </summary>
		public bool DeferAssert
		{
			get { return m_deferAssert;}
			set { m_deferAssert = value;}
		}

		public int ExecutedAtTickCount { get { return m_ExecuteTickCount;}}

		public Instruction Parent
		{
			get {return m_parent;}
			set {m_parent = value;}
		}

		public Instruction Ancestor(System.Type type)
		{
			Instruction ins = m_parent;
			while (ins != null && ! (type.IsInstanceOfType(ins)))
				ins = ins.Parent;
			return ins; // may be null!
		}

		public virtual AppHandle Application
		{
			get
			{
				AppHandle appHandle = null;
				OnApplication appCon = (OnApplication)Ancestor(typeof(OnApplication));
				if (appCon != null) appHandle = appCon.Application;
				return appHandle;
			}
			// Set by default does nothing, but apparently we can't override unless it's in the base.
			set { m_log.fail("Application can only be set on subclasses"); }
		}

		public int Wait
		{ // miliseconds to wait before executing an instruction
			get {return m_Rest;}
			set
			{
				if (value < 0) { m_wait = false; m_Rest = 0; }
				else           { m_wait = true;  m_Rest = value; }
			}
		}

		public string Id
		{
			get {return m_id;}
			set {m_id = value;}
		}

		public int Number
		{
			get {return m_number;}
			set {m_number = value;}
		}

		public string Tag
		{
			get {return m_tag;}
		}

		public bool Finished
		{
			get { return m_finished;}
			set { m_finished = value;}
		}

		public bool Success
		{
			get { return m_success;}
			set { m_success = value;}
		}

		public int Log
		{
			get { return m_logLevel;}
			set { m_logLevel = value;}
		}

		public string Path
		{
			get { return m_path; }
			set { m_path = value; }
		}

		public string Select
		{
			get { return m_select; }
			set { m_select = value; }
		}

		public string SelectPath
		{
			get { return m_selectPath; }
			set { m_selectPath = value; }
		}

		/// <summary>
		/// Interprets a model path (selection), selects the node and returns its path.
		/// </summary>
		/// <param name="con">Current context of the action instruction.</param>
		/// <returns>A "path" string</returns>
		protected XmlPath SelectToPath(Context con, string modelPath)
		{
			if (modelPath == null || modelPath == "") return null;
			// only one node or attribute selected gets clicked
			m_log.paragraph(makeNameTag() + " creating one select-path target via " + modelPath);
			XmlNodeList pathNodes = Instructionator.selectNodes(con, modelPath, makeNameTag());
			m_log.isNotNull(pathNodes, makeNameTag() + " select-path='" + modelPath + "' returned no model node");
			m_log.isTrue(pathNodes.Count > 0, makeNameTag() + " select-path='" + modelPath + "' returned no nodes");
			// The modelPath text may have selected a string that is itself xPath!
			// If so, select on that xPath
			if (pathNodes.Count == 1 && pathNodes.Item(0).NodeType == XmlNodeType.Text)
			{ // this text node should be an xpath statement
				m_log.isNotNull(pathNodes.Item(0).Value, makeNameTag() + " select-path='" + modelPath + "was a null string");
				m_log.isTrue(pathNodes.Item(0).Value != "", makeNameTag() + " select-path='" + modelPath + "was an empty string");
				string xPathImage = pathNodes.Item(0).Value;
				m_log.paragraph(makeNameTag() + " selected a text node with more XPATH: " + xPathImage);
				pathNodes = Instructionator.selectNodes(con, xPathImage, makeNameTag() + " selecting " + xPathImage);
				m_log.isNotNull(pathNodes, makeNameTag() + " selecting " + xPathImage + " from select='" + modelPath + "' returned no model node");
			}
			// Click the first node returned
			XmlNode node = pathNodes[0];
			XmlPath xPath = new XmlPath(node);
			if (!xPath.isValid()) m_log.fail(makeNameTag() + " XmlPath not constructable from " + node.OuterXml);
			return xPath;
		}

		/// <summary>
		/// Gets the offset integer specified in the xNode.
		/// If delta has a value other than defaultDelta, it is returned.
		/// Otherwise, the value returned is parsed from the deltaName attribute of
		/// xNode.
		/// </summary>
		/// <param name="delta">The current value of an offset</param>
		/// <param name="defaultDelta">Default value of the offset</param>
		/// <param name="xNode">XmlNode that might have an offset attribute</param>
		/// <param name="deltaName">Name of the offset attribute</param>
		/// <returns>The offset from the Xml node, the current value if not the default, the default otherwise</returns>
		protected int getOffset(int delta, int defaultDelta, XmlNode xNode, string deltaName)
		{
			int del = delta;
			string sdy = XmlFiler.getAttribute(xNode, deltaName);
			if (delta == defaultDelta && sdy != null)
			{
				try {del = Convert.ToInt32(sdy);}
				catch (FormatException) { del = defaultDelta; }
			}
			return del;
		}

		/// <summary>
		/// Determines how to treat passing and failing this instruction based on settings and context.
		/// </summary>
		/// <param name="onPassIn">Setting from xml.</param>
		/// <param name="onFailIn">Setting from xml.</param>
		/// <param name="onPass">Determined value considering context.</param>
		/// <param name="onFail">Determined value considering context.</param>
		public void PassFailInContext(string onPassIn, string onFailIn, out string onPass, out string onFail)
		{
			m_log.paragraph("PassFailInContext n=" + m_number + ": passIn=" + onPassIn + " failIn=" + onFailIn);
			bool Looking = true;
			Instruction ins = this;
			if (onPassIn == null) onPass = "not set";
			else onPass = onPassIn;
			if (onFailIn == null) onFail = "not set";
			else onFail = onFailIn;
			Context con = null;
			while (Looking && onFail == "not set" && onPass == "not set")
			{
				con = (Context)ins.Ancestor(typeof(Context));
				if (con != null)
				{
					if (con.OnPass != null) onPass = con.OnPass;
					if (con.OnFail != null) onFail = con.OnFail;
					ins = con;
					m_log.paragraph("PassFailInContext n="+m_number+": passIn="+onPassIn+" failIn="+onFailIn+" from ins n="+ins.m_number);
				}
				else
				Looking = false;
			}
			//if (onFail == "not set" && onPass == "not set") onFail = "assert";
			if (onFail == "not set" && onPass == "assert") onFail = "skip";
			if (onFail == "not set") onFail = "assert";
			if (onPass == "not set") onPass = "skip";

			// let it pass for now..
			if (DeferAssert) onFail = "skip";
		}

		/// <summary>
		/// Gets the image of the specified data. If name is null,
		/// the instruction's sequence number is returned.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public virtual string GetDataImage (string name)
		{
			if (name == null) name = "number";
			switch (name)
			{
				case "id":		return m_id;
				case "tag":		return m_tag;
				case "number":	return m_number.ToString();
				case "wait":	return m_Rest.ToString();
				case "select": return m_select;
				case "path": return m_path;
				case "done": return m_finished.ToString();
				default:		return "["+makeName()+" does not have data for '"+name+"']";
			}
		}

		/// <summary>
		/// WaitMsec for a specified amount of time in milliseconds
		/// </summary>
		public virtual void WaitMsec ()
		{
			Thread.Sleep(m_Rest);
		}

		public virtual void RemoveWaitTime()
		{
			m_Rest = 0;
		}

		/// <summary>
		/// Makes a unique name for an instruction by combining
		/// its class?, id and instruction sequence number.
		/// </summary>
		/// <returns>A unique name for this instruction</returns>
		public string makeName()
		{
			string id = m_tag;
			if (m_tag == "NONE") id = this.GetType().Name;
			if (m_id != "NONE") id += "("+m_id+")";
			id += m_number;
			return id;
		}

		/// <summary>
		/// Makes a unique name tag for an instruction by combining
		/// its unique name and some formatting.
		/// </summary>
		/// <returns>A unique name tag for this instruction</returns>
		public string makeNameTag()
		{
			return makeName() + ": ";
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public virtual string image()
		{
			string image = m_tag + @" n="""+m_number+@"""";
			if (m_id != "NONE")   image += @" id="""+m_id+@"""";
			if (m_Rest != 0)      image += @" wait="""+m_Rest+@"""";
			if (m_select != null) image += @" select=""" + m_select + @"""";
			if (m_selectPath != null) image += @" select-path=""" + m_selectPath + @"""";
			if (m_path != null) image += @" path=""" + Utilities.attrText(m_path) + @"""";
			if (1 == m_logLevel)
			{
				image += @" wait2=""" + m_wait + @"""";
				image += @" log=""" + m_logLevel + @"""";
				image += @" deferAssert=""" + m_deferAssert + @"""";
			}
			return image;
		}

		/// <summary>
		/// Returns an attribute that identifies the instruction whose
		/// results are to follow in other attributes for the Logger.
		/// </summary>
		/// <returns>An identifying attribute.</returns>
		public virtual string resultImage()
		{
			string image = @" tag=""" + m_tag + @""" ins=""" + m_number + @"""";
			if (0 < m_logLevel && m_ExecuteTickCount != 0)
				image += @" exeTicks="""+m_ExecuteTickCount+@"""";
			if (1 == m_logLevel)
			{
				image += @" wait2=""" + m_wait + @"""";
				image += @" deferAssert=""" + m_deferAssert + @"""";
				image += @" finished="""+m_finished+@"""";
				image += @" success="""+m_success+@"""";
			}
			return image;
		}
	}
}
