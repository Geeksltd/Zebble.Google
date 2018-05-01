namespace Zebble
{
    using Foundation;
    using System;
    using System.Threading.Tasks;
    using UIKit;

    public static partial class Google
    {
        static string ClientId;

        static Google()
        {
            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, NSDictionary> args) =>
            {
                var openUrlOptions = new UIApplicationOpenUrlOptions(args.Item3);
                global::Google.SignIn.SignIn.SharedInstance.HandleUrl(args.Item2, openUrlOptions.SourceApplication, openUrlOptions.Annotation);
            });
        }

        public static void Initilize(string clientId)
        {
            ClientId = clientId;
        }

        public static async Task SignIn()
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                Device.Log.Error("Please set the ClientId by calling Initilize method first!");
                return;
            }

            var googleSignIn = new GoogleSignIn();
            googleSignIn.DidUserSigneIn.Handle(userInfo => UserSignedIn.Raise(userInfo));

            global::Google.SignIn.SignIn.SharedInstance.ClientID = ClientId;
            global::Google.SignIn.SignIn.SharedInstance.UIDelegate = googleSignIn;
            global::Google.SignIn.SignIn.SharedInstance.Delegate = googleSignIn;
            global::Google.SignIn.SignIn.SharedInstance.SignInUser();

            await Task.CompletedTask;
        }
    }

    internal class GoogleSignIn : UIViewController, global::Google.SignIn.ISignInDelegate, global::Google.SignIn.ISignInUIDelegate
    {
        public readonly AsyncEvent<object> DidUserSigneIn = new AsyncEvent<object>();

        public void DidSignIn(global::Google.SignIn.SignIn signIn, global::Google.SignIn.GoogleUser user, NSError error)
        {
            if (user != null && error == null)
                DidUserSigneIn.Raise(user);
            else
                Device.Log.Error(error);
        }

        public void Dispose() { }
    }
}