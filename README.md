# XamarinLegacyDynamicAssetsExample
An Example of using Dynamic Asset Delivery with Xamarin Android.

## Project Layout

The solution is made up of 3 projects.

1. LegacyDynamicAssetsExample. This is the main Xamarin.Android application.
2. AssetsFeature. This is the project for the Asset only dynamic feature.
3. PlayCoreHelperbinding. This is a binding for a custom wrapper around the 
   SplitInstallManager from Google.Play.Core. It is required due to some 
   incompatibilities in the google API (It uses generics).

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

## Defining a Feature

To create a feature you need a few things. The first is a `Microsoft.Build.NoTargets` project
which imports the `Targets\DynamicFeatures.targets` file. See `AssetFeature\AssetFeature.csproj` for an 
example.

Next you need an `AndroidManifest.xml` file. This is where you define how they "Feature" will be
installed via the `dist:module` and `dist:delivery` elements. 

IMPORTANT: The `dist:title` MUST contain a string resource value which exists in the main application
`strings.xml`. It should NOT reference a resource in the "Feature". This is the value which google will
use to display to the user before the "Feature" is installed. So it has to exist in the main app. 

The `application` element in this `AndroidManifest.xml` must have the `android:hasCode` value set to 
`false`. This is because we only support "Dynamic Asset Delivery" features. These only include `Assets`
and cannot include any `code` or `resources`. 

The `package` attriute on the `manifest` element MUST match the value of the main application.
And finally the `featureSplit` value is the name which the "Feature" will be called in the final package
and when you install via the `SplitInstallManager` API. This is not user facing. 

## Installing a Feature

You need to use the `SplitInstallManager` to install features, this is available in the 
`Xamarin.Google.Android.Play.Core"` NuGet Package. However due to the API using Java Generics you cannot 
use all the `SplitInstallManager` features directly. This is why we have a `PlayCoreHelperBinding` project.
This project contains a non generic Java Wrapper around the `SplitInstallManager` which allows us to 
capture events from the `OnStatusUpdate` method. This lets us get feedback while we are installing
"Features".

As a result the code is slightly different from the Java code we see in the Google examples.

The first thing we need is a custom `Application` class which derives from `SplitCompatApplication`.
This is so `Google.Play.Core` can hook into the application lifecycle. 

Next we need to define the following in the `Activity` where we want to surface installing features.

```csharp
ISplitInstallManager manager;
SplitInstallStateUpdatedListenerWrapper listener;
```

The `ISplitInstallManager` is the C# version of the `SplitInstallManager` Java interface. The
`SplitInstallStateUpdatedListenerWrapper` class is the C# version of our `SplitInstallStateUpdatedListenerWrapper`
which comes from `PlayCoreHelperBinding`. This is the class which wraps the Java `SplitInstallStateUpdatedListener` from  the geneeric based google API and exposes a non generic API.

We now need to create both of these classes in the `OnCreate` method. This can probably be done elsewhere,
but for the example we do this in `OnCreate`.

```csharp
#if DEBUG
    var fakeManager = FakeSplitInstallManagerFactory.Create(this);
    manager = fakeManager;
#else
    manager = SplitInstallManagerFactory.Create (this);
#endif
    // Create our Wrapper and set up the event handler.
    listener = new SplitInstallStateUpdatedListenerWrapper();
    listener.StateUpdate += Listener_StateUpdate;
```

Note we can create two different `SplitInstallManager` instances. One for release the other for debug.
The debug one will look for features in a special location on the device which `bundletool` deploys
them to. This allows you to test installing features without having to publish your `aab` to google. 

The code that creates the `SplitInstallStateUpdatedListenerWrapper` is straight forward. It creates
the class and then hooks up the `StateUpdate` event. 

For the `StateUpdate` event to work we need to hook the `SplitInstallStateUpdatedListenerWrapper` to 
the `SplitInstallManager`. This is done in the `OnResume` override method 

```csharp
protected override void OnResume()
{
    // regsiter our Listener Wrapper with the SplitInstallManager so we get feedback.
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
create an `SplitInstallStateUpdatedListener` class directly and pass that into the `RegisterListener`
and `UnregisterListener`. But because we had to use our wrapper we need to pass in the 
`listener.Listener` value. This is the only real different between C# and Java code. 

Finally you need to hook into then `OnActivityResult` and handle the case where user
confirmation is required. See the `MainActivty.cs` for an example.

With all that in place you can use the following code to install a "Feature"

```csharp
if (!manager.InstalledModules.Contains("assetsfeature"))
{
    var builder = SplitInstallRequest.NewBuilder();
    builder.AddModule("assetsfeature");
    manager.StartInstall(builder.Build());
}
```

This creates a new `SplitInstallRequest` and starts the installation. The value we us for 
the `AddModule` method MUST match the one we defined for the `featureSplit` attribute in the 
"Feature" `AndroidManifest.xml` earlier. The same goes for when we use the `InstalledModules`
to check if something was installed. 


