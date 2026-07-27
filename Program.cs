using FluentValidation;
using QuestDetails.Services;
using QuestDetails.Validators;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// MVC / Razor Pages
// ---------------------------------------------------------------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Questionnaire", "");
})
.AddJsonOptions(options =>
{
    // The client posts enum-backed fields (Yes/No, water provider, etc.)
    // as plain strings, e.g. {"WaterProvider":"LADWP"}. This converter
    // lets [FromBody] binding map those strings straight onto the enums
    // instead of requiring numeric values.
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// FluentValidation is invoked manually in the submit handler. This keeps
// validation asynchronous-friendly and avoids MVC's legacy auto-validation.
builder.Services.AddValidatorsFromAssemblyContaining<FireQuestionnaireValidator>();

// ---------------------------------------------------------------
// Session: server-side state, HttpOnly + Secure signed cookie holds
// only the session id. 30-minute sliding timeout is typical for a
// long intake form; adjust to your org's security policy.
// ---------------------------------------------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "PalisadesQuestionnaire.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------
// Antiforgery: the JS submits via fetch(), so the token is exposed
// as a cookie + validated header rather than a hidden form field.
// ---------------------------------------------------------------
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// ---------------------------------------------------------------
// Typed HttpClient for the downstream questionnaire API, with a
// resilient retry policy (transient HTTP errors + 5xx + timeouts)
// using exponential backoff, matching production-grade practice.
// ---------------------------------------------------------------
builder.Services.AddHttpClient<IQuestionnaireApiService, QuestionnaireApiService>(client =>
{
    var baseUrl = builder.Configuration["QuestionnaireApi:BaseUrl"]
                  ?? "https://localhost:5443/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/xml, application/json");
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddSingleton<IQuestionnaireDraftStore, QuestionnaireDraftStore>();

var app = builder.Build();

// ---------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAntiforgery();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx and 408
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
