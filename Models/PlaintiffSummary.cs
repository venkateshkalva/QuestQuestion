namespace QuestDetails.Models;

/// <summary>
/// Read-only plaintiff metadata displayed above the questionnaire.
/// Populate this from the authenticated intake/case record when that
/// integration is available; it is intentionally not submitted as answers.
/// </summary>
public sealed class PlaintiffSummary
{
    public string? PlaintiffId { get; init; }
    public string? Name { get; init; }
    public string? PlaintiffType { get; init; }
    public DateOnly? DateOfBirth { get; init; }
}
