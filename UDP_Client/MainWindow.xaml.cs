using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

        using (MemoryStream ms = new MemoryStream(receivedData))
        {
            BinaryReader reader = new BinaryReader(ms);
            string recipe = reader.ReadString();
            int imageSize = reader.ReadInt32();
            byte[] imageData = reader.ReadBytes(imageSize);

            Dispatcher.Invoke(() =>
            {
                lstRecipes.Items.Add(recipe);
                if (imageData.Length > 0)
                {
                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream imgStream = new MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = imgStream;
                        bitmap.EndInit();
                    }

                    imgRecipe.Source = bitmap;
                }
            });
        }
    }
}