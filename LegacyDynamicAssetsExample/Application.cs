using System;
using Android.App;
using Android.Runtime;
using Xamarin.Google.Android.Play.Core.SplitCompat;

namespace LegacyDynamicAssetsExample
{
	[Application]
	public class DynamicApplication : SplitCompatApplication
	{
		public DynamicApplication(IntPtr handle, JniHandleOwnership jniHandle)
			: base(handle, jniHandle)
		{
		}

		public override void OnCreate()
		{
			base.OnCreate();
		}
	}
}
