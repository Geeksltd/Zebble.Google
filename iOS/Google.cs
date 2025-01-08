namespace Zebble
{
    using Foundation;
    using System;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;
    using GSignIn = global::Google.SignIn.SignIn;
    using GConfiguration = global::Google.SignIn.Configuration;

    public static partial class Google
    {
        static GConfiguration Configuration;

        static Google()
        {
            var scopes = new string[]
            {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile"
            };

            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, string, NSDictionary> args) =>
            {
                if (args is null) return;
                GSignIn.SharedInstance.HandleUrl(args.Item2);
            });
        }

        public static void Initialize(string clientId)
        {
            if (clientId.IsEmpty())
            {
                Log.For(typeof(Google)).Error("Please set the ClientId by calling Initialize method first!");
                return;
            }

            Configuration = new GConfiguration(clientId);
            GSignIn.SharedInstance.Configuration = Configuration;
        }

        public static Task SignIn() => Thread.UI.Run(() =>
        {
            if (UIRuntime.NativeRootScreen is UIViewController controller)
            {
                GSignIn.SharedInstance.SignInWithPresentingViewController(controller, (result, error) =>
                {
                    if (error != null)
                    {
                        Log.For(typeof(Google)).Error($"Sign in failed. {error.LocalizedDescription} ({error.Code})");
                        return;
                    }

                    var user = result.User;
                    user.RefreshTokensIfNeededWithCompletion((userResult, authError) =>
                    {
                        if (authError != null)
                        {
                            Log.For(typeof(Google)).Error($"GetTokens failed. {authError.LocalizedDescription} ({authError.Code})");
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
                            Token = user.IdToken.TokenString,
                        });
                    });
                });
            }
        });
    }
}