using Android.App;
using Android.Content;
using Android.Gms.Gcm.Iid;

namespace BotaNaRoda.Ndroid
{
	// My listener for registration token refreshes:
	[Service(Exported = false), IntentFilter(new[] { "com.google.android.gms.iid.InstanceID" })]
	public class MyInstanceIDListenerService : InstanceIDListenerService
	{
		// When a token refresh happens, start my RegistrationIntentService:
		public override void OnTokenRefresh()
		{
			var intent = new Intent(this, typeof(RegistrationIntentService));
			StartService(intent);
		}
	}
}