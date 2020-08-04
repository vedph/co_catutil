using System;
using System.Collections.Generic;
using System.Linq;

namespace Catutil.Migration
{
    /// <summary>
    /// General purpose partitioner. This is used to partition a list of items,
    /// so that it gets split into chunks whose length is around
    /// <see cref="MaxTreshold"/>, never below <see cref="MinTreshold"/>, and
    /// eventually can stretch or shrink for an amount equal to
    /// <see cref="DeltaRatio"/>.
    /// </summary>
    public sealed class StandardPartitioner : IPartitioner
    {
        private int _minTreshold;
        private int _maxTreshold;
        private double _deltaRatio;

        #region Properties        
        /// <summary>
        /// Gets or sets the minimum treshold (default 20).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value less than 0
        /// </exception>
        public int MinTreshold
        {
            get { return _minTreshold; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _minTreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum treshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value less than 0
        /// </exception>
        public int MaxTreshold
        {
            get { return _maxTreshold; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _maxTreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the delta ratio, i.e. the ratio by which a treshold
        /// can be shrinked or extended. Default is 0.1 i.e. 10%.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value less than 0
        /// or greater than 1.0</exception>
        public double DeltaRatio
        {
            get { return _deltaRatio; }
            set
            {
                if (value < 0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _deltaRatio = value;
            }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardPartitioner"/> class.
        /// </summary>
        public StandardPartitioner()
        {
            MinTreshold = 20;
            MaxTreshold = 50;
            DeltaRatio = 0.1;
        }

        /// <summary>
        /// Partitions the list with the specified count of items.
        /// </summary>
        /// <param name="count">The items count.</param>
        /// <param name="isBreakableFunc">The function to check whether the item
        /// at the specified index can be a break (=last item of chunk)
        /// candidate.</param>
        /// <returns>List of tuples where 1=start index and 2=length for each
        /// chunk.</returns>
        /// <exception cref="ArgumentNullException">isBreakableFunc</exception>
        /// <exception cref="InvalidOperationException">Min treshold greater
        /// than max</exception>
        public IList<Tuple<int, int>> Partition(int count,
            Func<int,bool> isBreakableFunc)
        {
            if (isBreakableFunc == null)
                throw new ArgumentNullException(nameof(isBreakableFunc));
            if (MinTreshold > MaxTreshold)
                throw new InvalidOperationException("Min treshold greater than max");

            List<Tuple<int, int>> ranges = new List<Tuple<int, int>>();
            if (count < 1) return ranges;

            // just use a single range if whithin max limit
            if (count <= MaxTreshold)
            {
                ranges.Add(Tuple.Create(0, count));
            }
            else
            {
                // else start looking for each break around multiples of max
                int start = 0, delta = (int)(MaxTreshold * DeltaRatio);
                int i = MaxTreshold - 1;

                while (i < count)
                {
                    // accept a break at exact max
                    if (isBreakableFunc(i))
                    {
                        ranges.Add(Tuple.Create(start, i + 1 - start));
                        start = i + 1;
                        i += MaxTreshold;
                        continue;
                    }
                    // else try forward
                    bool found = false;
                    for (int j = 1; j <= delta && !found && i + j < count; j++)
                    {
                        if (isBreakableFunc(i + j))
                        {
                            ranges.Add(Tuple.Create(start, i + j + 1 - start));
                            start = i + j + 1;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        i += MaxTreshold;
                        continue;
                    }

                    // not found forward, try backwards
                    int backDelta = delta;
                    if (MaxTreshold - delta < MinTreshold)
                        backDelta = i + 1 - start - MinTreshold;

                    for (int j = 1; j <= backDelta && !found && i - j > -1; j++)
                    {
                        if (isBreakableFunc(i - j))
                        {
                            ranges.Add(Tuple.Create(start, i - j + 1 - start));
                            start = i - j + 1;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        i += MaxTreshold;
                        continue;
                    }

                    // not found backwards, give up and truncate mercilessly
                    ranges.Add(Tuple.Create(start, i + 1 - start));
                    start = i + 1;
                    i += MaxTreshold;
                } // for

                // tail if any
                if (i > count)
                {
                    // extend last chunk if possible
                    if (count - start < MinTreshold
                        || count - start <= delta)
                    {
                        Tuple<int, int> last = ranges.Last();
                        ranges.RemoveAt(ranges.Count - 1);
                        ranges.Add(Tuple.Create(last.Item1, count - last.Item1));
                    }
                    // else append a last chunk
                    else
                    {
                        ranges.Add(Tuple.Create(start, count - start));
                    }
                } // tail
            } // else

            return ranges;
        }
    }
}
