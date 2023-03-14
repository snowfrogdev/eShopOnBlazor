using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(eShopLegacyWebForms.Startup))]
namespace eShopLegacyWebForms
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
