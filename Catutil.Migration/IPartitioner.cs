using System;
using System.Collections.Generic;

namespace Catutil.Migration
{
    /// <summary>
    /// A partitioner.
    /// </summary>
    public interface IPartitioner
    {
        /// <summary>
        /// Partitions the list with the specified count of items.
        /// </summary>
        /// <param name="count">The items count.</param>
        /// <param name="isBreakableFunc">The function to check whether the item
        /// at the specified index can be a break (=last item of chunk)
        /// candidate.</param>
        /// <returns>List of tuples where 1=start index and 2=length for each
        /// chunk.</returns>
        IList<Tuple<int, int>> Partition(int count, Func<int, bool> isBreakableFunc);
    }
}
