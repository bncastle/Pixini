// Copyright (c) 2015 Bryan Castleberry - Pixelbyte Studios
//
// Pixini version 0.1 - http://pixelbytestudios.com
//                      http://github.com/bncastle/pixini
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Pixelbyte
{
    /// <summary>
    /// As far as Pixini is concerned, each line of text
    //  that it parses is one of these
    /// </summary>
    public enum LineType : byte
    {
        None,
        Comment,
        KeyValue,
        Section,
    };

    /// <summary>
    /// This struct contains info about a single ini line
    /// </summary>
    public struct IniLine
    {
        public LineType type;
        public string section;
        public string comment;
        public string key;
        public string value;

        /// <summary>
        /// This is for comma separated values. If Pixini detects a valid CSV, it
        /// will separate them and stick them in this array. Note that while the 
        /// full unseparated value will still be available in the value field,
        /// this will be the field that is used when the ini data is output.
        /// </summary>
        public string[] array;

        /// <summary>
        /// if this is != 0, then the value was quoted with this char
        /// </summary>
        public char quotechar;
    }

    /// <summary>
    /// This class contains string extensions that Pixini uses
    /// </summary>
    static class StringExtensions
    {
        /// <summary>
        /// Checks to see if the string is just whitespace
        /// Note: A similar version method exists in later .NET versions (IsNullOrWhiteSpace)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsWhiteSpace(this string input)
        {
            //check for any Non-whitespace characters in the string
            for (int i = 0; i < input.Length; i++)
            {
                if (!char.IsWhiteSpace(input[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Figures out how many times the given character occurs in the given string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="c"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public static int CountChars(this string text, char c, int startIndex = 0, int endIndex = -1)
        {
            int cnt = 0;
            if (endIndex == -1) endIndex = text.Length;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (text[i] == c) cnt++;
            }
            return cnt;
        }
    }

    /// <summary>
    /// Pixini works with ini data. It knows how to load, change values and save ini data
    /// It is designed to work with .NET 3.5 (mono 2.0). Also no Regex objects are used.
    /// Notes:
    /// Leading a trailing spaces around values or key names are removed. If you want the spaces, enclose the string in double or single quotes
    /// </summary>
    public class Pixini
    {
        public const string VERSION = "0.1";

        /// <summary>
        /// If a key/value is not under a specific section, it goes in the default section
        /// </summary>
        const string DEFAULT_SECTION = "default";

        /// <summary>
        /// This just holds the default section lower-cased. we need this so we can do
        /// case-insensitive compares but still keep the case
        /// </summary>
        static string defaultSectionLowerCased;

        /// <summary>
        /// This is the separator character that Pixini will use when looking for a Key/Value Pair.
        /// Change this to your liking... 
        /// </summary>
        public char inputKVSeparator = '=';

        /// <summary>
        /// This is the separator character that Pixini will use when outputting a Key/Value Pair
        /// According to the specs, it is supposed to be the '=' sign but it is changeable here for your pleasure
        /// </summary>
        public char outputKVSeparator = '=';

        /// <summary>
        /// Tells us what line number the parser is on
        /// </summary>
        int lineNumber = 1;

        /// <summary>
        /// This dictionary Holds all the sections found in the ini file and is 
        /// also used to hold newly-constructed sections
        /// </summary>
        Dictionary<string, List<IniLine>> sections;


        /// <summary>
        /// Contains the structure of the sections of the ini file. Which sections are in what order
        ///  We use this to reconstruct the ini file in its approximate order
        /// </summary>
        List<string> structureOrder;

        /// <summary>
        /// Tells is which section of the ini file we are on
        /// </summary>
        string currentSection;

        #region Output Formatting Options
        /// <summary>
        /// Adds an empty line between the end of one section and the beginning of another
        /// </summary>
        public bool emptyLinesBetweenSections = true;

        /// <summary>
        /// Puts an empty line above a comment when it proceeds a key value
        /// </summary>
        public bool emptyLineAboveComments = true;

        /// <summary>
        /// Puts empty lines between all KeyValuePairs
        /// </summary>
        public bool emptyLinesBetweenKeyValuePairs = false;

        /// <summary>
        /// If true, then spaces are inserted between the = sign of a key value pair
        /// </summary>
        //public bool spaceBetweenEquals = false;
        #endregion

        #region Properties

        /// <summary>
        /// Gets/sets a key for the given section name
        /// If attempting to set a key that does not exist, it is created
        /// </summary>
        /// <param name="sectionName">The section from which to get/set the key</param>
        /// <param name="key">The key to get/set</param>
        /// <returns>the value string or null if not found</returns>
        public string this[string key, string sectionName = DEFAULT_SECTION]
        {
            get
            {
                IniLine iniLine;
                if (!GetLineInfo(key, sectionName, out iniLine))
                    return null;
                return iniLine.value;
            }

            set
            {
                List<IniLine> section;

                //Section names and key names are case insensitive 
                var sectionNameLowerCase = sectionName.ToLower();
                var keyLowerCase = key.ToLower();

                if (sections.TryGetValue(sectionNameLowerCase, out section))
                {
                    IniLine iniLine;
                    int index = -1;
                    //Search through the list to find the key if it exists
                    for (int i = 0; i < section.Count; i++)
                    {
                        if (section[i].type != LineType.KeyValue) continue;

                        if (section[i].key.ToLower() == keyLowerCase)
                        {
                            index = i;
                            iniLine = section[i];
                        }
                    }

                    if (index > -1)
                    {
                        iniLine = section[index];

                        IniLine info;
                        //If ParseValue returns false just set it = value
                        if (ParseValue(value, 0, out info))
                        {
                            iniLine.value = info.value;
                            iniLine.quotechar = info.quotechar;
                            iniLine.array = info.array;
                        }
                        else
                        {
                            iniLine.value = value;
                            iniLine.array = null;
                        }
                        section[index] = iniLine;
                    }
                    else
                    {
                        IniLine info;
                        if (!ParseValue(value, 0, out info))
                            section.Add(new IniLine() { type = LineType.KeyValue, section = sectionName, key = key, value = value, quotechar = '\0' });
                        else
                        {
                            info.section = sectionName;
                            info.key = key;
                            section.Add(info);
                        }
                    }
                }
                else
                {
                    sections[sectionName] = new List<IniLine>();

                    //Add the new Section Name if needed
                    AddIniLine(new IniLine() { type = LineType.Section, section = sectionName });

                    //Set the current section to the new one we just created
                    IniLine info;
                    if (!ParseValue(value, 0, out info))
                        AddIniLine(new IniLine() { type = LineType.KeyValue, section = sectionName, key = key, value = value });
                    else
                    {
                        info.section = sectionName;
                        info.key = key;
                        AddIniLine(info);
                    }
                }
            }
        }
        #endregion

        static Pixini()
        {
            defaultSectionLowerCased = DEFAULT_SECTION.ToLower();
        }

        public Pixini()
        {
            Init();
        }

        void Init()
        {
            structureOrder = new List<string>();
            sections = new Dictionary<string, List<IniLine>>();
            lineNumber = 1;

            //Add a default section
            currentSection = DEFAULT_SECTION;
        }

        #region I/O
        /// <summary>
        /// Loads and parses an existing ini file given the filename
        /// </summary>
        /// <param name="filename">The name of an existing ini file</param>
        /// <returns></returns>
        public static Pixini Load(string filename)
        {
            var ini = new Pixini();

            using (var sr = new StreamReader(filename))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    //Parse the line
                    ini.Parse(line.Trim());
                }
            }
            //Do any necessary post-processing after all the file is loaded and parsed
            ini.PostProcess();

            return ini;
        }

        /// <summary>
        /// Loads and parses an ini file from a string
        /// </summary>
        /// <param name="text">The string containing the ini text</param>
        /// <returns></returns>
        public static Pixini LoadFromString(string text)
        {
            var ini = new Pixini();

            using (var sr = new StringReader(text))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    //Parse the line
                    ini.Parse(line.Trim());
                }
            }

            //Do any necessary post-processing after all the file is loaded and parsed
            ini.PostProcess();

            return ini;
        }

        /// <summary>
        /// Saves the ini info to the given filename
        /// </summary>
        /// <param name="filename">The name of a file to which the ini data will be saved</param>
        /// <param name="saveBackupOfPrevious">If true and a previous file with the same name existsm it will be copied to filename.bak </param>
        public void Save(string filename, bool saveBackupOfPrevious = false)
        {
            //Save a backup if 
            if (saveBackupOfPrevious && File.Exists(filename))
            {
                File.Copy(filename, Path.GetFileNameWithoutExtension(filename) + ".bak");
            }

            //Make sure we handle the default section case
            HandleDefaultSection();

            using (var sw = new StreamWriter(filename))
            {
                //Write out all the lines to the given filename
                var enumerator = Lines();

                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                        sw.WriteLine(enumerator.Current);
                }
            }
        }
        #endregion

        /// <summary>
        /// Removes the given key from the given section
        /// </summary>
        /// <param name="sectionName">Name of the desired section</param>
        /// <param name="key">Name of the key to be removed</param>
        /// <returns>True if the key was found and removed, false if it was not found</returns>
        public bool Delete(string key, string sectionName)
        {
            //Section names and key names are case insensitive 
            sectionName = sectionName.ToLower();
            key = key.ToLower();

            List<IniLine> section;
            int index = -1;
            if (sections.TryGetValue(sectionName, out section))
            {
                //Search through the list to find the key if it exists
                for (int i = 0; i < section.Count; i++)
                {
                    if (section[i].type != LineType.KeyValue) continue;

                    if (section[i].key.ToLower() == key)
                    {
                        index = i;
                    }
                }
                if (index > -1)
                {
                    //remove the item from our list
                    section.RemoveAt(index);

                    //TOOD: This might not be a desired behavior
                    //See if we can delete the whole section
                    if (!IniListContainstype(section, LineType.KeyValue))
                    {
                        section.Clear();
                        sections[sectionName] = null;
                        for (int i = structureOrder.Count - 1; i >= 0; i--)
                        {
                            if (structureOrder[i] == sectionName)
                            {
                                structureOrder.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Given a section name, deletes the whole section if it is found
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns>true if section was deleted, false otherwise</returns>
        public bool DeleteSection(string sectionName)
        {
            List<IniLine> section;
            //Section names are case insensitive 
            sectionName = sectionName.ToLower();

            if (sections.TryGetValue(sectionName, out section))
            {
                section.Clear();
                sections[sectionName] = null;
                for (int i = structureOrder.Count - 1; i >= 0; i--)
                {
                    if (structureOrder[i] == sectionName)
                    {
                        structureOrder.RemoveAt(i);
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the given key from the default section
        /// </summary>
        /// <param name="key">Name of the key to be removed</param>
        /// <returns>True if the key was found and removed, false if it was not found</returns>
        public bool Delete(string key)
        {
            return Delete(DEFAULT_SECTION, key);
        }

        public void PostProcess()
        {
            HandleDefaultSection();
        }

        /// <summary>
        /// This method handles naming the default section and ordering the section header to be where it should be
        /// </summary>
        private void HandleDefaultSection()
        {
            //Here we look for the default section. If we don't find one, no worries
            List<IniLine> defsection;
            if (sections.TryGetValue(defaultSectionLowerCased, out defsection))
            {
                //If there is a section header in here and it is NOT the first item in the list, move it to BEFORE the first key value pair
                //This lets us support comments before the first section header
                if (defsection[0].type != LineType.Section && IniListContainstype(defsection, LineType.Section) && defsection.Count > 1)
                {
                    //Remove the previous default section header
                    for (int i = defsection.Count - 1; i >= 0; i--)
                    {
                        if (defsection[i].type == LineType.Section)
                        {
                            defsection.RemoveAt(i);
                            break;
                        }
                    }

                    //Now move it to just after the first couple of comments
                    int insertIndex = -1;
                    for (int i = 0; i < defsection.Count; i++)
                    {
                        if (defsection[i].type != LineType.Comment && defsection[i].type != LineType.Section)
                        {
                            insertIndex = i;
                            break;
                        }
                    }
                    if (insertIndex != -1)
                    {
                        defsection.Insert(insertIndex, new IniLine() { type = LineType.Section, section = DEFAULT_SECTION });
                    }
                }
            }
        }

        /// <summary>
        /// Given a Key and a section name, this method returns the corresponding IniLine object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sectionName"></param>
        /// <returns>true if the IniLine was found, false otherwise</returns>
        bool GetLineInfo(string key, string sectionName, out IniLine info)
        {
            //Section names and key names are case insensitive 
            sectionName = sectionName.ToLower();
            key = key.ToLower();

            List<IniLine> section;
            if (sections.TryGetValue(sectionName, out section))
            {
                //Search through the list to find the key if it exists
                for (int i = 0; i < section.Count; i++)
                {
                    if (section[i].type != LineType.KeyValue) continue;

                    if (section[i].key.ToLower() == key)
                    {
                        info = section[i];
                        return true;
                    }
                }
            }
            info = new IniLine() { type = LineType.None };
            return false;
        }

        /// <summary>
        /// Gets the section list for the given section name
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns>The section list or null if it is not found</returns>
        List<IniLine> GetSectionList(string sectionName)
        {
            //Section names are case insensitive 
            sectionName = sectionName.ToLower();

            List<IniLine> section;
            if (sections.TryGetValue(sectionName, out section))
                return section;
            else
                return null;
        }

        /// <summary>
        /// Gets the index for the given key and the given section List
        /// </summary>
        /// <param name="key"></param>
        /// <param name="section"></param>
        /// <returns>The index for the IniLine that contains the key, null otherwise</returns>
        int GetKeyIndex(string key, List<IniLine> section)
        {
            if (section == null) return -1;

            //keys are case insensitive 
            key = key.ToLower();

            //Search through the list to find the key if it exists
            for (int i = 0; i < section.Count; i++)
            {
                if (section[i].type != LineType.KeyValue) continue;

                if (section[i].key.ToLower() == key)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Replaces an existing iniLine with the new one given that
        /// the key name is still the same. If it is not , then this will do nothing
        /// </summary>
        /// <param name="newIniLine"></param>
        /// <returns></returns>
        bool ModifyIniLine(IniLine newIniLine)
        {
            var sectionList = GetSectionList(newIniLine.section);
            if (sectionList == null) return false;

            int index = GetKeyIndex(newIniLine.key, sectionList);

            if (index > -1)
            {
                sectionList[index] = newIniLine;
                return true;
            }
            return false;
        }

        #region Getters

        public string Get(string key, string sectionName = DEFAULT_SECTION, string defaultVal = null)
        {
            string val = this[key, sectionName];
            if (string.IsNullOrEmpty(val))
                return defaultVal;
            else
                return val;
        }

        public float GetF(string key, string sectionName = DEFAULT_SECTION, float defaultVal = float.NaN)
        {
            string s = Get(key, sectionName);
            if (s == null)
                return defaultVal;
            float num;
            if (float.TryParse(s, out num)) return num;
            else return defaultVal;
        }

        public int GetI(string key, string sectionName = DEFAULT_SECTION, int defaultVal = int.MinValue)
        {
            string s = Get(key, sectionName);
            if (s == null)
                return defaultVal;
            int num;
            if (int.TryParse(s, out num)) return num;
            else return defaultVal;
        }

        public bool GetB(string key, string sectionName = DEFAULT_SECTION, bool defaultVal = false)
        {
            string s = Get(key, sectionName);
            if (s == null)
                return defaultVal;

            bool flag;
            if (Boolean.TryParse(s, out flag)) return flag;
            return defaultVal;
        }
        #endregion

        #region Setters
        public void Set<T>(string key, string sectionName, T val)
        {
            this[key, sectionName] = val.ToString();
        }

        public void Set<T>(string key, T val)
        {
            this[key, DEFAULT_SECTION] = val.ToString();
        }
        #endregion

        #region Array Getters

        /// <summary>
        /// Gets the array associated with this key in this section given that one exists
        /// Note: The array returned here can be DIRECTLY modified and the changes will show
        /// up when rendering the ini data. Be careful thought. If you want to change the size of the
        /// array, then you must use the ArrSet() method instead!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sectionName"></param>
        /// <returns>The array or null if it does not exist</returns>
        public string[] ArrGet(string key, string sectionName = DEFAULT_SECTION)
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return null;
            else
                return iniLine.array;
        }

        /// <summary>
        /// Gets the array associated with this key in this section given that one exists
        /// Notes: 
        /// This WILL throw a System.FormatException if the data is not in the correct format
        /// To change the array data, you must use the ArrSet<T() method.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sectionName"></param>
        /// <returns>The array or null if it does not exist</returns>
        public float[] ArrGetF(string key, string sectionName = DEFAULT_SECTION)
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return null;
            else
            {
                float[] arr = null;
                //try
                {
                    arr = iniLine.array.Select((str) => float.Parse(str)).ToArray();
                }
                //catch (Exception) //eat it
                //{
                //    arr = null;
                //}
                return arr;
            }
        }

        /// <summary>
        /// Gets the array associated with this key in this section given that one exists
        /// Notes: 
        /// This WILL throw a System.FormatException if the data is not in the correct format
        /// To change the array data, you must use the ArrSet<T() method.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sectionName"></param>
        /// <returns>The array or null if it does not exist</returns>
        public int[] ArrGetI(string key, string sectionName = DEFAULT_SECTION)
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return null;
            else
            {
                int[] arr = null;
                //try
                {
                    arr = iniLine.array.Select((str) => int.Parse(str)).ToArray();
                }
                //catch (Exception) //eat it
                //{
                //    arr = null;
                //}
                return arr;
            }
        }

        /// <summary>
        /// Gets the array associated with this key in this section given that one exists
        /// Notes: 
        /// This WILL throw a System.FormatException if the data is not in the correct format
        /// To change the array data, you must use the ArrSet<T() method.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sectionName"></param>
        /// <returns>The array or null if it does not exist</returns>
        public bool[] ArrGetB(string key, string sectionName = DEFAULT_SECTION)
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return null;
            else
            {
                bool[] arr = null;
                //try
                {
                    arr = iniLine.array.Select((str) => bool.Parse(str)).ToArray();
                }
                //catch (Exception) //eat it
                //{
                //    arr = null;
                //}
                return arr;
            }
        }

        #endregion

        #region Array Setters

        /// <summary>
        /// Sets an array on the given key in the given section
        /// </summary>
        /// <param name="key"></param>
        /// <param name="section"></param>
        /// <param name="vals"></param>
        /// <returns>true on success, false otherwise</returns>
        bool ArrSet(string key, string sectionName, params string[] vals)
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return false;
            else
            {
                iniLine.value = null;
                iniLine.array = vals;
                iniLine.quotechar = '\0';
                return true;
            }
        }

        bool ArrSet(string key, params string[] vals)
        {
            return ArrSet(key, DEFAULT_SECTION, vals);
        }

        /// <summary>
        /// Sets an array on the given key in the given section
        /// </summary>
        /// <param name="key"></param>
        /// <param name="section"></param>
        /// <param name="vals"></param>
        /// <returns>true on success, false otherwise</returns>
        public bool ArrSet<T>(string key, string sectionName, params T[] vals) where T : struct
        {
            IniLine iniLine;
            if (!GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
                return false;
            else
            {
                iniLine.value = null;
                iniLine.array = vals.Select((val) => val.ToString()).ToArray();
                iniLine.quotechar = '\0';

                //Since we are dealing with structs, we must replace the actual struct instance in the section list...
                ModifyIniLine(iniLine);
                return true;
            }
        }

        public bool ArrSet<T>(string key, params T[] vals) where T : struct
        {
            return ArrSet<T>(key, DEFAULT_SECTION, vals);
        }
        #endregion

        /// <summary>
        /// Returns the index of the first input key/value separator it finds, or -1
        /// </summary>
        /// <param name="txt"></param>
        /// <returns>Index of the first input KV separator in the string or -1 if not found</returns>
        int IndexOfKvSeparator(string txt)
        {
            return txt.IndexOf(inputKVSeparator);
        }

        /// <summary>
        /// Adds the given IniLine to its apropriate section list. If the section list does not exist, it is created.
        /// If the IniLine type is a section header, it will also add the section name to the structure order list.
        /// This allows comments that are placed before the first section to show up in the right place when output
        /// </summary>
        /// <param name="current"></param>
        void AddIniLine(IniLine current)
        {
            string sectionLowerCased = current.section.ToLower();
            //Only Sections are added to our structure
            //Otherwise we add them to our sections dictionary
            //The structure list allows us to keep the general structure of the file
            if (current.type == LineType.Section)
            {
                //Add it to our structure order list if it does not already exist
                if (!structureOrder.Contains(current.section.ToLower()))
                    structureOrder.Add(current.section.ToLower());
            }
            else if (sectionLowerCased == defaultSectionLowerCased)
            {
                //If the section of this inline matches our default section, we must add it to the struct list
                if (!structureOrder.Contains(current.section.ToLower()))
                    structureOrder.Add(defaultSectionLowerCased);
            }

            //Add this key value pair to the specified section List
            List<IniLine> section;

            //If the section List does not exist, create it
            if (!sections.TryGetValue(sectionLowerCased, out section))
            {
                //Ini files are supposed to be case insensitive so we lowercase all our keys
                //for lookup only
                sections[sectionLowerCased] = new List<IniLine>();
                section = sections[sectionLowerCased];
            }

            //Otherwise, if it is a section header, make sure that one is not already in this section list
            if (current.type != LineType.Section || (
                current.type == LineType.Section && !IniListContainstype(section, LineType.Section)))
            {
                section.Add(current);
            }
        }

        /// <summary>
        /// Given a List of IniLines, this method checks to see if there are
        /// any lines within it of the given type
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="type"></param>
        /// <returns>true if a line with the given type was found, false otherwise</returns>
        bool IniListContainstype(List<IniLine> lines, LineType type)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].type == type) return true;
            }
            return false;
        }

        #region Logging methods
        void LogWarning(string text, params object[] args)
        {
            //string input = string.Format("[line {0}] WARN: {1}", lineNumber, text);
            //Console.WriteLine(input, args);
        }

        void LogError(string text, params object[] args)
        {
            //string input = string.Format("[line {0}] ERR: {1}", lineNumber, text);
            //Console.WriteLine(input, args);
        }
        #endregion

        #region Parse Methods

        /// <summary>
        /// Parses the current line of text looking for ini config elements 
        /// </summary>
        /// <param name="line"></param>
        void Parse(string line)
        {
            IniLine current;

            //Is this line a valid comment?
            if (ParseLineComment(line, out current))
            {
                AddIniLine(current);
            }
            else if (ParseSection(line, out current))
            {
                AddIniLine(current);
            }
            else if (ParseKeyValue(line, out current))
            {
                AddIniLine(current);
            }
            lineNumber++;
        }

        /// <summary>
        /// Looks for a line comment in the given string and if it finds one, it returns an IniLine representation of it
        /// </summary>
        /// <param name="line"></param>
        /// <returns>An IniLine with the line comment info, null otherwise</returns>
        bool ParseLineComment(string line, out IniLine info)
        {
            ///start out with an empty iniLine object
            info = new IniLine();

            if (string.IsNullOrEmpty(line) || line.IsWhiteSpace() || line[0] != ';')
                return false;
            else
            {
                info = new IniLine() { type = LineType.Comment, section = currentSection, comment = line.Substring(1, line.Length - 1) };
                return true;
            }
        }

        /// <summary>
        /// Looks for a section header in the given string and if it finds one, it returns an IniLine representation of it
        /// </summary>
        /// <param name="line"></param>
        /// <returns>An IniLine with the section header info, null otherwise</returns>
        bool ParseSection(string line, out IniLine info)
        {
            ///start out with an empty iniLine object
            info = new IniLine();

            if (string.IsNullOrEmpty(line) || line.IsWhiteSpace() || line[0] != '[' || line.IndexOf(']') == -1) return false;

            StringBuilder sectionName = new StringBuilder();

            for (int i = 1; i < line.Length; i++)
            {
                if (line[i] == ']') break;

                //Technically ini section names are not supposed to contain spaces, but we can handle it just fine so allow it
                //else if (char.IsWhiteSpace(line[i]))
                //{
                //    //TODO: Inidicate which line we are on and what we did
                //    LogWarning("Section names are not supposed to have spaces, but I can handle it.");
                //}

                sectionName.Append(line[i]);
            }

            if (sectionName.Length == 0)
                return false;
            else
            {
                //Is there a comment Inline with this section?
                string comment = null;
                if (line.IndexOf(';') != -1 && line.IndexOf(';') < line.Length - 1)
                {
                    comment = line.Substring(line.IndexOf(';') + 1, (line.Length - 1) - (line.IndexOf(';')));

                    //Is the comment empty?
                    if (string.IsNullOrEmpty(comment) || comment.IsWhiteSpace()) comment = null;
                }

                currentSection = sectionName.ToString();

                info = new IniLine() { type = LineType.Section, section = sectionName.ToString(), comment = comment };
                return true;
            }
        }

        /// <summary>
        /// This looks for a value, double or single quoted, or no quotes and
        /// optionally an inline comment and returns this info in an IniLine object
        /// It the value is an array, it is converted and returned also
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <returns>true on success, false otherwise</returns>
        bool ParseValue(string input, int startIndex, out IniLine info)
        {
            char quoteChar = '\0';

            if (startIndex >= input.Length)
            {
                info = new IniLine();
                return false;
            }

            //Zip past any leading whitespace
            while (Char.IsWhiteSpace(input[startIndex]) && startIndex < input.Length) startIndex++;

            //Look for a quoted string
            int numQuotes = input.CountChars('"', startIndex);

            //Ignore a quote with no matching end
            if (numQuotes < 2)
            {
                //Try single quotes
                numQuotes = input.CountChars('\'', startIndex);

                if (numQuotes < 2)
                    numQuotes = -1;
            }
            int endIndex = -1;

            StringBuilder val = new StringBuilder(input.Length - startIndex);

            //Parse the string 
            for (int i = startIndex; i < input.Length; i++)
            {
                //Have we discovered an inline Comment?
                if (numQuotes == -1 && input[i] == ';')
                {
                    endIndex = i;
                    break;
                }
                if (numQuotes > 0 && (input[i] == '"' || input[i] == '\''))
                {
                    numQuotes--;
                    if (numQuotes == 0)
                    {
                        //Set the quote character for this value
                        quoteChar = input[i];

                        //Remove the 1st quote from the value
                        val.Remove(0, 1);

                        //Put the end index just after our quote char, or if it is the last value, set endIndex = -1
                        endIndex = i + 1;
                        if (endIndex >= input.Length)
                            endIndex = -1;

                        break;
                    }
                }
                val.Append(input[i]);
            }

            //Is there an inline comment?
            string comment = null;
            if (endIndex > -1)
            {
                //zip us past any whitespace
                while (endIndex < input.Length && char.IsWhiteSpace(input[endIndex])) endIndex++;

                if (input[endIndex] == ';' & endIndex + 1 < input.Length)
                {
                    comment = input.Substring(endIndex + 1, input.Length - (endIndex + 1));
                }
            }

            string value = val.ToString();
            string[] vals = null;

            //If this was not a quoted string, check and see if it is a csv list
            if (quoteChar == '\0' && value.IndexOf(',') > -1)
            {
                vals = value.Split(',').Select((csv) => csv.Trim()).ToArray();

                //Either the value field or the array field will be valid. Not both
                if (vals != null && vals.Length == 1)
                    vals = null;
                else
                    value = null;
            }

            //Ok then return what we found
            info = new IniLine() { type = LineType.KeyValue, value = value, comment = comment, quotechar = quoteChar, array = vals };
            return true;
        }

        /// <summary>
        /// Looks for a Key/Value pair in the given string and if it finds one, it returns an IniLine representation of it
        /// </summary>
        /// <param name="input"></param>
        /// <returns>An IniLine with the parsed Key/Value info, null otherwise</returns>
        bool ParseKeyValue(string input, out IniLine info)
        {
            //To start, just make our info object empty
            info = new IniLine();

            if (string.IsNullOrEmpty(input) || input.IsWhiteSpace()) return false;

            var kvSeparatorIndex = IndexOfKvSeparator(input);
            if (kvSeparatorIndex == -1) return false;

            //Get the key
            StringBuilder k = new StringBuilder();
            for (int i = 0; i < kvSeparatorIndex; i++)
            {
                //There cannot be a space in the key name. if we see a space, we break out
                if (char.IsWhiteSpace(input[i]))
                {
                    LogWarning("Key names can't contain spaces. {0} was truncated to {1}", input.Substring(0, kvSeparatorIndex), k.ToString());
                    break;
                }

                k.Append(input[i]);
            }

            //Does a key exist? If not, Abort
            if (k.Length == 0) return false;


            //Is there a value?
            int startIndex = kvSeparatorIndex + 1;

            //Parse the rest of the string looking for a value, or a csv array and an optional inline comment
            if (!ParseValue(input, startIndex, out info))
                return false;
            else
            {
                //Add the current section name and the key to this and we are done
                info.section = currentSection;
                info.key = k.ToString();
                return true;
            }
        }
        #endregion

        /// <summary>
        /// When given an IniLine struct, this method returns the string representation of it
        /// </summary>
        /// <param name="iniStruct"></param>
        /// <returns></returns>
        string GetString(IniLine iniStruct)
        {
            switch (iniStruct.type)
            {
                case LineType.Comment:
                    return string.Format(";{0}", iniStruct.comment);
                case LineType.KeyValue:
                    string val = iniStruct.value;

                    if (iniStruct.array != null)
                    {
                        val = string.Join(", ", iniStruct.array);
                    }

                    if (!string.IsNullOrEmpty(iniStruct.comment))
                    {
                        if (iniStruct.quotechar > 0)
                            return string.Format("{0}{1}{4}{2}{4} ;{3}", iniStruct.key, outputKVSeparator, val, iniStruct.comment, iniStruct.quotechar);
                        else
                            return string.Format("{0}{1}{2} ;{3}", iniStruct.key, outputKVSeparator, val, iniStruct.comment);
                    }
                    else
                    {
                        //If the value begins or ends with whitespace, a ; or either the input or output kv separator, put it in quotes
                        if (iniStruct.quotechar > 0)
                            return string.Format("{0}{1}{3}{2}{3}", iniStruct.key, outputKVSeparator, val, iniStruct.quotechar);
                        else
                            return string.Format("{0}{1}{2}", iniStruct.key, outputKVSeparator, val);
                    }
                case LineType.Section:
                    if (!string.IsNullOrEmpty(iniStruct.comment))
                        return string.Format("[{0}] ;{1}", iniStruct.section, iniStruct.comment);
                    else
                        return string.Format("[{0}]", iniStruct.section);
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// This does the output formatting
        /// </summary>
        /// <returns></returns>
        IEnumerator<string> Lines()
        {
            //We will use this to tell us what line type came before the current
            LineType prevType = LineType.None;

            //By definition, these IniLine instances should all be sections
            foreach (var st in structureOrder)
            {
                List<IniLine> section;
                if (sections.TryGetValue(st, out section))
                {
                    //Console.WriteLine("Length [{0}]: {1}", st, section.Count);
                    foreach (var line in section)
                    {
                        //Here we check of we need to yield a blank line or not
                        if ((emptyLinesBetweenSections && line.type == LineType.Section && prevType == LineType.KeyValue) ||
                            (emptyLineAboveComments && line.type == LineType.Comment && prevType == LineType.KeyValue) ||
                            (emptyLinesBetweenKeyValuePairs && ((line.type == LineType.KeyValue && prevType == LineType.KeyValue) ||
                            (line.type == LineType.KeyValue && prevType == LineType.Section)))
                            )
                            yield return string.Empty;

                        yield return GetString(line);
                        prevType = line.type;
                    }
                }
                else
                {
                    throw new Exception("Unable to find the section in the dictionary list: " + st);
                }
            }
        }

        /// <summary>
        /// Returns a string containing the contents of the ini data
        /// </summary>
        public override string ToString()
        {
            var enumerator = Lines();

            StringBuilder sb = new StringBuilder();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null)
                    sb.AppendLine(enumerator.Current);
            }
            return sb.ToString();
        }
    }
}
