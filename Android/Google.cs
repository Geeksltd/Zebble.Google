namespace Zebble
{
    using Android.Gms.Auth.Api.SignIn;
    using Android.Gms.Common.Apis;
    using System.Threading.Tasks;
    using Olive;
    using System;
    using Microsoft.Extensions.Logging;
    using Android.App;
    using Android.Content;

    public static partial class Google
    {
        const int SIGNIN_REQUEST_CODE = 9001;
        static GoogleSignInClient Client;

        static ILogger Logger => Log.For(typeof(Google));

        public static void Initialize()
        {
            UIRuntime.OnActivityResult.Handle(HandleActivityResult);
            Client = CreateClient(UIRuntime.CurrentActivity);
        }

        public static async Task SignIn()
        {
            try
            {
                var signInAccount = await Client.SilentSignInAsync();

                if (TryToUseSignInAccount(signInAccount)) return;
            }
            catch (Exception ex) { Logger.Error(ex, "Faild to log in silently."); }

            InitiateSignIn();
        }

        static GoogleSignInClient CreateClient(Activity activity)
        {
            var serverClientIdStr = activity.Resources.GetIdentifier("server_client_id", "string", activity.PackageName);
            if (serverClientIdStr == 0)
                throw new ArgumentException("Google Client ID is not set on Android application. Please add server_client_id to the resource string file.");

            var clientId = activity.GetString(serverClientIdStr);
            var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .RequestProfile()
                .RequestIdToken(clientId)
                .Build();

            return GoogleSignIn.GetClient(activity, options);
        }

        static async Task HandleActivityResult(Tuple<int, Result, Intent> args)
        {
            if (args.Item1 != SIGNIN_REQUEST_CODE) return;

            var signInAccount = await GoogleSignIn.GetSignedInAccountFromIntentAsync(args.Item3);
            TryToUseSignInAccount(signInAccount);
        }

        static void InitiateSignIn()
        {
            var signInIntent = Client.SignInIntent;
            UIRuntime.CurrentActivity.StartActivityForResult(signInIntent, SIGNIN_REQUEST_CODE);
        }

        static bool TryToUseSignInAccount(GoogleSignInAccount account)
        {
            if (account == null) return false;

            UserSignedIn.Raise(new User
            {
                FamilyName = account.FamilyName,
                GivenName = account.GivenName,
                Id = account.Id,
                Name = account.DisplayName,
                Picture = account.PhotoUrl?.ToString(),
                Email = account.Email,
                Token = account.IdToken
            });

            return true;
        }
    }
}