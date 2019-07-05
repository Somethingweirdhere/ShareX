#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2019 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Newtonsoft.Json;
using ShareX.HelpersLib;
using ShareX.UploadersLib.Properties;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ShareX.UploadersLib.ImageUploaders
{
    public class RedditImageUploaderService : ImageUploaderService
    {
        public override ImageDestination EnumValue { get; } = ImageDestination.Reddit;

        public override Image ServiceImage => Resources.Reddit;

        public override bool CheckConfig(UploadersConfig config)
        {
            return OAuthInfo.CheckOAuth(config.RedditOAuthInfo);
        }

        public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
        {
            OAuthInfo redditOAuth = config.TwitterOAuthInfoList.ReturnIfValidIndex(config.TwitterSelectedAccount);

            return new Reddit(redditOAuth)
            {
                SkipMessageBox = config.TwitterSkipMessageBox,
                DefaultMessage = config.TwitterDefaultMessage ?? ""
            };
        }

        public override TabPage GetUploadersConfigTabPage(UploadersConfigForm form) => form.tpReddit;
    }

    public class Reddit : ImageUploader, IOAuth2
    {

        private const string AuthorizationEndpoint = "https://www.reddit.com/api/v1/authorize";
        private const string TokenEndpoint = "https://www.reddit.com/api/v1/access_token";
        public OAuth2Info AuthInfo { get; set; }

        public Reddit(OAuth2Info oauth)
        {
            AuthInfo = oauth;
        }

        public string GetAuthorizationURL()
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("client_id", AuthInfo.Client_ID);
            args.Add("scope", "submit");
            args.Add("response_type", "code");
            args.Add("duration", "permament");
            args.Add("redirect_uri", Links.URL_CALLBACK);

            return URLHelpers.CreateQueryString(AuthorizationEndpoint, args);
        }

        public bool GetAccessToken(string code)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("client_id", AuthInfo.Client_ID);
            args.Add("redirect_uri", Links.URL_CALLBACK);
            args.Add("client_secret", AuthInfo.Client_Secret);
            args.Add("code", code);
            args.Add("grant_type", "authorization_code");

            string response = SendRequestURLEncoded(HttpMethod.POST, TokenEndpoint, args);

            if (!string.IsNullOrEmpty(response))
            {
                OAuth2Token token = JsonConvert.DeserializeObject<OAuth2Token>(response);

                if (token != null && !string.IsNullOrEmpty(token.access_token))
                {
                    token.UpdateExpireDate();
                    AuthInfo.Token = token;
                    return true;
                }
            }

            return false;
        }

        public bool RefreshAccessToken()
        {
            if (OAuth2Info.CheckOAuth(AuthInfo) && !string.IsNullOrEmpty(AuthInfo.Token.refresh_token))
            {
                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("client_id", AuthInfo.Client_ID);
                args.Add("client_secret", AuthInfo.Client_Secret);
                args.Add("refresh_token", AuthInfo.Token.refresh_token);
                args.Add("grant_type", "refresh_token");

                string response = SendRequestURLEncoded(HttpMethod.POST, TokenEndpoint, args);

                if (!string.IsNullOrEmpty(response))
                {
                    OAuth2Token token = JsonConvert.DeserializeObject<OAuth2Token>(response);

                    if (token != null && !string.IsNullOrEmpty(token.access_token))
                    {
                        token.UpdateExpireDate();
                        string refresh_token = AuthInfo.Token.refresh_token;
                        AuthInfo.Token = token;
                        AuthInfo.Token.refresh_token = refresh_token;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CheckAuthorization()
        {
            if (OAuth2Info.CheckOAuth(AuthInfo))
            {
                if (AuthInfo.Token.IsExpired && !RefreshAccessToken())
                {
                    Errors.Add("Refresh access token failed.");
                    return false;
                }
            }
            else
            {
                Errors.Add("Login is required.");
                return false;
            }

            return true;
        }

        public override UploadResult Upload(Stream stream, string fileName)
        {
            throw new System.NotImplementedException();
        }
    }

    public class RedditOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Auth_token { get; set; }
    }
}