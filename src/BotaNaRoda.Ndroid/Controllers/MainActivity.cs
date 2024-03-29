﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using SupportToolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.V4.Widget;
using BotaNaRoda.Ndroid.Auth;
using BotaNaRoda.Ndroid.Controllers;
using BotaNaRoda.Ndroid.Data;
using BotaNaRoda.Ndroid.Gcm;
using BotaNaRoda.Ndroid.Models;
using Square.Picasso;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace BotaNaRoda.Ndroid
{
	[Activity(
        Label = "Bota na Roda", MainLauncher = true, Icon = "@drawable/icon",
        LaunchMode = Android.Content.PM.LaunchMode.SingleTask,
        Theme = "@style/MainTheme")]
    public class MainActivity : AppCompatActivity, AdapterView.IOnItemClickListener
	{
        private DrawerLayout _mDrawerLayout;
        private ListView _mLeftDrawer;
        private Dictionary<string, Tuple<Type, Lazy<Bundle>>> _mLeftDataSet;
        private ArrayAdapter<string> _mLeftAdapter;
		private MyActionBarDrawerToggle _mDrawerToggle;
	    private Fragment _currentFragment;
		private readonly object _lockObj = new object();

	    protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);

            _mDrawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
			_mLeftDrawer = FindViewById<ListView>(Resource.Id.left_drawer);
			SupportActionBar.SetDisplayHomeAsUpEnabled (true);
			SupportActionBar.SetHomeButtonEnabled(true);

	        _mLeftDataSet = new Dictionary<string, Tuple<Type, Lazy<Bundle>>>
            {
                {"Produtos próximos a mim", new Tuple<Type, Lazy<Bundle>>(typeof (ItemsFragment), null)},
                {"Meus produtos", new Tuple<Type, Lazy<Bundle>>(typeof (ItemsFragment), 
                    new Lazy<Bundle>(() =>
                    {
                        var b = new Bundle();
                        b.PutString(ItemsFragment.BundleItemsFilter, ItemsLoader.Filter.MyItemsOnly.ToString());
                        return b;
                    }))},
                {"Reservas", new Tuple<Type, Lazy<Bundle>>(typeof (ConversationsFragment), null)},
                {"Mapa", new Tuple<Type, Lazy<Bundle>>(typeof(ItemsMapFragment), null)}
	        };
	        _mLeftAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, _mLeftDataSet.Keys.ToArray());
			_mLeftDrawer.Adapter = _mLeftAdapter;
            _mLeftDrawer.OnItemClickListener = this;

			_mDrawerToggle = new MyActionBarDrawerToggle(
				this,								//Host Activity
				_mDrawerLayout,						//DrawerLayout
				Resource.String.ApplicationName,	//Opened Message
				Resource.String.ApplicationName,		//Closed Message
                new UserRepository()
			);

			_mDrawerLayout.SetDrawerListener(_mDrawerToggle);
			_mDrawerToggle.SyncState();

            //Container
            LoadFragment(_mLeftDataSet.First().Value);

            //Check for gplay
	        if (IsPlayServicesAvailable())
	        {
	            var intent = new Intent(this, typeof (RegistrationIntentService));
	            StartService(intent);
	        }
        }

	    protected override void OnStart()
	    {
	        base.OnStart();
	    }

	    public override bool OnOptionsItemSelected (IMenuItem item)
		{		
			switch (item.ItemId)
			{
			    case Android.Resource.Id.Home:
				    //The hamburger icon was clicked which means the drawer toggle will handle the event
				    //all we need to do is ensure the right drawer is closed so the don't overlap
				    _mDrawerToggle.OnOptionsItemSelected(item);
				    return true;
			    default:
				    return base.OnOptionsItemSelected (item);
			}
		}

		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.ItemsMenu, menu);
			return base.OnCreateOptionsMenu (menu);
		}

		protected override void OnPostCreate (Bundle savedInstanceState)
		{
			base.OnPostCreate (savedInstanceState);
			_mDrawerToggle.SyncState();
		}

		public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged (newConfig);
			_mDrawerToggle.OnConfigurationChanged(newConfig);
		}
        
        private void LoadFragment(Tuple<Type, Lazy<Bundle>> value)
        {
			for (int i = 0; i < SupportFragmentManager.BackStackEntryCount; i++) {
				SupportFragmentManager.PopBackStack ();
			}

            _currentFragment = (Fragment) Activator.CreateInstance(value.Item1);
            if (value.Item2 != null)
            {
                _currentFragment.Arguments = value.Item2.Value;
            }
            SupportFragmentManager
                .BeginTransaction()
                .Replace(Resource.Id.mainFragmentContainer, _currentFragment)
                .SetTransition(FragmentTransaction.TransitFragmentFade)
                .Commit();
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
	    {
            _mDrawerLayout.CloseDrawers();
            LoadFragment(_mLeftDataSet.ElementAt(position).Value);
	    }

        private bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                {
                    GoogleApiAvailability.Instance.ShowErrorNotification(this, resultCode);
                }
                else
                {
                    Toast.MakeText(this, "Sorry, this device is not supported", ToastLength.Long);
                    Finish();
                }
                return false;
            }
            return true;
        }
    }
}

