namespace SiteActionLedger.Models
{
    public class DashboardViewModel
    {
        public int CustomerCount { get; set; }

        public int OpenRequestCount { get; set; }

        public int ActiveWorkOrderCount { get; set; }

        public int TechnicianCount { get; set; }

        public int ActiveProjectCount { get; set; }

        public int UnresolvedIssueCount { get; set; }

        public int OverdueWorkOrderCount { get; set; }

        public List<ServiceRequest> RecentRequests { get; set; } = new();

        public List<WorkOrder> UpcomingWorkOrders { get; set; } = new();

        public List<WorkOrder> OverdueWorkOrders { get; set; } = new();

        public List<ProjectIssueSummary> ProjectIssueSummaries { get; set; } = new();
    }

    public class ProjectIssueSummary
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = "";

        public string CustomerName { get; set; } = "";

        public int TotalIssueCount { get; set; }

        public int UnresolvedIssueCount { get; set; }
    }
}
