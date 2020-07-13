using Catutil.Migration.Xls;
using Xunit;

namespace Catutil.Migration.Test
{
    public class AppTextMdHelperTest
    {
        [Fact]
        public void AdjustItalicSpaces_NoItalic_Unchanged()
        {
            const string expected = "Hello, world!";
            string actual = AppTextMdHelper.AdjustItalicSpaces(expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicOnWords_Unchanged()
        {
            const string expected = "Hello, _big_ and _strange_ world!";
            string actual = AppTextMdHelper.AdjustItalicSpaces(expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtStart_Adjusted()
        {
            string actual = AppTextMdHelper.AdjustItalicSpaces(
                "Hello,_ big_ world!");
            Assert.Equal("Hello, _big_ world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtEnd_Adjusted()
        {
            string actual = AppTextMdHelper.AdjustItalicSpaces(
                "Hello, _big _world!");
            Assert.Equal("Hello, _big_ world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicAtStartAndEnd_Adjusted()
        {
            string actual = AppTextMdHelper.AdjustItalicSpaces(
                "Hello,_ big _world!");
            Assert.Equal("Hello, _big_ world!", actual);
        }

        [Fact]
        public void AdjustItalicSpaces_ItalicMultiple_Adjusted()
        {
            string actual = AppTextMdHelper.AdjustItalicSpaces(
                "Hello_ start_, _end _| and_ both_ edges!");
            Assert.Equal("Hello _start_, _end_ | and _both_ edges!", actual);
        }
    }
}
