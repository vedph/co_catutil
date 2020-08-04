using System;
using System.Collections.Generic;
using Xunit;

namespace Catutil.Migration.Test
{
    public sealed class PartitionerTest
    {
        [Fact]
        public void Partition_Empty_Empty()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(0, _ => false);
            Assert.Empty(ranges);
        }

        [Fact]
        public void Partition_LessThanMax_Single()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(10, _ => false);
            Assert.Single(ranges);
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(10, ranges[0].Item2);
        }

        [Fact]
        public void Partition_EqualToMax_Single()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(50, _ => false);
            Assert.Single(ranges);
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(50, ranges[0].Item2);
        }

        [Fact]
        public void Partition_GreaterThanMaxSecondLessThanMin_Single()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(60,
                i =>
                {
                    return i switch
                    {
                        57 => true,
                        _ => false,
                    };
                });
            Assert.Single(ranges);
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(60, ranges[0].Item2);
        }

        [Fact]
        public void Partition_GreaterThanMaxSecondEqualToMin_Single()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(60,
                i =>
                {
                    return i switch
                    {
                        45 => true,
                        _ => false,
                    };
                });
            Assert.Single(ranges);
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(60, ranges[0].Item2);
        }

        [Fact]
        public void Partition_GreaterThanMaxSecondGreaterThanMin_2()
        {
            StandardPartitioner partitioner = new StandardPartitioner();
            IList<Tuple<int, int>> ranges = partitioner.Partition(80,
                i =>
                {
                    return i switch
                    {
                        60 => true,
                        _ => false,
                    };
                });
            Assert.Equal(2, ranges.Count);
            // 0-49
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(50, ranges[0].Item2);
            // 50-79
            Assert.Equal(50, ranges[1].Item1);
            Assert.Equal(30, ranges[1].Item2);
        }
    }
}
