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
// File: Question.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class Question
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Little class to support XML serialization
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	[XmlRoot(Namespace = "", IsNullable = false)]
	public class Question
	{
		[XmlAttribute("scrref")]
		public string ScriptureReference { get; set; }

		[XmlAttribute("startref")]
		public int StartRef { get; set; }

		[XmlAttribute("endref")]
		public int EndRef { get; set; }

		[XmlElement("Q", Form = XmlSchemaForm.Unqualified)]
		public string Text {get; set; }

		[XmlElement("A", Form = XmlSchemaForm.Unqualified, IsNullable = false)]
		public string[] Answers { get; set; }

		[XmlElement("Note", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string[] Notes { get; set; }

		[XmlElement("Alternative", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string[] AlternateForms { get; set; }
	}
	#endregion
}