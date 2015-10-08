using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Resources;
using Android.App;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using BotaNaRoda.Ndroid.Data;
using BotaNaRoda.Ndroid.Models;
using BotaNaRoda.Ndroid.Util;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Square.Picasso;
using System.IO;

namespace BotaNaRoda.Ndroid
{
    [Activity(Label = "Bota na Roda",
		Theme = "@style/MainTheme", ConfigurationChanges = (ConfigChanges.Orientation | ConfigChanges.ScreenSize),
        ParentActivity = typeof (MainActivity))]
    public class ChatActivity : AppCompatActivity
    {
        private UserRepository _userRepository;
        private ViewHolder _holder;
        private ChatMessageAdapter _adapter;
        private BackgroundWorker _refreshWorker;
        private IHubProxy _chatHubProxy;
        private ConversationChatConnectionViewModel _connectionViewModel;
        private string _conversationId;
        private ProgressDialog _loadingDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Chat);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _userRepository = new UserRepository();

            _conversationId = Intent.GetStringExtra("conversationId");
            _holder = new ViewHolder
            {
                ItemImage = FindViewById<ImageView>(Resource.Id.chatItemImage),
                ItemName = FindViewById<TextView>(Resource.Id.chatItemName),
                ItemDistance = FindViewById<TextView>(Resource.Id.chatItemDistance),
                UserImage = FindViewById<ImageView>(Resource.Id.chatUserImage),
                UserName = FindViewById<TextView>(Resource.Id.chatUserName),
				MessageList = FindViewById<ListView>(Resource.Id.chatMessageList),
                ChatMessageText = FindViewById<EditText>(Resource.Id.chatMessageText),
                ChatSendButton = FindViewById<ImageButton>(Resource.Id.chatSendButton),
            };

			_adapter = new ChatMessageAdapter(this, new List<ConversationChatMessage>(), _userRepository.Get());
            _holder.MessageList.Adapter = _adapter;
            _holder.ChatSendButton.Click += ChatSendButtonOnClick;

            _loadingDialog = ProgressDialog.Show(this, "Carregando...", "");
            Refresh();
        }

        private void ChatSendButtonOnClick(object sender, EventArgs eventArgs)
        {
            var msg = _holder.ChatMessageText.Text;
            //...
            _holder.ChatMessageText.Text = "";

            // Invoke the 'UpdateNick' method on the server
            _chatHubProxy.Invoke("SendMessage", new
            {
                conversationId = _conversationId,
                message = msg
            });
        }

        private void Refresh()
        {
            _refreshWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _refreshWorker.DoWork += (sender, args) =>
            {
                var authInfo = _userRepository.Get();
                // Connect to the server
				var hubConnection = new HubConnection(Path.Combine(Constants.BotaNaRodaEndpoint, "signalr"));
				hubConnection.Headers.Add("Authorization", "Bearer " + authInfo.AccessToken);

                // Create a proxy to the 'ChatHub' SignalR Hub
                _chatHubProxy = hubConnection.CreateHubProxy("ChatHub");

                // Wire up a handler for the 'UpdateChatMessage' for the server
                // to be called on our client
                _chatHubProxy.On<ConversationChatMessage>("OnMessageReceived", message =>
                {
						RunOnUiThread(() => MessageReceived(message));
                });

                // Start the connection
				hubConnection.Start().Wait();
                _connectionViewModel = _chatHubProxy.Invoke<ConversationChatConnectionViewModel>("Connect", _conversationId).Result;
                //---
            };
            _refreshWorker.RunWorkerCompleted += (sender, args) =>
            {
                RunOnUiThread(UpdateUi);
            };
            _refreshWorker.RunWorkerAsync();
        }

        private void UpdateUi()
        {
			if (_connectionViewModel == null) {
				Toast.MakeText (this, "Não foi possível carregar as mensagens", ToastLength.Short);
				Finish ();
				return;
			}

            var authInfo = _userRepository.Get();

            _holder.ItemName.Text = _connectionViewModel.Item.Name;
            _holder.ItemDistance.Text = _connectionViewModel.Item.DistanceTo(authInfo);
            _holder.UserName.Text = _connectionViewModel.ToUser.Name;

            Picasso.With(this).Load(_connectionViewModel.Item.ThumbImage.Url).Fit().Tag(this).Into(_holder.ItemImage);
            Picasso.With(this).Load(_connectionViewModel.ToUser.Avatar).Fit().Tag(this).Into(_holder.UserImage);

            _adapter.ChatMessages = _connectionViewModel.Messages;
            _adapter.NotifyDataSetChanged();

            _loadingDialog.Hide();
        }

		private void MessageReceived(ConversationChatMessage message){
			_adapter.ChatMessages.Add(message);
			_adapter.NotifyDataSetChanged();
		}

        protected override void OnDestroy()
        {
			if (_refreshWorker != null) {
				_refreshWorker.CancelAsync ();
			}
            base.OnDestroy();
        }

        private class ViewHolder
        {
            internal ImageView ItemImage;
            internal TextView ItemName;
            internal TextView ItemDistance;
            internal ImageView UserImage;
            internal TextView UserName;
            internal ListView MessageList;
            internal EditText ChatMessageText;
            internal ImageButton ChatSendButton;
        }
    }
}