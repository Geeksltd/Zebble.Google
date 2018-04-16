[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Google/master/Shared/NuGet/Icon.png "Zebble.Google"


## Zebble.Google

![logo]

A Zebble plugin for signing with Google.


[![NuGet](https://img.shields.io/nuget/v/Zebble.Google.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Google/)

> With this plugin you can get information from the user of Google like gmail, google plus, etc in Zebble application and it is available on all platforms.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Google/](https://www.nuget.org/packages/Zebble.Google/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage
Call `Zebble.Google.SignIn` from any project to gain access to APIs.
<br>

### Platform Specific Notes
Some platforms require some setting to make you able to use this plugin.

#### Android
First of all, you need to create a project by refering to https://console.developers.google.com and create your credentials for accessing Google Plus and enableing the Google API and download the "google-services.json" file and paste it to the root of Android project, then, set the Build Action property of it to "GoogleServiceJSON".
Finally, in android MainActivity add this code like below:
```csharp
Zebble.Google.Initialize();
```

So, your MainActivity will looks like this:
```csharp
protected override async void OnCreate(Bundle bundle)
{
    base.OnCreate(bundle);
    SetContentView(Resource.Layout.Main);

    Zebble.Google.Initialize();

    Setup.Start(FindViewById<FrameLayout>(Resource.Id.Main_Layout),this).RunInParallel();
    await (StartUp.Current = new UI.StartUp()).Run();
}
```

Then you can use `Zebble.Google.SignIn()` method whereever you need to show the sign in dialog of google.
 
#### iOS

In IOS platform you need to create a credentials and use your clientId to sign in to the google account and get user information like below:

```csharp
var googleSignIn = new Zebble.Google();
googleSignIn.SignIn("Your Client ID");
```

Also, you need to add some URL types into the `Info.plist` file like below:
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
    	<key>CFBundleURLSchemes</key>
    	<array>
    		<string>com.googleusercontent.apps.Your Client ID</string>
    	</array>
    </dict>
</array>    
```

<br>

#### UWP

In this platform you should create a new credentials with client secret and then call sign in method like below:
```csharp
await Zebble.Google.SignIn("Your Client ID","Your Client Secret");
```

<br>


### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| UserSignedIn            | AsyncEvent<object&gt;  | x       | x   | x       |


<br>


### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| Initilize         | void| -| x       |    |        |
| SignIn     | Task| clientId -> string<br> clientSecret -> string <br>| x       | x   | x       |
