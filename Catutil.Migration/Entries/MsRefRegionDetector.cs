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

        /// <summary>
        /// Initializes a new instance of the <see cref="MsRefRegionDetector"/>
        /// class.
        /// </summary>
        public MsRefRegionDetector()
        {
            _msRegex = new Regex(@"\bMS\.\s*[0-9][^,]*");
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
            }
        }
    }
}
