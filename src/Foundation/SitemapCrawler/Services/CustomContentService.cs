using Sitecore.Abstractions;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using Sitecore.Services.GraphQL.EdgeSchema.Services;
using Sitecore.Services.GraphQL.EdgeSchema.Services.Multisite;
using Sitecore.Sites;
using Sitecore.Web;
using System;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Multisite.Services;

namespace SitemapCrawler.Services
{
    public class CustomContentService : ContentService
    {
        private readonly IMultisiteService _multisiteService;
        private readonly BaseLinkManager _baseLinkManager;
        private ISiteInfoResolver SiteInfoResolver { get; set; }
        private ILinkProviderService LinkProviderService { get; set; }
        public CustomContentService()
        {
            _multisiteService = ServiceLocator.ServiceProvider.GetService<IMultisiteService>();
            _baseLinkManager = ServiceLocator.ServiceProvider.GetService<BaseLinkManager>();
            SiteInfoResolver = ServiceLocator.ServiceProvider.GetService<ISiteInfoResolver>();
            LinkProviderService = ServiceLocator.ServiceProvider.GetService<ILinkProviderService>();
        }
        public new Uri ResolveRouteUri(Item item, Language language)
        {
            SiteInfo info = this._multisiteService.ResolveSite(item);
            ItemUrlBuilderOptions urlBuilderOptions = new ItemUrlBuilderOptions();
            urlBuilderOptions.AlwaysIncludeServerUrl = new bool?(true);
            urlBuilderOptions.Language = language;
            urlBuilderOptions.LanguageEmbedding = new LanguageEmbedding?(LanguageEmbedding.Never);
            urlBuilderOptions.Site = info != null ? new SiteContext(info) : (SiteContext)null;
            urlBuilderOptions.SiteResolving = new bool?(false);
            ItemUrlBuilderOptions options = urlBuilderOptions;
            SiteContext site = new SiteContext(this.SiteInfoResolver.GetSiteInfo(item));
            LinkProvider provider = this.LinkProviderService.GetLinkProvider(site);
            return new Uri(provider.GetItemUrl(item, options));
        }
    }
}