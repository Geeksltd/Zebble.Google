﻿namespace Zebble
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Data.Json;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage;
    using Windows.Storage.Streams;

    public partial class Google
    {
        const string AUTH_END_POINT = "https://accounts.google.com/o/oauth2/v2/auth";
        const string TOKEN_END_POINT = "https://www.googleapis.com/oauth2/v4/token";
        const string USER_INFO_END_POINT = "https://www.googleapis.com/oauth2/v3/userinfo";

        static string ClientId, RedirectURI = "pw.oauth2:/oauth2redirect";

        public static async Task SignIn(string clientId, string applicationBundle = null)
        {
            try
            {
                if (applicationBundle != null)
                    RedirectURI = RedirectURI.Replace("pw.oauth2", applicationBundle);

                ClientId = clientId;

                string state = RandomDataBase64Url(32);
                string codeVerifier = RandomDataBase64Url(32);
                string codeChallenge = Base64UrlEncodeNoPadding(Sha256(codeVerifier));
                const string codeChallengeMethod = "S256";

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["state"] = state;
                localSettings.Values["code_verifier"] = codeVerifier;

                var authorizationRequest =
                    string.Format("{0}?response_type=code&scope=openid%20profile&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                    AUTH_END_POINT,
                    Uri.EscapeDataString(RedirectURI),
                    clientId,
                    state,
                    codeChallenge,
                    codeChallengeMethod);

                await Nav.ShowPopUp<GoogleUI>(new { Url = authorizationRequest });
            }
            catch (Exception ex)
            {
                Device.Log.Error(ex.Message);
            }
        }

        internal static string RandomDataBase64Url(uint length)
        {
            var buffer = CryptographicBuffer.GenerateRandom(length);
            return Base64UrlEncodeNoPadding(buffer);
        }

        internal static IBuffer Sha256(string inputStirng)
        {
            var sha = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            var buff = CryptographicBuffer.ConvertStringToBinary(inputStirng, BinaryStringEncoding.Utf8);
            return sha.HashData(buff);
        }

        internal static string Base64UrlEncodeNoPadding(IBuffer buffer)
        {
            string base64 = CryptographicBuffer.EncodeToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        internal class GoogleUI : Page
        {
            public override async Task OnInitializing()
            {
                await base.OnInitializing();

                this.Width(Root.ActualWidth - 50).Height(Root.ActualHeight - 70).Margin(top: 20).Background(color: Colors.White);

                var header = new Stack { Direction = RepeatDirection.Horizontal };

                var title = new TextView { Text = "GoogleSignIn" };
                title.Padding(all: 5).Border(bottom: 1, color: Colors.Silver).Width(100.Percent());

                var closeBtn = new Button { Text = "x" };
                closeBtn.TextColor(Colors.Gray).Width(20).Height(20);
                closeBtn.Tapped.Handle(() => Nav.HidePopUp());

                await header.Add(title);
                await header.Add(closeBtn);

                var authorizationRequest = Nav.Param<string>("Url");
                var browser = new WebView(authorizationRequest);
                browser.Width(100.Percent()).Height(100.Percent()).Margin(top: title.ActualHeight);
                browser.BrowserNavigating.Handle(args => OnNavigating(args));

                header.Height.BindTo(title.Height);

                await Add(header);
                await Add(browser);
            }

            public async Task OnNavigating(WebView.NavigatingEventArgs args)
            {
                if (args.Url != null && args.Url != "")
                {
                    // Gets URI from navigation parameters.
                    var authorizationResponse = new Uri(args.Url);
                    string queryString = authorizationResponse.Query;

                    var queryStringParams = queryString.Substring(1).Split('&')
                        .ToDictionary(c => c.Split('=')[0], c => Uri.UnescapeDataString(c.Split('=')[1]));

                    if (queryStringParams.ContainsKey("error"))
                    {
                        await Nav.HidePopUp();
                        return;
                    }

                    if (!queryStringParams.ContainsKey("code") || !queryStringParams.ContainsKey("state"))
                        return;

                    string code = queryStringParams["code"];
                    string incomingState = queryStringParams["state"];

                    var localSettings = ApplicationData.Current.LocalSettings;
                    var expectedState = (string)localSettings.Values["state"];

                    if (incomingState != expectedState)
                        return;

                    localSettings.Values["state"] = null;

                    string codeVerifier = (string)localSettings.Values["code_verifier"];
                    await PerformCodeExchangeAsync(code, codeVerifier);
                }
                else
                {
                    Device.Log.Message(args.Url);

                    await Nav.HidePopUp();
                    await Task.CompletedTask;
                }
            }

            async Task PerformCodeExchangeAsync(string code, string codeVerifier)
            {
                // Builds the Token request
                string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&scope=&grant_type=authorization_code",
                    code,
                    Uri.EscapeDataString(RedirectURI),
                    ClientId,
                    codeVerifier
                    );

                var content = new StringContent(tokenRequestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
                var handler = new HttpClientHandler { AllowAutoRedirect = true };
                var client = new HttpClient(handler);
                var response = await client.PostAsync(TOKEN_END_POINT, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return;

                var tokens = JsonObject.Parse(responseString);
                string accessToken = tokens.GetNamedString("access_token");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userinfoResponse = await client.GetAsync(USER_INFO_END_POINT);
                string userinfoResponseContent = await userinfoResponse.Content.ReadAsStringAsync();

                await Nav.HidePopUp();
                await UserSignedIn.Raise(userinfoResponse);
            }
        }
    }
}