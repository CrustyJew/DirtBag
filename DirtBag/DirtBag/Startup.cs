using Owin;
using System.Web.Http;
using Thinktecture.IdentityModel;
namespace DirtBag {
    public partial class Startup {
        public void Configuration(IAppBuilder appBuilder ) {
            HttpConfiguration config = new HttpConfiguration();
            WebApiConfig.Register( config );
            config.Filters.Add( new AuthorizeAttribute() );
            ConfigureAuth( appBuilder );
            appBuilder.UseWebApi( config );
        }
    }
}
