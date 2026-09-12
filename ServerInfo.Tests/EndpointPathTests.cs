using ServerInfo;
using Xunit;

namespace ServerInfo.Tests
{
    public class EndpointPathTests
    {
        [Theory]
        [InlineData(null, "/serverinfo")]
        [InlineData("", "/serverinfo")]
        [InlineData("   ", "/serverinfo")]
        [InlineData("/serverinfo", "/serverinfo")]
        [InlineData("serverinfo", "/serverinfo")]
        [InlineData("/Status", "/status")]
        [InlineData("  /custom-path  ", "/custom-path")]
        public void Normalize_ProducesLowercaseLeadingSlashPath(string input, string expected)
        {
            Assert.Equal(expected, EndpointPath.Normalize(input));
        }
    }
}
