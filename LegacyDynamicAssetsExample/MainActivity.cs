using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Xamarin.Google.Android.Play.Core.SplitInstall;
using Xamarin.Google.Android.Play.Core.SplitInstall.Testing;
using Xamarin.Google.Android.Play.Core.SplitInstall.Model;
using Android.Content;

namespace LegacyDynamicAssetsExample
{
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
	public class MainActivity : AppCompatActivity
	{
		// Dynamic Feature Section
		//
		//
		const int REQUEST_USER_CONFIRM_INSTALL_CODE = 101;

		// This is the underlying SplitInstallManager
		ISplitInstallManager manager;
		// This is the wrapper SplitInstallListener which allows us
		// to provide an event to get updates from the ISplitInstallManager.
		SplitInstallStateUpdatedListenerWrapper listener;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);

			FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
			fab.Click += FabOnClick;

			// Create a Fake SplitInstallManager when debugging.
#if DEBUG
			var fakeManager = FakeSplitInstallManagerFactory.Create(this);
			//fakeManager.SetShouldNetworkError (true); // uncomment to test network errors
			manager = fakeManager;
#else
            manager = SplitInstallManagerFactory.Create (this);
#endif
			// Create our Wrapper and set up the event handler.
			listener = new SplitInstallStateUpdatedListenerWrapper();
			listener.StateUpdate += Listener_StateUpdate;
		}

		private void Listener_StateUpdate(object sender, SplitInstallStateUpdatedListenerWrapper.StateUpdateEventArgs e)
		{
			var status = e.State.Status();
			switch (status)
			{
				case SplitInstallSessionStatus.Downloading:
					break;
				case SplitInstallSessionStatus.Downloaded:
					break;
				case SplitInstallSessionStatus.Installing:
					break;
				case SplitInstallSessionStatus.Installed:
					break;
				case SplitInstallSessionStatus.RequiresUserConfirmation:
					// user needs to confirm
					manager.StartConfirmationDialogForResult(e.State, this, REQUEST_USER_CONFIRM_INSTALL_CODE);
					break;
			}
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.menu_main, menu);
			return true;
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			int id = item.ItemId;
			if (id == Resource.Id.action_settings)
			{
				return true;
			}

			return base.OnOptionsItemSelected(item);
		}

		private void FabOnClick(object sender, EventArgs eventArgs)
		{
			// Try to install the new feature.
			if (!manager.InstalledModules.Contains("assetsfeature"))
			{
				var builder = SplitInstallRequest.NewBuilder();
				builder.AddModule("assetsfeature");
				manager.StartInstall(builder.Build());
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		protected override void OnResume()
		{
			// regsiter our Listener Wrapper with the SplitInstallManager so we get feedback.
			manager.RegisterListener(listener.Listener);
			base.OnResume();
		}

		protected override void OnPause()
		{
			manager.UnregisterListener(listener.Listener);
			base.OnPause();
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			// Handle the case where the user needs to confirm.
			if (requestCode == REQUEST_USER_CONFIRM_INSTALL_CODE)
			{
				if (resultCode == Result.Canceled)
					Android.Util.Log.Debug("FEATURE", "User Cancelled.");
				else
					Android.Util.Log.Debug("FEATURE", "User Accepted.");
			}
			base.OnActivityResult(requestCode, resultCode, data);
		}
	}
}
