using System;
usingSystem.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.AspNetCore.SignalR.Client;

namespace SmartQueue.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly HubConnection _hubConnection;

        public MainWindow()
        {
            InitializeComponent();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/queueHub") // Adapter l'URL à votre serveur
                .WithAutomaticReconnect()
                .Build();

            // Réception des appels de tickets
            _hubConnection.On<string, string>("ReceiveTicketUpdate", (ticketNumber, counterName) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LblCurrentCall.Text = $"📢 TICKET {ticketNumber} AU {counterName.ToUpper()}";

                    LstHistory.Items.Insert(
                        0,
                        $"[{DateTime.Now:HH:mm:ss}] Ticket {ticketNumber} appelé au {counterName}"
                    );
                });
            });

            // Gestion de la reconnexion
            _hubConnection.Reconnecting += error =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Reconnexion en cours...";
                    TxtStatus.Foreground = Brushes.Orange;
                });

                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Connecté";
                    TxtStatus.Foreground = Brushes.Green;
                });

                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = "Connexion perdue";
                    TxtStatus.Foreground = Brushes.Red;
                });

                await Task.Delay(5000);

                try
                {
                    await _hubConnection.StartAsync();
                }
                catch
                {
                    // Journaliser l'erreur si nécessaire
                }
            };

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                await _hubConnection.StartAsync();

                TxtStatus.Text = "Connecté au serveur ASP.NET Core";
                TxtStatus.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Erreur : {ex.Message}";
                TxtStatus.Foreground = Brushes.Red;
            }
        }

        private async void BtnCallNext_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTicketNumber.Text) ||
                string.IsNullOrWhiteSpace(TxtCounter.Text))
            {
                MessageBox.Show(
                    "Veuillez saisir le numéro du ticket et le guichet.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (_hubConnection.State != HubConnectionState.Connected)
            {
                MessageBox.Show(
                    "Le serveur n'est pas connecté.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                string currentTicket = TxtTicketNumber.Text.Trim();
                string counter = TxtCounter.Text.Trim();

                await _hubConnection.InvokeAsync(
                    "CallNextTicket",
                    currentTicket,
                    counter);

                // Auto-incrémentation du ticket
                var parts = currentTicket.Split('-');

                if (parts.Length == 2 &&
                    int.TryParse(parts[1], out int number))
                {
                    TxtTicketNumber.Text = $"{parts[0]}-{number + 1:D3}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}