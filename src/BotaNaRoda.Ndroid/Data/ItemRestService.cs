using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.Media;
using Android.Util;
using BotaNaRoda.Ndroid.Auth;
using BotaNaRoda.Ndroid.Controllers;
using BotaNaRoda.Ndroid.Models;
using IdentityModel.Client;
using ModernHttpClient;
using Newtonsoft.Json;
using Xamarin.Auth;
using Path = System.IO.Path;

namespace BotaNaRoda.Ndroid.Data
{
    public class ItemRestService : IItemService
    {
        private readonly Context _context;
        private readonly UserRepository _userRepository;
        private const string BotaNaRodaItemsEndpoint = Constants.BotaNaRodaEndpoint + "/items";
        private const string BotaNaRodaUsersEndpoint = Constants.BotaNaRodaEndpoint + "/users";
		private const string BotaNaRodaConversationsEndpoint = Constants.BotaNaRodaEndpoint + "/conversations";
		private const string BotaNaRodaAccountEndpoint = Constants.BotaNaRodaEndpoint + "/account";
		private const string BotaNaRodaReservationsEndpoint = Constants.BotaNaRodaEndpoint + "/reservations";

        private readonly HttpClient _httpClient;
        private readonly string _storagePath;

        public bool CanLoadMoreItems { get; set; }
        public bool IsBusy { get; set; }
		private readonly object lockObj = new object ();

        public ItemRestService(UserRepository userRepository)
        {
            _context = Application.Context;
            _userRepository = userRepository;

            _storagePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "BotaNaRoda");
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);

            //On android NativeMessageHandler will resolve to OkHttp
			_httpClient = new HttpClient(new NativeMessageHandler());
        }

        public void RefreshCache()
        {
        }

        public async Task<IList<ItemListViewModel>> GetAllItemsAsync(double lat, double lon, double radius, int skip, int limit)
        {
            await SetupAuthorizationHeader();

			var response = await _httpClient.GetAsync(BotaNaRodaItemsEndpoint + string.Format("?latitude={0}&longitude={1}&radius={2}&skip={3}&limit{4}", lat, lon, radius, skip, limit));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<ItemListViewModel>>(json);
            }

            return new List<ItemListViewModel>();
        }

        public async Task<IList<ItemListViewModel>> GetUserItems(string userId, double lat, double lon, double radius, int skip, int limit)
        {
            await SetupAuthorizationHeader();

            if (userId != null)
            {
                var response = await _httpClient.GetAsync(
                    Path.Combine(BotaNaRodaUsersEndpoint, userId, "items" + string.Format("?latitude={0}&longitude={1}&radius={2}&skip={3}&limit{4}", lat, lon, radius, skip, limit)));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<IList<ItemListViewModel>>(json);
                }
            }

            return new List<ItemListViewModel>();
        }

        public async Task<ItemDetailViewModel> GetItem(string id)
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.GetAsync(Path.Combine(BotaNaRodaItemsEndpoint, id));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ItemDetailViewModel>(json);
            }

            return null;
        }

        public async Task<string> PostItem(ItemCreateBindingModel item)
        {
            await SetupAuthorizationHeader();

            if (item.Images == null || item.Images.Length > 3)
            {
                throw new ArgumentException("item");
            }

            var imgs = await UploadImages(item.Images.Select(x => x.Url).Distinct().ToArray());
            if (imgs == null)
            {
                throw new ArgumentException("N�ao foi possivel carregar imagens", "item");    
            }

            item.ThumbImage = imgs.Last();
            imgs.Remove(imgs.Last());
            item.Images = imgs.ToArray();

            var response = await _httpClient.PostAsync(BotaNaRodaItemsEndpoint, 
				new StringContent(JsonConvert.SerializeObject(item), System.Text.Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        public async Task<bool> DeleteItem(string id)
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.DeleteAsync(Path.Combine(BotaNaRodaItemsEndpoint, id));
            return response.IsSuccessStatusCode;
        }

        public async Task<UserViewModel> GetUserProfileAsync(string userId = "me")
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.GetAsync(Path.Combine(BotaNaRodaUsersEndpoint, userId));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserViewModel>(json);
            }

            return new UserViewModel();
        }

        private async Task<IList<ImageInfo>> UploadImages(string[] imgPaths)
        {
            using (var content = new MultipartFormDataContent())
            {
                foreach (var path in imgPaths)
                {
                    var fs = new FileStream(path, FileMode.Open);
                    var imageContent = new StreamContent(fs);
					imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse ("image/jpeg"); 
                    content.Add(imageContent, "files", "image.jpg");
                }
				try{
					var response = await _httpClient.PostAsync(Path.Combine(BotaNaRodaItemsEndpoint, "images"), content);
					if (response.IsSuccessStatusCode)
					{
						var imageInfosJson = await response.Content.ReadAsStringAsync();
						return JsonConvert.DeserializeObject<ImageInfo[]>(imageInfosJson).ToList();
					}
				}catch(Exception ex){
					Console.WriteLine (ex);
				}

            }
            return null;
        }

        public async Task<bool> PostDeviceRegistrationId(string registrationId)
        {
            await SetupAuthorizationHeader();

			var response = await _httpClient.PostAsync(Path.Combine(BotaNaRodaAccountEndpoint, "DeviceRegistrationId"),
				new StringContent(JsonConvert.SerializeObject(registrationId), System.Text.Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PostUserLocalization(UserLocalizationBindingModel localization)
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.PostAsync(Path.Combine(BotaNaRodaAccountEndpoint, "Localization"),
				new StringContent(JsonConvert.SerializeObject(localization), System.Text.Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<IList<ConversationListViewModel>> GetAllConversations()
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.GetAsync(BotaNaRodaConversationsEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<ConversationListViewModel>>(json);
            }

            return new List<ConversationListViewModel>();
        }

        public async Task<ConversationDetailViewModel> GetConversation(string id)
        {
            await SetupAuthorizationHeader();

            var response = await _httpClient.GetAsync(Path.Combine(BotaNaRodaConversationsEndpoint, id));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ConversationDetailViewModel>(json);
            }

            return null;
        }

		public async Task<bool> Subscribe(string itemId)
		{
			await SetupAuthorizationHeader();

			var response = await _httpClient.PostAsync(Path.Combine(BotaNaRodaReservationsEndpoint, itemId, "Subscribe"),
				new StringContent("", System.Text.Encoding.UTF8, "application/json"));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> Unsubscribe(string itemId)
		{
			await SetupAuthorizationHeader();

			var response = await _httpClient.DeleteAsync(Path.Combine(BotaNaRodaReservationsEndpoint, itemId, "Subscribe"));
			return response.IsSuccessStatusCode;
		}

		public async Task<string> Promise(string itemId, string userId)
		{
			await SetupAuthorizationHeader();

			var response = await _httpClient.PostAsync(Path.Combine(BotaNaRodaReservationsEndpoint, itemId, "Promise", userId), 
				new StringContent("", System.Text.Encoding.UTF8, "application/json"));
			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<string>(json);
			}

			return null;
		}

		public async Task<bool> Unpromise(string itemId, string userId)
		{
			await SetupAuthorizationHeader();

			var response = await _httpClient.DeleteAsync(Path.Combine(BotaNaRodaReservationsEndpoint, itemId, "Promise", userId));
			return response.IsSuccessStatusCode;
		}

        private async Task SetupAuthorizationHeader()
        {
            if (_userRepository.IsLoggedIn)
            {
                var authInfo = _userRepository.Get();
                if (!authInfo.IsExpired())
                {
                    _httpClient.SetBearerToken(authInfo.AccessToken);
                }
                else
                {
                    try
                    {
                        var tokenResponse = await IdSvrOAuth2Util.ExchangeRefreshToken(authInfo.RefreshToken);
                        var userInfoResponse = await IdSvrOAuth2Util.GetUserInfoAsync(tokenResponse.RefreshToken);
                        if (!tokenResponse.IsError)
                        {
                            _userRepository.Update(tokenResponse, userInfoResponse);
                            _httpClient.SetBearerToken(tokenResponse.AccessToken);
                        }
                        else
                        {
                            _userRepository.DeleteExistingAccounts();
                            _context.StartActivity(typeof(LoginActivity));
                            Log.Error("ItemRestService", "Could not refresh token. " + tokenResponse.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _userRepository.DeleteExistingAccounts();
                        Log.Error("ItemRestService", "Could not refresh token. " + ex.Message);
                    }
                }

            }
        }

        public string GetTempImageFilename(int imageNumber)
        {
            return Path.Combine(_storagePath, string.Format("itemImg_{0}.jpg", imageNumber));
        }
    }
}