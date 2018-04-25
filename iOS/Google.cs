namespace Zebble
{
    using Foundation;
    using System;
    using UIKit;

    public partial class Google : global::Google.SignIn.ISignInDelegate, global::Google.SignIn.ISignInUIDelegate
    {
        static Google()
        {
            UIRuntime.OnOpenUrlWithOptions.Handle((Tuple<UIApplication, NSUrl, NSDictionary> args) =>
            {
                var openUrlOptions = new UIApplicationOpenUrlOptions(args.Item3);
                global::Google.SignIn.SignIn.SharedInstance.HandleUrl(args.Item2, openUrlOptions.SourceApplication, openUrlOptions.Annotation);
            });
        }

        public IntPtr Handle => (UIRuntime.NativeRootScreen as UIViewController).Handle;

        public void SignIn(string clientId)
        {
            global::Google.SignIn.SignIn.SharedInstance.ClientID = clientId;
            global::Google.SignIn.SignIn.SharedInstance.UIDelegate = this;
            global::Google.SignIn.SignIn.SharedInstance.Delegate = this;
            global::Google.SignIn.SignIn.SharedInstance.SignInUser();
        }

        public void DidSignIn(global::Google.SignIn.SignIn signIn, global::Google.SignIn.GoogleUser user, NSError error)
        {
            if (user != null && error == null)
                UserSignedIn.Raise(user);
            else
                Device.Log.Error(error);
        }

        public void Dispose() { }
    }
}