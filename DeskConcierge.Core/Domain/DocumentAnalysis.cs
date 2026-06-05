namespace DeskConcierge.Core.Domain;

public sealed record Appointment(string Date, string Subject);

// what the llm makes of a document — the fuzzy stuff regex can't reach
public sealed record DocumentAnalysis(
    string? Sender,
    string DocumentType,
    string Summary,
    IReadOnlyList<Appointment> Appointments,
    bool ActionRequired);
