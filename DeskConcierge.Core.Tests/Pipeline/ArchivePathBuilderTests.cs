using DeskConcierge.Core.Pipeline;
using Xunit;

namespace DeskConcierge.Core.Tests.Pipeline;

public class ArchivePathBuilderTests
{
    private static readonly DateTimeOffset Upload = new(2026, 6, 4, 9, 0, 0, TimeSpan.Zero);
    private const string Hash = "a1b2c3d4e5f6aaaa";

    [Fact]
    public void Build_WithSenderAndInvoice_ReadableFolderAndSlugName()
    {
        var path = ArchivePathBuilder.BuildRelativePath(Upload, "Telekommunikation Nord AG", "TK-2026-99812", Hash, ".png");

        Assert.Equal("2026/Telekommunikation Nord AG/2026-06-04_telekommunikation-nord-ag_tk-2026-99812.png", path);
    }

    [Fact]
    public void Build_NullSender_FallsBackToUnbekanntAndHash()
    {
        var path = ArchivePathBuilder.BuildRelativePath(Upload, null, null, Hash, ".pdf");

        Assert.Equal("2026/Unbekannt/2026-06-04_unbekannt_a1b2c3d4.pdf", path);
    }

    [Fact]
    public void Build_Umlauts_KeptInFolderTransliteratedInName()
    {
        var path = ArchivePathBuilder.BuildRelativePath(Upload, "Bürgeramt Musterstadt", null, Hash, "png");

        Assert.Equal("2026/Bürgeramt Musterstadt/2026-06-04_buergeramt-musterstadt_a1b2c3d4.png", path);
    }

    [Fact]
    public void Build_StripsPathBreakingCharsFromFolder()
    {
        var path = ArchivePathBuilder.BuildRelativePath(Upload, "AOK Plus / Nord", null, Hash, ".jpg");

        Assert.Equal("2026/AOK Plus Nord/2026-06-04_aok-plus-nord_a1b2c3d4.jpg", path);
    }
}
