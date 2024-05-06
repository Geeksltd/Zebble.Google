namespace Zebble
{
    using Foundation;
    using System;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;
    using GSignIn = global::Google.SignIn.SignIn;

    public static partial class Google
    {
        static Google()
        {
            GSignIn.SharedInstance.Scopes = [
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            ];

            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, string, NSDictionary> args) =>
            {
                if (args?.Item2 is null) return;
                GSignIn.SharedInstance.HandleUrl(new Uri(args.Item2.AbsoluteString));
            });
        }

        public static void Initialize(string clientId)
        {
            if (clientId.IsEmpty())
            {
                Log.For(typeof(Google)).Error("Please set the ClientId by calling Initialize method first!");
                return;
            }

            GSignIn.SharedInstance.ClientId = clientId;

            GSignIn.SharedInstance.SignedIn += async (s, args) =>
            {
                if (args.Error != null)
                {
                    Log.For(typeof(Google)).Error($"Error - {args.Error.LocalizedDescription} - {args.Error.Code}");
                    return;
                }

                var token = "";
                GSignIn.SharedInstance.CurrentUser.Authentication.GetTokens((auth, error) =>
                {
                    if (error == null) token = auth.IdToken;
                });

                await UserSignedIn.Raise(new User
                {
                    Id = args.User.UserId,
                    Email = args.User.Profile.Email,
                    Name = args.User.Profile.Name,
                    GivenName = args.User.Profile.GivenName,
                    FamilyName = args.User.Profile.FamilyName,
                    Picture = args.User.Profile.HasImage ? args.User.Profile.GetImageUrl(512)?.AbsoluteString : null,
                    Token = token,
                });
            };
        }

        public static Task SignIn() => Thread.UI.Run(() =>
        {
            if (UIRuntime.NativeRootScreen is UIViewController controller)
            {
                GSignIn.SharedInstance.PresentingViewController = controller;
                GSignIn.SharedInstance.SignInUser();
            }
        });
    }
}