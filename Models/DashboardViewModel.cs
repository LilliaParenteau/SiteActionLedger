namespace  SiteActionLedger.Models
{
    public class DashboardViewModel
    {
        public int CustomerCount { get; set; }

        public int OpenRequestCount { get; set; }

        public int ActiveWorkOrderCount { get; set; }

        public int TechnicianCount { get; set; }

        public List<ServiceRequest> RecentRequests { get; set; } = new();

        public List<WorkOrder> UpcomingWorkOrders { get; set; } = new();
    }
}
