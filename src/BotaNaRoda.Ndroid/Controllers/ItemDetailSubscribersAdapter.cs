using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using BotaNaRoda.Ndroid.Data;
using BotaNaRoda.Ndroid.Models;
using BotaNaRoda.Ndroid.Util;
using Xamarin.Auth;
using Square.Picasso;
using AlertDialog = Android.App.AlertDialog;
using System.Collections.Generic;
using Android.Content;

namespace BotaNaRoda.Ndroid.Controllers
{

	public class ItemDetailSubscribersAdapter : BaseAdapter<UserViewModel>
	{
		private readonly Activity _context;
	    private readonly IList<UserViewModel> _users;

		public ItemDetailSubscribersAdapter(Activity context, IList<UserViewModel> users)
		{
			_context = context;
			_users = users;
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.ItemDetailSubscribersListItem, null);

			var user = this[position];

			view.FindViewById<TextView>(Resource.Id.itemDetailSubscriberName).Text = user.Name;
			view.FindViewById<TextView>(Resource.Id.itemDetailSubscriberLocality).Text = user.Locality ?? "";

			var imageView = view.FindViewById<ImageView>(Resource.Id.itemDetailSubscriberProfileImage);
		    imageView.Click += (sender, args) =>
		    {
		        Intent userDetailIntent = new Intent(_context, typeof (UserDetailActivity))
		            .PutExtra(UserDetailActivity.UserIdExtra, user.Id);
		        _context.StartActivity(userDetailIntent);
		    };
			Picasso.With(_context)
				.Load(user.Avatar)
				.Fit()
				.Into(imageView);

			return view;
		}

		public override int Count
		{
			get { return _users.Count; }
		}

		public override UserViewModel this[int index]
		{
			get { return _users[index]; }
		}

	}
}
