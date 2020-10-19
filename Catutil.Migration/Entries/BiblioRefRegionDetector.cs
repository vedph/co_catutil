using Catutil.Migration.Biblio;
using Catutil.Migration.Xls;
using Fusi.Tools.Config;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Bibliographic references region detector. Note that these regions
    /// are detected inside text entries, so they do not span across entries.
    /// </summary>
    /// <seealso cref="EntryRegionDetectorBase" />
    [Tag("region-detector.co-biblio-ref")]
    public sealed class BiblioRefRegionDetector : EntryRegionDetectorBase,
        IConfigurable<BiblioRefRegionDetectorOptions>
    {
        private BiblioReferenceFinder _finder;

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(BiblioRefRegionDetectorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!string.IsNullOrEmpty(options.LookupFilePath))
            {
                XlsBiblioLookup lookup = new XlsBiblioLookup();

                // expanders
                List<IBiblioRefExpander> expanders = new List<IBiblioRefExpander>();
                if (options.IsInflectionEnabled)
                    expanders.Add(new InflectingBiblioRefExpander());
                if (!string.IsNullOrEmpty(options.AdditionsFilePath))
                {
                    using (var stream = new FileStream(options.AdditionsFilePath,
                        FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        expanders.Add(new ImportingBiblioRefExpander(stream));
                    }
                }

                lookup.LoadIndex(options.LookupFilePath, true, expanders.ToArray());
                _finder = new BiblioReferenceFinder(lookup);
            }
            else _finder = null;
        }

        /// <summary>
        /// Detects some specific regions in the specified list of decoded entries.
        /// </summary>
        /// <param name="entries">The list of decoded entries.</param>
        /// <param name="set">The target regions set.</param>
        /// <exception cref="ArgumentNullException">null entries or set</exception>
        public override void Detect(IList<DecodedEntry> entries, EntryRegionSet set)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (set == null) throw new ArgumentNullException(nameof(set));

            if (_finder == null) return;

            for (int index = 0; index < entries.Count; index++)
            {
                if (!(entries[index] is DecodedTextEntry txt)) continue;

                foreach (Tuple<int, int> range in _finder.FindAll(txt.Value))
                {
                    set.AddNewRegion(new EntryRange
                        (new EntryPoint
                        {
                            Entry = index,
                            Character = range.Item1
                        },
                        new EntryPoint
                        {
                            Entry = index,
                            Character = range.Item1 + range.Item2 - 1
                        }), "br");
                }
            }
        }
    }

    /// <summary>
    /// Options for <see cref="BiblioRefRegionDetector"/>.
    /// </summary>
    public sealed class BiblioRefRegionDetectorOptions
    {
        /// <summary>
        /// Gets or sets the path to the bibliographic references lookup data
        /// file. This is a JSON dump produced by the build-biblio command.
        /// </summary>
        public string LookupFilePath { get; set; }

        /// <summary>
        /// Gets or sets the path to the bibliographic lookup references
        /// additions to be imported and added to those loaded from the lookup
        /// file.
        /// </summary>
        public string AdditionsFilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether inflection is enabled
        /// for Latin names in -us.
        /// </summary>
        public bool IsInflectionEnabled { get; set; }
    }
}
