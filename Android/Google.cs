namespace Zebble
{
    using Android.Gms.Auth.Api;
    using Android.Gms.Auth.Api.SignIn;
    using Android.Gms.Common.Apis;

    public partial class Google
    {
        const int SIGNIN_REQUEST_CODE = 9001;
        static GoogleApiClient ApiClient;

        static Google()
        {
            UIRuntime.OnActivityResult.Handle(args =>
            {
                if(args.Item1 == SIGNIN_REQUEST_CODE && args.Item2 == Android.App.Result.Ok)
                {
                    UserSignedIn.Raise(args.Item3);
                }
            });
        }

        public static void Initilize()
        {
            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn).RequestProfile().Build();
            ApiClient = new GoogleApiClient.Builder(UIRuntime.CurrentActivity).AddApi(Auth.GOOGLE_SIGN_IN_API, gso).Build();
        }

        public static void SignIn()
        {
            var signInIntent = Auth.GoogleSignInApi.GetSignInIntent(ApiClient);
            UIRuntime.CurrentActivity.StartActivityForResult(signInIntent, SIGNIN_REQUEST_CODE);
        }
    }
}