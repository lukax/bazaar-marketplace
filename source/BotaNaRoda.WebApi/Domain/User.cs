﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BotaNaRoda.WebApi.Domain
{
    public class User : ILocalization
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public string Avatar { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}
