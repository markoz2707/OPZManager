using System.Collections.Concurrent;

namespace OPZManager.API.Services
{
    public class AnalysisProgress
    {
        public int OPZId { get; set; }
        public string Status { get; set; } = "idle"; // idle, running, completed, error, cancelled
        public int TotalEquipment { get; set; }
        public int CompletedEquipment { get; set; }
        public string CurrentEquipmentName { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }

    public interface IAnalysisProgressService
    {
        void Start(int opzId, int totalEquipment);
        void Update(int opzId, int completed, string currentEquipmentName);
        void Complete(int opzId);
        void Fail(int opzId, string errorMessage);
        void Cancel(int opzId);
        bool IsCancelled(int opzId);
        CancellationToken GetCancellationToken(int opzId);
        AnalysisProgress? GetProgress(int opzId);
    }

    public class AnalysisProgressService : IAnalysisProgressService
    {
        private readonly ConcurrentDictionary<int, AnalysisProgress> _progress = new();

        public void Start(int opzId, int totalEquipment)
        {
            // Cancel any previous run
            if (_progress.TryGetValue(opzId, out var old))
            {
                old.CancellationTokenSource?.Cancel();
                old.CancellationTokenSource?.Dispose();
            }

            _progress[opzId] = new AnalysisProgress
            {
                OPZId = opzId,
                Status = "running",
                TotalEquipment = totalEquipment,
                CompletedEquipment = 0,
                StartedAt = DateTime.UtcNow,
                CancellationTokenSource = new CancellationTokenSource()
            };
        }

        public void Update(int opzId, int completed, string currentEquipmentName)
        {
            if (_progress.TryGetValue(opzId, out var p))
            {
                p.CompletedEquipment = completed;
                p.CurrentEquipmentName = currentEquipmentName;
            }
        }

        public void Complete(int opzId)
        {
            if (_progress.TryGetValue(opzId, out var p))
            {
                p.Status = "completed";
                p.CompletedAt = DateTime.UtcNow;
                p.CurrentEquipmentName = "";
            }
        }

        public void Fail(int opzId, string errorMessage)
        {
            if (_progress.TryGetValue(opzId, out var p))
            {
                p.Status = "error";
                p.ErrorMessage = errorMessage;
                p.CompletedAt = DateTime.UtcNow;
            }
        }

        public void Cancel(int opzId)
        {
            if (_progress.TryGetValue(opzId, out var p))
            {
                p.CancellationTokenSource?.Cancel();
                p.Status = "cancelled";
                p.CompletedAt = DateTime.UtcNow;
                p.CurrentEquipmentName = "";
            }
        }

        public bool IsCancelled(int opzId)
        {
            return _progress.TryGetValue(opzId, out var p) &&
                   (p.CancellationTokenSource?.IsCancellationRequested ?? false);
        }

        public CancellationToken GetCancellationToken(int opzId)
        {
            if (_progress.TryGetValue(opzId, out var p) && p.CancellationTokenSource != null)
                return p.CancellationTokenSource.Token;
            return CancellationToken.None;
        }

        public AnalysisProgress? GetProgress(int opzId)
        {
            _progress.TryGetValue(opzId, out var p);
            return p;
        }
    }
}
