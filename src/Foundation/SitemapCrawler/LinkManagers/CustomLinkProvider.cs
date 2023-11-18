using Sitecore.Abstractions;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Links;
using Sitecore.Links.UrlBuilders;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace SitemapCrawler.LinkManagers
{
    public class CustomLinkProvider : LinkProvider
    {
        public CustomLinkProvider() : base(ServiceLocator.ServiceProvider.GetService<BaseFactory>())
        {
        }
        public override string GetItemUrl(Item item, ItemUrlBuilderOptions options)
        {
            if (item != null && item.TemplateName.Equals(Constants.ArticlePage, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("/{0}/{1}", Constants.ArticlesUrl, item.Name.Replace(" ", "-"));
            }
            return base.GetItemUrl(item, options);
        }
    }
}
