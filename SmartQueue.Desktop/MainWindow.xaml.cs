using System;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;

namespace SmartQueue.Desktop
{
    public partial class MainWindow : Window
    {
        private HubConnection _hubConnection;

        public MainWindow()
        {
            InitializeComponent();

            // Initialisation professionnelle de SignalR Client
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/queueHub")
                .WithAutomaticReconnect()
                .Build();

            // Gestion de la réception asynchrone des événements en temps réel
            _hubConnection.On<string, string>("ReceiveTicketUpdate", (ticketNumber, counterName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LblCurrentCall.Text = $"📢 TICKET {ticketNumber} AU {counterName.ToUpper()}";
                    LstHistory.Items.Insert(0, $"[{DateTime.Now.ToString("HH:mm:ss")}] Ticket {ticketNumber} appelé au {counterName}");
                });
            });

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _hubConnection.StartAsync();
                TxtStatus.Text = "Connecté au serveur ASP.NET Core (SignalR actif)";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Erreur de connexion : {ex.Message}";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async void BtnCallNext_Click(object sender, RoutedEventArgs e)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                string currentTicket = TxtTicketNumber.Text;
                string counter = TxtCounter.Text;

                // Invocation du hub distant
                await _hubConnection.InvokeAsync("CallNextTicket", currentTicket, counter);

                // Auto-incrémentation intelligente pour l'agent
                if (currentTicket.Contains("-") && int.TryParse(currentTicket.Split('-')[1], out int num))
                {
                    TxtTicketNumber.Text = $"{currentTicket.Split('-')[0]}-{num + 1}";
                }
            }
            else
            {
                MessageBox.Show("Impossible d'appeler le ticket. Le serveur n'est pas accessible.", "Erreur de Connexion", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}