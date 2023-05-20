using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace KeysExchange
{
	public interface TcpListenerInterface
	{
		Task StartListeningAsync(IPAddress ipAddress, int port, X509Certificate certificate);

    }
}

