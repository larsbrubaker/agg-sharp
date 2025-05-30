﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MatterHackers.Localizations
{
	[DebuggerStepThrough]
	public static class TranslationMapExtensions
	{
		public static string Localize(this string englishString)
		{
			if (TranslationMap.ActiveTranslationMap != null)
			{
				return TranslationMap.ActiveTranslationMap.Translate(englishString);
			}

			return englishString;
		}

		public static string Stars(this string englishString)
		{
			return "*" + englishString + "*";
		}
	}

	public class TranslationMap
	{
		private const string englishTag = "English:";
		private const string translatedTag = "Translated:";

		private Dictionary<string, string> machineTranslation = new Dictionary<string, string>();
		private Dictionary<string, string> humanTranslation = new Dictionary<string, string>();

		public static TranslationMap ActiveTranslationMap { get; set; }

		private string twoLetterIsoLanguageName;

		public TranslationMap(string twoLetterIsoLanguageName, string savePath, string sourcePath)
		{
			this.twoLetterIsoLanguageName = twoLetterIsoLanguageName;
            this.SavePath = savePath;
            this.SourceFilesPath = sourcePath;
        }

		public TranslationMap(StreamReader machineTranslation, StreamReader humanTranslation, string twoLetterIsoLanguageName, string savePath, string sourcePath)
			: this(twoLetterIsoLanguageName, savePath, sourcePath)
        {
			this.machineTranslation = ReadIntoDictionary(machineTranslation);
			this.humanTranslation = ReadIntoDictionary(humanTranslation);
		}

		private static Regex findLocalizedText = new Regex(@"\""(?:[^\""\\]|\\.)*\"".Localize\(\)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public virtual string Translate(string englishString)
        {
            // Skip dictionary lookups for English
#if DEBUG
            if (englishString == null)
            {
                return englishString;
            }
#else
			if (twoLetterIsoLanguageName == "en"
				|| englishString == null)
			{
				return englishString;
			}
#endif

            string humanTranslatedString = null;
            if (humanTranslation?.TryGetValue(englishString, out humanTranslatedString) == true)
            {
                if (englishString != humanTranslatedString)
                {
                    return humanTranslatedString;
                }
            }

            SearchCodeForTranslations();

            // Perform the lookup to the translation table
            if (!machineTranslation.TryGetValue(englishString, out string machineTranslatedString))
            {
#if DEBUG
                if (twoLetterIsoLanguageName == "en")
                {
					// don't have string that end in spaces or have lind=e feeds
                    if (englishString[englishString.Length - 1] == ' '
						|| englishString.Contains('\r'))
                    {
                        throw new Exception("Translation strings should not have a trailing space");
                    }

					AddNewString(englishString);
                }
#endif
                if (twoLetterIsoLanguageName == "l10n"
                    && englishString.Length > 0)
                {
                    var firstChar = 'a';
                    foreach (var c in englishString)
                    {
                        if ((c >= 'a' && c <= 'z')
                            || (c >= 'A' && c <= 'Z'))
                        {
                            firstChar = c;
                            break;
                        }
                    }
                    var newString = "";
                    foreach (var c in englishString)
                    {
                        if ((c >= 'a' && c <= 'z')
                            || (c >= 'A' && c <= 'Z'))
                        {
                            newString += firstChar;
                        }
                        else
                        {
                            newString += c;
                        }
                    }

                    return newString;
                }

                // Use English string if no mapping found
                return englishString;
            }

            return machineTranslatedString;
        }

        private void SearchCodeForTranslations()
        {
			return;
			// make sure we have not run already and that we have a translation file loaded
            if (!haveParsedSourceCode
                && this.machineTranslation != null)
            {
                var masterTranslationFile = Path.Combine(SavePath, "Master.txt");
				var fileInfo = new FileInfo(masterTranslationFile);

				// only build if we are more than 10 days out of date
                if (fileInfo.LastWriteTime.AddDays(10) < DateTime.UtcNow
					&& Directory.Exists(SourceFilesPath))
                {
                    // get a list of every .cs file
                    var sourceFilesDirectory = new DirectoryInfo(SourceFilesPath);

                    foreach (FileInfo file in sourceFilesDirectory.GetFiles("*.cs", SearchOption.AllDirectories))
                    {
                        var fileContent = File.ReadAllText(file.FullName);
                        var matches = findLocalizedText.Matches(fileContent);
                        for (int i = 0; i < matches.Count; i++)
                        {
                            var value = matches[i].Value;
                            var withoutQuotes = value.Substring(1, value.Length - 2 - ".Localize()".Length);
                            if (!withoutQuotes.Contains('\n'))
                            {
                                AddNewString(withoutQuotes);
                            }
                        }
                    }

					// Just in case we did not add any new strings set the last write time to now
					File.SetLastWriteTimeUtc(masterTranslationFile, DateTime.UtcNow);
				}

                haveParsedSourceCode = true;
            }
        }

        /// <summary>
        /// Encodes for saving, escaping newlines
        /// </summary>
        private string EncodeForSaving(string stringToEncode)
		{
			return stringToEncode.Replace("\n", "\\n");
		}

		private object locker = new object();
        private static bool haveParsedSourceCode;

		private string SavePath;
		private string SourceFilesPath;

		private void AddNewString(string englishString)
		{
			lock (locker)
			{
				if (!machineTranslation.ContainsKey(englishString))
				{
					machineTranslation.Add(englishString, englishString);

					if (!Directory.Exists(SavePath))
					{
						Directory.CreateDirectory(SavePath);
					}

					var newFile = Path.Combine(SavePath, "Master_new.txt");
					// save content to new file
					using (var masterFileStream = File.CreateText(newFile))
					{
						foreach(var kvp in machineTranslation.OrderBy(k => k.Key))
                        {
							masterFileStream.WriteLine("{0}{1}", englishTag, EncodeForSaving(kvp.Key));
							masterFileStream.WriteLine("{0}{1}", translatedTag, EncodeForSaving(kvp.Key));
							masterFileStream.WriteLine("");
						}
					}

					// delete the old file
					var oldFile = Path.Combine(SavePath, "Master.txt");
					File.Delete(oldFile);

					// rename the new file
					File.Move(newFile, oldFile);
				}
			}
		}

		public static void AssertDebugNotDefined()
		{
#if DEBUG
			throw new Exception("DEBUG is defined and should not be!");
#endif
		}

		protected Dictionary<string, string> ReadIntoDictionary(StreamReader streamReader)
		{
			if (streamReader == null)
            {
				return null;
            }

			var dictionary = new Dictionary<string, string>();

			bool lookingForEnglish = true;
			string englishString = "";

			string line;

			int i = 0;
			while ((line = streamReader.ReadLine()?.Trim()) != null)
			{
				if (line.Length == 0)
				{
					// we are happy to skip blank lines
					continue;
				}

				if (lookingForEnglish)
				{
					if (line.Length < englishTag.Length || !line.StartsWith(englishTag))
					{
						throw new Exception(string.Format("Found unknown string at line {0}. Looking for {1}.", i, englishTag));
					}
					else
					{
						englishString = line.Substring(englishTag.Length);
						lookingForEnglish = false;
					}
				}
				else
				{
					if (line.Length < translatedTag.Length || !line.StartsWith(translatedTag))
					{
						throw new Exception(string.Format("Found unknown string at line {0}. Looking for {1}.", i, translatedTag));
					}
					else
					{
						string translatedString = line.Substring(translatedTag.Length);
						// store the string
						if (!dictionary.ContainsKey(DecodeWhileReading(englishString)))
						{
							dictionary.Add(
								DecodeWhileReading(englishString),
								DecodeWhileReading(translatedString));
						}
						// go back to looking for English
						lookingForEnglish = true;
					}
				}

				i += 1;
			}

			return dictionary;
		}

		/// <summary>
		/// Decodes while reading, unescaping newlines
		/// </summary>
		private string DecodeWhileReading(string stringToDecode)
		{
			return stringToDecode.Replace("\\n", "\n");
		}
	}
}
