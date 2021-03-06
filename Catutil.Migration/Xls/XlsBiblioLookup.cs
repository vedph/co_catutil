﻿using Fusi.Tools;
using System;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using Fusi.Tools.Data;
using Catutil.Migration.Biblio;
using System.Diagnostics;
using System.Linq;

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
        /// Extract JSON-based lookup data for this lookup index from the
        /// specified XLS file, dumping the index keys once completed.
        /// </summary>
        /// <param name="xlsFilePath">The file path.</param>
        /// <param name="outputDir">The output directory.</param>
        /// <param name="cancel">The cancellation token.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <exception cref="ArgumentNullException">filePath</exception>
        public void ExtractIndex(string xlsFilePath, string outputDir,
            CancellationToken cancel, IProgress<ProgressReport> progress = null)
        {
            if (xlsFilePath == null)
                throw new ArgumentNullException(nameof(xlsFilePath));
            if (outputDir == null)
                throw new ArgumentNullException(nameof(outputDir));

            ProgressReport report =
                progress != null ? new ProgressReport() : null;

            string jsonFilePath = Path.Combine(outputDir, "biblio-lookup.json");
            string dumpFilePath = Path.Combine(outputDir, "biblio-lookup-dump.txt");

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
                foreach (BiblioItem item in reader.ReadForReference())
                {
                    JsonSerializer.Serialize(writer, item,
                        typeof(BiblioItem));

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

            // dump
            LoadIndex(jsonFilePath, true);
            using (StreamWriter writer = new StreamWriter(
                new FileStream(dumpFilePath, FileMode.Create, FileAccess.Write,
                FileShare.Read)))
            {
                DumpIndex(writer);
                writer.Flush();
            }
        }

        private void IndexItems(IList<BiblioItem> items)
        {
            foreach (BiblioItem i in items)
            {
                string datedRef = i.GetReference(true);
                _trie.Insert(datedRef);
            }
        }

        /// <summary>
        /// Loads the bibliographic lookup index from the specified JSON file.
        /// </summary>
        /// <param name="input">The input JSON stream.</param>
        /// <param name="noAlias">True to exclude loading alias items.</param>
        /// <param name="expanders">The expander(s) to use if any.</param>
        /// <exception cref="ArgumentNullException">jsonFilePath</exception>
        public void LoadIndex(Stream input, bool noAlias,
            params IBiblioRefExpander[] expanders)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            // init the index
            if (_trie == null) _trie = new Trie();
            else _trie.Clear();

            JsonDocument doc = JsonDocument.Parse(input);
            string prevAuthors = null;
            List<BiblioItem> items = new List<BiblioItem>();

            // for each item
            foreach (var itemElem in doc.RootElement.EnumerateArray())
            {
                // read it
                BiblioItem item =
                    JsonSerializer.Deserialize<BiblioItem>
                    (itemElem.GetRawText());

                // exclude alias items if requested
                if (noAlias && item.IsAlias()) continue;

                // if it's the first item, or it has the same author(s)
                // of the previously read one, just add it to the items list
                // and continue; else, process that list
                string currentAuthors = item.GetReference(false);
                if (currentAuthors == prevAuthors || prevAuthors == null)
                {
                    items.Add(item);
                    prevAuthors = currentAuthors;
                    continue;
                }

                // add group to index
                IndexItems(items);

                // reset the group adding to it the newly read item
                items.Clear();
                items.Add(item);
                prevAuthors = currentAuthors;
            } //for

            // last group if any
            if (items.Count > 0) IndexItems(items);

            // expand if required
            foreach (IBiblioRefExpander expander in expanders)
            {
                foreach (string addedKey in expander.GetExpansions(_trie))
                    _trie.Insert(addedKey, _ => { });
            }

            _loaded = true;
        }

        /// <summary>
        /// Loads the bibliographic lookup index from the specified JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The JSON file path.</param>
        /// <param name="noAlias">True to exclude loading alias items.</param>
        /// <param name="expanders">The expander(s) to use if any.</param>
        /// <exception cref="ArgumentNullException">jsonFilePath</exception>
        public void LoadIndex(string jsonFilePath, bool noAlias,
            params IBiblioRefExpander[] expanders)
        {
            using (Stream stream = new FileStream(jsonFilePath, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                LoadIndex(stream, noAlias, expanders);
            }
        }

        /// <summary>
        /// Adds the specified key to the index, if not already present.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <exception cref="ArgumentNullException">key</exception>
        public void AddToIndex(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (_trie == null) _trie = new Trie();
            _trie.Insert(key, _ => { });
        }

        /// <summary>
        /// Dumps the index keys into the specified text writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="ArgumentNullException">writer</exception>
        public void DumpIndex(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (!_loaded) return;

            foreach (var node in _trie.GetAll())
                writer.WriteLine(node.GetKey());
        }

        /// <summary>
        /// Gets the length of the shortest reference in this index.
        /// </summary>
        /// <returns>Min length.</returns>
        public int GetMinReferenceLength()
        {
            return _trie.GetAll().Min(n => n.GetKey().Length);
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

        /// <summary>
        /// Finds all the references matching the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>Keys.</returns>
        /// <exception cref="ArgumentNullException">prefix</exception>
        /// <exception cref="InvalidOperationException">index not loaded</exception>
        public IEnumerable<string> FindAll(IEnumerable<char> prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (!_loaded) throw new InvalidOperationException("Index not loaded");

            return InnerFindAll();

            IEnumerable<string> InnerFindAll()
            {
                foreach (var node in _trie.Find(prefix))
                {
                    yield return node.GetKey();
                }
            }
        }
    }
}
