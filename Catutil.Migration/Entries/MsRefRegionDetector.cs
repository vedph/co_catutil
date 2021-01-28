using Fusi.Tools.Config;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Detect codices recentiores references. These are nested inside some
    /// other region.
    /// </summary>
    /// <seealso cref="Proteus.Core.Regions.EntryRegionDetectorBase" />
    [Tag("region-detector.co-ms-ref")]
    public sealed class MsRefRegionDetector : EntryRegionDetectorBase
    {
        private readonly Regex _msRegex;
        private readonly Regex _mssRegex;
        private readonly Regex _pleriqueRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsRefRegionDetector"/>
        /// class.
        /// </summary>
        public MsRefRegionDetector()
        {
            _msRegex = new Regex(@"\bMS\.\s*[0-9][^,]*");
            _mssRegex = new Regex(
                @"\bMSS\.\s*(?:[0-9]+[^\s]*\s*" +
                @"(?:(?:post|ante)?\s*a\.\s*[0-9]+\s*(?:ca\.))?(?:\s*et\s*)?)*");
            _pleriqueRegex = new Regex(@"\bcodd\.\s*plerique\b");
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

            for (int index = 0; index < entries.Count; index++)
            {
                if (!(entries[index] is DecodedTextEntry txt)) continue;

                // MS.
                foreach (Match m in _msRegex.Matches(txt.Value))
                {
                    set.AddNewRegion(new EntryRange
                        (new EntryPoint
                        {
                            Entry = index,
                            Character = m.Index
                        },
                        new EntryPoint
                        {
                            Entry = index,
                            Character = m.Index + m.Length - 1
                        }), "ms");
                }

                // MSS.: split at each " et "
                // e.g. MSS. 3 et 18 et 65 et 104 post a. 1455 ca. et MS. 126 post a. 1486
                // will get 3, 18, 65, 104 post a. 1455 ca.
                foreach (Match m in _mssRegex.Matches(txt.Value))
                {
                    int start = 4, limit;
                    do
                    {
                        limit = m.Value.IndexOf(" et ", start);
                        if (limit == -1) limit = m.Value.Length;

                        set.AddNewRegion(new EntryRange
                            (new EntryPoint
                            {
                                Entry = index,
                                Character = m.Index + start
                            },
                            new EntryPoint
                            {
                                Entry = index,
                                Character = m.Index + limit - 1
                            }), "ms");

                        start = limit + 4;
                    } while (limit < m.Value.Length);
                }

                // codd. plerique
                foreach (Match m in _pleriqueRegex.Matches(txt.Value))
                {
                    set.AddNewRegion(new EntryRange
                        (new EntryPoint
                        {
                            Entry = index,
                            Character = m.Index
                        },
                        new EntryPoint
                        {
                            Entry = index,
                            Character = m.Index + m.Length - 1
                        }), "ms-x");
                }
            }
        }
    }
}
