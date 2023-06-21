using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Meteor_Rest
{
    public class AuthHandler
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private SqlServer _sqlserver;
        public AuthHandler(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _sqlserver = new SqlServer(logger, configuration);
        }
        public void CreateAccount(HttpRequest request, HttpResponse response)
        {
            if (!request.HasFormContentType)
            {
                return;
            }
            IFormCollection form = request.Form;

            Dictionary<string,string>? formData = form.ToDictionary(x => x.Key, x => x.Value.ToString());
            formData?.Remove("password");
            formData?.Remove("verifypassword");

            string newQuery = QueryHelpers.AddQueryString(_configuration["General:rest_create_user_page"], formData);
            string redirectPage = _configuration["General:rest_create_user_page"] + "?" + newQuery;

            string lang, region, username, password, verifypassword, email;
            if ((lang = form["lang"]) == null || lang.Length == 0)
            {
                response.Redirect(redirectPage + "#langError");
            }
            if ((region = form["region"]) == null || region.Length == 0)
            {
                response.Redirect(redirectPage + "#regionError");
            }
            if ((username = form["username"]) == null || username.Length == 0 || !Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
            {
                response.Redirect(redirectPage + "#usernameError");
            }
            if ((password = form["password"]) == null || password.Length == 0)
            {
                response.Redirect(redirectPage + "#passwordError");
            }
            if ((verifypassword = form["verifypassword"]) == null || verifypassword.Length == 0 || (verifypassword != password))
            {
                response.Redirect(redirectPage + "#verifyPasswordError");
            }
            if ((email = form["email"]) == null || email.Length == 0 || !(new EmailAddressAttribute().IsValid(email)))
            {
                response.Redirect(redirectPage + "#emailError");
            }

            if (_sqlserver.DoesUsernameExist(username))
            {
                _logger.LogInformation("Username " + username + " exists.");
                response.Redirect(redirectPage + "#usernameExistError");
            }

            if (_sqlserver.CreateAccount(username, password, email, lang, region))
            {
                response.Redirect(_configuration["General:rest_login_page"] + "#createSuccess");
            }
        }

        public void LoginAccount(HttpRequest request, HttpResponse response)
        {
            if (!request.HasFormContentType)
            {
                return;
            }

            IFormCollection form = request.Form;

            Dictionary<string, string> formData = form.ToDictionary(x => x.Key, x => x.Value.ToString());
            formData?.Remove("password");

            string redirectPage = QueryHelpers.AddQueryString(_configuration["General:rest_login_page"], formData);

            string username = "", password = "";
            if ((username = form["username"]) == null || username.Length == 0 || !Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
            {
                _logger.LogInformation($"Invalid username: {username}");
                response.Redirect($"{redirectPage}#usernameError");
                return;
            }
            if ((password = form["password"]) == null || password.Length == 0)
            {
                _logger.LogInformation($"Invalid password for: {username}");
                response.Redirect($"{redirectPage}#passwordError");
                return;
            }

            int uid = -1;
            if ((uid = _sqlserver.LoginAccount(username, password)) != -1)
            {
                _logger.LogInformation(String.Format("{0} ({1}) logged in.", username, uid));

                string? sid = _sqlserver.CreateOrRefreshSession(uid);
                formData.Add("session", sid);
                if (sid?.Length > 0)
                {
                    // TODO respond with lang/region
                    string successUrl = $"{QueryHelpers.AddQueryString(_configuration["General:rest_start_game_page"], formData)}";
                    response.Redirect(successUrl);

                    /* if for some reason javascript dies, we can always just output the html directly.
                    StreamWriter sb = new StreamWriter(response.OutputStream);
                    sb.Write($"<x-sqexauth sid=\"{sid}\" lang=\"en-us\" region=\"2\" utc=\"{DateTime.UtcNow}\" />");
                    sb.Flush();
                    sb.Dispose();
                    */

                }

            }
            else
            {
                _logger.LogInformation(String.Format("{0} ({1}) failed login.", username, uid));
                response.Redirect($"{redirectPage}#loginFailedError");
            }

            // Boot with 7U Launcher = "ffxiv://login_success?sessionId="+parsed.sid;
            // Boot with official launcher
            // document.getElementById("Error").innerHTML = "<x-sqexauth sid=\""+parsed.sid+"\" lang=\""+parsed.lang+"\" region=\""+parsed.region+"\" utc=\""+parsed.utc+"\" />";
        }
    }
}
