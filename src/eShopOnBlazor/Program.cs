using eShopOnBlazor.Models;
using eShopOnBlazor.Models.Infrastructure;
using eShopOnBlazor.Services;
using log4net;
using System.Data.Entity;
using eShopOnBlazor;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSystemWebAdapters()
 .AddRemoteAppClient(options =>
 {
  options.RemoteAppUrl = new(builder.Configuration["ReverseProxy:Clusters:fallbackCluster:Destinations:fallbackApp:Address"]);
  options.ApiKey = "SuperSecretApiKeyThatShouldReallyBeStoredSomewhereSafe";
 })
 .AddAuthenticationClient(true);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// add services

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

if (builder.Configuration.GetValue<bool>("UseMockData"))
{
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
}
else
{
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddScoped<IDatabaseInitializer<CatalogDBContext>, CatalogDBInitializer>();
    builder.Services.AddSingleton<CatalogItemHiLoGenerator>();
    builder.Services.AddScoped(_ => new CatalogDBContext(builder.Configuration.GetConnectionString("CatalogDBContext")));
}

var app = builder.Build();

new LoggerFactory().AddLog4Net("log4Net.xml");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

// Middleware for Application_BeginRequest
app.Use((ctx, next) =>
{
    LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper(ctx);
    LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(ctx);
    return next();
});

app.UseStaticFiles();

app.UseRouting();
app.UseSystemWebAdapters();

app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapBlazorPages("/_Host");
});

ConfigDataBase(app);

static void ConfigDataBase(IApplicationBuilder app)
{
    using (var scope = app.ApplicationServices.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetService<IDatabaseInitializer<CatalogDBContext>>();

        if (initializer != null)
        {
            Database.SetInitializer(initializer);
        }
    }
}
app.MapReverseProxy();

app.UseAuthentication();

app.Run();
