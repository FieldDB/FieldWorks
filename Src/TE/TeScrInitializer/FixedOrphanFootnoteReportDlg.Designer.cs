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
// File: FixedOrphanFootnoteReportDlg.Designer.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.TE
{
	partial class FixedOrphanFootnoteReportDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FixedOrphanFootnoteReportDlg));
			this.SuspendLayout();
			//
			// lblWarning
			//
			resources.ApplyResources(this.lblWarning, "lblWarning");
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// saveFileDialog
			//
			resources.ApplyResources(this.saveFileDialog, "saveFileDialog");
			//
			// FixedOrphanFootnoteReportDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "FixedOrphanFootnoteReportDlg";
			this.Controls.SetChildIndex(this.lblWarning, 0);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
