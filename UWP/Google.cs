namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Core;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;

    public partial class Google
    {
        const string AUTH_URL = "https://accounts.google.com/o/oauth2/auth";
        const string ACCESS_TOKEN = "https://accounts.google.com/o/oauth2/token";

        public static Task SignIn(string clientId, string clientSecret)
        {
            return Thread.UI.Run(async () =>
            {
                var auth = new Xamarin.Auth.OAuth2Authenticator(clientId, clientSecret, "openid email profile", new Uri(AUTH_URL),
                    new Uri("urn:ietf:wg:oauth:2.0:oob:auto"), new Uri(ACCESS_TOKEN), null, true)
                { AllowCancel = true };

                auth.Completed += (sender, ev) =>
                {
                    if (ev.IsAuthenticated)
                        UserSignedIn.Raise(ev.Account);
                };

                var ui = auth.GetUI();
                var newView = CoreApplication.CreateNewView();
                int newViewId = 0;
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var frame = new Windows.UI.Xaml.Controls.Frame();
                    frame.Navigate(ui, auth);
                    Windows.UI.Xaml.Window.Current.Content = frame;
                    // You have to activate the window in order to show it later.
                    Windows.UI.Xaml.Window.Current.Activate();

                    newViewId = ApplicationView.GetForCurrentView().Id;
                });
                bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
            });
        }
    }
}