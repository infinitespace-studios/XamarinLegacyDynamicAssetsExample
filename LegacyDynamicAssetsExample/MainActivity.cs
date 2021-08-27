using System;
using System.IO;
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
using Xamarin.Google.Android.Play.Core.Assetpacks;
using Xamarin.Google.Android.Play.Core.AssetPacks;
using Xamarin.Google.Android.Play.Core.AssetPacks.Model;

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
		IAssetPackManager assetPackManager;
		// This is the wrapper SplitInstallListener which allows us
		// to provide an event to get updates from the ISplitInstallManager.
		AssetPackStateUpdateListenerWrapper listener;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);

			FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
			fab.Click += FabOnClick;


			assetPackManager = AssetPackManagerFactory.GetInstance (this);
			// Create our Wrapper and set up the event handler.
			listener = new AssetPackStateUpdateListenerWrapper();
			listener.StateUpdate += Listener_StateUpdate;
		}

		private string GetAbsoluteAssetPath(string assetPack, string relativeAssetPath) {
			AssetPackLocation assetPackPath = assetPackManager.GetPackLocation(assetPack);
			string assetsFolderPath = assetPackPath?.AssetsPath() ?? null;
			if (assetsFolderPath == null) {
				// asset pack is not ready
				return null;
			}

			string assetPath = Path.Combine(assetsFolderPath, relativeAssetPath);
			return assetPath;
		}

		private void Listener_StateUpdate(object sender, AssetPackStateUpdateListenerWrapper.AssetPackStateEventArgs e)
		{
			var status = e.State.Status();
			switch (status)
			{
				case AssetPackStatus.Pending:
					break;
				case AssetPackStatus.Downloading:
					long downloaded = e.State.BytesDownloaded();
					long totalSize = e.State.TotalBytesToDownload ();
					double percent = 100.0 * downloaded / totalSize;
					Android.Util.Log.Info ("DynamicAssetsExample", $"Dowloading {percent}");
					break;

				case AssetPackStatus.Transferring:
					// 100% downloaded and assets are being transferred.
					// Notify user to wait until transfer is complete.
					break;

				case AssetPackStatus.Completed:
					// Asset pack is ready to use.
					string path = GetAbsoluteAssetPath ("assetsfeature", "Foo.txt");
					if (path != null) {
						Android.Util.Log.Info ("DynamicAssetsExample", $"Reading {path}");
						Android.Util.Log.Info ("DynamicAssetsExample", File.ReadAllText (path));
					}
					break;

				case AssetPackStatus.Failed:
					// Request failed. Notify user.
					break;

				case AssetPackStatus.Canceled:
					// Request canceled. Notify user.
					break;

				case AssetPackStatus.WaitingForWifi:
					// wait for wifi
					assetPackManager.ShowCellularDataConfirmation (this);
					break;

				case AssetPackStatus.NotInstalled:
				// Asset pack is not downloaded yet.
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
			var location = assetPackManager.GetPackLocation ("assetsfeature");
			if (location?.AssetsPath () == null) {
				var states = assetPackManager.GetPackStates(new string[] { "assetsfeature" });
				assetPackManager.Fetch(new string[] { "assetsfeature" });
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
			assetPackManager.RegisterListener(listener.Listener);
			base.OnResume();
		}

		protected override void OnPause()
		{
			assetPackManager.UnregisterListener(listener.Listener);
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
