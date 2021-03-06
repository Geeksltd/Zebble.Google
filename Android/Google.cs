﻿namespace Zebble
{
    using Android.Gms.Auth.Api;
    using Android.Gms.Auth.Api.SignIn;
    using Android.Gms.Common.Apis;
    using System.Threading.Tasks;
    using Olive;

    public static partial class Google
    {
        const int SIGNIN_REQUEST_CODE = 9001;
        static GoogleApiClient ApiClient;

        public static void Initialize()
        {
            UIRuntime.OnActivityResult.Handle(args =>
            {
                if (args.Item1 == SIGNIN_REQUEST_CODE && args.Item2 == Android.App.Result.Ok)
                {
                    if(args.Item3 == null)
                    {
                        Log.For(typeof(Google)).Error("[Zebble.Google] => The Google Play Services are not installed on your device, please make sure to installed them");
                        return;
                    }

                    var result = Auth.GoogleSignInApi.GetSignInResultFromIntent(args.Item3);
                    if (result.IsSuccess)
                    {
                        var account = result.SignInAccount;
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
                    }
                }
            });

            var context = UIRuntime.CurrentActivity;
            var serverClientIdStr = context.Resources.GetIdentifier("server_client_id", "string", context.PackageName);
            if (serverClientIdStr == 0)
            {
                Log.For(typeof(Google)).Error("Google Client ID is not set on Android application. Please add server_client_id to the resource string file.");
                return;
            }

            var clientId = context.GetString(serverClientIdStr);
            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .RequestProfile()
                .RequestIdToken(clientId)
                .Build();

            ApiClient = new GoogleApiClient.Builder(UIRuntime.CurrentActivity)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                .AddConnectionCallbacks(connectedCallback: bundle =>
                 {
                     if (bundle != null) Log.For(typeof(Google)).Debug("Google connected");
                     else Log.For(typeof(Google)).Error("Google connection filed");
                 }).Build();

            ApiClient.Connect();
        }

        public static async Task SignIn()
        {
            var signInIntent = Auth.GoogleSignInApi.GetSignInIntent(ApiClient);

            if (ApiClient != null && ApiClient.IsConnected)
                await ApiClient.ClearDefaultAccountAndReconnect();

            UIRuntime.CurrentActivity.StartActivityForResult(signInIntent, SIGNIN_REQUEST_CODE);

            await Task.CompletedTask;
        }
    }
}