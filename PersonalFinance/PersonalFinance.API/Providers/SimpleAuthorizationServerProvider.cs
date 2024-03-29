﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security.OAuth;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PersonalFinance.API.Providers
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }

        // Responsible for validating the username and password sent to the authorization server's token endpoint.
        // If credentials are valid, two claims ("sub", "role") are added and will be include in the signed token.
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            /* Allow CORS (Cross-Origin-Resource-Sharing) on the token middleware provider.
             * If not included, generating the token will fail when you try to call it from the browser.
             */
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            using (AuthRepository repo = new AuthRepository())
            {
                var user = await repo.FindUser(context.UserName, context.Password);
                if (user == null)
                {
                    context.SetError("invalid_grant", "The username or password is incorrect.");
                    return;
                }

                //var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                //identity.AddClaim(new Claim("sub", context.UserName));
                //identity.AddClaim(new Claim("role", "user"));
                // Token generation happens behind the scenes here!!!
                //context.Validated(identity);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(repo.UserManager, OAuthDefaults.AuthenticationType);
                AuthenticationProperties properties = CreateProperties(oAuthIdentity);
                AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
                context.Validated(ticket);
            }
        }

        private AuthenticationProperties CreateProperties(ClaimsIdentity oAuthIdentity)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "claims", JsonConvert.SerializeObject(oAuthIdentity.Claims.Select(x => new { ClaimType = x.Type, ClaimValue = x.Value }), new KeyValuePairConverter() )}
            };

            return new AuthenticationProperties(data);
        }
    }
}