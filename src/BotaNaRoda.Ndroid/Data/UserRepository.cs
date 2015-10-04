using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Xamarin.Auth;

namespace BotaNaRoda.Ndroid.Data
{
    public class UserRepository
    {
        private readonly Context _context;
        private const string ServiceId = "BotaNaRoda";

        public UserRepository()
        {
            _context = Application.Context;
        }

        public bool IsLoggedIn
        {
            get
            {
                return AccountStore.Create(_context)
                      .FindAccountsForService(ServiceId)
                      .Any(x => !string.IsNullOrWhiteSpace(x.Username));
            }
        }

        public AuthInfo Get()
        {
            AuthInfo user = new AuthInfo();
            var acc = AccountStore.Create(_context).FindAccountsForService(ServiceId).FirstOrDefault();
            if (acc != null)
            {
                string authInfo;
                if (acc.Properties.TryGetValue("authInfo", out authInfo))
                {
                    user = JsonConvert.DeserializeObject<AuthInfo>(authInfo);
                }
            }
            return user;
        }

        public void Save(AuthInfo authInfo)
        {
			DeleteExistingAccounts();

            Account acc = new Account(
                authInfo.Username ?? string.Empty,
                new Dictionary<string, string>
                {
                    {"authInfo", JsonConvert.SerializeObject(authInfo)}
                });

            AccountStore.Create(_context).Save(acc, ServiceId);
        }

        public void DeleteExistingAccounts()
        {
            foreach (var acc in AccountStore.Create(_context).FindAccountsForService(ServiceId))
            {
                AccountStore.Create(_context).Delete(acc, ServiceId);
            }
        }
    }
}