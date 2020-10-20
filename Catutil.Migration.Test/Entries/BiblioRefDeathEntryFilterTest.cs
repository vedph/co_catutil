using Catutil.Migration.Entries;
using Proteus.Core.Entries;
using System.Collections.Generic;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class BiblioRefDeathEntryFilterTest
    {
        private const string RES_NAME = "BiblioRefDeathEntryFilter.txt";

        [Fact]
        public void Apply_NoDeath_False()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "no-death");

            BiblioRefDeathEntryFilter filter = new BiblioRefDeathEntryFilter();
            Assert.False(filter.Apply(entries));

            Assert.Equal(3, entries.Count);
        }

        [Fact]
        public void Apply_Death_True()
        {
            List<DecodedEntry> entries =
                TestHelper.LoadResourceEntries(RES_NAME, "death");

            BiblioRefDeathEntryFilter filter = new BiblioRefDeathEntryFilter();
            Assert.True(filter.Apply(entries));

            Assert.Equal(5, entries.Count);

            // [0] start
            DecodedTextEntry txt = entries[0] as DecodedTextEntry;
            Assert.NotNull(txt);
            Assert.Equal("start", txt.Value);

            // [1] italic=1
            DecodedPropertyEntry prp = entries[1] as DecodedPropertyEntry;
            Assert.NotNull(txt);
            Assert.Equal("italic", prp.Name);
            Assert.Equal("1", prp.Value);

            // [2] merged txt
            txt = entries[2] as DecodedTextEntry;
            Assert.NotNull(txt);
            Assert.Equal("Parthenius 1485 et Guarinus (†1503) 1521 e Seruio",
                txt.Value);

            // [3] italic=0
            prp = entries[3] as DecodedPropertyEntry;
            Assert.NotNull(txt);
            Assert.Equal("italic", prp.Name);
            Assert.Equal("0", prp.Value);

            // [4] end
            txt = entries[4] as DecodedTextEntry;
            Assert.NotNull(txt);
            Assert.Equal("end", txt.Value);
        }
    }
}
