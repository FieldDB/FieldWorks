﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Implements the MSBuild task Substitute. This task copies a template file to an output file,
	/// replacing a few well-known symbols with computed values and optionally replacing others
	/// with ones specified in a Symbol file.
	/// The well-known symbols are $YEAR, $MONTH, $DAY, $NUMBEROFDAY, $GENERATEDFILECOMMENT.
	/// $GENERATEDFILECOMMENT will be replaced with the text "This file is generated from {0}. Do
	/// NOT modify!" and can be used to add the text to a different type of comment, e.g. an
	/// XML comment, or at a different place in the file. If $GENERATEDFILECOMMENT is not
	/// specified a C-style comment with the same text will be added at the top of the file.
	/// </summary>
	public class Substitute : Task
	{
		/// <summary>
		/// The name of the template file, in which substitutions will be made to produce the output.
		/// </summary>
		[Required]
		public string Template { get; set; }

		/// <summary>
		/// Symbol file, typically src/MasterVersionInfo.txt. Contains lines each with a pair
		/// Name=Value, e.g., FWMAJOR=7.
		/// Used to implement the substitutions $!(Name) and $!{Name:Default}.
		/// If the specified Name occurs on the LHS in the Symbols file, it will be replaced with
		/// the RHS. If not, $(Name) will replace it with nothing, and ${Name:Default} with the default.
		/// </summary>
		public string Symbols { get; set; }

		/// <summary>
		/// The name of the output file to be produced.
		/// </summary>
		[Required]
		public string Output { get; set; }


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the generation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{

			var substitutions = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(Symbols))
			{
				if (!File.Exists(Symbols))
				{
					Log.LogMessage(MessageImportance.High, "Symbol file " + Symbols + " not found");
					return false;
				}
				StreamReader reader = new StreamReader(Symbols);
				int lineNumber = 1;
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					// Ignore empty lines, comments, or if we somehow get a null at the end.
					if (line == null || line.Trim().Length == 0 || line.StartsWith("//"))
						continue;
					var items = line.Split('=');
					if (items.Length != 2 || items[0].Trim().Length == 0)
					{
						Log.LogMessage(MessageImportance.High, "Invalid symbol file: " + Symbols + " line " + lineNumber + " should be Name=Value");
						return false;
					}
					lineNumber++;
					substitutions[items[0].Trim()] = items[1].Trim();
				}
			}

			string template = Template;
			bool addedComment = false;
			try
			{
				StreamReader stream = new StreamReader(template);
				string fileContents = stream.ReadToEnd();
				stream.Close();

				Regex regex = new Regex("\\$YEAR");
				fileContents = regex.Replace(fileContents, DateTime.Now.Year.ToString());

				regex = new Regex("\\$MONTH");
				fileContents = regex.Replace(fileContents, string.Format("{0:MM}", DateTime.Now));

				regex = new Regex("\\$DAY");
				fileContents = regex.Replace(fileContents, string.Format("{0:dd}", DateTime.Now));

				regex = new Regex("\\$NUMBEROFDAYS");
				fileContents = regex.Replace(fileContents,
					Convert.ToInt32(Math.Truncate(DateTime.Now.ToOADate())).ToString());

				regex = new Regex("\\$GENERATEDFILECOMMENT");
				if (regex.IsMatch(fileContents))
				{
					fileContents = regex.Replace(fileContents,
						string.Format("This file is generated from {0}. Do NOT modify!",
						Path.GetFileName(template)));
					addedComment = true;
				}

				regex = new Regex("\\$!((?<env>\\w+)|\\{(?<env>\\w+):(?<default>\\w+)\\})");
				Match match = regex.Match(fileContents);
				while (match.Success)
				{
					string strEnv = match.Result("${env}");
					string strDefault = match.Result("${default}");
					string strEnvValue;
					substitutions.TryGetValue(strEnv, out strEnvValue);
					if (strEnvValue != null && strEnvValue != string.Empty)
						fileContents = regex.Replace(fileContents, strEnvValue, 1, match.Index);
					else
						fileContents = regex.Replace(fileContents, strDefault, 1, match.Index);

					match = regex.Match(fileContents);
				}

				if (!addedComment)
				{
					fileContents = string.Format("// This file is generated from {0}. Do NOT modify!\n{1}",
						Path.GetFileName(template), fileContents);
				}

				// Don't write the output file if it hasn't changed from a previous run.
				if (File.Exists(Output))
				{
					string oldFileContents = File.ReadAllText(Output);
					if (fileContents == oldFileContents)
					{
						Log.LogMessage(MessageImportance.Low, "Skipping generating {0} from {1} because it hasn't changed.", Output, Path.GetFileName(template));
						return true;
					}
				}
				using (MemoryStream memoryStream = new MemoryStream())
				{
					StreamWriter outStream = new StreamWriter(memoryStream);
					outStream.Write(fileContents);

					outStream.Close();

					Log.LogMessage(MessageImportance.Low, "Generating {0} from {1}", Output,
							Path.GetFileName(template));

					StreamWriter outFileStream = new StreamWriter(Output);
						outFileStream.Write(fileContents);
						outFileStream.Close();
				}
			}
			catch (Exception e)
			{
				Log.LogMessage(MessageImportance.High, "Generating {0} from {1} threw an exception {2}", Output,
						template, e.Message);
				return false;
			}
			return true;
		}
	}
}
