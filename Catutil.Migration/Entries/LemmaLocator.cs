using Cadmus.Philology.Parts.Layers;
using Microsoft.Extensions.Logging;
using Proteus.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Lemma locator.
    /// </summary>
    /// <seealso cref="Proteus.Core.IHasLogger" />
    public sealed class LemmaLocator : IHasLogger
    {
        private double _treshold;

        public ILogger Logger { get; set; }

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

        public LemmaLocator()
        {
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

        private bool LocateExact(ApparatusLayerFragment fr, int y,
            string normLine)
        {
            foreach (ApparatusEntry entry in fr.Entries.Where(
                e => !string.IsNullOrEmpty(e.NormValue)))
            {
                int i = normLine.IndexOf(entry.NormValue);
                if (i > -1)
                {
                    int x = GetX(normLine, i);
                    fr.Location = $"{y}.{x}";
                    Logger?.LogInformation(
                        $"Location {fr.Location} got from entry {entry} on {normLine}");
                    return true;
                }
            }
            return false;
        }

        private bool LocateFuzzy(ApparatusLayerFragment fr, int y,
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
                fr.Location = $"{y}.{x}";
                Logger?.LogInformation(
                    $"Location {fr.Location} got from entry {matchedEntry} on {normLine}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Locates the specified fragments in <paramref name="line"/>.
        /// </summary>
        /// <param name="fragments">The fragments.</param>
        /// <param name="y">The Y value for <paramref name="line"/>.</param>
        /// <param name="line">The line.</param>
        /// <exception cref="ArgumentNullException">fragments or line</exception>
        public void Locate(IList<ApparatusLayerFragment> fragments, int y,
            string line)
        {
            if (fragments == null) throw new ArgumentNullException(nameof(fragments));
            if (line == null) throw new ArgumentNullException(nameof(line));

            string normLine = LemmaFilter.Apply(line);

            foreach (ApparatusLayerFragment fr in fragments)
            {
                foreach (ApparatusEntry entry in fr.Entries.Where(
                    e => !string.IsNullOrEmpty(e.NormValue)))
                {
                    if (LocateExact(fr, y, normLine)) break;
                }

                if (fr.Location == null && !LocateFuzzy(fr, y, normLine))
                {
                    Logger?.LogWarning("Unable to locate fragment " +
                        $"at line {y} index {fragments.IndexOf(fr)}");
                }
            }
        }
    }
}
