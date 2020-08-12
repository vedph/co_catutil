using Cadmus.Philology.Parts.Layers;
using Microsoft.Extensions.Logging;
using Proteus.Core;
using System;
using System.Linq;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Lemma locator.
    /// </summary>
    /// <seealso cref="IHasLogger" />
    public sealed class LemmaLocator : IHasLogger
    {
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
        /// Initializes a new instance of the <see cref="LemmaLocator"/> class.
        /// </summary>
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
        /// Locates the specified fragment in the specified normalized line.
        /// </summary>
        /// <param name="fragment">The fragment to locate.</param>
        /// <param name="y">The Y value for <paramref name="line"/>.</param>
        /// <param name="line">The normalized line.</param>
        /// <returns>The fragment's location or null.</returns>
        /// <exception cref="ArgumentNullException">fragments or line</exception>
        public string Locate(ApparatusLayerFragment fragment, int y, string line)
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
    }
}
