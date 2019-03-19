namespace Zebble
{
    using Android.Gms.Auth.Api;
    using Android.Gms.Auth.Api.SignIn;
    using Android.Gms.Common.Apis;
    using System.Threading.Tasks;

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
                    var result = Auth.GoogleSignInApi.GetSignInResultFromIntent(args.Item3);
                    if (result.IsSuccess)
                    {
                        var account = result.SignInAccount;
                        UserSignedIn.Raise(new Google.User
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
            if(serverClientIdStr == 0)
            {
                Device.Log.Error("Google Client ID is not set on Android application. Please add server_client_id to the resource string file.");
                return;
            }

            var clientId = context.GetString(serverClientIdStr);
            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .RequestProfile()
                .RequestIdToken(serverClientId: clientId)
                .Build();

            ApiClient = new GoogleApiClient.Builder(UIRuntime.CurrentActivity)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                .AddConnectionCallbacks(connectedCallback: bundle =>
                 {
                     if (bundle != null) Device.Log.Message("Google connected");
                     else Device.Log.Error("Google connection filed");
                 }).Build();
        }

        public static Task SignIn()
        {
            var signInIntent = Auth.GoogleSignInApi.GetSignInIntent(ApiClient);
            UIRuntime.CurrentActivity.StartActivityForResult(signInIntent, SIGNIN_REQUEST_CODE);

            return Task.CompletedTask;
        }
    }
}