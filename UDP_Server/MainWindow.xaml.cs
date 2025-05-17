using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace UDP_2;

public partial class MainWindow : Window
{
    private UdpClient server;
    private IPEndPoint clientEndPoint;
    
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
                        string request = Encoding.UTF8.GetString(receivedData);
    
                        Dispatcher.Invoke(() =>
                        {
                            lstRequests.Items.Add($"Request: {request}");
                        });
    
                        string response = FindRecipes(request);
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        server.Send(responseData, responseData.Length, clientEndPoint);
                    }
                });
            }
    
    private string FindRecipes(string ingredients)
    {
        switch (ingredients.ToLower()) // Convert to lowercase for case-insensitive matching
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