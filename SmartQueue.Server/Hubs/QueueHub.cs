using Microsoft.AspNetCore.SignalR;
using SmartQueue.Server.Models;
using System.Collections.Concurrent;

namespace SmartQueue.Server.Hubs
{
    public class QueueHub : Hub
    {
        // Collection thread-safe
        private static readonly ConcurrentDictionary<string, Ticket> _activeTickets = new();

        public async Task CallNextTicket(string ticketNumber, string counterName)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber))
                throw new HubException("Le numéro du ticket est obligatoire.");

            if (string.IsNullOrWhiteSpace(counterName))
                throw new HubException("Le nom du guichet est obligatoire.");

            var ticket = _activeTickets.AddOrUpdate(
                ticketNumber,
                key => new Ticket
                {
                    Number = key,
                    Counter = counterName,
                    Status = TicketStatus.Called,
                    CalledAt = DateTime.UtcNow
                },
                (key, existing) =>
                {
                    existing.Counter = counterName;
                    existing.Status = TicketStatus.Called;
                    existing.CalledAt = DateTime.UtcNow;
                    return existing;
                });

            await Clients.All.SendAsync(
                "ReceiveTicketUpdate",
                ticket.Number,
                ticket.Counter);

            await SendQueueToAll();
        }

        public async Task CompleteTicket(string ticketNumber)
        {
            if (_activeTickets.TryGetValue(ticketNumber, out var ticket))
            {
                ticket.Status = TicketStatus.Completed;

                await Clients.All.SendAsync(
                    "TicketCompleted",
                    ticket.Number);

                await SendQueueToAll();
            }
        }

        public async Task RequestCurrentStatus()
        {
            var queue = _activeTickets.Values
                .Where(t => t.Status == TicketStatus.Called)
                .OrderByDescending(t => t.CalledAt)
                .Take(5)
                .ToList();

            await Clients.Caller.SendAsync("ReceiveFullQueue", queue);
        }

        private async Task SendQueueToAll()
        {
            var queue = _activeTickets.Values
                .Where(t => t.Status == TicketStatus.Called)
                .OrderByDescending(t => t.CalledAt)
                .Take(5)
                .ToList();

            await Clients.All.SendAsync("ReceiveFullQueue", queue);
        }

        public override async Task OnConnectedAsync()
        {
            await RequestCurrentStatus();
            await base.OnConnectedAsync();
        }
    }
}