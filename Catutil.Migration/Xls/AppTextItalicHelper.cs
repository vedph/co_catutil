using System;
using System.Text;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// Italic helper for apparatus text.
    /// </summary>
    public static class AppTextItalicHelper
    {
        /// <summary>
        /// Reduce any sequence of the specified character to a single occurrence
        /// of it. This is used to sanitize cases like "{{" for "{".
        /// </summary>
        /// <param name="c">The character to look for.</param>
        /// <param name="sb">The string builder to modify.</param>
        /// <exception cref="ArgumentNullException">sb</exception>
        public static void ReduceSequencesOf(char c, StringBuilder sb)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));

            int i = sb.Length - 1;
            while (i > 0)
            {
                if (sb[i] == c && sb[i - 1] == c)
                {
                    int start = i;
                    while (start > 0 && sb[start - 1] == c) start--;
                    sb.Remove(start + 1, i + 1 - (start + 1));
                    i = start - 1;
                }
                else i--;
            }
        }

        /// <summary>
        /// Appends the text from <paramref name="start"/> for
        /// <paramref name="length"/> characters into <paramref name="target"/>,
        /// sanitizing it for italics. Sanitization implies removing any curly
        /// brace from the text. There should be no curly brace in the text,
        /// but sanitization is performed to ensure their absence.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="start">The start.</param>
        /// <param name="length">The length.</param>
        /// <param name="target">The target string builder.</param>
        /// <exception cref="ArgumentNullException">target</exception>
        public static void AppendAndSanitize(string text,
            int start, int length, StringBuilder target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (string.IsNullOrEmpty(text)) return;

            for (int i = 0; i < length; i++)
            {
                char c = text[start + i];
                if (c != '{' && c != '}') target.Append(c);
            }
        }

        private static int LocateRightmostSpace(string text, int index)
        {
            if (index >= text.Length || text[index] != ' ') return -1;
            do
            {
                index++;
            } while (text[index] == ' ');
            return index - 1;
        }

        private static int LocateLeftmostSpace(string text, int index)
        {
            if (index < 0 || text[index] != ' ') return -1;
            do
            {
                index--;
            } while (text[index] == ' ');
            return index + 1;
        }

        /// <summary>
        /// Adjusts the spaces connected to italic markers.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>Adjusted text.</returns>
        public static string AdjustItalicSpaces(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder sb = new StringBuilder(text);
            int i = 0;
            while (i < text.Length)
            {
                // italic on
                if (sb[i] == '{')
                {
                    // if spaces after initial {, swap them
                    int target = LocateRightmostSpace(text, i + 1);
                    if (target > -1)
                    {
                        sb[i] = ' ';
                        sb[target] = '{';
                        i = target + 1;
                        continue;
                    }
                }
                // italic off
                else if (sb[i] == '}')
                {
                    // if spaces before ending }, swap them
                    int target = LocateLeftmostSpace(text, i - 1);
                    if (target > -1)
                    {
                        sb[i] = ' ';
                        sb[target] = '}';
                    }
                }
                i++;
            }

            // fix spaces next to pipes
            sb.Replace("|{ ", "| {");
            sb.Replace(" }| ", "} |");

            return sb.ToString();
        }
    }
}
