using AIExamIDE.Models;
using System.Collections.Concurrent;

namespace AIExamIDE.Services
{
    public class ExamState : IDisposable
    {
        private readonly Timer _timer;
        private bool _disposed = false;
        private Func<Func<Task>, Task>? _invokeAsync;

        public ExamMetadata? CurrentExam { get; private set; }
        public List<ExamFile> Files { get; private set; } = new();
        public List<ExamFile> OpenFiles { get; private set; } = new();
        public ExamFile? ActiveFile { get; private set; }
        public int CurrentTaskIndex { get; set; } = 0;
        public int TimeRemainingSeconds { get; private set; } = 3000; 
        public bool IsSubmitted { get; private set; } = false;
        public string ConsoleOutput { get; private set; } = "Ready to run code...";

    // Evaluation state
    public EvaluationResult? LastEvaluation { get; private set; }
    public bool IsEvaluating { get; private set; } = false;

    // Timing state
    public int InitialTimeSeconds { get; private set; } = 0;
    public DateTime ExamStartUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? SubmittedUtc { get; private set; }
    public DateTime? EvaluatedUtc { get; private set; }

        public event Action? OnChange;

        public ExamState()
        {
            _timer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void SetInvokeAsync(Func<Func<Task>, Task> invokeAsync)
        {
            _invokeAsync = invokeAsync;
        }

        private async void TimerCallback(object? state)
        {
            if (_disposed) return;

            if (TimeRemainingSeconds > 0 && !IsSubmitted)
            {
                TimeRemainingSeconds--;
                
                if (_invokeAsync != null)
                {
                    try
                    {
                        await _invokeAsync(() =>
                        {
                            OnChange?.Invoke();
                            return Task.CompletedTask;
                        });
                    }
                    catch (ObjectDisposedException)
                    {
                        // Component has been disposed, stop the timer
                        return;
                    }
                }

                if (TimeRemainingSeconds <= 0)
                {
                    // Time's up - auto submit
                    MarkAsSubmitted();
                    SetConsoleOutput("⏰ Time's up! Exam automatically submitted.");
                }
            }
        }

        public void LoadExam(ExamMetadata exam, List<ExamFile> files)
        {
            if (_disposed) return;

            CurrentExam = exam;
            Files = files;
            // Set default time limit if not specified in exam metadata
            InitialTimeSeconds = (exam.Duration ?? 60) * 60;
            TimeRemainingSeconds = InitialTimeSeconds; // Convert minutes to seconds
            ExamStartUtc = DateTime.UtcNow;
            SubmittedUtc = null;
            EvaluatedUtc = null;
            CurrentTaskIndex = 0;
            NotifyStateChanged();
        }

        public void OpenFile(ExamFile file)
        {
            if (_disposed) return;

            if (!OpenFiles.Contains(file))
            {
                OpenFiles.Add(file);
            }
            ActiveFile = file;
            NotifyStateChanged();
        }

        public void CloseFile(ExamFile file)
        {
            if (_disposed) return;

            OpenFiles.Remove(file);
            if (ActiveFile == file)
            {
                ActiveFile = OpenFiles.LastOrDefault();
            }
            NotifyStateChanged();
        }

        public void SetActiveFile(ExamFile file)
        {
            if (_disposed) return;

            if (OpenFiles.Contains(file))
            {
                ActiveFile = file;
                NotifyStateChanged();
            }
        }

        public ExamFile? GetFileByPath(string path)
        {
            return Files.FirstOrDefault(f => f.Path == path);
        }

        public List<ExamFile> GetAllFiles()
        {
            return Files.ToList();
        }

        public List<ExamFile> GetRunnableFiles()
        {
            return Files.Where(f => !f.IsDirectory && HasMainMethod(f)).ToList();
        }

        public bool HasMainMethod(ExamFile file)
        {
            if (file.IsDirectory || !file.Name.EndsWith(".java"))
                return false;

            return file.Content.Contains("public static void main(String[]") ||
                   file.Content.Contains("public static void main(String []");
        }

        public event Action? OnConsoleOutput;
    
    public void SetConsoleOutput(string output)
    {
        ConsoleOutput = output;
        OnConsoleOutput?.Invoke();
        OnChange?.Invoke();
    }

        public void MarkAsSubmitted()
        {
            if (_disposed) return;

            IsSubmitted = true;
            SubmittedUtc = DateTime.UtcNow;
            NotifyStateChanged();
        }

        public void SetEvaluating(bool evaluating)
        {
            if (_disposed) return;
            IsEvaluating = evaluating;
            NotifyStateChanged();
        }

        public void SetEvaluationResult(EvaluationResult result)
        {
            if (_disposed) return;
            LastEvaluation = result;
            EvaluatedUtc = DateTime.UtcNow;
            NotifyStateChanged();
        }

         public event Action? OnCodeRunStarted;

    public void NotifyCodeRunStarted()
        {
            OnCodeRunStarted?.Invoke();
        }

        public void ShowCsvOverlay(ExamFile csvFile)
{
    OnShowCsvOverlay?.Invoke(csvFile);
}

public event Action<ExamFile>? OnShowCsvOverlay;

        public void AddTime(int seconds)
        {
            if (_disposed) return;

            TimeRemainingSeconds += seconds;
            NotifyStateChanged();
        }

        public void SetTime(int seconds)
        {
            if (_disposed) return;

            TimeRemainingSeconds = seconds;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            if (_disposed) return;

            try
            {
                OnChange?.Invoke();
            }
            catch (ObjectDisposedException)
            {
                // Component has been disposed, ignore
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _timer?.Dispose();
                OnChange = null;
                Console.WriteLine("🔧 ExamState disposed - Timer stopped");
            }
        }
    }
}