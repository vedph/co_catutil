using Catutil.Migration.Xls;
using System.Text;
using Xunit;

namespace Catutil.Migration.Test
{
    public class AppTextItalicHelperTest
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("abc", "abc")]
        [InlineData("hello {world}", "hello {world}")]
        [InlineData("hello {world} of {fame}", "hello {world} of {fame}")]
        [InlineData("hello {{world}}", "hello {world}")]
        [InlineData("hello {{{world}}}", "hello {world}")]
        public void ReduceSequencesOf_Ok(string input, string expected)
        {
            StringBuilder sb = new StringBuilder(input);
            AppTextItalicHelper.ReduceSequencesOf('{', sb);
            AppTextItalicHelper.ReduceSequencesOf('}', sb);
            string actual = sb.ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AdjustItalicSpaces_NoItalic_Unchanged()
        {
            const string expected = "Hello, world!";
            string actual = AppTextItalicHelper.AdjustItalicSpaces(expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicOnWords_Unchanged()
        {
            const string expected = "Hello, {big} and {strange} world!";
            string actual = AppTextItalicHelper.AdjustItalicSpaces(expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtStart_Adjusted()
        {
            string actual = AppTextItalicHelper.AdjustItalicSpaces(
                "Hello,{ big} world!");
            Assert.Equal("Hello, {big} world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtEnd_Adjusted()
        {
            string actual = AppTextItalicHelper.AdjustItalicSpaces(
                "Hello, {big }world!");
            Assert.Equal("Hello, {big} world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtStartAndEnd_Adjusted()
        {
            string actual = AppTextItalicHelper.AdjustItalicSpaces(
                "Hello,{ big }world!");
            Assert.Equal("Hello, {big} world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicMultiple_Adjusted()
        {
            string actual = AppTextItalicHelper.AdjustItalicSpaces(
                "Hello{ start}, {end }| and{ both} edges!");
            Assert.Equal("Hello {start}, {end} | and {both} edges!", actual);
        }
    }
}
