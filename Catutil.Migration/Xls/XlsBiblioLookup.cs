using Fusi.Tools;
using System;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using Fusi.Tools.Data;

namespace Catutil.Migration.Xls
{
    /// <summary>
    /// XLS-based bibliography lookup tool. This can parse bibliography from
    /// its original XLS file, saving the output into a single JSON file, and
    /// then load this file and use it for lookup.
    /// </summary>
    public sealed class XlsBiblioLookup
    {
        private Trie _trie;
        private bool _loaded;

        /// <summary>
        /// Extract JSON-based lookup data from the specified XLS file.
        /// </summary>
        /// <param name="xlsFilePath">The file path.</param>
        /// <param name="jsonFilePath">The output file path.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
        public void ExtractJson(string xlsFilePath, string jsonFilePath,
            CancellationToken cancel, IProgress<ProgressReport> progress = null)
        {
            if (xlsFilePath == null)
                throw new ArgumentNullException(nameof(xlsFilePath));
            if (jsonFilePath == null)
                throw new ArgumentNullException(nameof(jsonFilePath));

            ProgressReport report =
                progress != null ? new ProgressReport() : null;

            using (Stream output = new FileStream(jsonFilePath, FileMode.Create,
                FileAccess.Write, FileShare.Read))
            using (XlsBiblioReader reader = new XlsBiblioReader(xlsFilePath))
            {
                Utf8JsonWriter writer = new Utf8JsonWriter(output,
                    new JsonWriterOptions
                    {
                        Indented = true
                    });

                writer.WriteStartArray();
                foreach (XlsBiblioItem item in reader.Read(true))
                {
                    JsonSerializer.Serialize(writer, item,
                        typeof(XlsBiblioItem));

                    if (cancel.IsCancellationRequested) break;

                    if (progress != null)
                    {
                        report.Message = item.GetReference(true);
                        progress.Report(report);
                    }
                }

                writer.WriteEndArray();
                writer.Flush();
            }
        }

        private void IndexItems(string authorRef, IList<XlsBiblioItem> items)
        {
            // process the items group: if it's a single item we
            // want a reference without the date; else we want one
            // reference for each date
            if (items.Count == 1)
            {
                _trie.Insert(authorRef);
            }
            else
            {
                foreach (XlsBiblioItem i in items)
                {
                    string datedRef = i.GetReference(true);
                    _trie.Insert(datedRef);
                }
            }
        }

        /// <summary>
        /// Loads the bibliographic lookup data from the specified JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The JSON file path.</param>
        /// <exception cref="ArgumentNullException">jsonFilePath</exception>
        public void Load(string jsonFilePath)
        {
            if (jsonFilePath is null)
                throw new ArgumentNullException(nameof(jsonFilePath));

            // init the index
            if (_trie == null) _trie = new Trie();
            else _trie.Clear();

            using (Stream stream = new FileStream(jsonFilePath, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                JsonDocument doc = JsonDocument.Parse(stream);
                string prevAuthors = null;
                List<XlsBiblioItem> items = new List<XlsBiblioItem>();

                // for each item
                foreach (var itemElem in doc.RootElement.EnumerateArray())
                {
                    // read it
                    XlsBiblioItem item =
                        JsonSerializer.Deserialize<XlsBiblioItem>
                        (itemElem.GetRawText());

                    // if it's the first item, or it has the same author(s)
                    // of the previously read one, just add it to the items list
                    // and continue; else, process that list
                    string currentAuthors = item.GetReference(false);
                    if (currentAuthors == prevAuthors || prevAuthors == null)
                    {
                        items.Add(item);
                        continue;
                    }

                    // add group to index
                    IndexItems(prevAuthors, items);

                    // reset the group adding to it the newly read item
                    items.Clear();
                    items.Add(item);
                    prevAuthors = item.GetReference(false);
                } //for

                // last group if any
                if (items.Count > 0) IndexItems(prevAuthors, items);
            }

            _loaded = true;
        }

        /// <summary>
        /// Determines whether this lookup has the specified bibliographic
        /// reference.
        /// </summary>
        /// <param name="reference">The bibliographic reference.</param>
        /// <returns>
        ///   <c>true</c> if the specified reference was found; otherwise,
        ///   <c>false</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">Bibliographic reference
        /// lookup invoked without loading data</exception>
        public bool HasReference(string reference)
        {
            if (!_loaded)
            {
                throw new InvalidOperationException(
                    "Bibliographic reference lookup requires loading");
            }

            if (string.IsNullOrEmpty(reference)) return false;
            return _trie.Get(reference) != null;
        }
    }
}
