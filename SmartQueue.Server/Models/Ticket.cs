namespace SmartQueue.Server.Models
{
    public class Ticket
    {
        public string Number { get; set; } = string.Empty;

        public string Counter { get; set; } = string.Empty;

        public DateTime CalledAt { get; set; } = DateTime.UtcNow;

        public TicketStatus Status { get; set; } = TicketStatus.Waiting;
    }

    public enum TicketStatus
    {
        Waiting,
        Called,
        Completed
    }
}