﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using BotaNaRoda.WebApi.Models;
using BotaNaRoda.WebApi.Util;
using IdentityServer3.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace BotaNaRoda.WebApi.Entity
{
    [BsonIgnoreExtraElements]
    public class User : ILocalization, IUpdatable
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public string Name { get; set; }
        public string Avatar { get; set; }

        public GeoJson2DGeographicCoordinates Loc { get; set; }
        public string Address { get; set; }
        public string Locality { get; set; }
        public string CountryCode { get; set; }
        public string PostalCode { get; set; }

        public int Credits { get; set; }

        [BsonIgnoreIfDefault]
        public ICollection<UserReview> Reviews { get; set; }

        public string Provider { get; set; }
        public string ProviderId { get; set; }

        public string PushDeviceRegistrationId { get; set; }

        public User()
        {
            Id = ObjectId.GenerateNewId().ToString();
            CreatedAt = DateProvider.Get;
            Reviews = new HashSet<UserReview>();
        }

        public User(RegisterUserBindingModel model)
            :this()
        {
            Username = model.Username;
            Avatar = model.Avatar;
            Loc = GeoJson.Geographic(model.Longitude, model.Latitude);
            Address = model.Address;
            Locality = model.Locality;
            CountryCode = model.CountryCode;
            PostalCode = model.PostalCode;
        }

        public List<Claim> GetClaims()
        {
            return new List<Claim>
                {
                    new Claim(Constants.ClaimTypes.Name, Name),
                    new Claim(Constants.ClaimTypes.PreferredUserName, Username),
                    new Claim(Constants.ClaimTypes.Email, Email),
                    new Claim(Constants.ClaimTypes.Picture, Avatar ?? ""),
                    new Claim(Constants.ClaimTypes.Address, Address ?? ""),
                };
        } 
    }
}
