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
// File: FwData.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FixData
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix errors in a FieldWorks data XML file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwData
	{
		string m_filename;
		IProgress m_progress;
		int m_crt;
		HashSet<Guid> m_guids = new HashSet<Guid>();
		Dictionary<Guid, Guid> m_owners = new Dictionary<Guid, Guid>();

		List<string> m_errors = new List<string>();
		List<XElement> m_dupGuidElements = new List<XElement>();
		List<XElement> m_dupOwnedElements = new List<XElement>();
		List<XElement> m_danglingLinks = new List<XElement>();
		string m_logfile;

		/// <summary>
		/// Constructor.  Reads the file and stores any data needed for corrections later on.
		/// </summary>
		public FwData(string filename, IProgress progress)
		{
			m_filename = filename;
			m_progress = progress;

			m_progress.Minimum = 0;
			m_progress.Maximum = 1000;
			m_progress.Position = 0;
			m_progress.Message = String.Format(Strings.ksReadingTheInputFile, m_filename);
			m_crt = 0;
			using (XmlReader xrdr = XmlReader.Create(m_filename))
			{
				xrdr.MoveToContent();
				if (xrdr.Name != "languageproject")
					throw new Exception(String.Format("Unexpected outer element (expected <Lists>): {0}", xrdr.Name));
				xrdr.Read();
				xrdr.MoveToContent();
				if (xrdr.Name == "AdditionalFields")
					xrdr.ReadToFollowing("rt");
				while (xrdr.Name == "rt")
				{
					string rtXml = xrdr.ReadOuterXml();
					XElement rt = XElement.Parse(rtXml);
					StoreGuidInfo(rt);
					xrdr.MoveToContent();
					++m_crt;
					if (m_progress.Position == m_progress.Maximum)
						m_progress.Position = 0;
					if ((m_crt % 1000) == 0)
						m_progress.Step(1);
				}
				xrdr.Close();
			}
		}

		/// <summary>
		/// Fix any errors you can, and write the results out.  If successful, the input
		/// file is renamed with a ".bak" extension, the output file is renamed to the
		/// original input filename, and a log file with a ".fixes" extension is written.
		/// </summary>
		public void FixErrorsAndSave()
		{
			m_progress.Maximum = m_crt;
			m_progress.Position = 0;
			m_progress.Message = String.Format(Strings.ksLookingForAndFixingErrors, m_filename);
			string outfile = m_filename + "-x";
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = String.Empty;
			using (XmlWriter xw = XmlWriter.Create(outfile, settings))
			{
				xw.WriteStartDocument();

				using (XmlReader xrdr = XmlReader.Create(m_filename))
				{
					xrdr.MoveToContent();
					if (xrdr.Name != "languageproject")
						throw new Exception(String.Format("Unexpected outer element (expected <Lists>): {0}", xrdr.Name));
					xw.WriteStartElement("languageproject");
					xw.WriteAttributes(xrdr, false);
					xrdr.Read();
					xrdr.MoveToContent();
					if (xrdr.Name == "AdditionalFields")
					{
						string sXml = xrdr.ReadOuterXml();
						XElement xe = XElement.Parse(sXml);
						xe.WriteTo(xw);
						xrdr.MoveToContent();
					}
					while (xrdr.Name == "rt")
					{
						var rtXml = xrdr.ReadOuterXml();
						var rt = XElement.Parse(rtXml);
						FixErrors(rt);
						rt.WriteTo(xw);
						xrdr.MoveToContent();
						m_progress.Step(1);
					}
					xrdr.Close();
				}
				xw.WriteEndDocument();
				xw.Close();
			}

			var bakfile = Path.ChangeExtension(m_filename,
				Resources.FwFileExtensions.ksFwDataFallbackFileExtension);
			if (File.Exists(bakfile))
				File.Delete(bakfile);
			File.Move(m_filename, bakfile);
			File.Move(outfile, m_filename);

			if (m_dictWsChanges.Count > 0)
				FixLdmlFiles(Path.GetDirectoryName(m_filename));

			if (Errors.Count > 0)
			{
				m_logfile = Path.ChangeExtension(m_filename, ".fixes");
				using (TextWriter tw = new StreamWriter(m_logfile, false, System.Text.Encoding.UTF8))
				{
					foreach (var err in Errors)
						tw.WriteLine(err);
					tw.Close();
				}
			}
		}

		private void FixLdmlFiles(string projectDirectory)
		{
			if (projectDirectory == null)
				projectDirectory = "";
			var wsDirectory = Path.Combine(projectDirectory, "WritingSystemStore");
			if (!Directory.Exists(wsDirectory))
				return;
			foreach (var key in m_dictWsChanges.Keys)
			{
				var wses = key.Split('\t');
				var oldTag = wses[0];
				var newTag = wses[1];
				var oldLdmlFile = Path.Combine(wsDirectory, oldTag + ".ldml");
				var newLdmlFile = Path.Combine(wsDirectory, newTag + ".ldml");
				if (!File.Exists(oldLdmlFile))
				{
					if (oldTag.StartsWith("cmn"))
						oldLdmlFile = Path.Combine(wsDirectory, oldTag.Remove(0,3).Insert(0,"zh") + ".ldml");
					else
						oldLdmlFile = newLdmlFile;
					if (!File.Exists(oldLdmlFile))
						continue;
				}
				var xeLdml = XElement.Load(oldLdmlFile);
				var parts = newTag.Split('-');
				var xeIdentity = xeLdml.Descendants("identity").FirstOrDefault();
				if (xeIdentity == null)
					continue;
				var xeLanguage = xeIdentity.Descendants("language").FirstOrDefault();
				if (xeLanguage == null)
					continue;
				var itag = 0;
				var tagLang = parts[itag++];
				if (tagLang == "x" && itag < parts.Length)
					tagLang = parts[itag++];
				xeLanguage.SetAttributeValue("type", tagLang);
				var xeRegion = xeIdentity.Descendants("territory").FirstOrDefault();
				if (xeRegion == null && itag < parts.Length)
				{
					var tag = parts[itag++];
					if (tag.Length == 4 && itag < parts.Length)
						tag = parts[itag];		// skip script.
					if (tag.Length == 2)
					{
						xeRegion = new XElement("territory");
						xeRegion.SetAttributeValue("type", tag);
					}
					else
					{
						continue;
					}
					xeLanguage.AddAfterSelf(xeRegion);
				}
				File.Move(oldLdmlFile, oldLdmlFile + "-old");
				if (File.Exists(newLdmlFile))
					File.Move(newLdmlFile, newLdmlFile + "-bak");
				using (var outFile = File.Create(newLdmlFile))
				{
					var settings = new XmlWriterSettings
						{
							Encoding = Encoding.UTF8,
							Indent = true,
							IndentChars = "\t"
						};
					var writer = XmlWriter.Create(outFile, settings);
					xeLdml.WriteTo(writer);
					writer.Close();
				}
			}
		}

		private void StoreGuidInfo(XElement rt)
		{
			Guid guid = new Guid(rt.Attribute("guid").Value);
			if (m_guids.Contains(guid))
			{
				m_errors.Add(String.Format(Strings.ksObjectWithGuidAlreadyExists, guid));
				m_dupGuidElements.Add(rt);
			}
			else
			{
				m_guids.Add(guid);
			}
			foreach (var objsur in rt.Descendants("objsur"))
			{
				XAttribute xaType = objsur.Attribute("t");
				if (xaType == null || xaType.Value != "o")
					continue;
				XAttribute xaGuidObj = objsur.Attribute("guid");
				Guid guidObj = new Guid(xaGuidObj.Value);
				if (m_owners.ContainsKey(guidObj))
				{
					m_errors.Add(String.Format(Strings.ksObjectWithGuidAlreadyOwned,
						guidObj, guid));
					m_dupOwnedElements.Add(objsur);
				}
				else
				{
					m_owners.Add(guidObj, guid);
				}
			}
		}

		private void FixErrors(XElement rt)
		{
			Guid guid = new Guid(rt.Attribute("guid").Value);
			Guid storedOwner;
			if (!m_owners.TryGetValue(guid, out storedOwner))
				storedOwner = Guid.Empty;
			var xaClass = rt.Attribute("class");
			var className = xaClass == null ? "<unknown>" : xaClass.Value;
			XAttribute xaOwner = rt.Attribute("ownerguid");
			if (xaOwner != null)
			{
				Guid guidOwner = new Guid(xaOwner.Value);
				if (guidOwner != storedOwner)
				{
					if (storedOwner != Guid.Empty && m_guids.Contains(storedOwner))
					{
						m_errors.Add(String.Format(Strings.ksChangingOwnerGuidValue,
							guidOwner, storedOwner, className, guid));
						xaOwner.Value = storedOwner.ToString();
					}
					else if (!m_guids.Contains(guidOwner))
					{
						m_errors.Add(String.Format(Strings.ksRemovingLinkToNonexistentOwner,
							guidOwner, className, guid));
						xaOwner.Remove();
					}
				}
			}
			else if (storedOwner != Guid.Empty)
			{
				m_errors.Add(String.Format(Strings.ksAddingLinkToOwner,
					storedOwner, className, guid));
				xaOwner = new XAttribute("ownerguid", storedOwner);
				rt.Add(xaOwner);
			}
			foreach (var objsur in rt.Descendants("objsur"))
			{
				XAttribute xaType = objsur.Attribute("t");
				if (xaType == null)
					continue;
				XAttribute xaGuidObj = objsur.Attribute("guid");
				Guid guidObj = new Guid(xaGuidObj.Value);
				if (!m_guids.Contains(guidObj))
				{
					m_errors.Add(String.Format(Strings.ksRemovingLinkToNonexistingObject,
						guidObj, className, guid, objsur.Parent.Name));
					m_danglingLinks.Add(objsur);
					continue;
				}
				if (xaType.Value == "o")
				{
					Guid guidStored;
					if (m_owners.TryGetValue(guidObj, out guidStored))
					{
						if (guidStored != guid)
						{
							m_errors.Add(String.Format(Strings.ksRemovingMultipleOwnershipLink,
								guidObj, className, guid, objsur.Parent.Name));
							m_danglingLinks.Add(objsur);	// excessive ownership
						}
					}
				}
			}
			foreach (var objsur in m_danglingLinks)
				objsur.Remove();
			m_danglingLinks.Clear();

			foreach (var run in rt.Descendants("Run"))
			{
				XAttribute xa = run.Attribute("editable");
				if (xa != null)
				{
					m_errors.Add(String.Format(Strings.ksRemovingEditableAttribute,
						rt.Attribute("class")));
					run.SetAttributeValue("editable", null);
				}
				FixWsAttributeIfNeeded(run);
			}
			foreach (var auni in rt.Descendants("AUni"))
			{
				FixWsAttributeIfNeeded(auni);
			}
			foreach (var auni in rt.Descendants("AStr"))
			{
				FixWsAttributeIfNeeded(auni);
			}
			foreach (var auni in rt.Descendants("WsProp"))
			{
				FixWsAttributeIfNeeded(auni);
			}
			FixDuplicateWritingSystems(rt, guid, "AUni");
			FixDuplicateWritingSystems(rt,guid, "AStr");
			switch (className)
			{
				case "LangProject":
					foreach (var xeUni in rt.Descendants("Uni"))
					{
						Debug.Assert(xeUni.Parent != null);
						switch (xeUni.Parent.Name.LocalName)
						{
							case "AnalysisWss":
							case "CurAnalysisWss":
							case "VernWss":
							case "CurVernWss":
							case "CurPronunWss":
								FixWsList(xeUni);
								break;
						}
					}
					break;
				case "CmPossibilityList":
				case "CmBaseAnnotation":
				case "FsOpenFeature":
				case "ReversalIndex":
				case "ScrImportSource":
				case "ScrMarkerMapping":
				case "WordformLookupList":
					foreach (var xeUni in rt.Descendants("Uni"))
					{
						if (xeUni.Parent != null && xeUni.Parent.Name.LocalName == "WritingSystem")
						{
							var wsVal = xeUni.Value;
							var wsValNew = GetProperWs(wsVal);
							if (wsVal != wsValNew)
							{
								RecordWsChange(wsVal, wsValNew);
								xeUni.SetAttributeValue("ws", null);	// shouldn't have a ws attribute!
								xeUni.SetValue(wsValNew);
							}
						}
					}
					break;
				case "RnGenericRec":
					FixGenericDate("DateOfEvent", rt, className, guid);
					break;
				case "CmPerson":
					FixGenericDate("DateOfBirth", rt, className, guid);
					FixGenericDate("DateOfDeath", rt, className, guid);
					break;
			}
		}

		/// <summary>
		/// Fix any cases where a multistring has duplicate writing systems.
		/// </summary>
		/// <param name="eltName"></param>
		private void FixDuplicateWritingSystems(XElement rt, Guid guid, string eltName)
		{
			// Get all the alternatives of the given type.
			var alternatives = rt.Descendants(eltName);
			// group them by parent
			var groups = new Dictionary<XElement, List<XElement>>();
			foreach (var item in alternatives)
			{
				List<XElement> children;
				if (!groups.TryGetValue(item.Parent, out children))
				{
					children = new List<XElement>();
					groups[item.Parent] = children;
				}
				children.Add(item);
			}
			foreach (var kvp in groups)
			{
				var list = kvp.Value;
				list.Sort((x, y) => x.Attribute("ws").Value.CompareTo(y.Attribute("ws").Value));
				for (int i = 0; i < list.Count - 1; i++)
				{
					if (list[i].Attribute("ws").Value == list[i+1].Attribute("ws").Value)
					{
						m_errors.Add(string.Format(Strings.ksRemovingDuplicateAlternative, list[i + 1], kvp.Key.Name.LocalName, guid, list[i]));
						list[i+1].Remove();
						// Note that we did not remove it from the LIST, only from its parent.
						// It is still available to be compared to the NEXT item, which might also have the same WS.
					}
				}
			}
		}

		private void FixGenericDate(string fieldName, XElement rt, string className, Guid guid)
		{
			foreach (var xeGenDate in rt.Descendants(fieldName).ToList()) // ToList because we may modify things and mess up iterator.
			{
				var genDateAttr = xeGenDate.Attribute("val");
				if (genDateAttr == null)
					continue;
				var genDateStr = genDateAttr.Value;
				GenDate someDate;
				if (GenDate.TryParse(genDateStr, out someDate))
					continue; // all is well, valid GenDate
				xeGenDate.Remove();
				m_errors.Add(string.Format(Strings.ksRemovingGenericDate, genDateStr, fieldName, className, guid));
				// possible enhancement: take it apart like this and see whether swapping month and day makes it valid.
				//var ad = true;
				//if (genDateStr.StartsWith("-"))
				//{
				//    ad = false;
				//    genDateStr = genDateStr.Substring(1);
				//}
				//genDateStr = genDateStr.PadLeft(9, '0');
				//var year = Convert.ToInt32(genDateStr.Substring(0, 4));
				//var month = Convert.ToInt32(genDateStr.Substring(4, 2));
				//var day = Convert.ToInt32(genDateStr.Substring(6, 2));
				//var precision = (GenDate.PrecisionType)Convert.ToInt32(genDateStr.Substring(8, 1));
				//return new GenDate(precision, month, day, year, ad);

			}
		}

		private void FixWsList(XElement xeUni)
		{
			var wssValue = xeUni.Value;
			var wss = wssValue.Split(' ');
			var sb = new StringBuilder(xeUni.Value.Length + 10);
			for (var i = 0; i < wss.Length; ++i)
			{
				if (i > 0)
					sb.Append(" ");
				var wsValNew = GetProperWs(wss[i]);
				if (wsValNew != wss[i])
					RecordWsChange(wss[i], wsValNew);
				sb.Append(GetProperWs(wss[i]));
			}
			var wssValueNew = sb.ToString();
			if (wssValue != wssValueNew)
				xeUni.SetValue(sb.ToString());
		}

		static string GetProperWs(string wsVal)
		{
			if (wsVal.StartsWith("cmn"))
			{
				if (wsVal == "cmn")
					return "zh-CN";
				var wsValNew = wsVal.Remove(0, 3).Insert(0, "zh");
				if (!wsValNew.ToLowerInvariant().Contains("-cn-") &&
					!wsValNew.ToLowerInvariant().EndsWith("-cn"))
				{
					var parts = wsValNew.Split('-');
					var sb = new StringBuilder(wsValNew.Length + 5);	// +5 is overkill...
					sb.Append(parts[0]);
					var fNeedRegion = true;
					var fHaveScript = false;
					for (var i = 1; i < parts.Length; ++i)
					{
						sb.Append("-");
						if (i == 1)
						{
							if (parts[i].Length == 4)
							{
								sb.Append(parts[i]); // Script
								fHaveScript = true;
								if (parts.Length == 2)
									sb.Append("-CN");
							}
							else if (parts[i].Length == 2)
							{
								sb.Append(parts[i]); // Region
								fNeedRegion = false;
							}
							else
							{
								sb.Append("CN-");
								fNeedRegion = false;
								sb.Append(parts[i]);
							}
						}
						else
						{
							if (fHaveScript && i == 2 && parts[i].Length == 2)
								fNeedRegion = false;
							if (fNeedRegion)
							{
								sb.Append("CN-");
								fNeedRegion = false;
							}
							sb.Append(parts[i]);
						}
					}
					wsValNew = sb.ToString();
				}
				return wsValNew;
			}
			if (wsVal.StartsWith("pes"))
				return wsVal.Remove(0, 3).Insert(0, "fa");
			if (wsVal.StartsWith("zlm"))
				return wsVal.Remove(0, 3).Insert(0, "ms");
			if (wsVal.StartsWith("arb"))
				return wsVal.Remove(2, 1);	// change arb to ar
			return wsVal;
		}

		private void FixWsAttributeIfNeeded(XElement xe)
		{
			var xa = xe.Attribute("ws");
			if (xa != null)
			{
				var wsVal = xa.Value;
				var wsValNew = GetProperWs(wsVal);
				if (wsValNew != wsVal)
				{
					RecordWsChange(wsVal, wsValNew);
					xe.SetAttributeValue("ws", wsValNew);
				}
			}
		}

		private readonly Dictionary<string, int> m_dictWsChanges = new Dictionary<string, int>();
		private void RecordWsChange(string oldWs, string newWs)
		{
			var key = String.Format("{0}\t{1}", oldWs, newWs);
			int count;
			if (!m_dictWsChanges.TryGetValue(key, out count))
			{
				count = 0;
				m_dictWsChanges.Add(key, count);
			}
			m_dictWsChanges[key] = count + 1;
		}

		/// <summary>
		/// Get the list of error messages.
		/// </summary>
		public List<string> Errors
		{
			get
			{
				foreach (var key in m_dictWsChanges.Keys)
				{
					var wses = key.Split('\t');
					var msg = String.Format("Changed the writing system tag {0} to {1} {2} times.",
						wses[0], wses[1], m_dictWsChanges[key]);
					m_errors.Add(msg);
				}
				m_dictWsChanges.Clear();
				return m_errors;
			}
		}

		/// <summary>
		/// Get the pathname of the log file containing all the error messages.
		/// </summary>
		public string LogFile
		{
			get { return m_logfile; }
		}
	}
}