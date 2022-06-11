using Olive;

namespace Zebble
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Activation;
    using Windows.Data.Json;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml;

    public partial class Google
    {
        const string AUTH_END_POINT = "https://accounts.google.com/o/oauth2/v2/auth";
        const string TOKEN_END_POINT = "https://www.googleapis.com/oauth2/v4/token";
        const string USER_INFO_END_POINT = "https://www.googleapis.com/oauth2/v3/userinfo";

        static string ClientId, RedirectURI = "zbl.oauth2:/oauth2redirect";

        public static void Initialize(string clientId, string applicationBundle = null)
        {
            ClientId = clientId;

            if (applicationBundle != null)
                RedirectURI = RedirectURI.Replace("zbl.oauth2", applicationBundle);

            UIRuntime.OnActivated.Handle(OnNavigatingTo);
        }

        public static async Task SignIn()
        {
            try
            {
                if (ClientId.IsEmpty())
                {
                    Log.For(typeof(Google)).Error("Please set the ClientId by calling Initialize method first!");
                    return;
                }

                var state = RandomDataBase64Url(32);
                var codeVerifier = RandomDataBase64Url(32);
                var codeChallenge = Base64UrlEncodeNoPadding(Sha256(codeVerifier));
                const string CODE_CHALLENGE_METHOD = "S256";

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["state"] = state;
                localSettings.Values["code_verifier"] = codeVerifier;

                var authorizationRequest =
                    string.Format("{0}?response_type=code&scope=openid%20profile%20email&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                    AUTH_END_POINT,
                    Uri.EscapeDataString(RedirectURI),
                    ClientId,
                    state,
                    codeChallenge,
                    CODE_CHALLENGE_METHOD);

                // Windows Launch URL must be called from the UI thread, otherwise it won't work.
                await Thread.UI.Run(async () =>
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(authorizationRequest));
                });
                
            }
            catch (Exception ex)
            {
                Log.For(typeof(Google)).Error(ex);
            }
        }

        internal static string RandomDataBase64Url(uint length)
        {
            var buffer = CryptographicBuffer.GenerateRandom(length);
            return Base64UrlEncodeNoPadding(buffer);
        }

        internal static IBuffer Sha256(string inputString)
        {
            var sha = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            var buff = CryptographicBuffer.ConvertStringToBinary(inputString, BinaryStringEncoding.Utf8);
            return sha.HashData(buff);
        }

        internal static string Base64UrlEncodeNoPadding(IBuffer buffer)
        {
            var base64 = CryptographicBuffer.EncodeToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        internal static async Task OnNavigatingTo(Tuple<IActivatedEventArgs, Window> args)
        {
            var protocol = args.Item1 as ProtocolActivatedEventArgs;
            if (protocol != null && protocol.Uri != null)
            {
                var authorizationResponse = protocol.Uri;
                var queryString = authorizationResponse.Query;
                if (string.IsNullOrEmpty(queryString))
                    return;
                var queryStringParams = queryString.Substring(1).Split('&')
                    .ToDictionary(c => c.Split('=')[0], c => Uri.UnescapeDataString(c.Split('=')[1]));

                if (queryStringParams.ContainsKey("error"))
                    return;

                if (!queryStringParams.ContainsKey("code") || !queryStringParams.ContainsKey("state"))
                    return;

                var code = queryStringParams["code"];
                var incomingState = queryStringParams["state"];

                var localSettings = ApplicationData.Current.LocalSettings;
                var expectedState = (string)localSettings.Values["state"];

                if (incomingState != expectedState)
                    return;

                localSettings.Values["state"] = null;

                var codeVerifier = (string)localSettings.Values["code_verifier"];

                await PerformCodeExchangeAsync(code, codeVerifier);
            }
            else
            {
                Log.For(typeof(Google)).Debug(protocol.Uri.AbsoluteUri);
                await Task.CompletedTask;
            }
        }

        internal static async Task<string> GetAccessToken(string code, string codeVerifier)
        {
            var content = new StringContent($"code={code}&redirect_uri={Uri.EscapeDataString(RedirectURI)}&client_id={ClientId}&code_verifier={codeVerifier}&grant_type=authorization_code",
                                          Encoding.UTF8, "application/x-www-form-urlencoded");

            using (var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                var response = await httpClient.PostAsync(TOKEN_END_POINT, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.For(typeof(Google)).Error("Authorization code exchange failed.");
                    return null;
                }

                var tokens = JsonObject.Parse(responseString);
                return tokens.GetNamedString("access_token");
            }
        }

        internal static async Task PerformCodeExchangeAsync(string code, string codeVerifier)
        {
            var accessToken = await GetAccessToken(code, codeVerifier);

            using (var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var userInfoResponse = await httpClient.GetAsync(USER_INFO_END_POINT);
                var userInfoResponseContent = await userInfoResponse.Content.ReadAsStringAsync();

                var account = JsonConvert.DeserializeObject<JObject>(userInfoResponseContent);
                var user = new User { Token = accessToken };
                if (!userInfoResponseContent.Contains("error"))
                {
                    user.FamilyName = account["family_name"].Value<string>();
                    user.GivenName = account["given_name"].Value<string>();
                    user.Email = account["email"].Value<string>();
                    user.Name = account["name"].Value<string>();
                    user.Id = account["sub"].Value<string>();
                    user.Picture = account["picture"].Value<string>();
                }
                await UserSignedIn.Raise(user);
            }
        }
    }
}