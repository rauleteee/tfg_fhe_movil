using System;
namespace KeysExchange
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    public class TcpListenerSSL : TcpListenerInterface
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly X509Certificate _certificate;

        public TcpListenerSSL(IPAddress ipAddress, int port, X509Certificate certificate)
        {
            _ipAddress = ipAddress;
            _port = port;
            _certificate = certificate;
        }

        public async Task StartListeningAsync(IPAddress ipAddress, int port, X509Certificate certificate)
        {
            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("SSL listener started...");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"[TCP] Listening on port: {port}, ipaddr: {ipAddress}. Client availability:" +
                    $"{client.Available}");

                // Wrap the stream with an SSL stream
                SslStream sslStream = new SslStream(client.GetStream(), false);
                try
                {
                    sslStream.AuthenticateAsServer(certificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                    // Read the incoming message
                    using (StreamReader reader = new StreamReader(sslStream))
                    {
                        var message = await reader.ReadLineAsync();
                        Console.WriteLine($"Received message: {message}");

                        if (message == "SKReq")
                        {
                            // TODO: Handle the SKReq message
                        }
                    }

                    // Close the SSL stream and the client connection
                    sslStream.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    client.Close();
                }
            }
        }
    }

    
}

