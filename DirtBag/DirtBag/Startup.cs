using Owin;
using System.Web.Http;
using Thinktecture.IdentityModel;
using Microsoft.Practices.Unity.WebApi;

namespace DirtBag {
    public partial class Startup {
        public void Configuration(IAppBuilder appBuilder ) {
            HttpConfiguration config = new HttpConfiguration();
            config.DependencyResolver = new UnityDependencyResolver( Helpers.UnityHelpers.GetConfiguredContainer() );
            WebApiConfig.Register( config );
            ConfigureAuth( appBuilder );
            appBuilder.UseWebApi( config );
        }
    }
}
