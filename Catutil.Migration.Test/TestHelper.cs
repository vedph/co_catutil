using Proteus.Core.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Catutil.Migration.Test
{
    static internal class TestHelper
    {
        static public Stream LoadResourceStream(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(
                $"Catutil.Migration.Test.Assets.{name}");
        }

        public static string LoadResourceText(string name)
        {
            using (StreamReader reader = new StreamReader(
                LoadResourceStream(name), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static List<DecodedEntry> LoadResourceEntries(string name,
            string id)
        {
            string text = LoadResourceText(name);
            bool inSection = false;
            StandardCmdNamingConvention convention = new StandardCmdNamingConvention();
            List<DecodedEntry> entries = new List<DecodedEntry>();

            using (StringReader reader = new StringReader(text))
            {
                string line;
                int index = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(";", StringComparison.Ordinal)
                        || line.Length == 0)
                        continue;

                    if (line[0] == '#' && line.Length > 1)
                    {
                        if (inSection) break;
                        string idText = line.Substring(1);
                        if (idText == id) inSection = true;
                        continue;
                    }
                    if (!inSection) continue;

                    DecodedEntry entry = EntryPattern.ParseEntry(line, true, convention);
                    entry.SourceIndex = index;

                    DecodedTextEntry txt = entry as DecodedTextEntry;
                    entry.SourceLength = txt?.Value.Length ?? 1;

                    entries.Add(entry);
                    index += entry.SourceLength;
                }
            }

            return entries;
        }
    }
}
