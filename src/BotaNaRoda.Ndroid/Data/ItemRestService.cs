using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Media;
using BotaNaRoda.Ndroid.Controllers;
using BotaNaRoda.Ndroid.Models;
using Newtonsoft.Json;
using Xamarin.Auth;
using Path = System.IO.Path;
using ModernHttpClient;

namespace BotaNaRoda.Ndroid.Data
{
    public class ItemRestService : IItemService
    {
        private readonly Context _context;
        private readonly UserService _userService;
        private const string BotaNaRodaItemsEndpoint = "https://botanaroda.azurewebsites.net/api/items";
        private readonly HttpClient _httpClient;

        public ItemRestService(Context context, string storagePath, UserService userService)
        {
            _context = context;
            _userService = userService;

            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            //On android NativeMessageHandler will resolve to OkHttp
			_httpClient = new HttpClient(new NativeMessageHandler());
			//_httpClient = new HttpClient();
        }

		public async Task<IEnumerable<ItemListViewModel>> GetAllItems(double lat, double lon)
        {
            SetupAuthorizationHeader();

			var response = await _httpClient.GetAsync(BotaNaRodaItemsEndpoint + string.Format("?latitude={0}&longitude={1}&radius={2}&offset={3}", lat, lon, 10000, 0));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<ItemListViewModel>>(json);
            }

            LoginUserIfUnauthorizedResponse(response);
            return new List<ItemListViewModel>();
        }

        public void RefreshCache()
        {
        }

        public async Task<ItemDetailViewModel> GetItem(string id)
        {
            SetupAuthorizationHeader();

            var response = await _httpClient.GetAsync(Path.Combine(BotaNaRodaItemsEndpoint, id));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ItemDetailViewModel>(json);
            }

            LoginUserIfUnauthorizedResponse(response);
            return null;
        }

        public async Task<string> SaveItem(ItemCreateBindingModel item)
        {
            SetupAuthorizationHeader();

            if (item.Images == null || item.Images.Length > 3)
            {
                throw new ArgumentException("item");
            }

            var imgs = await UploadImages(item.Images.Select(x => x.Url).ToArray());
            if (imgs == null)
            {
                throw new ArgumentException("Não foi possível carregar imagens", "item");    
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

            LoginUserIfUnauthorizedResponse(response);
            return null;
        }


        public async Task<bool> DeleteItem(string id)
        {
            SetupAuthorizationHeader();

            var response = await _httpClient.DeleteAsync(Path.Combine(BotaNaRodaItemsEndpoint, id));
            LoginUserIfUnauthorizedResponse(response);
            return response.IsSuccessStatusCode;
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

        private void SetupAuthorizationHeader()
        {
            var account = _userService.GetCurrentUser();
            if (account != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", account.Properties["access_token"]);
            }
        }

        private void LoginUserIfUnauthorizedResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode && _httpClient.DefaultRequestHeaders.Authorization != null)
            {
                _context.StartActivity(typeof(LoginActivity));
            }
        }
    }
}