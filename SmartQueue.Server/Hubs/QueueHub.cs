using Microsoft.AspNetCore.SignalR;
using SmartQueue.Server.Models;

namespace SmartQueue.Server.Hubs
{
    public class QueueHub : Hub
    {
        private static List<Ticket> _activeTickets = new List<Ticket>();

        public async Task CallNextTicket(string ticketNumber, string counterName)
        {
            var ticket = _activeTickets.FirstOrDefault(t => t.Number == ticketNumber);
            if (ticket == null)
            {
                ticket = new Ticket { Number = ticketNumber, Counter = counterName, Status = "Called" };
                _activeTickets.Add(ticket);
            }
            else
            {
                ticket.Counter = counterName;
                ticket.Status = "Called";
                ticket.CalledAt = DateTime.Now;
            }

            await Clients.All.SendAsync("ReceiveTicketUpdate", ticketNumber, counterName);
        }

        public async Task RequestCurrentStatus()
        {
            await Clients.Caller.SendAsync("ReceiveFullQueue", _activeTickets.Where(t => t.Status == "Called").OrderByDescending(t => t.CalledAt).Take(5).ToList());
        }
    }
}