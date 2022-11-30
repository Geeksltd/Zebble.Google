namespace Zebble
{
    using Android.Gms.Auth.Api.SignIn;
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

                if (await TryToUseSignInAccount(signInAccount)) return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to log in silently.");
            }

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

            try
            {
                var signInAccount = await GoogleSignIn.GetSignedInAccountFromIntentAsync(args.Item3);

                await TryToUseSignInAccount(signInAccount);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to log in.");
            }
        }

        static void InitiateSignIn()
        {
            try
            {
                var signInIntent = Client.SignInIntent;

                UIRuntime.CurrentActivity.StartActivityForResult(signInIntent, SIGNIN_REQUEST_CODE);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to init sign in.");
            }
        }

        static async Task<bool> TryToUseSignInAccount(GoogleSignInAccount account)
        {
            if (account == null)
            {
                Logger.Warning("The passed in account is null.");
                return false;
            }

            try
            {
                await UserSignedIn.Raise(new User
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to raise the event.");
                return false;
            }
        }
    }
}