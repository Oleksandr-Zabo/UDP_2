using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace UDP_Client;


public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void SendRequest(object sender, RoutedEventArgs e)
    {
        UdpClient client = new UdpClient();
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

        string request = txtIngredients.Text;
        byte[] data = Encoding.UTF8.GetBytes(request);

        client.Send(data, data.Length, serverEndPoint);
            
        byte[] receivedData = client.Receive(ref serverEndPoint);
        string response = Encoding.UTF8.GetString(receivedData);

        lstRecipes.Items.Add(response);
    }
}