using System.Text;

namespace Catutil.Migration.Xls
{
    public static class AppTextMdHelper
    {
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

        public static string AdjustItalicSpaces(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder sb = new StringBuilder(text);
            bool italic = false;
            int i = 0;
            while (i < text.Length)
            {
                // italic switch (_ not preceded by \)
                if (sb[i] == '_' && (i == 0 || sb[i - 1] != '\\'))
                {
                    // entering italic
                    if (!italic)
                    {
                        italic = true;
                        // if spaces after initial _, swap them
                        int target = LocateRightmostSpace(text, i + 1);
                        if (target > -1)
                        {
                            sb[i] = ' ';
                            sb[target] = '_';
                            i = target + 1;
                            continue;
                        }
                    }
                    // exiting italic
                    else
                    {
                        italic = false;
                        // if spaces before ending _, swap them
                        int target = LocateLeftmostSpace(text, i - 1);
                        if (target > -1)
                        {
                            sb[i] = ' ';
                            sb[target] = '_';
                        }
                    }
                }
                i++;
            }

            // fix spaces next to pipes
            sb.Replace("|_ ", "| _");
            sb.Replace(" _| ", "_ |");

            return sb.ToString();
        }
    }
}
