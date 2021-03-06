﻿// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwVersionInfoProvider.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for getting version information out of a FieldWorks assembly
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwVersionInfoProvider
	{
		/// <summary>Default copyright string if no assembly could be found</summary>
		public const string kDefaultCopyrightString = "(C) 2002-2012, SIL International.";
		/// <summary>Copyright string to use in sensitive areas (i.e. when m_fShowSILInfo is
		/// true)</summary>
		public const string kSensitiveCopyrightString = "(C) 2002-2012";

		private readonly Assembly m_assembly;
		private readonly bool m_fShowSILInfo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwVersionInfoProvider"/> class.
		/// </summary>
		/// <param name="assembly">The assembly used to get the information.</param>
		/// <param name="fShowSILInfo">if set to <c>false</c>, any SIL-identifying information
		/// will be hidden.</param>
		/// ------------------------------------------------------------------------------------
		public FwVersionInfoProvider(Assembly assembly, bool fShowSILInfo)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			m_assembly = assembly;
			m_fShowSILInfo = fShowSILInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the product.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProductName
		{
			get
			{
				object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				return (attributes != null && attributes.Length > 0) ?
					((AssemblyTitleAttribute)attributes[0]).Title : Application.ProductName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ApplicationVersion
		{
			get
			{
				// Set the application version text
				object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				string appVersion = (attributes != null && attributes.Length > 0) ?
					((AssemblyFileVersionAttribute)attributes[0]).Version :
					Application.ProductVersion;
				// Extract the fourth (and final) field of the version to get a date value.
				int ich = 0;
				for (int i = 0; i < 3; i++)
					ich = appVersion.IndexOf('.', ich + 1);
				string productDate = string.Empty;
				if (ich >= 0)
				{
					int iDate = Convert.ToInt32(appVersion.Substring(ich + 1));
					if (iDate > 0)
					{
						DateTime dt = DateTime.FromOADate(iDate);
						productDate = dt.ToString("yyyy/MM/dd");
					}
				}
#if DEBUG
				return string.Format(FwUtilsStrings.kstidAppVersionFmt, appVersion, productDate, "(Debug version)");
#else
				return string.Format(FwUtilsStrings.kstidAppVersionFmt, appVersion, productDate, "");
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of FieldWorks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FieldWorksVersion
		{
			get
			{
				// Set the Fieldworks version text
				object[] attributes = m_assembly.GetCustomAttributes(
					typeof(AssemblyInformationalVersionAttribute), false);
				string fwVersion = (attributes != null && attributes.Length > 0) ?
					((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion :
					Application.ProductVersion;
				return string.Format(FwUtilsStrings.kstidFwVersionFmt, fwVersion);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string containing the SIL copyright.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CopyrightString
		{
			get
			{
				// Get copyright information from assembly info. By doing this we don't have
				// to update the splash screen each year.
				string copyRight;
				if (!m_fShowSILInfo)
					copyRight = kSensitiveCopyrightString;
				else
				{
					object[] attributes = m_assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
					if (attributes != null && attributes.Length > 0)
						copyRight = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
					else
					{
						// if we can't find it in the assembly info, use generic one (which
						// might be out of date)
						copyRight = kDefaultCopyrightString;
					}
				}
				return string.Format(FwUtilsStrings.kstidCopyrightFmt, copyRight.Replace("(C)", "©"));
			}
		}
	}
}
