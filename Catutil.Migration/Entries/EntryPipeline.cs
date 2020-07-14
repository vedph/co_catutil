using Microsoft.Extensions.Logging;
using Proteus.Core.Entries;
using Proteus.Core.Regions;
using Proteus.Entries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catutil.Migration.Entries
{
    /// <summary>
    /// Proteus entries processor pipeline.
    /// </summary>
    public sealed class EntryPipeline
    {
        private ILogger _logger;

        /// <summary>
        /// Gets the entry filters.
        /// </summary>
        public IList<IEntryFilter> Filters { get; private set; }

        /// <summary>
        /// Gets the region detectors.
        /// </summary>
        public IList<IEntryRegionDetector> RegionDetectors { get; private set; }

        /// <summary>
        /// Gets the region filters.
        /// </summary>
        public IList<IEntryRegionFilter> RegionFilters { get; private set; }

        /// <summary>
        /// Gets the region parsers.
        /// </summary>
        public IList<IEntryRegionParser> RegionParsers { get; private set; }

        /// <summary>
        /// Configures this pipeline using the specified factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        public void Configure(EntryParserFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            _logger = factory.Logger;
            Filters = factory.GetEntryFilters();
            RegionDetectors = factory.GetRegionDetectors();
            RegionFilters = factory.GetRegionFilters();
            RegionParsers = factory.GetRegionParsers();
        }

        /// <summary>
        /// Sets the regions snapshot from the specified set.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="snapshot">The snapshot.</param>
        /// <exception cref="ArgumentNullException">set or snapshot</exception>
        private static void SetRegionSetSnapshot(EntryRegionSet set,
            List<string> snapshot)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            snapshot.Clear();
            foreach (EntryRegion region in set.Regions)
                snapshot.Add($"{region.Range} {region.Tag}");
        }

        /// <summary>
        /// Determines whether the specified set of regions has changed.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="snapshot">The snapshot.</param>
        /// <returns><c>true</c> if changed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">set or snapshot</exception>
        private static bool HasRegionSetChanged(EntryRegionSet set,
            List<string> snapshot)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            if (set.Regions.Count != snapshot.Count) return true;
            for (int i = 0; i < snapshot.Count; i++)
            {
                EntryRegion region = set.Regions[i];
                if ($"{region.Range} {region.Tag}" != snapshot[i]) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a set of entry regions by applying entry filters, region
        /// detectors, and region filters.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <returns>The regions set.</returns>
        /// <exception cref="ArgumentNullException">set</exception>
        public EntryRegionSet GetRegionSet(EntrySet set)
        {
            if (set is null) throw new ArgumentNullException(nameof(set));

            EntryRegionSet regionSet = new EntryRegionSet();

            // entry filters
            bool repeat = false;
            int repeatCount = 0;
            List<string> snapshot = new List<string>();

            do
            {
                SetRegionSetSnapshot(regionSet, snapshot);

                // (1) apply entry filters
                if (Filters?.Count > 0)
                {
                    foreach (IEntryFilter filter in Filters.Where(f => !f.IsDisabled))
                    {
                        filter.Reset();
                        filter.Apply(set.Entries);
                    }
                }

                // (2) detect regions
                if (RegionDetectors?.Count > 0)
                {
                    foreach (IEntryRegionDetector detector in RegionDetectors)
                        detector.Detect(set.Entries, regionSet);
                }

                // (3) filter regions
                if (RegionFilters?.Count > 0)
                {
                    foreach (IEntryRegionFilter filter in
                        RegionFilters.Where(f => !f.IsDisabled))
                    {
                        repeat = filter.Apply(set.Entries, regionSet);
                        if (repeat) break;
                    }
                }

                if (repeat)
                {
                    // avoid infinite loop if the set did not change,
                    // or if it we repeated more than a max count
                    // (to avoid corner cases where e.g. two subsequent
                    // calls to the filters produce a cyclically returning
                    // set)
                    if (!HasRegionSetChanged(regionSet, snapshot)
                        || ++repeatCount > 1000) break;

                    // reset and reloop
                    regionSet.Clear();
                }
            } while (repeat);

            return regionSet;
        }

        /// <summary>
        /// Executes this pipeline on the specified set of entries, using
        /// <paramref name="target"/> as the target object which will get
        /// the results of this execution.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="set">The entries set.</param>
        /// <param name="target">The target object or null.</param>
        /// <returns>The entry region set built.</returns>
        /// <exception cref="ArgumentNullException">set</exception>
        public EntryRegionSet Execute<T>(EntrySet set, T target) where T : class
        {
            if (set is null) throw new ArgumentNullException(nameof(set));

            // get regions
            EntryRegionSet regionSet = GetRegionSet(set);

            // apply the first matching parser for each region
            for (int i = 0; i < regionSet.Regions.Count; i++)
            {
                IEntryRegionParser parser = RegionParsers.FirstOrDefault(
                    p => p.IsApplicable(set, regionSet.Regions, i));
                if (parser != null)
                {
                    parser.Parse(set, regionSet.Regions, i, target);
                }
                else
                {
                    _logger?.LogWarning($"Unhandled region at {i}: {regionSet.Regions[i]}");
                }
            }

            return regionSet;
        }
    }
}
