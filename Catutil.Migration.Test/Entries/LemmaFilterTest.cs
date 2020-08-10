using Catutil.Migration.Entries;
using Xunit;

namespace Catutil.Migration.Test.Entries
{
    public sealed class LemmaFilterTest
    {
        [Fact]
        public void Apply_Null_Null()
        {
            string filtered = LemmaFilter.Apply(null);
            Assert.Null(filtered);
        }

        [Fact]
        public void Apply_Empty_Empty()
        {
            string filtered = LemmaFilter.Apply("");
            Assert.Equal("", filtered);
        }

        [Theory]
        [InlineData(" ", "")]
        [InlineData("  ", "")]
        [InlineData(" \t ", "")]
        [InlineData(" abc", "abc")]
        [InlineData("abc ", "abc")]
        [InlineData("ab c \td", "ab c d")]
        public void Apply_Whitespaces_Ok(string input, string expected)
        {
            string filtered = LemmaFilter.Apply(input);
            Assert.Equal(expected, filtered);
        }

        [Theory]
        [InlineData(" ", "")]
        [InlineData("  ", "")]
        [InlineData(" \t ", "")]
        [InlineData(" àbc", "abc")]
        [InlineData("Ábç ", "abc")]
        public void Apply_Diacritics_Ok(string input, string expected)
        {
            string filtered = LemmaFilter.Apply(input);
            Assert.Equal(expected, filtered);
        }
    }
}
