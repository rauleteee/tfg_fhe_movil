using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace UsersFlow.View
{	
	public partial class CipheredDataShown : ContentPage
	{

		
		public CipheredDataShown ()
		{
            InitializeComponent();

            CipherIban.Text = "This is your ciphered IBAN number of your Bank Account: \n" +
				NewUser.newUser.cipherIban.ToString();
        }
    }
}

