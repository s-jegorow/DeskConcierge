using System.Text;
using DeskConcierge.Core.Pipeline;
using Xunit;

namespace DeskConcierge.Core.Tests.Pipeline;

public class IngestStageTests
{
    [Fact]
    public void Ingest_ProducesDocumentWithPathAndHash()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("abc"));

            var doc = new IngestStage().Ingest(path);

            Assert.Equal(path, doc.OriginalPath);
            Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", doc.ContentHash);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
