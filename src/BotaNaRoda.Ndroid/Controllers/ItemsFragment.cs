﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using BotaNaRoda.Ndroid.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Xamarin.Auth;
using com.refractored.fab;

namespace BotaNaRoda.Ndroid.Controllers
{
    [Activity(Label = "Bota na Roda")]
	public class ItemsFragment : Android.Support.V4.App.Fragment, ILocationListener
    {
        private GridView _itemsListView;
        private ItemsListAdapter _adapter;
        private LocationManager _locMgr;
        private SwipeRefreshLayout _refresher;
        private UserService _userService;
        private ItemData _itemData;

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view =  inflater.Inflate (Resource.Layout.Items, container, false);
			_userService = new UserService(Activity);
			_itemData = new ItemData(Activity);

			_refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
			_refresher.Refresh += delegate
			{
				Refresh();
			};

			view.FindViewById<FloatingActionButton> (Resource.Id.fab).Click += NewItem;
			_itemsListView = view.FindViewById<GridView> (Resource.Id.itemsGridView);
			_adapter = new ItemsListAdapter (Activity, _itemData);
			_itemsListView.Adapter = _adapter;
			_itemsListView.ItemClick += _itemsListView_ItemClick;

			return view;
		}

		public override void OnStart ()
		{
			base.OnStart ();
			//_locMgr = Activity.GetSystemService(Context.LocationService) as LocationManager;
		}

		public override void OnResume ()
		{
			base.OnResume ();
            Refresh();

			//string provider = _locMgr.GetBestProvider (new Criteria
			//	{
			//		Accuracy = Accuracy.Coarse,
			//		PowerRequirement = Power.NoRequirement
			//	}, true);
			//_locMgr.RequestLocationUpdates (provider, 20000, 100, this);
		}

		public override void OnPause ()
		{
			base.OnPause ();
			//_locMgr.RemoveUpdates (this);
		}

		void _itemsListView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			Intent itemDetailIntent = new Intent (Activity, typeof(ItemDetailActivity));
			itemDetailIntent.PutExtra ("itemId", e.Position);
			StartActivity (itemDetailIntent);
		}

		public void OnLocationChanged (Location location)
		{
			_adapter.CurrentLocation = location;
			_adapter.NotifyDataSetChanged ();
		}

		public void OnProviderDisabled (string provider)
		{
		}

		public void OnProviderEnabled (string provider)
		{
		}

		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
		}

		private void NewItem(object sender, EventArgs args)
		{
		    if (_userService.IsLoggedIn)
		    {
		        Activity.StartActivity(typeof (ItemCreateActivity));
		    }
		    else
		    {
		        Activity.StartActivity(typeof(LoginActivity));
		    }
		}

        private void Refresh()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += async (sender, args) =>
            {
                _itemData.Service.RefreshCache();
                _adapter.Items = await _itemData.Service.GetAllItems();
            };
            worker.RunWorkerCompleted += (sender, args) => {
                Activity.RunOnUiThread(() =>
                {
                    _refresher.Refreshing = false;
                    _adapter.NotifyDataSetChanged();
                });
            };
            worker.RunWorkerAsync();
       }

	}
}
