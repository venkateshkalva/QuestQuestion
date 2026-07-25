# Palisades Fire Plaintiff Questionnaire — Razor Pages Sample

A single-page Razor Pages form that mirrors the "Plaintiff Fact Sheet"
questionnaire (Ignition & Spread, Outages, Equipment & Poles, Water Use,
Brush Clearance, Other Defendant Info), with:

- **Server-side model validation** via Data Annotations (`[Required]`,
  `[StringLength]`, `[EmailAddress]`, etc.) using **nullable enums** for
  Yes/No questions so an unanswered radio group is correctly rejected.
- **Client-side UX**: Bootstrap 5 accordion sections, conditional
  follow-up fields that appear only when relevant, live character
  counters for the 500/5000-char limits, and inline field-level errors.
- **Explicit Save / Save & Next** actions: answers remain on the page after
  a save, and both actions use the same server-side save path.
- **XML API submission**: a flattened `<NewDataSet>` payload is sent through
  a typed, Polly-resilient `HttpClient`. The first save sends
  `<LQID>-1</LQID>`; the returned LQ ID is held in the server session and
  reused on subsequent saves to update the same record.
- **CSRF protection** on each save request via the antiforgery token, sent
  as a header rather than a hidden form field.

## Project layout

```
Models/
  FireQuestionnaireModel.cs   Section models + validation attributes
  ApiModels.cs                SubmitResult / API request envelope
Services/
  IQuestionnaireApiService.cs / QuestionnaireApiService.cs
                               Typed HttpClient wrapper for the downstream API
  IQuestionnaireDraftStore.cs / QuestionnaireDraftStore.cs
                               Session-backed draft persistence
Pages/
  Questionnaire.cshtml(.cs)   The form + page handlers (Get / Submit)
  Shared/_Layout.cshtml       Bootstrap 5 shell
wwwroot/
  js/questionnaire.js         Conditional fields, Save actions, AJAX submit, modal
  css/questionnaire.css       Minor styling
```

## Running locally

```bash
dotnet restore
dotnet run
```

Then browse to `https://localhost:{port}/` (the questionnaire is mapped
to the site root via `AddPageRoute`).

## Wiring up your real API

Set the downstream API base URL in `appsettings.json` (or an environment
variable / user secret, per environment):

```json
{
  "QuestionnaireApi": { "BaseUrl": "https://your-api.example.com/" }
}
```

`QuestionnaireApiService` posts XML to `POST {BaseUrl}api/v1/questionnaires`
and expects either:

- `200/201` with an XML `<LQID>...</LQID>` element or JSON `lqId` field →
  retained for future saves and shown in the success modal
- `400` with `{ "errors": { "FieldName": ["message"] } }` → mapped back
  onto the form fields
- Any other status → treated as a generic failure with a friendly message

Adjust `SubmitEndpoint` / the response DTOs in `QuestionnaireApiService.cs`
to match your actual API contract.

## Notes / things to double check before production use

- **HTTPS/cookies**: `Cookie.SecurePolicy = Always` assumes the app is
  served over HTTPS (recommended). Relax this only for local HTTP-only dev.
- **Session store**: `AddDistributedMemoryCache()` is in-memory and
  single-instance only — swap for Redis/SQL Server distributed cache
  before running more than one instance behind a load balancer.
- **Validation is intentionally conservative**: only the top-level
  Yes/No questions are `[Required]`; nested follow-up detail fields are
  optional server-side because they're conditionally shown/hidden by
  the client. If your business rules require them, add
  `[RequiredIf]`-style custom validation.
