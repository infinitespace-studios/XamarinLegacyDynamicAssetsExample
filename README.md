# XamarinLegacyDynamicAssetsExample
An Example of using Dynamic Asset Delivery with Xamarin Android.

## Project Layout

The solution is made up of 4 projects.

1. OnDemand/OnDemandExample. This is the main Xamarin.Android application for the
   OnDemand Example.
2. OnDemand/AssetsFeature. This is the project for the Asset only dynamic feature.
3. InstallTime/InstallTimeExample. This is the main Xamarin.Android application for the
   InstallTime Example.
4. InstallTime/AssetsFeature. This is the project for the Asset only dynamic feature.

There is a `global.json` file which pulls in the `Microsoft.Build.NoTargets` SDK
which we use for the "Feature" projects. This is so they do not produce an assembly. 

## How it works.

The way the build works is we have a custom set of targets in the `Targets\DynamicFeature.targets`
file. The two targets are `BuildAssetFeature` and `PackageAssets`. The first target
is build as part of the main Xamarin.Android application. It is responsible for 
finding "Feature" projects and then calling the `PackageAssets` target on each of 
them. 

It does this by looking for `ProjectReferences` which have the `DynamicFeature` metadata
set to `true`.

```
    <ProjectReference Include="..\AssetsFeature\AssetsFeature.csproj">
      <Project>{EABACE4D-E999-48FA-B417-ECD29C8AB6E5}</Project>
      <Name>AssetsFeature</Name>
      <!-- These next two items are REALLY IMPORTANT!!!! -->
      <DynamicFeature>true</DynamicFeature>
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
```

NOTE: Feature references should ALSO have the `ReferenceOutputAssembly` set to false. This 
stops the Xamarin.Android packaging system from including it in the base package.

The `PackageAssets` target will run for each "feature" project. It is responsible for 
using `aapt2` to package up the files in the `Assets` folder (including subdirectories)
and generating a "Feature" package/zip file. 

The outputs of `PackageAssets` are then passed back to `BuildAssetFeature` which then includes
those zip files in the `@(AndroidAppBundleModules)` ItemGroup. These will then be included
in the final `aab` file as dynamic features. 

## Defining an Asset Pack

To create a feature you need a few things. The first is a `Microsoft.Build.NoTargets` project
which imports the `Targets\DynamicFeatures.targets` file. See `OnDemand\AssetFeature\AssetFeature.csproj` or `InstallTime\AssetFeature\AssetFeature.csproj`  for an example.

Next you need an `AndroidManifest.xml` file. This is where you define how they "Feature" will be
installed via the `dist:module` and `dist:delivery` elements. 

IMPORTANT: The `dist:type` MUST be set to `asset-pack`!

The `package` attriute on the `manifest` element MUST match the value of the main application.
And finally the `split` value is the name which the "Feature" will be called in the final package
and when you install via the `AssetPackManager` API. This is not user facing. 

## Installing and Install Time Asset Pack

Install time asset packs as installed along side your app during the installation 
process. There is no additional work needed to download them.

Accessing these assets can be done via the normal `Assets` property on your main 
Activity. 

```
var stream = Assets.Open ("Foo.txt");
```

## Installing an On Demand Asset Pack

You need to use the `AssetPackManager` to install on-demand asset packs, this is available in the 
`Xamarin.Google.Android.Play.Core"` NuGet Package. However due to the API using Java Generics you cannot 
use all the `AssetPackManager` directly. The `Xamarin.Google.Android.Play.Core` version `1.10.2.3` contains
a non generic Java Wrapper around the `AssetPackManager` which allows us to 
capture events from the `OnStatusUpdate` method. This lets us get feedback while we are installing
"Features".

As a result the code is slightly different from the Java code we see in the Google examples.

The first thing we need is a custom `Application` class which derives from `SplitCompatApplication`.
This is so `Google.Play.Core` can hook into the application lifecycle. 

Next we need to define the following in the `Activity` where we want to surface installing features.

```csharp
IAssetPackManager manager;
AssetPackStateUpdateListenerWrapper listener;
```

The `IAssetPackManager` is the C# version of the `AssetPackManager` Java interface. The
`AssetPackStateUpdateListenerWrapper` class is the C# version of our `AssetPackStateUpdateListenerWrapper`
which comes from the `Xamarin.Google.Android.Play.Core`. This is the class which wraps the Java `AssetPackStateUpdateListener` from the generic based google API and exposes a non generic API.

We now need to create both of these classes in the `OnCreate` method. This can probably be done elsewhere,
but for the example we do this in `OnCreate`.

```csharp
    manager = AssetPackManagerFactory.GetInstance (this);
    // Create our Wrapper and set up the event handler.
    listener = new AssetPackStateUpdateListenerWrapper();
    listener.StateUpdate += Listener_StateUpdate;
```

The code that creates the `AssetPackStateUpdateListenerWrapper` is straight forward. It creates
the class and then hooks up the `StateUpdate` event. 

For the `StateUpdate` event to work we need to hook the `AssetPackStateUpdateListenerWrapper` to 
the `AssetPackManager`. This is done in the `OnResume` override method 

```csharp
protected override void OnResume()
{
    // regsiter our Listener Wrapper with the AssetPackManager so we get feedback.
    manager.RegisterListener(listener.Listener);
    base.OnResume();
}
```

and we need to `UnregisterListener` on `OnPause`.

```csharp
protected override void OnPause()
{
    manager.UnregisterListener(listener.Listener);
    base.OnPause();
}
```

These two snippets are where things differ from the Java examples you will see. Those will 
create an `AssetPackStateUpdateListener` class directly and pass that into the `RegisterListener`
and `UnregisterListener`. But because we had to use our wrapper we need to pass in the 
`listener.Listener` value. This is the only real different between C# and Java code. 

Finally you need to hook into then `OnActivityResult` and handle the case where user
confirmation is required. See the `MainActivty.cs` for an example.

With all that in place you can use the following code to install an asset pack.

```csharp
var location = assetPackManager.GetPackLocation ("assetsfeature");
if (location == null)
{
    assetPackManager.Fetch(new string[] { "assetsfeature" });
}
```



