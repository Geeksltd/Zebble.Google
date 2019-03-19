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

```csharp
public override async Task OnInitializing()
{
    await base.OnInitializing();

    await Zebble.Google.SignIn();
}
```
Then you can get the user information by handling the `UserSignedIn` event like below:

```csharp
Google.UserSignedIn.Handle(user =>
{
    //user.GivenName
    //...
});
```

The `UserSignedIn` has an argument which is an instance of `Zebble.Google.User` object which contains these properties:
```csharp
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Picture { get; set; }
    public string Email { get; }
    public string Token { get; }
}
```

<br>

### Platform Specific Notes
Some platforms require some setting to make you able to use this plugin.

#### Android
First of all, you need to create a project by refering to https://console.developers.google.com and create your credentials for accessing Google Plus and enableing the Google API and download the "google-services.json" file and paste it to the root of Android project, then, set the Build Action property of it to "GoogleServiceJSON".
Secoundly, add the folowing resource into your **strings.xml** file:
```xml
<string name="server_client_id">PASTE HERE YOU CLIENT-ID<string>
```
Finally, in android MainActivity add this code like below:
```csharp
Zebble.Google.Initialize();
```

So, your **MainActivity** will looks like this:
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

**AppDelegate.cs**

```csharp
protected override async Task Initialize()
{
    ...

    Zebble.Google.Initialize("Your Client ID");

    ...
}
```
Then you can sign in by google account like below:
```csharp
await Zebble.Google.SignIn();
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

In this platform you should create a new credentials with client secret and then call `Zebble.Google.SignIn`:
```csharp
Zebble.Google.Initialize("Your Client ID","Your application protocol name");
```

So, your **Program.cs** will looks like below:
```csharp
public static void Main()
{
    ...

    Zebble.Google.Initilize("Your Client ID","Your application protocol name");

    ...
}
```
Finally, you can call sign in method to sign in with google:
```csharp
await Zebble.Google.SignIn();
```

##### UWP credentials instruction:

1. Visit the [Credentials page of the Developers Console](https://console.developers.google.com/apis/credentials?project=_)
2. Create a new OAuth 2.0 client, select `iOS` (yes, it's a little strange to
   select iOS, but the way the OAuth client works with UWP is similar to iOS, 
   so this is currently the correct client type to create).
3. As your bundle ID, enter your domain name in reverse DNS notation. E.g.
   if your domain was "example.com", use "com.example" as your bundle ID.
   Note that your bundle ID MUST contain a period character `.`, and MUST be
   less than 39 characters long
4. Copy the created client-id and replace the clientID value in this sample
5. Edit the manifest by right-clicking and selecting "View Code" (due to a
   limitation of Visual Studio it wasn't possible to declare a URI scheme
   containing a period in the UI).
6. Find the "Protocol" scheme, and replace it with the bundle id you registered
   in step 3. (e.g. "com.example")
<br>


### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| UserSignedIn            | AsyncEvent<Zebble.Google.User&gt;  | x       | x   | x       |


<br>


### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| Initilize         | void| clientId -> string<br>, applicationBundle -> string |        |    |    x    |
| Initilize         | void| clientId -> string<br> |        |  x  |        |
| Initilize         | void| - |   x     |    |        |
| SignIn     | Task| -| x       | x   | x       |
