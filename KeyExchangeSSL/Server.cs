using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KeysExchange;
using Newtonsoft.Json;

public class Server
{
	X509Certificate serverCertificate;
	IPAddress ipAddress;
	string requestedSK = "";
	string requestedClient = "";
	string requestedClientToken = "";
	TcpListener listener;
	public Server(string ip, int port)
	{
		// Create the instance to FCM
		FirebaseApp.Create(new AppOptions()
		{
			Credential = GoogleCredential.FromFile("private_key_messaging.json")
		});
		
		// if it is not the user, we have to request the manager for the user
		// private key via ssl tunnel
		// Load the SSL certificate
		// Load the server certificate from an embedded resource
		//Console.WriteLine("Please type X509 certificate password: ");
		//var input = Console.ReadLine();
		// Load the certificate with the associated private key
		serverCertificate= new X509Certificate("server.pfx", "root");
		//int port = 443;
		ipAddress = IPAddress.Parse(ip);
		listener = new TcpListener(ipAddress, port);
		listener.Start();
		StartListening();
	}
	public void StartListening(){
		
		if (serverCertificate != null)
		{
			Console.WriteLine("SSL listener started...");
			
			while(true){
			try	
			{
				TcpClient client = listener.AcceptTcpClient();
				var clientEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
				
				string clientIp = "";
				if (clientEndpoint != null && client != null)
				{
					clientIp = clientEndpoint.Address.ToString();
					Console.WriteLine($"[TCP]Client connected from IP address: {clientIp}");
					Console.WriteLine($"[TCP] Listening on port: 10001, ipaddr: {ipAddress}. Client availability:" +
					$"{client.Available}. ");
					Thread t = new(new ParameterizedThreadStart(ThreadedClient));
                    t.Start(client);

				}
			}catch(Exception ex){Console.WriteLine(ex.Message);}
			}
			
		}
	}
	public async void ThreadedClient(object? obj){
				TcpClient? client = obj as TcpClient;
				// Wrap the stream with an SSL stream
				var sslStream = new SslStream(client.GetStream(), false);
				// Set timeouts for the read and write to 5 seconds.
				// Authenticate the SSL connection with the private key password
				sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls12, true);
				// Read the incoming message
				Console.WriteLine("	CLIENT AND SERVER AUTHENTICATED");
				while (true)
				{
					string? message = ReadMessage(sslStream);
					//todo
					
					if(message =="HelloServer<EOF>"){
						WriteMessage("HelloClient<EOF>", client, sslStream);
					}
							
					else if(message =="SKReq<EOF>"){
						WriteMessage("Client?<EOF>", client, sslStream);
					}
					else if(message.EndsWith("$<EOF>")){
						
						// Read the client's response with the name of the requested client
						requestedClient = message.Replace("$<EOF>", "");
						// Retrievethe manager's token from the database
						string managerToken = "";
							// Get user token
						managerToken =  GetUsersToken("manager").Result;
						requestedClientToken =  GetUsersToken(requestedClient).Result;

						Console.WriteLine($"Manager's token: {managerToken}");
							// Send Firebase message
						// See documentation on defining a message payload.
						var notification = new Message()
						{
							Data = new Dictionary<string, string>()
							{
								{ "user", requestedClient },
							},
							Token = managerToken,
							Notification = new Notification()
							{
								Title = "Secret key retrieval",
								Body = $"{requestedClient} wants its secret key..."
							}
						};

						// Send a message to the device corresponding to the provided
						// registration token.

						string notifResp = FirebaseMessaging.DefaultInstance.SendAsync(notification).Result;
						// Response is a message ID string.
						Console.WriteLine("[FCM] Successfully sent message: " + notifResp);
						//close the connection with the client:
						WriteMessage("Received username correctly", client, sslStream);
						sslStream.Close();
						client.Close();
						break;
					}
					else if(message ==  "Requested secret key!<EOF>"){
						WriteMessage("Tell me!<EOF>", client, sslStream);
						// Retrieve the SK of the user
						requestedSK = ReadMessage(sslStream);
						Console.WriteLine($"[SERVER] Requested client token: {requestedClientToken}");
						try{
						if(requestedClientToken != ""){
							var notification1 = new Message()
							{
								Data = new Dictionary<string, string>()
							{
								{ "title", "SecretKeyRetrieval" },
							},

									Token = requestedClientToken,
									Notification = new Notification()
									{
										Title = "Secret key retrieval",
										Body = "Your secret key has been retrieved correctly from the HR Manager."
									}
							};
						
						

						// Send a message to the device corresponding to the provided
						// registration token.
							string notifResp = FirebaseMessaging.DefaultInstance.SendAsync(notification1).Result;
							// Response is a message ID string.
							Console.WriteLine("[FCM] Successfully sent message: " + notifResp);
							
						}else{
							Console.WriteLine("[Server] Requested Client Token is null.");
						}		
						}catch(Exception ex){Console.WriteLine(ex.Message);}
							// Send Firebase message
						// See documentation on defining a message payload.
						sslStream.Close();
						client.Close();
						break;
						
					}
					else if(message== "TellMeTheSecret<EOF>"){
						WriteMessage(requestedSK, client, sslStream);
						Console.WriteLine("[SERVER] Keys exchange finished correctly."	);
						sslStream.Close();
						client.Close();
						break;
					}else{
						client.Close();
						break;
					}
					
					
			}

			
			

		
	}
	private static bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		if(certificate.Subject.ToString() == "E=raul.gimenez.lorente@alumnos.upm.es, L=Madrid, S=Madrid, C=ES")
			return true;
		return false;
        
	}
	#region Http Functions
	static async Task<string> GetUsersToken(string username)
	{
		/*****************************************
             * PUT (UPDATE) request the user's info 
             *****************************************/
            try
            {
                Uri requestUri = new Uri("http://82.223.103.136:5025/api/user/token/" + username);
                var client = new HttpClient();
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync(requestUri);
                Console.WriteLine("[API-DATABASE] Database:" + response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    string token = JsonConvert.DeserializeObject<string>(content);
					return token;
                }
                return "Not found";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
                return null;
            }

	}
	#endregion
	#region Write message
	static void WriteMessage(string mess, TcpClient client, SslStream sslStream)
	{
		Console.WriteLine($"[SERVER] Write to client: {mess}");
		byte[] buffer = Encoding.UTF8.GetBytes(mess);
		int bytesSent = 0;
		while (bytesSent < buffer.Length)
		{
			sslStream.Write(buffer, bytesSent, buffer.Length - bytesSent);
        	bytesSent += buffer.Length - bytesSent;
		}
		sslStream.Flush();
	}
	#endregion
	#region Read Message
	static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client. The client signals the end of the message using the "<EOF>" marker.
            byte[ ] buffer = new byte[300000];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                // Read the client's test message.
                bytes = sslStream.Read(buffer, 0, buffer.Length);
 
                // Use Decoder class to convert from bytes to UTF8 in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[ ] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
				Console.WriteLine($"[SERVER] Read from client: {messageData}");		
                // Check for EOF or an empty message.
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);
 
            return messageData.ToString();
        }
		#endregion


}
