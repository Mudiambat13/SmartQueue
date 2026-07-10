namespace SmartQueue.Server.Models
{
    public class Ticket
    {
        public string Number { get; set; } = string.Empty;
        public string Counter { get; set; } = string.Empty;
        public DateTime CalledAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Waiting"; // Waiting, Called, Completed
    }
}