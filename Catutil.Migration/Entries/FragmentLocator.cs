using Cadmus.Philology.Parts.Layers;
using Microsoft.Extensions.Logging;
using Proteus.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Apparatus fragment locator.
    /// </summary>
    /// <seealso cref="IHasLogger" />
    public sealed class FragmentLocator : IHasLogger
    {
        private double _treshold;
        private readonly Func<string,string> _getLine;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the minimum treshold to have a match in a fuzzy
        /// comparison. The default value is 0.8.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value less than 0
        /// or greater than 1</exception>
        public double Treshold
        {
            get { return _treshold; }
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _treshold = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FragmentLocator" /> class.
        /// </summary>
        /// <param name="getLine">The function to be called to get a line's text
        /// from its ID.</param>
        /// <exception cref="ArgumentNullException">getLine</exception>
        public FragmentLocator(Func<string,string> getLine)
        {
            _getLine = getLine ?? throw new ArgumentNullException(nameof(getLine));
            _treshold = 0.8;
        }

        private static int GetX(string text, int index)
        {
            int i = 0, x = 1;
            while (i <= index)
            {
                if (text[i] == ' ') x++;
                i++;
            }
            return x;
        }

        private string LocateExact(ApparatusLayerFragment fr, int y,
            string normLine)
        {
            foreach (ApparatusEntry entry in fr.Entries.Where(
                e => !string.IsNullOrEmpty(e.NormValue)))
            {
                int i = normLine.IndexOf(entry.NormValue);
                if (i > -1)
                {
                    int x = GetX(normLine, i);
                    Logger?.LogInformation(
                        $"Location {fr.Location} got from entry {entry} on {normLine}");
                    return $"{y}.{x}";
                }
            }
            return null;
        }

        private string LocateFuzzy(ApparatusLayerFragment fr, int y,
            string normLine)
        {
            // brute force fuzzy matching, no special need to optimize,
            // that's run-once stuff
            int bestIndex = -1;
            double bestScore = 0;
            ApparatusEntry matchedEntry = null;

            foreach (ApparatusEntry entry in fr.Entries.Where(
                e => !string.IsNullOrEmpty(e.NormValue)))
            {
                // we assume that the length of the compared forms is the same
                for (int i = 0; i < normLine.Length - entry.NormValue.Length; i++)
                {
                    string b = normLine.Substring(i, entry.NormValue.Length);
                    double p = JaroWinkler.Proximity(entry.NormValue, b);
                    if (p >= _treshold && p > bestScore)
                    {
                        matchedEntry = entry;
                        bestIndex = i;
                        bestScore = p;
                        break;
                    }
                }
            }

            if (bestIndex > -1)
            {
                int x = GetX(normLine, bestIndex);
                Logger?.LogInformation(
                    $"Location {fr.Location} got from entry {matchedEntry} on {normLine}");
                return $"{y}.{x}";
            }
            return null;
        }

        /// <summary>
        /// Get the location of <paramref name="fragment"/> in the specified
        /// normalized line.
        /// </summary>
        /// <param name="fragment">The fragment to locate.</param>
        /// <param name="y">The Y value for <paramref name="line"/>.</param>
        /// <param name="line">The normalized line.</param>
        /// <returns>The fragment's location or null.</returns>
        /// <exception cref="ArgumentNullException">fragments or line</exception>
        public string LocateFragment(ApparatusLayerFragment fragment, int y, string line)
        {
            if (fragment == null) throw new ArgumentNullException(nameof(fragment));
            if (line == null) throw new ArgumentNullException(nameof(line));

            string loc = null;
            foreach (ApparatusEntry entry in fragment.Entries.Where(
                e => !string.IsNullOrEmpty(e.NormValue)))
            {
                loc = LocateExact(fragment, y, line);
                if (loc != null) return loc;
            }

            if ((loc = LocateFuzzy(fragment, y, line)) == null)
            {
                Logger?.LogWarning($"Unable to locate fragment at line {y}");
                return null;
            }
            return loc;
        }

        /// <summary>
        /// Get the location of <paramref name="fragment"/> in the specified
        /// normalized line, using only the head entry with a value.
        /// </summary>
        /// <param name="fragment">The fragment to locate.</param>
        /// <param name="y">The Y value for <paramref name="line"/>.</param>
        /// <param name="line">The normalized line.</param>
        /// <returns>The fragment's location or null.</returns>
        /// <exception cref="ArgumentNullException">fragments or line</exception>
        public string LocateFragmentFromHead(ApparatusLayerFragment fragment,
            int y, string line)
        {
            if (fragment == null) throw new ArgumentNullException(nameof(fragment));
            if (line == null) throw new ArgumentNullException(nameof(line));

            // consider only the head entry with a value
            ApparatusEntry entry = fragment.Entries[0];
            if (string.IsNullOrEmpty(entry.NormValue))
            {
                Logger?.LogWarning($"No head entry for locating fragment at line {y}");
                return null;
            }

            string loc = LocateExact(fragment, y, line);
            if (loc != null) return loc;

            if ((loc = LocateFuzzy(fragment, y, line)) == null)
            {
                Logger?.LogWarning(
                    $"Unable to locate fragment from head entry at line {y}");
                return null;
            }
            return loc;
        }

        private void MergeFragmentTags(ApparatusLayerFragment source,
            ApparatusLayerFragment target)
        {
            StringBuilder sb = new StringBuilder();
            int srcTagSepIndex = source.Tag.LastIndexOf('@');
            int dstTagSepIndex = target.Tag.LastIndexOf('@');

            // source fragment ID
            for (int i = 0; i < srcTagSepIndex; i++) sb.Append(source.Tag[i]);
            // -target fragment ID
            sb.Append('-');
            for (int i = 0; i < dstTagSepIndex; i++) sb.Append(target.Tag[i]);

            // separator
            sb.Append(' ');
            // source line ID
            for (int i = srcTagSepIndex; i < source.Tag.Length; i++)
                sb.Append(source.Tag[i]);
            // -target line ID
            sb.Append('-');
            for (int i = dstTagSepIndex; i < target.Tag.Length; i++)
                sb.Append(target.Tag[i]);

            target.Tag = sb.ToString();
        }

        private void MergeFragments(ApparatusLayerFragment source,
            ApparatusLayerFragment target)
        {
            // tag
            MergeFragmentTags(source, target);

            // entries
            target.Entries.AddRange(source.Entries);
        }

        /// <summary>
        /// Locates all the fragments specified.
        /// </summary>
        /// <param name="fragments">The fragments to locate.</param>
        /// <exception cref="ArgumentNullException">fragments</exception>
        public void LocateFragments(List<ApparatusLayerFragment> fragments)
        {
            if (fragments == null)
                throw new ArgumentNullException(nameof(fragments));

            // collect all the detected fragments locations in a list,
            // having a location (or null) for each fragment
            List<string> frLocations = new List<string>();
            string normLine = null, currentLineId = null;

            for (int i = 0; i < fragments.Count; i++)
            {
                ApparatusLayerFragment fr = fragments[i];

                // y is in the fake location
                int y = int.Parse(fr.Location.Substring(0, fr.Location.IndexOf('.')),
                    CultureInfo.InvariantCulture);

                // line ID is in the tag after a space
                string lineId = fr.Tag.Substring(fr.Tag.LastIndexOf(' ') + 1);
                if (currentLineId != lineId)
                {
                    normLine = LemmaFilter.Apply(_getLine(lineId));
                    currentLineId = lineId;
                }

                frLocations.Add(LocateFragmentFromHead(fr, y, normLine));
            }

            // assign locations merging those fragments with the same location,
            // or those fragments between two fragments with the same location
            for (int frIndex = 0; frIndex < frLocations.Count; frIndex++)
            {
                if (frLocations[frIndex] != null)
                {
                    int lastFrIndex =
                        frLocations.FindLastIndex(l => l == frLocations[frIndex]);
                    fragments[lastFrIndex].Location = frLocations[frIndex];

                    for (int i = frIndex; i < lastFrIndex; i++)
                    {
                        MergeFragments(fragments[i], fragments[i + 1]);
                    }
                    fragments.RemoveRange(frIndex, lastFrIndex - frIndex);
                }
            }
        }
    }
}
