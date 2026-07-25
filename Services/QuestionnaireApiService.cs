using System.Net;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestDetails.Models;

namespace QuestDetails.Services
{
    /// <summary>
    /// Talks to the downstream questionnaire intake API using a named/typed
    /// HttpClient (registered in Program.cs with base address + Polly retry policy).
    /// Every failure path returns a SubmitResult rather than throwing, so the
    /// calling PageModel/controller doesn't need its own try/catch for HTTP concerns.
    /// </summary>
    public class QuestionnaireApiService : IQuestionnaireApiService
    {
        private const string SubmitEndpoint = "api/v1/questionnaires";
        private static long _nextMockLqId = 10000;

        private readonly HttpClient _httpClient;
        private readonly ILogger<QuestionnaireApiService> _logger;
        private readonly bool _useMockApi;

        public QuestionnaireApiService(
            HttpClient httpClient,
            ILogger<QuestionnaireApiService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _useMockApi = configuration.GetValue<bool>("QuestionnaireApi:UseMock");
        }

        public async Task<SubmitResult> SubmitAsync(
            QuestionnaireSubmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var xml = BuildRequestXml(request);
                if (_useMockApi)
                {
                    var lqId = request.LqId > 0
                        ? request.LqId
                        : Interlocked.Increment(ref _nextMockLqId);

                    _logger.LogInformation("Mock questionnaire API saved LQ ID {LqId} for session {SessionId}", lqId, request.SessionId);
                    return new SubmitResult
                    {
                        Success = true,
                        LqId = lqId,
                        Message = $"Mock API success. Submitted XML:\n{xml}"
                    };
                }

                using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
                using var response = await _httpClient.PostAsync(SubmitEndpoint, content, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var lqId = ExtractLqId(responseBody) ?? (request.LqId > 0 ? request.LqId : null);

                    // The first save must return an ID. Without it, a later
                    // save would be forced to use -1 and could create a second
                    // record in the downstream system.
                    if (request.LqId <= 0 && lqId is null)
                    {
                        _logger.LogError("Questionnaire create succeeded but did not return an LQID for session {SessionId}", request.SessionId);
                        return new SubmitResult
                        {
                            Success = false,
                            Message = "The record was saved, but the API did not return an LQ ID. Please do not save again until support can verify the record."
                        };
                    }

                    return new SubmitResult
                    {
                        Success = true,
                        Message = "Your questionnaire was saved successfully.",
                        LqId = lqId,
                        ReferenceNumber = ExtractReferenceNumber(responseBody)
                    };
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemPayload>(
                        cancellationToken: cancellationToken);

                    return new SubmitResult
                    {
                        Success = false,
                        Message = "The API rejected the submission because of validation errors.",
                        Errors = problem?.Errors
                    };
                }

                _logger.LogWarning(
                    "Questionnaire submission failed with status {StatusCode} for session {SessionId}",
                    response.StatusCode, request.SessionId);

                return new SubmitResult
                {
                    Success = false,
                    Message = $"The server responded with an unexpected status ({(int)response.StatusCode}). Please try again."
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Questionnaire submission timed out for session {SessionId}", request.SessionId);
                return new SubmitResult
                {
                    Success = false,
                    Message = "The request timed out. Please check your connection and try again."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting questionnaire for session {SessionId}", request.SessionId);
                return new SubmitResult
                {
                    Success = false,
                    Message = "We couldn't reach the server. Please try again in a moment."
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed API response for session {SessionId}", request.SessionId);
                return new SubmitResult
                {
                    Success = false,
                    Message = "The server returned an unexpected response. Please contact support if this continues."
                };
            }
        }

        private static string BuildRequestXml(QuestionnaireSubmissionRequest request)
        {
            var root = new XElement("NewDataSet", new XElement("LQID", request.LqId));
            AddModelProperties(root, request.Answers);
            return new XDocument(new XDeclaration("1.0", "utf-8", null), root)
                .ToString(SaveOptions.DisableFormatting);
        }

        private static void AddModelProperties(XElement root, object model)
        {
            foreach (var property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || property.Name is nameof(FireQuestionnaireModel.SessionId) or nameof(FireQuestionnaireModel.LqId))
                {
                    continue;
                }

                var value = property.GetValue(model);
                if (value is null)
                {
                    continue;
                }

                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (type == typeof(string))
                {
                    if (!string.IsNullOrWhiteSpace((string)value))
                    {
                        root.Add(new XElement(property.Name, value));
                    }
                    continue;
                }

                if (type.IsEnum)
                {
                    root.Add(new XElement(property.Name, Convert.ToInt32(value, CultureInfo.InvariantCulture)));
                    continue;
                }

                if (type.IsClass)
                {
                    AddModelProperties(root, value);
                    continue;
                }

                root.Add(new XElement(property.Name, Convert.ToString(value, CultureInfo.InvariantCulture)));
            }
        }

        private static long? ExtractLqId(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody)) return null;

            try
            {
                var document = XDocument.Parse(responseBody);
                var value = document.Descendants()
                    .FirstOrDefault(element => string.Equals(element.Name.LocalName, "LQID", StringComparison.OrdinalIgnoreCase))
                    ?.Value;
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lqId) && lqId > 0
                    ? lqId
                    : null;
            }
            catch (System.Xml.XmlException)
            {
                // Some deployments return JSON. Support it while still posting XML.
                try
                {
                    using var document = JsonDocument.Parse(responseBody);
                    return FindJsonLong(document.RootElement, "lqid");
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        private static string? ExtractReferenceNumber(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                return FindJsonString(document.RootElement, "referenceNumber");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static long? FindJsonLong(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt64(out var number)
                            ? number
                            : long.TryParse(property.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? number : null;
                    }
                    var nested = FindJsonLong(property.Value, propertyName);
                    if (nested is not null) return nested;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                {
                    var nested = FindJsonLong(child, propertyName);
                    if (nested is not null) return nested;
                }
            }
            return null;
        }

        private static string? FindJsonString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.GetString();
                }
            }
            return null;
        }

        private sealed class ValidationProblemPayload
        {
            public Dictionary<string, string[]>? Errors { get; set; }
        }
    }
}
