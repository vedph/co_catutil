using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Catutil.Services
{
    /// <summary>
    /// File enumerator utility.
    /// </summary>
    public static class FileEnumerator
    {
        /// <summary>
        /// Enumerates all the files matching <paramref name="mask" /> in
        /// <paramref name="directory" />, sorting them by their name.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="mask">The mask.</param>
        /// <param name="regex">If set to <c>true</c>, the mask is a regular
        /// expression.</param>
        /// <param name="recursive">If set to <c>true</c>, enumeration is
        /// recursive across subdirectories.</param>
        /// <returns>
        /// Files, sorted by their full path.
        /// </returns>
        /// <exception cref="ArgumentNullException">directory or mask</exception>
        public static IEnumerable<string> Enumerate(
            string directory,
            string mask,
            bool regex = false,
            bool recursive = false)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (mask == null)
                throw new ArgumentNullException(nameof(mask));

            if (!regex)
            {
                return Directory.EnumerateFiles(directory, mask,
                    recursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly)
                    .OrderBy(s => s);
            }
            Regex r = new Regex(mask, RegexOptions.IgnoreCase);
            return from file in Directory.EnumerateFiles(directory,
                "*", recursive ? SearchOption.AllDirectories
                             : SearchOption.TopDirectoryOnly)
                   orderby file
                   where r.IsMatch(Path.GetFileName(file))
                   select file;
        }
    }
}
