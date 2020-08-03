using Catutil.Migration.Biblio;
using Catutil.Migration.Xls;
using System;
using System.Linq;
using Xunit;

namespace Catutil.Migration.Test.Biblio
{
    public sealed class BiblioReferenceFinderTest
    {
        private BiblioReferenceFinder GetFinder()
        {
            XlsBiblioLookup lookup = new XlsBiblioLookup();
            lookup.LoadIndex(TestHelper.LoadResourceStream("BiblioLookup.json"));
            return new BiblioReferenceFinder(lookup);
        }

        [Fact]
        public void FindAll_Empty_Empty()
        {
            BiblioReferenceFinder finder = GetFinder();
            Tuple<int, int>[] ranges = finder.FindAll("").ToArray();
            Assert.Empty(ranges);
        }

        [Fact]
        public void FindAll_None_Empty()
        {
            BiblioReferenceFinder finder = GetFinder();
            Tuple<int, int>[] ranges =
                finder.FindAll("Hello, world!").ToArray();
            Assert.Empty(ranges);
        }

        [Fact]
        public void FindAll_Shorter_Empty()
        {
            BiblioReferenceFinder finder = GetFinder();
            Tuple<int, int>[] ranges =
                finder.FindAll("Hello, Aga world!").ToArray();
            Assert.Empty(ranges);
        }

        [Fact]
        public void FindAll_Longer_Empty()
        {
            BiblioReferenceFinder finder = GetFinder();
            Tuple<int, int>[] ranges =
                finder.FindAll("Hello, Agaro world!").ToArray();
            Assert.Empty(ranges);
        }

        [Fact]
        public void FindAll_AtStartMidEnd_3()
        {
            //                   0         1         2         3         4
            //                   0123456789-123456789-123456789-123456789-
            const string text = "Agar dub., probante Akbar Khan 1967 cum Agnesini";
            BiblioReferenceFinder finder = GetFinder();
            Tuple<int, int>[] ranges = finder.FindAll(text).ToArray();

            Assert.Equal(3, ranges.Length);
            // Agar
            Assert.Equal(0, ranges[0].Item1);
            Assert.Equal(4, ranges[0].Item2);
            // Akbar Khan 1967
            Assert.Equal(20, ranges[1].Item1);
            Assert.Equal(15, ranges[1].Item2);
            // Agnesini
            Assert.Equal(40, ranges[2].Item1);
            Assert.Equal(8, ranges[2].Item2);
        }
    }
}
