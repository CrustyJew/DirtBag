using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;

namespace DirtBag {
    public partial class Startup {
        static Dictionary<string, string> basicUsers;
        public void ConfigureAuth( IAppBuilder app ) {
            basicUsers = new Dictionary<string, string>();
            string appSet = System.Configuration.ConfigurationManager.AppSettings["BasicAuthUsers"];
            if ( !string.IsNullOrWhiteSpace( appSet ) ) {
                foreach ( string ident in appSet.Split( ';' ) ) {
                    string[] login = ident.Split( ',' );
                    basicUsers.Add( login[0].Trim().ToLower(), login[1].Trim() );
                }
            }
            Func<string, string, Task<IEnumerable<Claim>>> validateUser = ( string uname, string password ) => {
                IEnumerable<Claim> toReturn = null;
                if ( basicUsers.ContainsKey( uname.ToLower() ) ) {
                    if ( basicUsers[uname.ToLower()] == password ) {
                        toReturn = new List<Claim>() { new Claim( "authed", "true" ) };
                    }
                }
                return Task.FromResult( toReturn );
            };
            Thinktecture.IdentityModel.Owin.BasicAuthenticationOptions opts = new Thinktecture.IdentityModel.Owin.BasicAuthenticationOptions( "BloodGulch",
                new Thinktecture.IdentityModel.Owin.BasicAuthenticationMiddleware.CredentialValidationFunction( validateUser ) );
            app.UseBasicAuthentication( opts );
        }
    }
}
