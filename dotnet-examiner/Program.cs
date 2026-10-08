using System.Net.Http.Json;

namespace ExaminerClient;

static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ExaminerForm());
    }
}

public class ExaminerForm : Form
{
    DataGridView grid = new();
    TextBox details = new();
    Button refresh = new();
    List<Submission> submissions = new();

    public ExaminerForm()
    {
        Text = "Academic Integrity - Examiner Client";
        Width = 1000; Height = 700; StartPosition = FormStartPosition.CenterScreen;

        refresh.Text = "Refresh Submissions"; refresh.Dock = DockStyle.Top; refresh.Height = 45;
        refresh.Click += async (_,_) => await LoadData();

        grid.Dock = DockStyle.Top; grid.Height = 260; grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.SelectionChanged += ShowDetails;

        details.Dock = DockStyle.Fill; details.Multiline = true; details.ReadOnly = true;
        details.ScrollBars = ScrollBars.Vertical;

        Controls.Add(details); Controls.Add(grid); Controls.Add(refresh);
        Shown += async (_,_) => await LoadData();
    }

    async Task LoadData()
    {
        try
        {
            using var client = new HttpClient();
            submissions = await client.GetFromJsonAsync<List<Submission>>("http://localhost:5000/api/submissions") ?? new();
            grid.DataSource = submissions.Select(s => new {
                s.Id, Student=s.StudentName, s.Assignment, s.ContentType,
                Similarity=$"{s.Analysis.Similarity*100:0.0}%",
                OverallRisk=$"{s.Analysis.OverallRisk*100:0.0}%",
                s.Analysis.Message
            }).ToList();
        }
        catch(Exception ex)
        {
            MessageBox.Show("Cannot connect to ASP.NET backend. Make sure it is running on http://localhost:5000.\n\n"+ex.Message,
                "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void ShowDetails(object? sender, EventArgs e)
    {
        if(grid.CurrentRow == null || grid.CurrentRow.Index < 0 || grid.CurrentRow.Index >= submissions.Count) return;
        var s=submissions[grid.CurrentRow.Index];
        details.Text=$"Student: {s.StudentName}\r\nAssignment: {s.Assignment}\r\nType: {s.ContentType}\r\nSubmitted: {s.SubmittedAt:f}\r\n\r\n"+
            $"Similarity: {s.Analysis.Similarity*100:0.0}%\r\nAI/Paraphrase Risk: {s.Analysis.AiRisk*100:0.0}%\r\nOverall Risk: {s.Analysis.OverallRisk*100:0.0}%\r\n"+
            $"Result: {s.Analysis.Message}\r\n\r\n---------------- SUBMISSION ----------------\r\n\r\n{s.Content}";
    }
}

public class Submission
{
    public int Id {get;set;} public string StudentName {get;set;}="";
    public string Assignment {get;set;}=""; public string ContentType {get;set;}="";
    public string Content {get;set;}=""; public DateTime SubmittedAt {get;set;}
    public Analysis Analysis {get;set;}=new();
}
public class Analysis
{
    public double Similarity {get;set;} public double AiRisk {get;set;}
    public double OverallRisk {get;set;} public string Message {get;set;}="";
}
