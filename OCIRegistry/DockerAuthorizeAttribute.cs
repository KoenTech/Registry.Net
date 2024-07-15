using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OCIRegistry.Models;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OCIRegistry
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DockerAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public DockerAuthorizeAttribute(RepoScope scope)
        {
            _scope = scope;
        }

        private RepoScope _scope;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool hasRepo = context.RouteData.Values.TryGetValue("name", out object? _repoObj);
            bool hasPrefix = context.RouteData.Values.TryGetValue("prefix", out object? _prefixObj);
            string? repo = (string?)_repoObj;
            if (hasPrefix) repo = (string?)_prefixObj + "/" + repo;
            
            if (context.HttpContext.User?.Identity?.IsAuthenticated ?? false)
            {
                if (!hasRepo || _scope == RepoScope.None) return;

                var accessClaim = context.HttpContext.User.FindAll("access");

                foreach (var claim in accessClaim)
                {
                    try
                    {
                        var access = JsonSerializer.Deserialize<AccessClaim>(claim.Value)!;
                        if (access.Name == repo && repo is not null)
                        {
                            var jwtAccess = access.Actions.Select(x => Enum.Parse<RepoScope>(x, true)).Aggregate((a, b) => a | b);
                            if (jwtAccess.HasFlag(_scope)) return;
                        }
                    }
                    catch (JsonException)
                    {
                        context.Result = new UnauthorizedResult();
                        return;
                    }
                }
            }
            string authUrl = $"http://{context.HttpContext.Request.Host}/api/auth";
            string authHeader = $"Bearer realm=\"{authUrl}\",service=\"registry\"";
            if (hasRepo) authHeader += $",scope=\"{FormatScope(repo, _scope)}\"";

            context.HttpContext.Response.Headers.Append("www-authenticate", authHeader);
            context.Result = new UnauthorizedResult();
        }
        private static string FormatScope(string? repo, RepoScope scope)
        {
            return $"repository:{repo}:{scope.ToString().Replace(" ", "").ToLowerInvariant()}";
        }
    }

    [Flags]
    public enum RepoScope
    {
        None =      0,
        Pull =      0b00000001,
        Push =      0b00000010,
        Delete =    0b00000100,
    } // TODO: Add the proper scopes
}
