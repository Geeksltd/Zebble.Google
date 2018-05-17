namespace Zebble
{
    using Foundation;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using UIKit;

    public static partial class Google
    {
        const string AUTH_END_POINT = "https://accounts.google.com/o/oauth2/v2/auth";
        const string TOKEN_END_POINT = "https://www.googleapis.com/oauth2/v4/token";
        const string USER_INFO_END_POINT = "https://www.googleapis.com/oauth2/v3/userinfo";

        internal static Xamarin.Auth.OAuth2Authenticator Auth;
        internal static UIViewController UI;

        static Google()
        {
            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, NSDictionary> args) =>
            {
                Auth.OnPageLoading(new Uri(args.Item2.AbsoluteString));
                UI.DismissViewController(true, null);
            });
        }

        public static void Initilize(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                Device.Log.Error("Please set the ClientId by calling Initilize method first!");
                return;
            }

            Auth = new Xamarin.Auth.OAuth2Authenticator(clientId, "", "openid profile", new Uri(AUTH_END_POINT),
            new Uri($"com.googleusercontent.apps.{clientId.Replace(".apps.googleusercontent.com", "")}:/oauth2redirect"), new Uri(TOKEN_END_POINT), null, true)
            { AllowCancel = true };

            Auth.Completed += async (s, args) =>
            {
                if (args.IsAuthenticated)
                {
                    var request = new Xamarin.Auth.OAuth2Request("GET", new Uri(USER_INFO_END_POINT), null, args.Account);
                    var response = await request.GetResponseAsync();
                    var accountStr = await response.GetResponseTextAsync();
                    var account = JsonConvert.DeserializeObject<JObject>(accountStr);
                    await UserSignedIn.Raise(new Google.User
                    {
                        FamilyName = account["family_name"].Value<string>(),
                        GivenName = account["given_name"].Value<string>(),
                        Name = account["name"].Value<string>(),
                        Id = account["sub"].Value<string>(),
                        Picture = account["picture"].Value<string>(),
                        Token = args.Account.Properties["id_token"] ?? ""
                    });
                }
            };
        }

        public static Task SignIn()
        {
            return Thread.UI.Run(() =>
            {
                if (UIRuntime.NativeRootScreen is UIViewController controller)
                {
                    UI = Auth.GetUI();
                    controller.PresentViewController(UI, true, null);
                }
            });
        }
    }
}