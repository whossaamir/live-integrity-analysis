using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddCors(o => o.AddPolicy("local", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHttpClient("python", client => client.Timeout = TimeSpan.FromSeconds(10));

var app = builder.Build();
app.UseCors("local");

const string SubmissionFile = "submissions.json";
const int HistoryHours = 48;
var submissions = LoadSubmissions();
var nextId = submissions.Count == 0 ? 1 : submissions.Max(x => x.Id) + 1;

void SaveSubmissions()
{
    try { File.WriteAllText(SubmissionFile, JsonSerializer.Serialize(submissions, new JsonSerializerOptions { WriteIndented = true })); }
    catch { }
}

void RemoveExpiredSubmissions()
{
    var cutoff = DateTime.Now.AddHours(-HistoryHours);
    var before = submissions.Count;
    submissions.RemoveAll(x => x.SubmittedAt < cutoff);
    if (submissions.Count != before) SaveSubmissions();
}

List<Submission> LoadSubmissions()
{
    try
    {
        if (!File.Exists(SubmissionFile)) return new();
        var loaded = JsonSerializer.Deserialize<List<Submission>>(File.ReadAllText(SubmissionFile)) ?? new();
        var cutoff = DateTime.Now.AddHours(-HistoryHours);
        return loaded.Where(x => x.SubmittedAt >= cutoff).OrderBy(x => x.SubmittedAt).ToList();
    }
    catch { return new(); }
}

app.MapGet("/", () => new { project = "AI-Powered Academic Integrity", status = "running" });

app.MapGet("/api/assignments", () => new[] {
    new Assignment(1, "Secure Software Engineering"),
    new Assignment(2, "Information Security"),
    new Assignment(3, "Programming Assignment")
});

app.MapGet("/api/submissions", () => {
    RemoveExpiredSubmissions();
    return Results.Ok(submissions.OrderByDescending(x => x.SubmittedAt).ToList());
});

app.MapGet("/api/live-status", async (IHttpClientFactory factory) =>
{
    try
    {
        var client = factory.CreateClient("python");
        var response = await client.GetAsync("http://127.0.0.1:8000/");
        return Results.Ok(new { backend = "online", python = response.IsSuccessStatusCode ? "online" : "error" });
    }
    catch { return Results.Ok(new { backend = "online", python = "offline" }); }
});

app.MapPost("/api/live-analyze", async (LiveAnalysisRequest request, IHttpClientFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.Content)) return Results.BadRequest(new { error = "No live transcript supplied." });
    try
    {
        var client = factory.CreateClient("python");
        var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/analyze", new { content = request.Content });
        if (!response.IsSuccessStatusCode) return Results.Problem(await response.Content.ReadAsStringAsync());
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResult>();
        if (analysis is null) return Results.Problem("No analysis result was returned by Python.");
        return Results.Ok(new { analysis, analyzedAt = DateTime.Now, wordCount = request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length });
    }
    catch (TaskCanceledException) { return Results.Problem("Python analysis timed out. Make sure python app.py is running."); }
    catch (HttpRequestException) { return Results.Problem("Cannot connect to Python AI engine at http://127.0.0.1:8000."); }
});

app.MapPost("/api/submissions", async (SubmissionRequest request, IHttpClientFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.StudentName) || string.IsNullOrWhiteSpace(request.Content))
        return Results.BadRequest(new { error = "Student name and submission content are required." });
    try
    {
        var client = factory.CreateClient("python");
        var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/analyze", new { content = request.Content });
        if (!response.IsSuccessStatusCode) return Results.Problem("Python analysis service returned an error.");
        var analysis = await response.Content.ReadFromJsonAsync<AnalysisResult>();
        if (analysis is null) return Results.Problem("No analysis result was returned.");
        RemoveExpiredSubmissions();
        var submission = new Submission(nextId++, request.StudentName, request.Assignment, request.ContentType, request.Content, DateTime.Now, analysis);
        submissions.Add(submission);
        SaveSubmissions();
        return Results.Ok(submission);
    }
    catch (HttpRequestException) { return Results.Problem("Python AI engine is not running. Start it with: python app.py"); }
});

app.Run();

record Assignment(int Id, string Name);
record SubmissionRequest(string StudentName, string Assignment, string ContentType, string Content);
record LiveAnalysisRequest(string Content);
record AnalysisResult(double Similarity, double AiRisk, double OverallRisk, string Message);
record Submission(int Id, string StudentName, string Assignment, string ContentType, string Content, DateTime SubmittedAt, AnalysisResult Analysis);
