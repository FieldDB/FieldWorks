// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TermRenderingCtrl.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control to display information about the available renderings for a key term and allow
	/// the user to select the desired one for a particular occurrence in the translation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class TermRenderingCtrl : UserControl, ITermRenderingInfo
	{
		#region Data members
		private readonly KeyTermMatch m_term;
		private Rectangle m_rectToInvalidateOnResize;
		private readonly Action<bool> m_selectKeyboard;
		#endregion

		#region Events and Delegates
		public delegate void RenderingChangedHandler(TermRenderingCtrl sender);
		public event RenderingChangedHandler SelectedRenderingChanged;
		public Action BestRenderingsChanged;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TermRenderingCtrl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TermRenderingCtrl(KeyTermMatch term, int endOffsetOfPrev,
			Action<bool> selectKeyboard)
		{
			InitializeComponent();

			DoubleBuffered = true;
			m_term = term;
			m_selectKeyboard = selectKeyboard;
			m_lblKeyTermColHead.Text = term.Term;
			EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm = endOffsetOfPrev;
			m_lbRenderings.Items.AddRange(term.Renderings.Distinct().ToArray());
			term.BestRenderingChanged += term_BestRenderingChanged;
		}
		#endregion

		#region Public properties
		public string SelectedRendering
		{
			get { return m_lbRenderings.SelectedItem.ToString(); }
			set
			{
				if (string.IsNullOrEmpty(value))
					m_lbRenderings.SelectedIndex = -1;
				else
					m_lbRenderings.SelectedItem = value;
			}
		}

		public Font VernacularFont
		{
			get { return m_lbRenderings.Font; }
			set
			{
				m_lbRenderings.Font = value;

				m_lbRenderings.ItemHeight = Math.Max(Properties.Resources.check_circle.Height,
					(int)Math.Ceiling(CreateGraphics().MeasureString("Q", value).Height)) + 2;
			}
		}
		#endregion

		#region Implementation of ITermRenderingInfo
		public IKeyTerm Term
		{
			get { return m_term; }
		}

		public IEnumerable<string> Renderings
		{
			get { return Term.Renderings; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will almost always be 0, but if a term occurs more than once in a phrase, this
		/// will be the character offset of the end of the preceding occurrence of the rendering
		/// of the term in the translation string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm { get; set; }
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a change (probably from another TermRenderingCtrl) to our term's best
		/// rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void term_BestRenderingChanged(KeyTermMatch sender)
		{
			m_lbRenderings.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_lbRenderings control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lbRenderings_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedRenderingChanged != null)
				SelectedRenderingChanged(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuSetAsDefault control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuSetAsDefault_Click(object sender, EventArgs e)
		{
			if (SelectedRendering == m_term.BestRendering)
				return; // already the (implicit or explicit default)
			m_term.BestRendering = SelectedRendering;
			m_lbRenderings.Invalidate();
			if (BestRenderingsChanged != null)
				BestRenderingsChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseDown event of the renderings list. If the user clicks with the
		/// right mouse button we have to select the rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lbRenderings_MouseDown(object sender, MouseEventArgs e)
		{
			m_lbRenderings.Focus(); // This can fail if validation fails in control that had focus.
			if (m_lbRenderings.Focused && e.Button == MouseButtons.Right)
			{
				int index = m_lbRenderings.IndexFromPoint(e.Location);
				if (index >= 0)
					m_lbRenderings.SelectedIndex = index;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseUp event of the renderings list. If the user clicks with the right
		/// mouse button we have to bring up the context menu if the mouse up event occurs over
		/// the selected rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lbRenderings_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (m_lbRenderings.IndexFromPoint(e.Location) == m_lbRenderings.SelectedIndex)
					contextMenuStrip.Show(m_lbRenderings, e.Location);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DrawItem event of the m_lbRenderings control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lbRenderings_DrawItem(object sender, DrawItemEventArgs e)
		{
			bool selected = ((e.State & DrawItemState.Selected) != 0);

			// Draw the item's background fill.
			e.Graphics.FillRectangle(new SolidBrush((selected ?
				SystemColors.Highlight : SystemColors.Window)), e.Bounds);

			// Don't bother doing any more painting if there isn't anything to paint.
			if (e.Index < 0)
				return;

			RectangleF rect = new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
			rect.Inflate(-1, 0);
			rect.X += 2;

			// Get the item being drawn.
			string item = m_lbRenderings.Items[e.Index].ToString();

			// Draw the icon if this is the default rendering.
			if (item == m_term.BestRendering)
			{
				Image icon = Properties.Resources.check_circle;
				rect.Width -= (icon.Width + 2);
				Rectangle rectIcon = new Rectangle(e.Bounds.Right - icon.Width - 1, e.Bounds.Top + (e.Bounds.Height - icon.Height) / 2, icon.Width + 2, icon.Height);
				e.Graphics.DrawImage(icon, rectIcon);
				m_rectToInvalidateOnResize = selected ? new Rectangle() : e.Bounds;
			}

			item = item.Normalize(NormalizationForm.FormC);
			SizeF textSize = e.Graphics.MeasureString(item, VernacularFont);

			if (textSize.Height < rect.Height)
			{
				float diff = rect.Height - textSize.Height;
				rect.Y += diff / 2;
				rect.Height = textSize.Height;
			}

			if (textSize.Width < rect.Width)
			{
				// In some cases where we go from a narrow size to a wide size really fast, debris can get left behind.
				m_rectToInvalidateOnResize = Rectangle.Union(m_rectToInvalidateOnResize,
					new Rectangle((int)Math.Floor(rect.X), (int)Math.Floor(rect.Y),
					(int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height)));
			}

			// Draw the item's text, considering the item's selection state.
			e.Graphics.DrawString(item, VernacularFont,
				new SolidBrush(selected ? SystemColors.HighlightText : SystemColors.WindowText),
				rect);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Resize event of the m_lbRenderings control to force repainting of part
		/// of the listbox contents under certain circumstances.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_lbRenderings_Resize(object sender, EventArgs e)
		{
			m_lbRenderings.Invalidate(m_rectToInvalidateOnResize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the addRenderingToolStripMenuItem control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuAddRendering_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddRenderingDlg(m_selectKeyboard))
			{
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					try
					{
						m_term.AddRendering(dlg.Rendering);
						m_lbRenderings.Items.Add(dlg.Rendering);
					}
					catch (ArgumentException ex)
					{
						MessageBox.Show(FindForm(), ex.Message, dlg.Text);
					}
					SelectedRendering = dlg.Rendering;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opening event of the contextMenuStrip control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			mnuDeleteRendering.Enabled = m_lbRenderings.SelectedItem != null &&
				m_term.CanRenderingBeDeleted(m_lbRenderings.SelectedItem.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuDeleteRendering control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void mnuDeleteRendering_Click(object sender, EventArgs e)
		{
			string rendering = m_lbRenderings.SelectedItem.ToString();
			SelectedRendering = m_term.BestRendering;
			if (SelectedRenderingChanged != null)
				SelectedRenderingChanged(this);
			m_term.DeleteRendering(rendering);
			m_lbRenderings.Items.Remove(rendering);
		}
		#endregion
	}
}