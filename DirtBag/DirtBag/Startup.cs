using Owin;
using System.Web.Http;
namespace DirtBag {
    public class Startup {
        public void Configuration(IAppBuilder appBuilder ) {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}",
                defaults: new {} 
            );

            appBuilder.UseWebApi( config );
        }
    }
}
