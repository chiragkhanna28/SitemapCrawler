using Sitecore.Abstractions;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using System;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.Web;

namespace SitemapCrawler.LinkManagers
{
    public class CustomLinkProvider : LinkProvider
    {
        private ISiteInfoResolver SiteInfoResolver { get; set; }
        public CustomLinkProvider() : base(ServiceLocator.ServiceProvider.GetService<BaseFactory>())
        {
            SiteInfoResolver = ServiceLocator.ServiceProvider.GetService<ISiteInfoResolver>();
        }
        public override string GetItemUrl(Item item, ItemUrlBuilderOptions options)
        {
            if (item != null && item.TemplateName.Equals(Constants.ArticlePage, StringComparison.OrdinalIgnoreCase))
            {
                SiteInfo site = this.SiteInfoResolver.GetSiteInfo(item);
                return string.Format("https://{0}/{1}/{2}", site.HostName, Constants.ArticlesUrl, item.Name.Replace(" ", "-"));
            }
            return base.GetItemUrl(item, options);
        }
    }
}
