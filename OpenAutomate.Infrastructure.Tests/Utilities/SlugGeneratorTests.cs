using OpenAutomate.Infrastructure.Utilities;
using Xunit;

namespace OpenAutomate.Infrastructure.Tests.Utilities
{
    public class SlugGeneratorTests
    {
        [Fact]
        public void GenerateSlug_RemovesAccentsAndInvalidCharacters()
        {
            var result = SlugGenerator.GenerateSlug("  Héllö Wörld!  ");
            Assert.Equal("hello-world", result);
        }

        [Fact]
        public void GenerateSlug_EmptyString_ReturnsEmpty()
        {
            var result = SlugGenerator.GenerateSlug(string.Empty);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EnsureUniqueSlug_ReturnsBaseWhenUnique()
        {
            string result = SlugGenerator.EnsureUniqueSlug("test", s => false);
            Assert.Equal("test", result);
        }

        [Fact]
        public void EnsureUniqueSlug_AppendsCounterWhenExists()
        {
            int calls = 0;
            bool SlugExists(string slug)
            {
                calls++;
                return slug == "test" || slug == "test-2";
            }

            string result = SlugGenerator.EnsureUniqueSlug("test", SlugExists);
            Assert.Equal("test-3", result);
            Assert.True(calls >= 2);
        }
    }
}
