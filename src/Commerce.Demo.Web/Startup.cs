using CmsContentScaffolding.Optimizely.Startup;
using Commerce.Demo.Business.Features.BackgroundJobs.Filters;
using Commerce.Demo.Business.Features.BackgroundJobs.Jobs;
using Commerce.Demo.Business.Features.BackgroundJobs.Options;
using Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;
using Commerce.Demo.Business.Features.CookieConsentAudit.Options;
using Commerce.Demo.Business.Features.CookieConsentAudit.Services;
using Commerce.Demo.Business.Features.FilesImport.Interfaces;
using Commerce.Demo.Business.Features.FilesImport.Services;
using Commerce.Demo.Business.Features.Sitemaps;
using Commerce.Demo.Business.Features.SiteSettings.Interfaces;
using Commerce.Demo.Business.Features.SiteSettings.Services;
using Commerce.Demo.Business.Infrastructure.Caching;
using Commerce.Demo.Business.Infrastructure.Cookies;
using Commerce.Demo.Business.Infrastructure.Email;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Blocks;
using Commerce.Demo.Optimizely.Core.Constants;
using Commerce.Demo.Optimizely.Core.Infrastructure.Catalog;
using Commerce.Demo.Optimizely.Core.Infrastructure.Middleware;
using Commerce.Demo.Optimizely.Core.Infrastructure.Rendering;
using Commerce.Demo.Web.Api;
using Commerce.Demo.Web.Api.Services;
using Commerce.Demo.Web.Filters;
using Commerce.Demo.Web.Infrastructure;
using EPiServer.Authorization;
using EPiServer.CloudPlatform.Cms;
using EPiServer.Cms.Shell;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.Shell.UI.Configurations;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Cms.TinyMce.Core;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Cms.UI.VisitorGroups;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Visistor;
using EPiServer.DependencyInjection;
using EPiServer.Framework.Web.Resources;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization;
using Geta.Optimizely.Categories.Configuration;
using Geta.Optimizely.Categories.Infrastructure.Initialization;
using Geta.Optimizely.Sitemaps;
using Geta.Optimizely.Sitemaps.Commerce;
using Geta.Optimizely.Sitemaps.Configuration;
using Geta.Optimizely.Sitemaps.XML;
using Hangfire;
using Hangfire.SqlServer;
using Mediachase.Commerce.Anonymous;
using Mediachase.Commerce.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;
using Optimizely.Cms.OpalChat;
using Optimizely.Graph.Commerce;
using Stott.Optimizely.RobotsHandler.Configuration;
using Stott.Security.Optimizely.Features.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Commerce.Demo.Web;

public class Startup
{
    private readonly IWebHostEnvironment _webHostingEnvironment;
    private readonly IConfiguration _configuration;
    private bool _contentGraphEnabled;
    private bool _optimizelyIdentityEnabled;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _webHostingEnvironment = webHostingEnvironment;
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (_webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(_webHostingEnvironment.ContentRootPath, "App_Data"));

            services
                .Configure<SchedulerOptions>(o => o.Enabled = false)
                .Configure<ClientResourceOptions>(o => { o.Debug = true; })
                .AddAdminUserRegistration(o => o.Behavior = RegisterAdminUserBehaviors.LocalRequestsOnly);
        }

        #region Mapster initialization

        MapsterConfig.Initialize();

        #endregion

        #region Optimizely Services

        // CMS 13: AddCms() must be called explicitly (even with AddCommerce), otherwise the host fails
        // at startup with "Sequence contains no matching element". Visitor groups and Forms are no
        // longer switched on by the EPiServer.CMS metapackage.
        services
            .AddCms()
            .AddCommerce()
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddVisitorGroupsCore()
            .AddVisitorGroupsMvc()
            .AddVisitorGroupsUI()
            .AddForms()
            // Must be registered whenever EPiServer.Cms.UI.ContentManager is referenced: its editor
            // descriptors are auto-discovered and the host fails to start without these services.
            .AddContentManager()
            // EPiServer.Labs.LanguageManager 6 (CMS 13) must be registered explicitly; 5.x registered itself.
            .AddLanguageManager()
            .AddEmbeddedLocalization<Startup>()
            .AddRobotsHandler()
            .AddSitemaps(x =>
            {
                x.SiteArchitecture = SiteArchitecture.Mvc;
                x.EnableRealtimeCaching = true;
                x.EnableRealtimeSitemap = false;
            })
            .AddSitemapsCommerce()
            .AddNotFoundHandler(
            o =>
            {
                o.UseSqlServer(_configuration.GetConnectionString("EPiServerDB"));
                o.Logging = LoggerMode.On;
                o.LogWithHostname = true;
                o.IgnoredResourceExtensions = ["js", "css", "map", "png", "jpg", "jpeg", "webp", "gif", "ico", "swf", "woff", "svg"];
            },
            p => p.RequireRole(Roles.WebAdmins))
            .AddOptimizelyNotFoundHandler(o => o.AutomaticRedirectsEnabled = true)
            .AddStottSecurity()
            .AddGetaCategories()
            .AddCmsContentScaffolding<StartPage>(_configuration);

        #region Optional CMS 13 features (enabled only when their credentials are configured)

        // Credentials come from user-secrets locally and from the DXP App Settings portal in cloud
        // environments; appsettings.json only holds empty placeholders. Content Manager and Opal are
        // registered unconditionally (AddContentManager above, AddOpalChat inside AddCms). Opal's options
        // are bound explicitly from EPiServer:OpalChat (InstanceId).
        services.Configure<OpalChatOptions>(_configuration.GetSection("EPiServer:OpalChat"));

        // Optimizely Graph for CMS + catalog content (replaces Find, ADR-008). AddCommerceGraph() calls
        // AddContentGraph() itself. Keys: Optimizely:ContentGraph:AppKey / Secret (Base64) / SingleKey.
        // Registered only when keys exist: once registered, Graph pushes its schema at startup and the site
        // fails to start without valid keys. Without keys, Graph's scheduled jobs are listed but log
        // "Failed to create an instance" until Graph is enabled.
        _contentGraphEnabled = IsConfigured("Optimizely:ContentGraph:AppKey") && IsConfigured("Optimizely:ContentGraph:Secret");
        if (_contentGraphEnabled)
        {
            services.AddCommerceGraph();
        }

        // Opti ID for the back office only (useAsDefault: false keeps ASP.NET Identity for the rest of the
        // site). Needs EPiServer:OptimizelyIdentity:InstanceId / ClientId / ClientSecret and the local site on
        // https://localhost:5000 (Opti ID's registered local redirect URI).
        _optimizelyIdentityEnabled = IsConfigured("EPiServer:OptimizelyIdentity:InstanceId")
            && IsConfigured("EPiServer:OptimizelyIdentity:ClientId")
            && IsConfigured("EPiServer:OptimizelyIdentity:ClientSecret");
        if (_optimizelyIdentityEnabled)
        {
            // Bound explicitly: the options are not picked up from EPiServer:OptimizelyIdentity on their own
            // (verified: ClientId/InstanceId stayed empty). With Opti ID registered, invalid settings make
            // every request fail, so only real credentials should be configured.
            services.AddOptimizelyIdentity(
                useAsDefault: false,
                o => _configuration.GetSection("EPiServer:OptimizelyIdentity").Bind(o));
        }

        #endregion

        #endregion

        #region Optimizely DXP Services

        if (!_webHostingEnvironment.IsDevelopment())
        {
            services.AddAzureBlobProvider(o =>
            {
                o.ConnectionString = _configuration.GetSection("ConnectionStrings").GetValue<string>("EPiServerAzureBlobs");
                o.ContainerName = $"container-name";
            });

            services.AddCmsCloudPlatformSupport(_configuration);
            services.AddCommerceCloudPlatformSupport(_configuration);
        }

        #endregion

        #region Optimizely API Services

        //services.ConfigureContentApiOptions(o =>
        //{
        //    o.EnablePreviewFeatures = true;
        //    o.IncludeEmptyContentProperties = true;
        //    o.FlattenPropertyModel = false;
        //    o.IncludeMasterLanguage = false;

        //});

        //// Content Delivery API
        //services.AddContentDeliveryApi()
        //    .WithFriendlyUrl()
        //    .WithSiteBasedCors();
        //services.AddContentDeliveryApi(OpenIDConnectOptionsDefaults.AuthenticationScheme, options =>
        //{
        //    options.SiteDefinitionApiEnabled = true;
        //}).WithFriendlyUrl()
        //      .WithSiteBasedCors();

        //// Content Delivery Forms API
        //services.AddFormsApi();

        //// Content Delivery Commerce API
        //services.AddCommerceApi<AnaApplicationUser>(OpenIDConnectOptionsDefaults.AuthenticationScheme, o =>
        //{
        //    o.DisableScopeValidation = true;
        //});

        //// Content Definitions API
        //services.AddContentDefinitionsApi(options =>
        //{
        //    // Accept anonymous calls
        //    options.DisableScopeValidation = true;
        //});

        //// Content Management
        //services.AddContentManagementApi(OpenIDConnectOptionsDefaults.AuthenticationScheme, options =>
        //{
        //    // Accept anonymous calls
        //    options.DisableScopeValidation = true;
        //});

        #endregion

        #region Other Services

        services
            .AddSingleton<EnableCatalogRoot>()
            .AddScoped<IApiService, ApiService>()
            .AddScoped<ICacheService, CacheService>()
            .AddScoped<ICookiesService, CookiesService>()
            .AddScoped<ISiteSettingsManager, SiteSettingsManager>()
            .Decorate<ISiteSettingsManager, SiteSettingsManagerCacheDecorator>()
            .AddScoped<ICommerceAndStandardSitemapXmlGenerator, DefaultSitemapXmlGenerator>()
            .AddScoped<ICookieConsentAuditService, CookieConsentAuditService>()
            .AddScoped<IFilesImportService, FilesImportService>();

        services.Configure<CookieConsentAuditOptions>(
            _configuration.GetSection(CookieConsentAuditOptions.SectionName));

        // Throttle the anonymous consent-audit endpoint per client IP to prevent audit-store flooding.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("consent-audit", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        #endregion

        #region Hangfire & Email Services

        var hangfireOptions = _configuration
            .GetSection(HangfireOptions.SectionName)
            .Get<HangfireOptions>() ?? new HangfireOptions();

        services.Configure<HangfireOptions>(_configuration.GetSection(HangfireOptions.SectionName));

        services.Configure<EmailOptions>(o =>
        {
            var smtpSection = _configuration.GetSection("EPiServer:Cms:Smtp:Network");
            o.Host = smtpSection.GetValue<string>("Host") ?? o.Host;
            o.Port = smtpSection.GetValue<int?>("Port") ?? o.Port;
            o.UserName = smtpSection.GetValue<string>("UserName");
            o.Password = smtpSection.GetValue<string>("Password");

            var notificationSection = _configuration.GetSection("EPiServer:Cms:Notification");
            o.DefaultFromAddress = notificationSection.GetValue<string>("NotificationEmailAddress") ?? o.DefaultFromAddress;
            o.DefaultFromDisplayName = notificationSection.GetValue<string>("NotificationEmailDisplayName") ?? o.DefaultFromDisplayName;
        });

        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<SendOrderJob>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                _configuration.GetConnectionString("EcfSqlConnection"),
                new SqlServerStorageOptions
                {
                    SchemaName = "HangFire",
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    DisableGlobalLocks = true,
                    PrepareSchemaIfNecessary = true
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireOptions.WorkerCount;
            options.Queues = hangfireOptions.Queues;
            options.ServerTimeout = hangfireOptions.ServerTimeout;
        });

        #endregion

        services
            .AddOutputCache()
            .AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromSeconds(20);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });

        #region Configure Service Options

        services
            .Configure<CatalogOptions>(o =>
            {
                o.Cache.UseCache = true;
                o.Cache.ContentVersionCacheExpiration = TimeSpan.FromMinutes(5);
                o.Cache.CollectionCacheExpiration = TimeSpan.FromMinutes(5);
                o.Cache.EntryCacheExpiration = TimeSpan.FromMinutes(5);
                o.Cache.NodeCacheExpiration = TimeSpan.FromMinutes(5);
            })
            .Configure<TinyMceConfiguration>(config =>
            {
                config.Default()
                    .DisableMenubar()
                    .AddEpiserverSupport()
                    .AddPlugin("image media fullscreen lists searchreplace anchor")
                    .Height(400)
                    .Width(440)
                    .Resize(TinyMceResize.Both)
                    .BodyClass("custom_body_class")
                    //.ContentCss("/static/css/editor.css")
                    .Toolbar("formatselect | bold italic | epi-link anchor image epi-image-editor media epi-personalized-content | bullist numlist outdent indent | searchreplace fullscreen html");
            })
            // CMS 13 removed [InlineBlockEditSettings(ShowNameProperty = true)]: inline blocks have no Name.
            // Label inline blocks in content areas by one of their own properties instead.
            .Configure<InlineBlockNamePropertiesOptions>(o =>
            {
                o.Add(nameof(TeaserBlock), nameof(TeaserBlock.Heading));
                o.Add(nameof(LabelXhtmlBlock), nameof(LabelXhtmlBlock.Label));
            })
            .Configure<DisplayOptions>(o =>
            {
                o.Add("full", "/displayoptions/full", Globals.ContentAreaTags.FullWidth, string.Empty, "epi-icon__layout--full");
                o.Add("wide", "/displayoptions/wide", Globals.ContentAreaTags.WideWidth, string.Empty, "epi-icon__layout--wide");
                o.Add("half", "/displayoptions/half", Globals.ContentAreaTags.HalfWidth, string.Empty, "epi-icon__layout--half");
                o.Add("narrow", "/displayoptions/narrow", Globals.ContentAreaTags.NarrowWidth, string.Empty, "epi-icon__layout--narrow");
            })
            .Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new SiteViewEngineLocationExpander()))
            .Configure<MvcOptions>(o => o.Filters.Add<PageContextActionFilter>())
            .Configure<MediaFileOptions>(options => options.FileSizeLimit = 62914560)
            .Configure<InstrumentationOptions>(o => o.DisableUnloadEvent = true)
            .Configure<JsonSerializerOptions>(o =>
            {
                o.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;
                o.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                o.AllowTrailingCommas = true;
                o.WriteIndented = true;
                o.PropertyNameCaseInsensitive = true;
                o.Converters.Add(new JsonStringEnumConverter());
            });

        #endregion
    }

    private bool IsConfigured(string key) => !string.IsNullOrWhiteSpace(_configuration[key]);

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Feature status only — never log the credential values themselves.
        app.ApplicationServices.GetRequiredService<ILogger<Startup>>().LogInformation(
            "Optional CMS 13 features: Content Graph {ContentGraph}, Opti ID {OptimizelyIdentity}",
            _contentGraphEnabled ? "enabled" : "disabled (no keys configured)",
            _optimizelyIdentityEnabled ? "enabled" : "disabled (no credentials configured)");

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseGetaCategories();

        #region CMS Content Scaffolding

        app.UseCmsContentScaffolding<StartPage>(b =>
        {
            #region CMS Pages Initialization

            b.UsePages()
            .WithStartPage<StartPage>(p =>
            {
                p.Name = "Start Page";
                p.Heading = "Optimizely Commerce DEMO";
            }, l1 => l1
            .WithPage<SiteSettingsPage>(p =>
            {
                p.Name = "[Site Settings]";
                p.SiteName = "Commerce DEMO";
                p.SomeApiKey = "test-api-key";
            })
            .WithPage<NotFoundPage>(p =>
            {
                p.Name = "404";
                p.Heading = "Page Not Found!";
            }));

            #endregion

            #region COMMERCE Catalog Initialization

            var referenceConverter = app.ApplicationServices.GetRequiredService<ReferenceConverter>();

            b.UseAssets(referenceConverter.GetRootLink())
            .WithContent<CatalogContent>(x =>
            {
                x.Name = "Products";
                x.DefaultCurrency = "USD";
                x.DefaultLanguage = "en";
                x.WeightBase = "lbs";
                x.LengthBase = "in";
                //x.Sites = new ItemCollection<string>([SiteDefinition.Current.Id.ToString()]);
            });

            #endregion
        });

        #endregion

        #region Geta NotFound Handler

        app.UseNotFoundHandler()
            .UseOptimizelyNotFoundHandler()
            .UseStatusCodePagesWithReExecute("/{0}");

        #endregion

        app.UseAnonymousId();
        app.UseVisitor();

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=604800, must-revalidate");
            }
        });

        app.UseRobotsHandler();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseOutputCache();
        app.UseAuthentication();
        app.UseAuthorization();

        #region Hangfire Dashboard

        var hangfireOpts = app.ApplicationServices.GetRequiredService<IOptions<HangfireOptions>>().Value;

        GlobalJobFilters.Filters.Add(new JobFailureNotificationFilter(
            app.ApplicationServices,
            app.ApplicationServices.GetRequiredService<ILogger<JobFailureNotificationFilter>>()));

        app.UseHangfireDashboard(hangfireOpts.DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter(
                app.ApplicationServices.GetRequiredService<IWebHostEnvironment>())]
        });

        #endregion

        app.UseMiddleware<AnonymousCartMergingMiddleware>();

        #region Security Response Headers

        app.UseStottSecurity();

        #endregion

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapContent();
            endpoints.MapRazorPages();
            endpoints.AddApiEndpoints();
            endpoints.MapHangfireDashboard();
        });
    }
}
