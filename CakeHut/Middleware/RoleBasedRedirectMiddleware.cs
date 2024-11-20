using System.Security.Claims;

namespace CakeHut.Middleware
{
    public class RoleBasedRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleBasedRedirectMiddleware> _logger;

        public RoleBasedRedirectMiddleware(RequestDelegate next, ILogger<RoleBasedRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            if (user.Identity.IsAuthenticated)
            {
                var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (role == "admin" && !context.Request.Path.StartsWithSegments("/Dashboard/Index"))
                {
                    context.Response.Redirect("/Dashboard/Index");
                    return;
                }
                else if (role == "user" && !context.Request.Path.StartsWithSegments("/Home/UserHome"))
                {
                    context.Response.Redirect("/Home/UserHome");
                    return;
                }
            }
            await _next(context);
        }
    }

}
