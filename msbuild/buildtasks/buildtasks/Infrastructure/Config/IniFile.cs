namespace RJCP.MSBuildTasks.Infrastructure.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A class to read elements from an INI file that is cross platform.
    /// </summary>
    /// <remarks>
    /// Loads elements from a configuration file. It is similar to the Windows implementation, but may have subtle
    /// differences in corner use cases.
    /// <para>
    /// An INI file is a text file that is formed by section headers and key/value pairs. Comments are prefixed with a semicolon.
    /// </para>
    /// <code language="ini"><![CDATA[
    /// ; Comment
    /// [SECTION]
    /// KEY=VALUE ; Comment
    ///
    /// [SECTION2]
    /// KEY=VALUE
    /// ]]></code>
    /// <para>
    /// When reading the file, the sections and keys are case insensitive. This is consistent with Windows. The values of
    /// a key are case sensitive.
    /// </para>
    /// <para>
    /// If there are two sections of the same name in the file, only the first section is loaded. The further sections
    /// are ignored. If there are two keys having the same name within a section, only the first key is read, the second
    /// key of the same name is ignored.
    /// </para>
    /// </remarks>
    internal class IniFile : IniKeyPair<IniSection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class. There are no sections defined.
        /// </summary>
        public IniFile() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IniFile"/> class, by loading the contents from a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fileName"/> is an empty string (""), contains only white space, or contains one or more
        /// invalid characters.
        /// <para>- or -</para>
        /// <paramref name="fileName"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in an NTFS environment.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="fileName"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.
        /// </exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path is invalid, such as being on an unmapped drive.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// The access requested is not permitted by the operating system for the specified path.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on
        /// Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
        /// </exception>
        public IniFile(string fileName)
        {
            LoadIniFile(fileName);
        }

        private void LoadIniFile(string fileName)
        {
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (TextReader reader = new StreamReader(file, Encoding.UTF8, true)) {
                IniSection section = null;

                while (true) {
                    string line = reader.ReadLine();

                    if (line is null) {
                        // End of the file. Add the pending section.
                        if (section is object) {
                            Add(section.Header, section);
                        }
                        return;
                    }

                    line = line.Trim();

                    if (IsBlankLine(line)) continue;

                    if (GetSectionHeader(line, out string header)) {
                        if (section is object) {
                            Add(section.Header, section);
                        }

                        if (ContainsKey(header)) {
                            // Duplicate section, ignore it until a new section appears.
                            section = null;
                        } else {
                            section = new IniSection(header);
                        }
                        continue;
                    }

                    if (section is object && GetKeyValuePair(line, out string key, out string value)) {
                        if (section.ContainsKey(key)) continue;
                        section.Add(key, value);
                    }
                }
            }
        }

        private static bool GetSectionHeader(string line, out string header)
        {
            if (line.Length < 2) {
                header = null;
                return false;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']') {
                if (line.Length == 2) {
                    header = string.Empty;
                } else {
                    header = line.Substring(1, line.Length - 2).Trim();
                }
                return true;
            }

            header = null;
            return false;
        }

        private static bool IsBlankLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return true;
            if (line[0] == ';') return true;
            return false;
        }

        private static bool GetKeyValuePair(string line, out string key, out string value)
        {
            key = string.Empty;    // Must set this, the compiler can't figure out it's guaranteed to be set prior to returning.

            bool quote = false;
            int keyFound = -1;
            for (int i = 0; i < line.Length; i++) {
                char c = line[i];

                if (keyFound == -1) {
                    if (c == ';') {
                        key = line.Substring(0, i).TrimEnd();
                        value = string.Empty;
                        return true;
                    }

                    if (c == '=') {
                        key = line.Substring(0, i).TrimEnd();
                        keyFound = i + 1;
                    }

                    // INI specifications state that a '=' is an invalid value inside the key. This leads us to not
                    // interpreting quotes. Specifying a quote (or a comment) inside the key portion is therefore
                    // undefined behavior.
                } else {
                    if (c == ';' && !quote) {
                        value = line.Substring(keyFound, i - keyFound).Trim();
                        return true;
                    }

                    if (c == '\"') {
                        quote = !quote;
                    }
                }
            }

            if (keyFound == -1) {
                key = line;
                value = string.Empty;
            } else {
                value = line.Substring(keyFound).Trim();
            }

            return true;
        }

        private readonly static Dictionary<string, IniFile> m_Files = new Dictionary<string, IniFile>();

        private static string GetFullFileName(string fileName)
        {
            if (Path.IsPathRooted(fileName)) return fileName;
            fileName = Path.Combine(Environment.CurrentDirectory, fileName);
            try {
                FileInfo fileInfo = new FileInfo(fileName);
                return fileInfo.FullName;
            } catch (PathTooLongException) {            // The specified path, file name, or both exceed the system-defined maximum length
                return null;
            } catch (System.Security.SecurityException) {  // The caller does not have the required permission.
                return null;
            } catch (ArgumentException) {               // The file name is empty, contains only white spaces, or contains invalid characters.
                return null;
            } catch (UnauthorizedAccessException) {     // Access to fileName is denied.
                return null;
            } catch (NotSupportedException) {           // fileName contains a colon (:) in the middle of the string.
                return null;
            }
        }

        private static IniFile GetIniFile(string fileName)
        {
            string fullName = GetFullFileName(fileName);
            if (fullName is null) return new IniFile();

            if (!m_Files.TryGetValue(fullName, out IniFile iniFile)) {
                try {
                    iniFile = new IniFile(fullName);
                } catch (FileNotFoundException) {       // The file cannot be found
                    return new IniFile();
                } catch (DirectoryNotFoundException) {  // The specified path is invalid, such as being on an unmapped drive
                    return new IniFile();
                } catch (UnauthorizedAccessException) { // The access requested is not permitted by the operating system for the specified path
                    return new IniFile();
                } catch (ArgumentException) {           // path is an empty string (""), contains only white space, or contains one or more invalid characters
                    return new IniFile();
                } catch (System.Security.SecurityException) { // The caller does not have the required permission
                    return new IniFile();
                } catch (PathTooLongException) {        // The specified path, file name, or both exceed the system-defined maximum length
                    return new IniFile();
                } catch (IOException) {
                    return new IniFile();
                }
                m_Files.Add(fullName, iniFile);
            }
            return iniFile;
        }

        /// <summary>
        /// Gets the section of an INI file given the file name and section.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <returns>
        /// The section requested as key/value pairs, where the keys are case insensitive. If the <paramref
        /// name="fileName"/> or <paramref name="section"/> are not found, an empty <see cref="IniSection"/> is returned.
        /// </returns>
        /// <remarks>
        /// The file is loaded into memory and cached for the lifetime of your program in static memory, so that further
        /// accesses are faster by not having to read the INI file into memory again. There is no mechanism to release
        /// this memory. If you don't want the INI file to be cached, you should use
        /// <see cref="IniFile.IniFile(string)"/> to load the INI file from disk.
        /// </remarks>
        public static IniSection GetSection(string fileName, string section)
        {
            IniFile iniFile = GetIniFile(fileName);
            if (!iniFile.TryGetValue(section, out IniSection iniSection)) {
                return new IniSection(section.Trim());
            }
            return iniSection;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <returns>
        /// The string found in the INI file (it's trimmed), else if not found, then the <paramref name="defaultValue"/>
        /// (which is not modified) is returned.
        /// </returns>
        /// <remarks>
        /// The file is loaded into memory and cached for the lifetime of your program in static memory, so that further
        /// accesses are faster by not having to read the INI file into memory again. There is no mechanism to release
        /// this memory. If you don't want the INI file to be cached, you should use
        /// <see cref="IniFile.IniFile(string)"/> to load the INI file from disk.
        /// </remarks>
        public static string GetKey(string fileName, string section, string key, string defaultValue)
        {
            IniFile iniFile = GetIniFile(fileName);
            if (!iniFile.TryGetValue(section, out IniSection iniSection)) {
                return defaultValue;
            }
            if (!iniSection.TryGetValue(key, out string value)) {
                return defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <returns>The value stored in the file.</returns>
        public static int GetKey(string fileName, string section, string key, int defaultValue)
        {
            string value = GetKey(fileName, section, key, null);
            if (value is null) return defaultValue;

            if (!int.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out int result))
                return defaultValue;
            return result;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <param name="style">
        /// A bitwise combination of enumeration values that indicates the style elements that can be present in the
        /// configuration file. A typical value to specify is Integer.
        /// </param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information about the string to convert.
        /// </param>
        /// <returns>The value stored in the file.</returns>
        public static int GetKey(string fileName, string section, string key, int defaultValue, System.Globalization.NumberStyles style, IFormatProvider provider)
        {
            string value = GetKey(fileName, section, key, null);
            if (value is null) return defaultValue;

            if (!int.TryParse(value, style, provider, out int result))
                return defaultValue;
            return result;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <returns>The value stored in the file.</returns>
        public static long GetKey(string fileName, string section, string key, long defaultValue)
        {
            string value = GetKey(fileName, section, key, null);
            if (value is null) return defaultValue;

            if (!long.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out long result)) return defaultValue;
            return result;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <param name="style">
        /// A bitwise combination of enumeration values that indicates the style elements that can be present in the
        /// configuration file. A typical value to specify is Integer.
        /// </param>
        /// <param name="provider">
        /// An object that supplies culture-specific formatting information about the string to convert.
        /// </param>
        /// <returns>The value stored in the file.</returns>
        public static long GetKey(string fileName, string section, string key, long defaultValue, System.Globalization.NumberStyles style, IFormatProvider provider)
        {
            string value = GetKey(fileName, section, key, null);
            if (value is null) return defaultValue;

            if (!long.TryParse(value, style, provider, out long result)) return defaultValue;
            return result;
        }

        /// <summary>
        /// Gets a value for an INI file given the file name, section and key.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="section">The section (case insensitive).</param>
        /// <param name="key">The key (case insensitive).</param>
        /// <param name="defaultValue">
        /// The default value if the <paramref name="fileName"/>, <paramref name="section"/> or <paramref name="key"/>
        /// cannot be found.
        /// </param>
        /// <returns>The value stored in the file.</returns>
        public static bool GetKey(string fileName, string section, string key, bool defaultValue)
        {
            string value = GetKey(fileName, section, key, null);
            if (value is null) return defaultValue;

            if (!bool.TryParse(value, out bool result)) return defaultValue;
            return result;
        }
    }
}
