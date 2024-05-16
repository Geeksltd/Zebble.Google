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
        static global::Google.SignIn.ISignInDelegate Delegate = new GoogleSignInDelegate();

        static Google()
        {
            GSignIn.SharedInstance.Scopes = [
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            ];

            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, string, NSDictionary> args) =>
            {
                if (args is null) return;
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
            GSignIn.SharedInstance.Delegate = Delegate;
        }

        public static Task SignIn() => Thread.UI.Run(() =>
        {
            if (UIRuntime.NativeRootScreen is UIViewController controller)
            {
                GSignIn.SharedInstance.PresentingViewController = controller;
                GSignIn.SharedInstance.SignInUser();
            }
        });

        class GoogleSignInDelegate : NSObject, global::Google.SignIn.ISignInDelegate
        {
            public void DidSignIn(GSignIn signIn, global::Google.SignIn.GoogleUser user, NSError error)
            {
                if (error != null)
                {
                    Log.For(typeof(Google)).Error($"Sign in failed. {error.LocalizedDescription} ({error.Code})");
                    return;
                }

                GSignIn.SharedInstance.CurrentUser.Authentication.GetTokens((auth, error) =>
                {
                    if (error != null)
                    {
                        Log.For(typeof(Google)).Error($"GetTokens failed. {error.LocalizedDescription} ({error.Code})");
                        return;
                    }

                    UserSignedIn.Raise(new User
                    {
                        Id = user.UserId,
                        Email = user.Profile.Email,
                        Name = user.Profile.Name,
                        GivenName = user.Profile.GivenName,
                        FamilyName = user.Profile.FamilyName,
                        Picture = user.Profile.HasImage ? user.Profile.GetImageUrl(512)?.AbsoluteString : null,
                        Token = auth.IdToken,
                    });
                });
            }
        }
    }
}