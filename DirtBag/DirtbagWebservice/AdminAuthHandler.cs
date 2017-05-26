using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtbagWebservice
{
    public class AdminAuthRequirement : IAuthorizationRequirement
    {

    }
    public class AdminAuthHandler : AuthorizationHandler<AdminAuthRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminAuthRequirement requirement)
        {
            if (context.User.HasClaim("uri:dirtbag", "admin"))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
