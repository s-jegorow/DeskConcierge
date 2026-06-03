using System.Text;
using DeskConcierge.Core.Pipeline;
using Xunit;

namespace DeskConcierge.Core.Tests.Pipeline;

public class Sha256HasherTests
{
    [Theory]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    public void Compute_ReturnsKnownVector(string input, string expected)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        var hash = Sha256Hasher.Compute(stream);

        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Compute_ReturnsLowercaseHex()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        var hash = Sha256Hasher.Compute(stream);

        Assert.Equal(hash.ToLowerInvariant(), hash);
    }
}
