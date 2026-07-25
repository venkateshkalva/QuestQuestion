using QuestDetails.Models;

namespace QuestDetails.Services
{
    /// <summary>
    /// Persists an in-progress questionnaire to the user's session so a page
    /// refresh, accidental navigation, or session timeout warning doesn't
    /// lose their answers. Backed by ASP.NET Core's ISession (server-side,
    /// signed cookie holds only the session id).
    /// </summary>
    public interface IQuestionnaireDraftStore
    {
        FireQuestionnaireModel LoadOrCreate(ISessionAccessor session);
        void Save(ISessionAccessor session, FireQuestionnaireModel model);
        void Clear(ISessionAccessor session);
    }

    /// <summary>
    /// Thin wrapper so the store doesn't take a hard dependency on
    /// HttpContext.Session directly, which keeps it unit-testable.
    /// </summary>
    public interface ISessionAccessor
    {
        string? GetString(string key);
        void SetString(string key, string value);
        void Remove(string key);
    }
}
