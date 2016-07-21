using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HISWebClient.Startup))]
namespace HISWebClient
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
