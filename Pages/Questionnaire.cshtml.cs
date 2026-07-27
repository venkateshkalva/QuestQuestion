using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuestDetails.Models;
using QuestDetails.Services;

namespace QuestDetails.Pages
{
    public class QuestionnaireModel : PageModel
    {
        private const string LqIdSessionKey = "Questionnaire.LqId";
        private readonly IQuestionnaireApiService _apiService;
        private readonly IValidator<FireQuestionnaireModel> _validator;
        private readonly IQuestionnaireDraftStore _draftStore;
        private readonly ILogger<QuestionnaireModel> _logger;

        public QuestionnaireModel(
            IQuestionnaireApiService apiService,
            IValidator<FireQuestionnaireModel> validator,
            IQuestionnaireDraftStore draftStore,
            ILogger<QuestionnaireModel> logger)
        {
            _apiService = apiService;
            _validator = validator;
            _draftStore = draftStore;
            _logger = logger;
        }

        [BindProperty]
        public FireQuestionnaireModel Form { get; set; } = new();

        /// <summary>
        /// GET: restore any in-progress draft from session so the user
        /// never loses answers to a refresh or timeout.
        /// </summary>
        public void OnGet()
        {
            var session = new HttpSessionAccessor(HttpContext.Session);
            Form = _draftStore.LoadOrCreate(session);
            Form.LqId = long.TryParse(session.GetString(LqIdSessionKey), out var lqId) && lqId > 0
                ? lqId
                : -1;
        }

        /// <summary>
        /// Full submit: the client posts the completed form as a raw JSON
        /// body (not a traditional form-encoded post), so we bind it
        /// explicitly with [FromBody] rather than relying on [BindProperty].
        /// Validates server-side (never trust the client), forwards to the
        /// downstream API, and returns a JSON envelope the front-end uses to
        /// show a success or error modal without a full page reload.
        /// </summary>
        public async Task<IActionResult> OnPostSubmitAsync(
            [FromBody] FireQuestionnaireModel submittedForm,
            CancellationToken cancellationToken)
        {
            if (submittedForm is null)
            {
                return new JsonResult(new SubmitResult
                {
                    Success = false,
                    Message = "No form data was received. Please try again."
                })
                { StatusCode = StatusCodes.Status400BadRequest };
            }

            // Never trust a page-posted LQ ID. The session-held value is the
            // authoritative record identifier for this browser session.
            var session = new HttpSessionAccessor(HttpContext.Session);
            submittedForm.LqId = long.TryParse(session.GetString(LqIdSessionKey), out var existingLqId) && existingLqId > 0
                ? existingLqId
                : -1;

            var validation = await _validator.ValidateAsync(submittedForm, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(error => $"Form.{error.PropertyName}")
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray());

                return new JsonResult(new SubmitResult
                {
                    Success = false,
                    Message = "Please correct the highlighted fields and try again.",
                    Errors = errors
                })
                { StatusCode = StatusCodes.Status400BadRequest };
            }

            var request = new QuestionnaireSubmissionRequest
            {
                SessionId = submittedForm.SessionId,
                LqId = submittedForm.LqId,
                Answers = submittedForm
            };

            var result = await _apiService.SubmitAsync(request, cancellationToken);

            if (result.Success)
            {
                if (result.LqId is > 0)
                {
                    session.SetString(LqIdSessionKey, result.LqId.Value.ToString());
                    submittedForm.LqId = result.LqId.Value;
                }
                _logger.LogInformation(
                    "Questionnaire {SessionId} saved successfully. LQ ID: {LqId}",
                    submittedForm.SessionId, result.LqId);
            }

            return new JsonResult(result)
            {
                StatusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status502BadGateway
            };
        }
    }
}
