using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Timer = System.Timers.Timer;

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

        public MainWindow()
        {
            InitializeComponent();
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

                    Dispatcher.Invoke(() =>
                    {
                        lstRequests.Items.Add($"Request from {clientIP}: {request} at {requestTime}");
                    });

                    LogClientRequest(clientIP, request, requestTime);

                    if (!activeClients.ContainsKey(clientIP) && activeClients.Count >= MAX_CLIENTS)
                    {
                        string response = "Server full. Try again later.";
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        server.Send(responseData, responseData.Length, clientEndPoint);
                        continue;
                    }

                    activeClients[clientIP] = requestTime;

                    string responseMessage = ProcessRequest(clientIP, request);
                    byte[] responseDataFinal = Encoding.UTF8.GetBytes(responseMessage);
                    server.Send(responseDataFinal, responseDataFinal.Length, clientEndPoint);
                }
            });
        }

        private void LogClientRequest(string clientIP, string request, DateTime requestTime)
        {
            string logEntry = $"{requestTime} | Client: {clientIP} | Request: {request}";
            File.AppendAllText(LOG_FILE, logEntry + Environment.NewLine);
        }

        private string ProcessRequest(string clientIP, string ingredients)
        {
            if (!requestLog.ContainsKey(clientIP))
                requestLog[clientIP] = new List<DateTime>();

            requestLog[clientIP].RemoveAll(t => (DateTime.Now - t).TotalMinutes > TIME_WINDOW_MINUTES);

            if (requestLog[clientIP].Count >= REQUEST_LIMIT)
                return "Too many requests. Please try again later.";

            requestLog[clientIP].Add(DateTime.Now);

            return FindRecipes(ingredients);
        }

        private string FindRecipes(string ingredients)
        {
            switch (ingredients.ToLower())
            {
                case string s when s.Contains("tomato"):
                    return "Recipe: Italian tomato soup";
                case string s when s.Contains("chicken"):
                    return "Recipe: Chicken in creamy sauce";
                default:
                    return "No matching recipes found.";
            }
        }
    }