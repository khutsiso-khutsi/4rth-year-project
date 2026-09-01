using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class AuditLogEntry
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        // Who performed the action
        public string UserEmail { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty; // Manager, Doctor, Technician, etc.

        // What they did
        public AuditAction Action { get; set; }

        // What it was done to
        public string Module { get; set; } = string.Empty;     // e.g. "Staff", "Test Request", "Consumables"
        public string ReferenceId { get; set; } = string.Empty; // e.g. "REQ-889", "STF-014"

        public string Details { get; set; } = string.Empty;   // short human-readable summary
        public string? IpAddress { get; set; }
    }

    public enum AuditAction
    {
        Created,
        Updated,
        Deleted,
        Login,
        Logout,
        Approved,
        Rejected
    }
    public class AuditLogViewModel
    {
        // Filter inputs (bound from the FROM / TO date pickers and the
        // optional action dropdown on the page)
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public AuditAction? ActionFilter { get; set; }

        // Result set after filtering
        public List<AuditLogEntry> Entries { get; set; } = new();

        public int TotalCount => Entries.Count;
    }
}
