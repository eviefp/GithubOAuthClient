
namespace GithubOAuthClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using DotNetOpenAuth.AspNet.Clients;
    using Newtonsoft.Json.Linq;

    public class GithubClient : OAuth2Client
    {
        private const string AuthorizationEndpoint = "https://github.com/login/oauth/authorize?client_id={0}&redirect_uri={1}";
        private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
        private const string TokenPostFormat = "client_id={0}&client_secret={1}&code={2}&state";
        private readonly string applicationId_;
        private readonly string applicationSecret_;

        public GithubClient(string appId, string appSecret)
            : base("github")
        {
            if (string.IsNullOrEmpty(appId))
                throw new ArgumentException("appId");

            if (string.IsNullOrEmpty(appSecret))
                throw new ArgumentException("appSecret");

            this.applicationId_ = appId;
            this.applicationSecret_ = appSecret;
        }

        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            return new Uri(
                string.Format(AuthorizationEndpoint, applicationId_, returnUrl.AbsoluteUri)
            );
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var message = string.Format(TokenPostFormat, applicationId_, applicationSecret_, authorizationCode);

            var tokenRequest = WebRequest.Create(TokenEndpoint);
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.ContentLength = message.Length;
            tokenRequest.Method = "POST";

            using (var requestStream = tokenRequest.GetRequestStream())
            {
                var writer = new StreamWriter(requestStream);
                writer.Write(message);
                writer.Flush();
            }

            var tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();
            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var responseStream = tokenResponse.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream);
                    var responseText = reader.ReadToEnd();
                    try
                    {
                        var token = Regex.Match(responseText, "access_token=(.*)&token_type=(.*)").Groups[1].Value;
                        return token;
                    }
                    catch (Exception e)
                    {
                        throw new UriFormatException("Unexpected format", e);
                    }
                }
            }

            return null;
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var request = WebRequest.Create("https://api.github.com/user?access_token=" + accessToken);
            JObject json = null;

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream);
                    json = JObject.Parse(reader.ReadToEnd());
                }
            }

            var userData = new Dictionary<string, string>();

            userData.Add("login", (string)json["login"]);
            userData.Add("id", (string)json["id"]);
            userData.Add("avatar_url", (string)json["avatar_url"]);
            userData.Add("gravatar_id", (string)json["gravatar_id"]);
            userData.Add("url", (string)json["url"]);
            userData.Add("name", (string)json["name"]);
            userData.Add("company", (string)json["company"]);
            userData.Add("blog", (string)json["blog"]);
            userData.Add("location", (string)json["location"]);
            userData.Add("email", (string)json["email"]);
            userData.Add("hireable", (string)json["hireable"]);
            userData.Add("bio", (string)json["bio"]);
            userData.Add("public_repos", (string)json["public_repos"]);
            userData.Add("public_gists", (string)json["public_gists"]);
            userData.Add("followers", (string)json["followers"]);
            userData.Add("following", (string)json["following"]);
            userData.Add("html_url", (string)json["html_url"]);
            userData.Add("created_at", (string)json["created_at"]);
            userData.Add("type", (string)json["type"]);
            userData.Add("total_private_repos", (string)json["total_private_repos"]);
            userData.Add("owned_private_repos", (string)json["owned_private_repos"]);
            userData.Add("private_gists", (string)json["private_gists"]);
            userData.Add("disk_usage", (string)json["disk_usage"]);
            userData.Add("collaborators", (string)json["collaborators"]);
            userData.Add("plan_name", (string)json["plan"]["name"]);
            userData.Add("plan_space", (string)json["plan"]["space"]);
            userData.Add("plan_collaborators", (string)json["plan"]["collaborators"]);
            userData.Add("plan_private_repos", (string)json["plan"]["private_repos"]);

            return userData;
        }
    }
}
