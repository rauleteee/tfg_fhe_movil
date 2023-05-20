using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Research.SEAL;
using Newtonsoft.Json;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using UsersFlow.Interfaces;
using UsersFlow.Model;
using UsersFlow.ModelView;
using UsersFlow.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;


namespace UsersFlow.View
{
    public partial class ProfilePage : ContentPage, IReceiveBT, INotificationManager
    {
        #region Bluetooth variables
        private IAdapter adapter;
        private IBluetoothLE ble;
        private IList<IDevice> devices;
        public static IDevice selectedDevice;
        private string isConnected;
        public static ICharacteristic characteristicRxTx; //Characteristic for Rx and Tx of BT Module
        IReadOnlyList<ICharacteristic> characteristics0;
        IReadOnlyList<ICharacteristic> characteristics1;
        IReadOnlyList<IService> servicesList;
        public static String stringBuff2;
        /** Socket TCP */
        private static int serverPortTCP = 10001;
        private static TcpClient client = null;
        private static NetworkStream nwStream = null;
        CancellationTokenSource cts;
        CancellationTokenSource ctsRcv;
        CancellationTokenSource ctsToken;
        #endregion
        #region User variables
        public string Name { get; set; }
        private bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }
        UserModel CurrentUser;
        UserModel DecryptedUser;
        public ObservableCollection<UserModel> Users { get; set; }
        #endregion
        #region Constructor
        public ProfilePage()
        {
            InitializeComponent();
            //MessagingCenter.Unsubscribe<INotificationManager, string>(this, "SecretKeyRequirement");
            MessagingCenter.Subscribe<object, string>(this, "SecretKeyRequirement",
                async (sender, arg) =>
                {
                    var answ = await DisplayAlert("Secret Key Retrieval", $"Do you want to share its secret key to {arg}?", "Yes", "No");
                    if (answ)
                    {
                        await Device.InvokeOnMainThreadAsync(async () =>
                        {
                            if (arg.Length <= 10)
                            {
                                string username1 = arg;
                                Console.WriteLine("[SERVER] RECEIVED ORDER TO RETRIEVED SECRET KEY");
                                // await Navigation.PushAsync(new RetrieveSecretKey(username));
                                // 6 * - Send the secret key via ssl to the server
                                // Establish a TCP connection to the server
                                var client = new TcpClient("82.223.103.136", serverPortTCP);
                                // Retrieve self-signed certificate in order to handle the connections in the server
                                X509Certificate ClientCertif = new X509Certificate("client.pfx", "root");
                                X509CertificateCollection coll = new X509CertificateCollection();
                                coll.Add(ClientCertif);
                                Console.WriteLine($"[X509] Client certitificate subject: {ClientCertif.Subject.ToString()}");
                                // SSL CONNECTION
                                SslStream sslStream = new SslStream(client.GetStream(),
                                    false,
                                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                                    null);
                                sslStream.AuthenticateAsClient("82.223.103.136", coll, SslProtocols.Tls12, false);
                                // Retrieve the key from secure storage of the phone
                                var strSecretKey = await SecureStorage.GetAsync("SecretKey-" + username1);
                               
                               
                                WriteMessage("Requested secret key!<EOF>", client, sslStream);
                                string answFromSSLServer = ReadMessage(sslStream);
                                if (answFromSSLServer == "Tell me!<EOF>")
                                {
                                    WriteMessage(strSecretKey, client, sslStream);
                                    //Debug.WriteLine($"[CLIENT] Sended secret key: {strSecretKey}");
                                    sslStream.Close();
                                    client.Close();
                                }
                                else
                                {
                                    Console.WriteLine("[CLIENT] Keys exchange failed.");
                                }

                            }
                        });
                    }
                   
                    
                });
                //MessagingCenter.Unsubscribe<INotificationManager, string>(this, "SecretKeyRetrieval");
                MessagingCenter.Subscribe<object, string>(this, "SecretKeyRetrieval",
                    async (s, a) =>
                    {
                        await DisplayAlert("Secret Key Retrieval", "Your secret key has been retrieved correctly from the HR Manager.", "Ok");
                        if(a == "SecretKeyRetrieval")
                        {
                            /**
                                * SSL HANDSHAKE
                                */
                            var client = new TcpClient("82.223.103.136", serverPortTCP);
                            X509Certificate ClientCertif = new X509Certificate("client.pfx", "root");
                            X509CertificateCollection coll = new X509CertificateCollection();
                            coll.Add(ClientCertif);
                            Console.WriteLine($"[X509] Client certitificate subject: {ClientCertif.Subject.ToString()}");
                            SslStream sslStream = new SslStream(client.GetStream(),
                                false,
                                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                                null);
                            sslStream.AuthenticateAsClient("82.223.103.136", coll, SslProtocols.Tls12, false);
                            //Tell me the secret key
                            WriteMessage("TellMeTheSecret<EOF>", client, sslStream);
                            string secretKeyAnswer = ReadMessage(sslStream);
                            // close ssl connection for finishing the loop
                            try
                            {
                                // DECRYPTING THE USER WITH ITS SECRETE KEY RETRIEVEN
                                 var bytesSecretKey = Convert.FromBase64String(secretKeyAnswer);
                                //MemoryStream streamSecretKey = new MemoryStream(bytesSecretKey);
                               
                                var strSecretKey = Convert.ToBase64String(bytesSecretKey.ToArray());
                                
                                await SecureStorage.SetAsync("SecretKey-" + CurrentUser.username, strSecretKey);
                                var secretKeySaved = await SecureStorage.GetAsync("SecretKey-" + CurrentUser.username);
                                await App.Current.SavePropertiesAsync();
                                Console.WriteLine($"[FHE] Secret key saved. : {secretKeyAnswer}");
                                await Task.Run(async () =>
                                {
                                    if(secretKeySaved != null)
                                    {
                                        string CurrentUserJson1 = App.Current.Properties["CurrentUser"].ToString();
                                        CurrentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson1);
                                        DecryptedUser = new UserModel(null, null);
                                        // Decrypting the user
                                        //Console.WriteLine($"[UsersFlow] EncParms of the Current User: {CurrentUser.encParms}");
                                        DecryptedUser = await FHEHandler.decryptUserFromDB(CurrentUser);
                                        await Device.InvokeOnMainThreadAsync(() =>
                                        {
                                            username.Text = DecryptedUser.username;
                                            Birth.Text = DecryptedUser.Birth;
                                            DNI.Text = DecryptedUser.DNI;
                                            SegSocialNumber.Text = DecryptedUser.SegSocialNumber;
                                            IBAN.Text = DecryptedUser.IBAN;
                                            Privilege.Text = DecryptedUser.Privilege;
                                            ciphDNI.Text = DecryptedUser.cipherDNI.Substring(0, 30) + "\n" +
                                                DecryptedUser.cipherDNI.Substring(31, 35) + "\n" +
                                                DecryptedUser.cipherDNI.Substring(36, 37) + "\n...\n";
                                            ciphSeg.Text = DecryptedUser.cipherSegSocial.Substring(0, 30) + "\n" +
                                                DecryptedUser.cipherSegSocial.Substring(31, 35) + "\n" +
                                                DecryptedUser.cipherSegSocial.Substring(36, 37) + "\n...\n";
                                            ciphIban.Text = DecryptedUser.cipherIban.Substring(0, 30) + "\n" +
                                                DecryptedUser.cipherIban.Substring(31, 35) + "\n" +
                                                DecryptedUser.cipherIban.Substring(36, 37) + "\n...\n";
                                        });
                                    }
                                    
                                });

                            }catch(Exception ex) { Console.WriteLine(ex.ToString()); }
                            // retrieve the secret key and decrypt the user
                           
                        }
                    });
            
            Users = new ObservableCollection<UserModel>();
            // Save the returned user in the login into the session storage
            string CurrentUserJson = App.Current.Properties["CurrentUser"].ToString();
            CurrentUser = JsonConvert.DeserializeObject<UserModel>(CurrentUserJson);
            Name = CurrentUser.Name;
            /** Initialize bluetooth handlers */
            adapter = CrossBluetoothLE.Current.Adapter;
            ble = CrossBluetoothLE.Current;
            devices = new List<IDevice>();
            servicesList = new ObservableCollection<IService>();
            stringBuff2 = "";
            isConnected = "disconnected";

            // Set the scan timeout to 5 seconds
            adapter.ScanTimeout = 5000;
            // Subscribe to the Bluetooth state change event
            ble.StateChanged += (s, e) =>
            {
                switch (e.NewState)
                {
                    case BluetoothState.Off:
                        btButton.IsEnabled = false;
                        btButton.BackgroundColor = Color.FromHex("D3D3D3");
                        btButton.BorderColor = Color.FromHex("D3D3D3");
                        DisplayAlert("Bluetooth",
                           $"Please turn on your Phone Bluetooth",
                           "OK");
                        break;
                    case BluetoothState.On:
                        btButton.IsEnabled = true;
                        btButton.BackgroundColor = Color.FromHex("#B0E0E6");
                        btButton.BorderColor = Color.FromHex("#B0E0E6");
                        break;
                }
            };

            /** Binding Context */
            BindingContext = this;

        }
        #endregion
        #region buttons
        async void btn_logout(object sender, EventArgs e)
        {
            App.Current.Logout();
        }
        async void save_schedule_btn(object o, EventArgs e)
        {
            await Navigation.PushAsync(new ScheduleView());
        }
        #endregion
        #region Decrypt user
        async void btn_retrive_noCiph(object sender, EventArgs e)
        {
            spinner.IsVisible = true;
            spinner.IsRunning = true;
            try
            {
                // Decrypt user's information
                if (CurrentUser.Privilege == "Manager")
                {

                    //if the user is the mananger, it will have al the necessary private keys to decrypt the user
                    DecryptedUser = await FHEHandler.decryptUserFromDB(CurrentUser);
                    username.Text = DecryptedUser.username;
                    Birth.Text = DecryptedUser.Birth;
                    DNI.Text = DecryptedUser.DNI;
                    SegSocialNumber.Text = DecryptedUser.SegSocialNumber;
                    IBAN.Text = DecryptedUser.IBAN;
                    Privilege.Text = DecryptedUser.Privilege;
                    ciphDNI.Text = DecryptedUser.cipherDNI.Substring(0, 30) + "\n" +
                        DecryptedUser.cipherDNI.Substring(31, 35) + "\n" +
                        DecryptedUser.cipherDNI.Substring(36, 37) + "\n...\n";
                    ciphSeg.Text = DecryptedUser.cipherSegSocial.Substring(0, 30) + "\n" +
                        DecryptedUser.cipherSegSocial.Substring(31, 35) + "\n" +
                        DecryptedUser.cipherSegSocial.Substring(36, 37) + "\n...\n";
                    ciphIban.Text = DecryptedUser.cipherIban.Substring(0, 30) + "\n" +
                        DecryptedUser.cipherIban.Substring(31, 35) + "\n" +
                        DecryptedUser.cipherIban.Substring(36, 37) + "\n...\n";
                }
                else if (CurrentUser.Privilege == "Developer")
                {

                    /**
                     * SSL KEYS ECHANGE
                     */
                    try
                    {
                        // Establish a TCP connection to the server
                        var client = new TcpClient("82.223.103.136", serverPortTCP);
                        // Retrieve self-signed certificate in order to handle the connections in the server
                        X509Certificate ClientCertif = new X509Certificate("client.pfx", "root");
                        X509CertificateCollection coll = new X509CertificateCollection();
                        coll.Add(ClientCertif);
                        Console.WriteLine($"[X509] Client certitificate subject: {ClientCertif.Subject.ToString()}");
                        // SSL CONNECTION
                        SslStream sslStream = new SslStream(client.GetStream(),
                            false,
                            new RemoteCertificateValidationCallback(ValidateServerCertificate),
                            null);
                        sslStream.AuthenticateAsClient("82.223.103.136", coll, SslProtocols.Tls12, false);
                        // This is where you read and send data
                        // 1 - Hello Server
                        WriteMessage("HelloServer<EOF>", client, sslStream);
                        while (true)
                        {
                            string serverMessage = ReadMessage(sslStream);
                            if (serverMessage == "HelloClient<EOF>")
                            {
                                WriteMessage("SKReq<EOF>", client, sslStream);
                            }
                            else if (serverMessage == "Client?<EOF>")
                            {
                                WriteMessage(CurrentUser.username + "$<EOF>", client, sslStream);
                            }
                            else
                            {
                                sslStream.Close();
                                client.Close();
                            }
                        }



                    }
                    catch (AuthenticationException ex) { Console.WriteLine(ex.ToString()); }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                }
            }catch(Exception ex) { Console.WriteLine(ex); }
           
           
            spinner.IsVisible = false;
            spinner.IsRunning = false;
            this.ForceLayout();
        }
        #endregion
        #region Write SSL Message
        static void WriteMessage(string mess, TcpClient client, SslStream sslStream)
        {
            Console.WriteLine($"[CLIENT] Sended to server via SSL: {mess}");
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
        #region Read SSL Message
        static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            byte[] buffer = new byte[300000];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
                
            } while (bytes != 0);
            Console.WriteLine($"[SERVER] Server sended: {messageData.ToString()}");
            return messageData.ToString();
        }
        #endregion
        #region Remove User
        async void btn_remove_user(object o, EventArgs e)
            {
                bool answer1 = false;
                bool answer = await DisplayAlert("Remove User",
                    "Are you sure you want to delete the user?", "Yes", "No");
            if (answer)
            {
                answer1 = await DisplayAlert("Remove User",
                    "All the user data will be deleted", "Continue", "Cancel");

            }
            else
            {
                await Navigation.PopToRootAsync();
            }

                if (answer1)
                {
                    var responseFromServer = await ApiConnection.DeleteUser(CurrentUser.username);
                    if (responseFromServer.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Success",
                            "User removed correctly.", "Ok");
                    // If the user is an employee (developer), remove its secret key for security
                        if (CurrentUser.Privilege == "Developer")
                        {
                            await SecureStorage.SetAsync("SecretKey-" + CurrentUser.username, "");
                        }
                        App.Current.Properties.Clear();
                        await App.Current.SavePropertiesAsync();
                        App.Current.Logout();
                    }
                    else
                    {
                        await DisplayAlert("Error",
                            "User has not been removed. Response from server: \n" +
                            responseFromServer.StatusCode, "Ok");
                    }
                }
            }
        #endregion
        #region Validate Server Certificate
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        #endregion
        #region Bluetooth
        async void bt_button(object sender, EventArgs e)
            {
                try
                {

                    spinner.IsVisible = true;
                    spinner.IsRunning = true;

                    // Scan for Bluetooth devices
                    devices.Clear();
                    // Create a list of device names
                    var deviceNames = new List<string>();
                    adapter.DeviceDiscovered += (s, a) =>
                    {
                        if (!devices.Any(d => d.Id == a.Device.Id)) // Check if the device name is already in the list
                        {
                            devices.Add(a.Device);
                        }
                    };
                    await adapter.StartScanningForDevicesAsync();


                    foreach (var device in devices)
                    {
                        deviceNames.Add(device.Name);
                    }
                    spinner.IsVisible = false;
                    spinner.IsRunning = false;
                    // Display the list of device names
                    var selectedName = await DisplayActionSheet(
                        "Select a device",
                        "Cancel", null,
                        deviceNames.ToArray());
                    if (selectedName != "Cancel")
                    {
                        // Get the selected device
                        foreach (var device in devices)
                        {
                            if (device.Name == selectedName)
                            {
                                selectedDevice = device;
                                break;
                            }
                        }

                        // Connect to the selected device
                        await adapter.ConnectToDeviceAsync(selectedDevice);
                        //isConnected = selectedDevice.State.ToString();
                        if (selectedDevice.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                        {
                            btButton.BackgroundColor = Color.FromHex("#81FF68");
                            btButton.BorderColor = Color.FromHex("#81FF68");
                            await DisplayAlert("Connected",
                                $"Connected to {selectedName}",
                                "OK");

                            //characteristics0 = await servicesList[0].GetCharacteristicsAsync();
                            //characteristics1 = await servicesList[1].GetCharacteristicsAsync();
                            /*
                            foreach (ICharacteristic c0 in characteristics0)
                            {

                                Debug.WriteLine("[BT] Characteristic for service 0: ");
                                Debug.WriteLine(c0.Id);
                                Debug.WriteLine(c0.Properties);
                                Debug.WriteLine("Read: " + c0.CanRead);
                                Debug.WriteLine("Write: " + c0.CanWrite);

                                if ((c0.CanRead == true) & (c0.CanWrite == true))
                                {
                                    characteristicRxTx = c0;
                                }
                            }*/


                            try
                            {
                                if (characteristicRxTx != null)
                                {
                                    byte[] answ = await characteristicRxTx.ReadAsync();
                                    characteristicRxTx.ValueUpdated += (o, args) =>
                                    {
                                        byte[] receivedBytes = args.Characteristic.Value;
                                        stringBuff2 = Encoding.GetEncoding("iso-8859-1").GetString(receivedBytes, 0, receivedBytes.Length);
                                        try
                                        {
                                            MessagingCenter.Send<IReceiveBT, string>(this, "ReceiveBT", stringBuff2);
                                            //Clear BT buffer after sending by MessagingCenter
                                            stringBuff2 = "";
                                        }
                                        catch (Exception _) { }
                                    };
                                    await characteristicRxTx.StartUpdatesAsync();
                                    Debug.WriteLine("[BT] BLE ready");
                                }

                            }
                            catch (Exception _) { }

                        }
                        else
                        {
                            btButton.BackgroundColor = Color.FromHex("D3D3D3");
                            btButton.BorderColor = Color.FromHex("D3D3D3");
                            await DisplayAlert("Error",
                                $"Could not connect to {selectedName}",
                                "OK");
                        }


                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
                finally
                {
                    // Stop scanning for Bluetooth devices
                    await adapter.StopScanningForDevicesAsync();
                }
            }
            async void disconnect_bt_button(object o, EventArgs e)
            {
                try
                {
                    if (selectedDevice.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                    {
                        _ = adapter.DisconnectDeviceAsync(selectedDevice);
                        await DisplayAlert("Disconnected",
                                $"Disconnected from {selectedDevice.Name}",
                                "OK");
                    }
                    else
                    {
                        DisplayAlert("Error", "There is no device for disconnection", "OK");
                    }
                }
                catch (Exception ex) { }

                /** Advertise Bluetooth's State */
                //MessagingCenter.Send<IReceiveBT, string>(this, "BluetoothStatus", isConnected); 
            }
            /**
             * Function used to write on the Bluetooth physical device
             * 
             * @param msgToSend the data to be sent to the BT device
             */
             async void WriteBT(string msgToSend)
            {
                if (selectedDevice != null)
                {
                    byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(msgToSend);
                    //Console.WriteLine("Message sended in BT:" + msgToSend);
                    await characteristicRxTx.WriteAsync(bytes);
                }
                else
                {
                    Console.WriteLine("[BT] No device connected");
                }
            }


            #endregion
        }
    }
