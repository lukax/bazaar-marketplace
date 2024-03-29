﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using BotaNaRoda.WebApi.Data;
using BotaNaRoda.WebApi.Entity;
using BotaNaRoda.WebApi.Models;
using BotaNaRoda.WebApi.Util;
using IdentityServer3.Core.Extensions;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;

namespace BotaNaRoda.WebApi.Controllers
{
    [Route("[controller]")]
    public class ItemsController : Controller
    {
        private readonly ItemsContext _itemsContext;
        private readonly NotificationService _notificationService;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ILogger<ItemsController> logger, IOptions<AppSettings> appSettings,
            ItemsContext itemsContext, NotificationService notificationService)
        {
            _logger = logger;
            _appSettings = appSettings;
            _itemsContext = itemsContext;
            _notificationService = notificationService;
        }

        // GET: api/items
        [HttpGet]
        public async Task<IEnumerable<ItemListViewModel>> GetAll(double latitude, double longitude, double radius, int skip = 0, int limit = 20)
        {
            const double earthRadiusInKm = 6371.009;

            //new BsonDocument
            //{
                //{ "loc", new BsonDocument
                //    {
                //        { "$geoWithin", new BsonDocument
                //            {
                //                { "$centerSphere", new BsonArray { new BsonArray { longitude, latitude }, radius / earthRadiusInKm } }
                //            }
                //        } 
                //    }
                //},
                //{ "status", 0 }
            //}
            var filter = Builders<Item>.Filter
                .Not(Builders<Item>.Filter.Eq(x => x.Status, ItemStatus.Unavailable));

            var items = await _itemsContext.Items.Find(filter).Skip(skip).Limit(limit).ToListAsync();
            return items
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ItemListViewModel(x));
        }

        // GET api/items/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(string id)
        {
            var item = await _itemsContext.Items.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item == null)
            {
                return HttpNotFound("Unable to find item with id: " + id);
            }

            var user = await _itemsContext.Users.Find(x => x.Id == item.UserId).FirstAsync();

            var viewModel = new ItemDetailViewModel(item, new UserViewModel(user));

            if (User.Identity.IsAuthenticated)
            {
                viewModel.IsSubscribed = item.Subscribers.Contains(User.GetSubjectId());
                if (User.GetSubjectId() == item.UserId)
                {
                    foreach (var subscriberId in item.Subscribers)
                    {
                        var subscriberDetail = await _itemsContext.Users.Find(x => x.Id == subscriberId).FirstOrDefaultAsync();
                        viewModel.Subscribers.Add(new UserViewModel(subscriberDetail));
                    }
                }
            }

            return new ObjectResult(viewModel);
        }

        // POST api/items
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] ItemCreateBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogError("Tried to post item with invalid model. " + ModelState.GetValues());
                return HttpBadRequest(ModelState);
            }

            var item = new Item(model, User.GetSubjectId());
            await _itemsContext.Items.InsertOneAsync(item);

            return new JsonResult(item.Id);
        }

        // DELETE api/items/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            return HttpBadRequest();
            //TODO remove every association with item
            var item = await _itemsContext.Items.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item == null)
            {
                return HttpNotFound("Unable to find item with id: " + id);
            }

            _notificationService.OnItemDelete(item);

            var userId = User.GetSubjectId();
            var result = await _itemsContext.Items.DeleteOneAsync(x => x.Id == id && x.UserId == userId);
            
            //TODO acknowledge result
            //if (result.DeletedCount > 0)
            //{
                return new HttpStatusCodeResult(204); // No content
            //}
            //return HttpNotFound();
        }

        [Route("images")]
        [HttpPost]
        [Authorize]
        public IActionResult PostImage(IList<IFormFile> files)
        {
            if (!ModelState.IsValid || !files.Any() || files.Count > 3)
            {
                _logger.LogWarning("Tried to access items/images endpoint with invalid files. " + ModelState.GetValues());
                return HttpBadRequest(ModelState);
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_appSettings.Options.StorageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("item-imgs");
            // Create the container if it doesn't already exist.
            try
            {
                container.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to create storage container", ex);
                throw;
            }
            container.SetPermissions(new BlobContainerPermissions{ PublicAccess = BlobContainerPublicAccessType.Blob });

            string imagesId = ObjectId.GenerateNewId().ToString();
            string imagePartUrl = $"{User.GetSubjectId()}/{imagesId}/";

            List<ImageInfo> imageInfoList = new List<ImageInfo>();
            foreach (var file in files)
            {
                if (file.Length > 10000000 || file.ContentType != "image/jpeg")
                {
                    _logger.LogError("Tried to access items/images endpoint with image too large: " + file.Length);
                    return HttpBadRequest("Imagem inválida");
                }

                var imageInfo = new ImageInfo
                {
                    Name = imagePartUrl + $"image_{files.IndexOf(file)}.jpg"
                };

                // Retrieve reference to a blob named "myblob".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(imageInfo.Name);

                // Create or overwrite the "myblob" blob with contents from a local file.
                try
                {
                    using (var imgStream = ImageUtil.CompressImage(file.OpenReadStream(), 90))
                    {
                        blockBlob.UploadFromStream(imgStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unable to upload image to azure storage", ex);
                    throw;
                }

                imageInfo.Url = blockBlob.Uri.ToString();
                imageInfoList.Add(imageInfo);
            }

            //Thumbnail image processing
            var thumbImageInfo = new ImageInfo
            {
                Name = imagePartUrl + "image_thumb.jpg"
            };

            CloudBlockBlob thumbBlockBlob = container.GetBlockBlobReference(thumbImageInfo.Name);
            using (var thumbnailStream = ImageUtil.CreateThumbnail(files.First().OpenReadStream(), 200))
            {
                thumbBlockBlob.UploadFromStream(thumbnailStream);
            }

            thumbImageInfo.Url = thumbBlockBlob.Uri.ToString();
            imageInfoList.Add(thumbImageInfo);

            return new ObjectResult(imageInfoList);
        }
    }
}
