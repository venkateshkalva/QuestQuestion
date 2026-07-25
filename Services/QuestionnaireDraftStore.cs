using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuestDetails.Models;

namespace QuestDetails.Services
{
    public class QuestionnaireDraftStore : IQuestionnaireDraftStore
    {
        private const string DraftSessionKey = "Questionnaire.Draft";
        private readonly ILogger<QuestionnaireDraftStore> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QuestionnaireDraftStore(ILogger<QuestionnaireDraftStore> logger)
        {
            _logger = logger;
        }

        public FireQuestionnaireModel LoadOrCreate(ISessionAccessor session)
        {
            var raw = session.GetString(DraftSessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new FireQuestionnaireModel();
            }

            try
            {
                var restored = JsonSerializer.Deserialize<FireQuestionnaireModel>(raw, JsonOptions);
                return restored ?? new FireQuestionnaireModel();
            }
            catch (JsonException ex)
            {
                // A corrupted or stale draft should never block the user from
                // starting a fresh form -- log and fall back gracefully.
                _logger.LogWarning(ex, "Failed to deserialize questionnaire draft from session; starting fresh.");
                return new FireQuestionnaireModel();
            }
        }

        public void Save(ISessionAccessor session, FireQuestionnaireModel model)
        {
            var json = JsonSerializer.Serialize(model, JsonOptions);
            session.SetString(DraftSessionKey, json);
        }

        public void Clear(ISessionAccessor session)
        {
            session.Remove(DraftSessionKey);
        }
    }

    /// <summary>ISessionAccessor implementation backed by HttpContext.Session.</summary>
    public class HttpSessionAccessor : ISessionAccessor
    {
        private readonly ISession _session;

        public HttpSessionAccessor(ISession session)
        {
            _session = session;
        }

        public string? GetString(string key) => _session.GetString(key);

        public void SetString(string key, string value) => _session.SetString(key, value);

        public void Remove(string key) => _session.Remove(key);
    }
}
