using System;
using System.Windows.Forms;
using System.Drawing;
using DevComponents.DotNetBar;
using SIL.Utils; // for ImageCollection

namespace XCore
{

	class PanelButton : PanelEx
	{
		private ImageCollection m_images;
		private bool mouseOverControl = false;

		public PanelButton(XCore.ChoiceBase choice, ImageCollection images):base()
		{
			m_images = images;
			
			this.Dock = System.Windows.Forms.DockStyle.Right;
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(576, 2);
			this.Name = "panelEx1";
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Size = new System.Drawing.Size(120, 20);
			this.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.Style.GradientAngle = 90;

			this.MouseEnter += new EventHandler(panelButton_MouseEnter);
			this.MouseLeave += new EventHandler(panelButton_MouseLeave);
			this.MouseDown += new MouseEventHandler(panelButton_MouseDown);

			this.Click += new EventHandler(PanelButton_Click);
			this.TabIndex = 0;
			
			this.Tag = choice;
			SetLabel();
		}

		/// <summary>
		/// is what we are displaying affected by this XCore property?
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool IsRelatedProperty(string name)
		{
			//for now, only handles Boolean properties
			BoolPropertyChoice choice = this.Tag as XCore.BoolPropertyChoice;
			if (choice == null)
				return false;

			return choice.BoolPropertyName == name;
		}

		private void SetLabel()
		{
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();
			
			string s = display.Text.Replace("_", "&");
			Graphics g = this.CreateGraphics();
			this.Width = (int)(g.MeasureString(s, this.Font).Width);
			this.Text = s;
			this.Style.Alignment =  System.Drawing.StringAlignment.Center;

			if(display.Checked)
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedText;
			}
			else		
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			}

			// Simulate a mouse enter or leave event to get the correct highlighting
			if(mouseOverControl)
				this.panelButton_MouseEnter(null, null);
			else
				this.panelButton_MouseLeave(null, null);

			this.Controls.Clear(); // Clear out any previous checkboxes and images

			// Add in a checkbox that reflects the "checked" status of the button
			CheckBox checkBox = new CheckBox();
			checkBox.Checked = display.Checked;
			checkBox.Click +=new EventHandler(PanelButton_Click);
			checkBox.Location = new Point(0,0);
			checkBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			checkBox.Dock = System.Windows.Forms.DockStyle.None;
			checkBox.Width = 17;
			this.Width += checkBox.Width;
			checkBox.MouseEnter += new EventHandler(panelButton_MouseEnter);
			checkBox.MouseLeave += new EventHandler(panelButton_MouseLeave);
			checkBox.MouseDown += new MouseEventHandler(panelButton_MouseDown);
			this.Controls.Add(checkBox);
			this.Style.Alignment =  System.Drawing.StringAlignment.Far;

			if(display.ImageLabel != null && display.ImageLabel !="" && display.ImageLabel !="default")
			{
				PanelEx p = new PanelEx();
				Image i = m_images.GetImage(display.ImageLabel);
				p.Style.BackgroundImage = i;
				p.Style.BackgroundImagePosition = eBackgroundImagePosition.Center;
				p.Location = new Point(checkBox.Width,0);
				p.Anchor = System.Windows.Forms.AnchorStyles.Left;
				p.Dock = System.Windows.Forms.DockStyle.None;
				p.Size = new Size(17,this.Height);
				this.Width += p.Size.Width;
				this.Controls.Add(p);
				p.Click +=new EventHandler(PanelButton_Click);
				p.MouseEnter += new EventHandler(panelButton_MouseEnter);
				p.MouseLeave += new EventHandler(panelButton_MouseLeave);
				p.MouseDown += new MouseEventHandler(panelButton_MouseDown);
			}

			this.Refresh();
		}

		/// <summary>
		/// 
		/// </summary>
		public void UpdateDisplay()
		{
			SetLabel();	
		}

		private void PanelButton_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;
			XCore.ChoiceBase c = (XCore.ChoiceBase )this.Tag;
			c.OnClick(this, null);
			Cursor.Current = Cursors.Default;
		}

		private void panelButton_MouseEnter(object sender, EventArgs e)
		{
			mouseOverControl = true;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			if(display.Checked)
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
			}
			else
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotText;
			}

			this.Refresh();
		}

		private void panelButton_MouseDown(object sender, MouseEventArgs e)
		{
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemPressedBackground;
			this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemPressedBackground2;

			this.Refresh();
		}

		private void panelButton_MouseLeave(object sender, EventArgs e)
		{
			mouseOverControl = false;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			if(display.Checked)
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
			}
			else		
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			}

			this.Refresh();
		}
	}
}
