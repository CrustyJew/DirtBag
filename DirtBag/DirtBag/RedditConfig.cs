using System.Configuration;

namespace DirtBag {

    //Use this in the future to only exclude the really "private" info into a seperate file.
    public sealed class RedditConfig : ConfigurationSection {
        [ConfigurationProperty( "botCredentials", IsRequired = true )]
        public BotCredentialsElement BotCredentials {
            get { return (BotCredentialsElement) base["botCredentials"]; }
            set { base["botCredentials"] = value; }
        }
        public class BotCredentialsElement : ConfigurationElement {
            [ConfigurationProperty( "username", IsRequired = true )]
            public string Username {
                get { return (string) base["username"]; }
                set { base["username"] = value; }
            }
            [ConfigurationProperty( "password", IsRequired = true )]
            public string Password {
                get { return (string) base["password"]; }
                set { base["password"] = value; }
            }
        }
    }


}
