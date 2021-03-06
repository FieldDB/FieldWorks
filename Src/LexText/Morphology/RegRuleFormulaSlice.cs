﻿using System;
using System.Windows.Forms;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a <c>PhRegularRule</c> slice.
	/// </summary>
	public class RegRuleFormulaSlice : RuleFormulaSlice
	{
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new RegRuleFormulaControl(m_configurationNode);
		}

		RegRuleFormulaControl RegRuleFormulaControl
		{
			get
			{
				return Control as RegRuleFormulaControl;
			}
		}

		public bool OnDisplayContextSetOccurrence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = RegRuleFormulaControl.CanModifyContextOccurrence;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnContextSetOccurrence(object args)
		{
			CheckDisposed();

			var cmd = (XCore.Command) args;
			if (cmd.Parameters.Count > 0)
			{
				string minStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "min");
				string maxStr = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "max");
				int min = Int32.Parse(minStr);
				int max = Int32.Parse(maxStr);
				RegRuleFormulaControl.SetContextOccurrence(min, max);
			}
			else
			{
				int min, max;
				RegRuleFormulaControl.GetContextOccurrence(out min, out max);
				using (var dlg = new OccurrenceDlg(m_mediator.HelpTopicProvider, min, max, false))
				{
					if (dlg.ShowDialog((XCore.XWindow)Mediator.PropertyTable.GetValue("window")) == DialogResult.OK)
						RegRuleFormulaControl.SetContextOccurrence(dlg.Minimum, dlg.Maximum);
				}
			}
			return true;
		}

		public bool OnDisplayContextSetVariables(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = RuleFormulaControl.IsFeatsNCContextCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

	}
}
