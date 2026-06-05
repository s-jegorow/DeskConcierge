namespace DeskConcierge.Infrastructure.Llm;

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gemma3:12b";
}
