﻿using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using BotaNaRoda.Ndroid.Auth;
using BotaNaRoda.Ndroid.Data;
using Xamarin.Auth;

namespace BotaNaRoda.Ndroid.Controllers
{
	[Activity (Label = "LoginActivity",
		ConfigurationChanges = (ConfigChanges.Orientation | ConfigChanges.ScreenSize), 
        ParentActivity = typeof(MainActivity))]
	public class LoginActivity : Activity
	{
		private static readonly TaskScheduler UiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		private UserRepository _userRepository;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			//SetContentView (Resource.Layout.Login);
            //ActionBar.SetDisplayHomeAsUpEnabled(true);

            _userRepository = new UserRepository(this);
            Login();
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			Finish ();
		}

        void Login()
        {
            var auth = new CustomOAuth2Authenticator();
            auth.Error += (sender, args) =>
            {
            
            };
            // If authorization succeeds or is canceled, .Completed will be fired.
            auth.Completed += (s, ee) => {
                if (!ee.IsAuthenticated)
                {
                    return;
                }

                //Stores account
                _userRepository.Save(ee.Account);
            };

            var intent = auth.GetUI(this);
            StartActivity(intent);
        }

	}
}

