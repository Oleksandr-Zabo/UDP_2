using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Collections.Generic;

namespace UDP_2;

public partial class MainWindow : Window
    {
        private UdpClient server;
        private IPEndPoint clientEndPoint;
        private Dictionary<string, List<DateTime>> requestLog = new Dictionary<string, List<DateTime>>();
        private const int REQUEST_LIMIT = 10;
        private const int TIME_WINDOW_MINUTES = 60;

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

                    Dispatcher.Invoke(() => lstRequests.Items.Add($"Request from {clientIP}: {request}"));

                    string response = ProcessRequest(clientIP, request);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    server.Send(responseData, responseData.Length, clientEndPoint);
                }
            });
        }

        private string ProcessRequest(string clientIP, string ingredients)
        {
            // Initialize request log for new clients
            if (!requestLog.ContainsKey(clientIP))
                requestLog[clientIP] = new List<DateTime>();

            // Remove old requests beyond the time window
            requestLog[clientIP].RemoveAll(t => (DateTime.Now - t).TotalMinutes > TIME_WINDOW_MINUTES);

            // Check if the client exceeds the request limit
            if (requestLog[clientIP].Count >= REQUEST_LIMIT)
                return "Too many requests. Please try again later.";

            // Log the current request
            requestLog[clientIP].Add(DateTime.Now);

            return FindRecipes(ingredients);
        }

        private string FindRecipes(string ingredients)
        {
            if (ingredients.Contains("tomato"))
                return "Recipe: Italian tomato soup";
            if (ingredients.Contains("chicken"))
                return "Recipe: Chicken in creamy sauce";
            return "No matching recipes found.";
        }

    }