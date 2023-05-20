using System;
namespace UsersFlow.Interfaces
{
	public class INotificationEventArgs: EventArgs
	{
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string Message { get; set; }

    }
}

