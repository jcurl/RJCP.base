namespace RJCP.MSBuildTasks.Infrastructure
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Helper class for interpreting strings for a Semantic Version.
    /// </summary>
    internal static class SemVerHelper
    {
        /// <summary>
        /// Parses the number from the version at the current cursor, stopping on a specific separator.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="cursor">
        /// The cursor position where the number starts. On return, this is updated to the location of the separator, or
        /// beyond the string if the separator is not found.
        /// </param>
        /// <param name="separator">The separator character terminating the number.</param>
        /// <param name="leadingZeroesAllowed">if set to <c>true</c> then allow leading zeroes.</param>
        /// <returns>The version portion.</returns>
        /// <exception cref="ArgumentException">
        /// Invalid number in the version field. May be caused by not being able to parse the number, or the number has
        /// leading zeroes and <paramref name="leadingZeroesAllowed"/> is false.
        /// </exception>
        public static int ParseNumber(string version, ref int cursor, char separator, bool leadingZeroesAllowed)
        {
            int part = version.IndexOf(separator, cursor);
            if (part == -1) part = version.Length;
            return ParseNumber(version, ref cursor, part - cursor, leadingZeroesAllowed);
        }

        /// <summary>
        /// Parses the number from the version at the current cursor, stopping on a specific separator.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="cursor">
        /// The cursor position where the number starts. On return, this is updated to the location of the separator, or
        /// beyond the string if the separator is not found.
        /// </param>
        /// <param name="separator">The separator characters terminating the number.</param>
        /// <param name="leadingZeroesAllowed">if set to <c>true</c> then allow leading zeroes.</param>
        /// <returns>The version portion.</returns>
        /// <exception cref="ArgumentException">
        /// Invalid number in the version field. May be caused by not being able to parse the number, or the number has
        /// leading zeroes and <paramref name="leadingZeroesAllowed"/> is false.
        /// </exception>
        public static int ParseNumber(string version, ref int cursor, char[] separator, bool leadingZeroesAllowed)
        {
            int part = version.IndexOfAny(separator, cursor);
            if (part == -1) part = version.Length;
            return ParseNumber(version, ref cursor, part - cursor, leadingZeroesAllowed);
        }

        private static int ParseNumber(string version, ref int cursor, int length, bool leadingZeroesAllowed)
        {
            int value;
            try {
#if NETFRAMEWORK
                value = int.Parse(version.Substring(cursor, length), NumberStyles.None, CultureInfo.InvariantCulture);
#else
                value = int.Parse(version.AsSpan(cursor, length), NumberStyles.None, CultureInfo.InvariantCulture);
#endif
            } catch (FormatException e) {
                throw new ArgumentException(Resources.Infra_SemVer_InvalidVersion, e);
            } catch (OverflowException e) {
                throw new ArgumentException(Resources.Infra_SemVer_InvalidVersion, e);
            }

            if (!leadingZeroesAllowed && length > 1 && version[cursor] == '0')
                throw new ArgumentException(Resources.Infra_SemVer_InvalidVersionLeadingZero);

            cursor += length;
            return value;
        }

        /// <summary>
        /// Parses a string portion from the version from the cursor position given, stopping on a specific separator.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="cursor">
        /// The cursor position where the number starts. On return, this is updated to the location of the separator, or
        /// beyond the string if the separator is not found.
        /// </param>
        /// <param name="separator">The separator character terminating the number.</param>
        /// <returns>The validated sub-string portion.</returns>
        /// <exception cref="ArgumentException">Illegal character in string.</exception>
        public static string ParseString(string version, ref int cursor, char separator)
        {
            string value;
            int part = version.IndexOf(separator);
            if (part == -1) part = version.Length;

            value = version.Substring(cursor, part - cursor);
            cursor = part;
            return value;
        }

        /// <summary>
        /// Parses the string.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="cursor">The cursor.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentException">Illegal character in string.</exception>
        public static string ParseString(string version, ref int cursor)
        {
            string value = version.Substring(cursor);
            cursor = version.Length;
            return value;
        }
    }
}
