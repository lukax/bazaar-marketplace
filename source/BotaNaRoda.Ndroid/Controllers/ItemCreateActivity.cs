﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using BotaNaRoda.Ndroid.Data;
using BotaNaRoda.Ndroid.Entity;
using Java.IO;
using Environment = System.Environment;
using Uri = Android.Net.Uri;

namespace BotaNaRoda.Ndroid.Controllers
{
	[Activity (Label = "ItemCreateActivity",
		ConfigurationChanges = (ConfigChanges.Orientation | ConfigChanges.ScreenSize))]			
	public class ItemCreateActivity : Activity, ILocationListener
	{
		const int CAPTURE_PHOTO = 0;
		EditText _itemDescriptionView;
		Item _item;
		LocationManager _locMgr;
		EditText _itemTitleView;
		Location currentLocation; 
		ProgressDialog _progressDialog;
		ImageView _itemImageView;
	    Spinner _itemCategory;
		Button _saveButton;

	    protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.ItemCreate);
		
			_locMgr = GetSystemService(LocationService) as LocationManager;

			_itemTitleView = FindViewById<EditText> (Resource.Id.itemTitle);
			_itemDescriptionView = FindViewById<EditText> (Resource.Id.itemDescription);
			_itemImageView = FindViewById<ImageView> (Resource.Id.itemImageView);
            _itemImageView.Click += _imageButton_Click;
		    _itemCategory = FindViewById<Spinner>(Resource.Id.itemCategory);
	        var categoriesAdapter = ArrayAdapter.CreateFromResource(this, Resource.Array.item_categories, Android.Resource.Layout.SimpleSpinnerItem);
            categoriesAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _itemCategory.Adapter = categoriesAdapter;
            _itemCategory.ItemSelected += ItemCategoryOnItemSelected;
			_saveButton = FindViewById<Button> (Resource.Id.saveButton);
			_saveButton.Click += _saveButton_Click;

			_item = new Item ();
			if (Intent.HasExtra ("itemId")) {
				_item = ItemData.Service.GetAllItems () [Intent.GetIntExtra ("itemId", 0)];
				UpdateUI ();
			}
		}



	    private void ItemCategoryOnItemSelected(object sender, AdapterView.ItemSelectedEventArgs itemSelectedEventArgs)
	    {
	    }

	    protected override void OnResume()
	    {
	        base.OnResume();
            GetLocation();
	    }

	    public override bool OnCreateOptionsMenu (IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.ItemCreateMenu, menu);
			return base.OnCreateOptionsMenu (menu);
		}

		public override bool OnPrepareOptionsMenu (IMenu menu)
		{
			base.OnPrepareOptionsMenu (menu);
			// disable delete for a new POI
            //if (_item.Id == null) {
            //    IMenuItem delete = menu.FindItem (Resource.Id.actionDelete);
            //    delete.SetEnabled (false);
            //}
			return true;
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			switch (item.ItemId)
			{
                //case Resource.Id.actionDelete:
                //    DeleteItem ();
                //    return true;
                case Resource.Id.actionMap:
			        OpenMap();
			        return true;
                case Resource.Id.actionLocation:
                    GetLocation();
			        return true;
				default :
					return base.OnOptionsItemSelected(item);
			}
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if (requestCode == CAPTURE_PHOTO) {
				if (resultCode == Result.Ok) {
					// display saved image
					using (Bitmap itemImage = ItemData.GetImageFile (_item.Id, _itemImageView.Width, _itemImageView.Height)) {
						_itemImageView.SetImageBitmap (itemImage);
					}
				} else {
					// let the user know the photo was cancelled
					Toast toast = Toast.MakeText (this, "No picture captured.", ToastLength.Short);
					toast.Show ();
				} 
			} else {
				base.OnActivityResult (requestCode, resultCode, data);
			}
		}

		public void OnLocationChanged (Location location)
		{
			currentLocation = location;
			//Geocoder geocdr = new Geocoder (this);
			//var addresses = geocdr.GetFromLocation (location.Latitude, location.Longitude, 1);
			//if (addresses.Any ()) {
			//	UpdateAddressFields (addresses.First ());
			//}
			_progressDialog.Cancel ();
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

		void UpdateUI ()
		{
			_itemDescriptionView.Text = _item.Description;
		}


		void _saveButton_Click (object sender, EventArgs e)
		{
			if (currentLocation == null) {
				Toast toast = Toast.MakeText (this, "Não é possivel salvar item sem a localização!", ToastLength.Short);
				toast.Show ();
				return;
			}

			_item.Description = _itemDescriptionView.Text;
			_item.Latitude = currentLocation.Latitude;
			_item.Longitude = currentLocation.Longitude;
			ItemData.Service.SaveItem (_item);
			Finish ();
		}

		void _imageButton_Click (object sender, EventArgs e)
		{
			File imageFile = new File(
				ItemData.Service.GetImageFileName(_item.Id));
			var imageUri = Uri.FromFile (imageFile);

			Intent cameraIntent = new Intent(MediaStore.ActionImageCapture);
			cameraIntent.PutExtra (MediaStore.ExtraOutput, imageUri);
			cameraIntent.PutExtra (MediaStore.ExtraSizeLimit, 1 * 1024);

			PackageManager packageManager = PackageManager;
			IList<ResolveInfo> activities =
				packageManager.QueryIntentActivities(cameraIntent, 0);
			if (activities.Count == 0) {
				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("OK", delegate {});
				alertConfirm.SetMessage ("No camera app available.");
				alertConfirm.Show ();
			}
			else {
				StartActivityForResult (cameraIntent, CAPTURE_PHOTO);
			}
		}

		void OpenMap ()
		{
			Uri geoUri;
			if (String.IsNullOrEmpty (_itemTitleView.Text)) {
				geoUri = Uri.Parse (String.Format ("geo:{0},{1}", _item.Latitude, _item.Longitude)); 
			} else {
				geoUri = Uri.Parse (String.Format ("geo:0,0?q={0}", _itemTitleView.Text));
			}
			Intent mapIntent = new Intent (Intent.ActionView, geoUri);

			PackageManager packageManager = PackageManager;
			IList<ResolveInfo> activities =
				packageManager.QueryIntentActivities(mapIntent, 0);
			if (activities.Count == 0) {
				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("OK", delegate {});
				alertConfirm.SetMessage ("No map app available.");
				alertConfirm.Show ();
			} else {
				StartActivity (mapIntent);
			}
		}

		void GetLocation ()
		{
			Criteria criteria = new Criteria ();
			criteria.Accuracy = Accuracy.Fine;
			criteria.PowerRequirement = Power.High;
			_locMgr.RequestSingleUpdate (criteria, this, null);
			_progressDialog = ProgressDialog.Show (this, "", "Obtendo localização");
		}


	}
}
