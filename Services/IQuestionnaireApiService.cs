using QuestDetails.Models;

namespace QuestDetails.Services
{
    /// <summary>
    /// Abstraction over the downstream questionnaire intake API.
    /// Kept as an interface so it can be mocked in unit tests and
    /// swapped for a different implementation without touching the page.
    /// </summary>
    public interface IQuestionnaireApiService
    {
        Task<SubmitResult> SubmitAsync(QuestionnaireSubmissionRequest request, CancellationToken cancellationToken = default);
    }
}
