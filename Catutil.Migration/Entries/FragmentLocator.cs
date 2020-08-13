﻿using Cadmus.Philology.Parts.Layers;
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
    /// <remarks>This locator uses a very raw approach for locating apparatus
    /// layer fragments with reference to their text. In the original CO data
    /// each apparatus entry is attached to its line as a whole; in Cadmus
    /// instead we would rather want to attach it to its exact reference in the
    /// text. Anyway, only some entries have an explicit lemma, and it can also
    /// be the case that it's not exactly in the form it appears in the reference
    /// text. The wording in entries is so various that attempting to provide
    /// an automated location procedure would be much more costly and error-prone
    /// than letting an operator adjust it during editing. So, here the approach
    /// is minimalist, and just wants to provide a location for those entries
    /// which have an evident clue (e.g. the word repeated literally, or with
    /// a few differences, from the source text, even though there might be
    /// corner cases where that word is repeated in the line), while assigning
    /// a fake, unique location to all the others. This location is always true
    /// for the Y value, while it starts from 1000 for the X value, which
    /// grants that it cannot reference any existing word in the line.
    /// <para>Given that often entries referring to the same lemma follow each
    /// other without an explicit text reference, the strategy is just looking
    /// for this text reference in the first entry of each fragment. When found,
    /// the whole fragment gets located according to that.</para>
    /// <para>Also, if several fragments get the same location, all the fragments
    /// included between the first and the last one having that location get
    /// merged into a unique fragment. This avoids overlapping fragments,
    /// which are not allowed by design. Merges are signaled by the fact that
    /// the fragment ID and line ID in the tag of each fragment get appended
    /// to the target fragment after a dash. For instance, if fragment A with
    /// tag <c>1@1.1</c> gets merged into fragment B with tag <c>2@1000.2</c>,
    /// the resulting tag will be <c>1-2@1.1-1000.2</c>.</para>
    /// </remarks>
    /// <seealso cref="IHasLogger" />
    public sealed class FragmentLocator : IHasLogger
    {
        private readonly Func<string, string> _getLine;
        private double _treshold;

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

            if (fragment.Entries == null || fragment.Entries.Count == 0)
                return null;

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

            if (fragment.Entries == null || fragment.Entries.Count == 0)
                return null;

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
            sb.Append('@');
            // source line ID
            for (int i = srcTagSepIndex + 1; i < source.Tag.Length; i++)
                sb.Append(source.Tag[i]);
            // -target line ID
            sb.Append('-');
            for (int i = dstTagSepIndex + 1; i < target.Tag.Length; i++)
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
        /// Locates all the fragments specified, merging them as required.
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

                // true y is in the fake location
                int y = int.Parse(fr.Location.Substring(0, fr.Location.IndexOf('.')),
                    CultureInfo.InvariantCulture);

                // line ID is in the tag after @
                string lineId = fr.Tag.Substring(fr.Tag.LastIndexOf('@') + 1);
                if (currentLineId != lineId)
                {
                    normLine = LemmaFilter.Apply(_getLine(lineId));
                    currentLineId = lineId;
                }

                frLocations.Add(LocateFragmentFromHead(fr, y, normLine));
            }

            // assign locations merging those fragments with the same location,
            // or those fragments between two fragments with the same location
            for (int frIndex = 0; frIndex < fragments.Count; frIndex++)
            {
                // if located
                if (frLocations[frIndex] != null)
                {
                    // assign location to it
                    fragments[frIndex].Location = frLocations[frIndex];

                    // find the last fragment with the same location
                    int lastFrIndex =
                        frLocations.FindLastIndex(l => l == frLocations[frIndex]);
                    if (lastFrIndex != frIndex)
                        fragments[lastFrIndex].Location = frLocations[frIndex];

                    // merge all the fragments in range into the last one
                    for (int i = frIndex; i < lastFrIndex; i++)
                    {
                        // location
                        if (frLocations[i + 1] == null)
                            fragments[i + 1].Location = fragments[i].Location;
                        MergeFragments(fragments[i], fragments[i + 1]);
                    }
                    // remove all the fragments in range except the last one
                    if (lastFrIndex > frIndex)
                        fragments.RemoveRange(frIndex, lastFrIndex - frIndex);
                }
            }
        }
    }
}
