namespace QuestDetails.Models
{
    /// <summary>Standard envelope returned by the internal submit endpoint / AJAX calls.</summary>
    public class SubmitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public long? LqId { get; set; }
        public IDictionary<string, string[]>? Errors { get; set; }
    }

    /// <summary>What we actually POST to the downstream questionnaire API.</summary>
    public class QuestionnaireSubmissionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public long LqId { get; set; } = -1;
        public FireQuestionnaireModel Answers { get; set; } = new();
    }
}
