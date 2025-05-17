using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Windows.Media.Imaging;

namespace UDP_2;

public partial class MainWindow : Window
    {
        private UdpClient server;
        private IPEndPoint clientEndPoint;
        private Dictionary<string, DateTime> activeClients = new Dictionary<string, DateTime>();
        private Dictionary<string, List<DateTime>> requestLog = new Dictionary<string, List<DateTime>>();
        private const int MAX_CLIENTS = 5;
        private const int REQUEST_LIMIT = 10;
        private const int TIMEOUT_MINUTES = 10;
        private const int TIME_WINDOW_MINUTES = 60;
        private const string LOG_FILE = "server_log.txt";
        private Timer cleanupTimer;

        public MainWindow()
        {
            InitializeComponent();
            cleanupTimer = new Timer(60000);
            cleanupTimer.Elapsed += CleanupInactiveClients;
            cleanupTimer.Start();
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            server = new UdpClient(8080);
            clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            lblStatus.Text = "Server running...";

            await Task.Run(() =>
            {
                while (true)
                {
                    byte[] receivedData = server.Receive(ref clientEndPoint);
                    string clientIP = clientEndPoint.Address.ToString();
                    string request = Encoding.UTF8.GetString(receivedData);
                    DateTime requestTime = DateTime.Now;

                    Dispatcher.Invoke(() => lstRequests.Items.Add($"Request from {clientIP}: {request} at {requestTime}"));

                    LogClientRequest(clientIP, request, requestTime);

                    if (!activeClients.ContainsKey(clientIP) && activeClients.Count >= MAX_CLIENTS)
                    {
                        SendTextResponse("Server full. Try again later.");
                        continue;
                    }

                    activeClients[clientIP] = requestTime;

                    ProcessRequest(clientIP, request);
                }
            });
        }

        private void LogClientRequest(string clientIP, string request, DateTime requestTime)
        {
            string logEntry = $"{requestTime} | Client: {clientIP} | Request: {request}";
            File.AppendAllText(LOG_FILE, logEntry + Environment.NewLine);
        }

        private void ProcessRequest(string clientIP, string ingredients)
        {
            if (!requestLog.ContainsKey(clientIP))
                requestLog[clientIP] = new List<DateTime>();

            requestLog[clientIP].RemoveAll(t => (DateTime.Now - t).TotalMinutes > TIME_WINDOW_MINUTES);

            if (requestLog[clientIP].Count >= REQUEST_LIMIT)
            {
                SendTextResponse("Too many requests. Please try again later.");
                return;
            }

            requestLog[clientIP].Add(DateTime.Now);

            string recipe = FindRecipe(ingredients);
            byte[] imageData = LoadImage(recipe);

            SendResponse(recipe, imageData);
        }

        private string FindRecipe(string ingredients)
        {
            return ingredients.ToLower() switch
            {
                string s when s.Contains("tomato") => "Recipe: Italian tomato soup",
                string s when s.Contains("chicken") => "Recipe: Chicken in creamy sauce",
                string s when s.Contains("caesar") => "Recipe: Caesar salad",
                _ => "No matching recipes found."
            };
        }

        private byte[] LoadImage(string recipe)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", recipe switch
            {
                "Recipe: Italian tomato soup" => "tomato_soup.jpg",
                "Recipe: Chicken in creamy sauce" => "chicken_sauce.jpg",
                "Recipe: Caesar salad" => "caesar_salad.jpg",
                _ => "default.jpg"
            });

            return File.Exists(imagePath) ? File.ReadAllBytes(imagePath) : new byte[0];
        }

        private void SendResponse(string recipe, byte[] imageData)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(recipe);
            writer.Write(imageData.Length);
            writer.Write(imageData);

            byte[] responseData = ms.ToArray();
            server.Send(responseData, responseData.Length, clientEndPoint);
        }

        private void SendTextResponse(string message)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(message);
            server.Send(responseData, responseData.Length, clientEndPoint);
        }

        private void CleanupInactiveClients(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (var client in new List<string>(activeClients.Keys))
            {
                if ((now - activeClients[client]).TotalMinutes > TIMEOUT_MINUTES)
                {
                    Dispatcher.Invoke(() => lstRequests.Items.Add($"Disconnected {client} (Inactive)"));
                    activeClients.Remove(client);
                }
            }
        }
    }