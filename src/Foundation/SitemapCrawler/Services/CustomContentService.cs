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
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.ItemResolvers;
using Sitecore.Services.GraphQL.EdgeSchema.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Text;

namespace SitemapCrawler.Services
{
    public class CustomContentService : IContentService
    {
        private readonly IMultisiteService _multisiteService;
        private readonly BaseLinkManager _baseLinkManager;
        private readonly ItemPathResolver _itemPathResolver;
        private readonly BaseMediaManager _baseMediaManager;
        private readonly IEdgeSettings _settings;
        private ISiteInfoResolver SiteInfoResolver { get; set; }
        private ILinkProviderService LinkProviderService { get; set; }
        public CustomContentService()
        {
            _multisiteService = ServiceLocator.ServiceProvider.GetService<IMultisiteService>();
            _baseLinkManager = ServiceLocator.ServiceProvider.GetService<BaseLinkManager>();
            SiteInfoResolver = ServiceLocator.ServiceProvider.GetService<ISiteInfoResolver>();
            LinkProviderService = ServiceLocator.ServiceProvider.GetService<ILinkProviderService>();
            _itemPathResolver = ServiceLocator.ServiceProvider.GetService<ItemPathResolver>();
            _baseMediaManager = ServiceLocator.ServiceProvider.GetService<BaseMediaManager>();
            _settings = ServiceLocator.ServiceProvider.GetService<IEdgeSettings>();
        }
        public Uri ResolveRouteUri(Item item, Language language)
        {
            SiteInfo info = this._multisiteService.ResolveSite(item);
            ItemUrlBuilderOptions urlBuilderOptions = new ItemUrlBuilderOptions();
            urlBuilderOptions.AlwaysIncludeServerUrl = new bool?(true);
            urlBuilderOptions.Language = language;
            urlBuilderOptions.LanguageEmbedding = new LanguageEmbedding?(LanguageEmbedding.Never);
            urlBuilderOptions.Site = info != null ? new SiteContext(info) : (SiteContext)null;
            urlBuilderOptions.SiteResolving = new bool?(false);
            ItemUrlBuilderOptions options = urlBuilderOptions;
            SiteInfo siteInfo = this.SiteInfoResolver.GetSiteInfo(item);
            if (siteInfo != null)
            {
                SiteContext site = new SiteContext(this.SiteInfoResolver.GetSiteInfo(item));
                LinkProvider provider = this.LinkProviderService.GetLinkProvider(site);
                return new Uri(provider.GetItemUrl(item, options));
            }
            return new Uri(this._baseLinkManager.GetItemUrl(item, options));

        }

        public bool TryResolveItem(
       Database database,
       string inputPathOrIdOrShortId,
       Language language,
       int? version,
       out Item item)
        {
            if (database == null)
            {
                item = (Item)null;
                return false;
            }
            Sitecore.Data.Version version1 = !version.HasValue || version.Value < 1 ? Sitecore.Data.Version.Latest : Sitecore.Data.Version.Parse(version.Value);
            Language language1 = language;
            if ((object)language1 == null)
                language1 = Context.ContentLanguage ?? Context.Language;
            Language language2 = language1;
            using (new LanguageFallbackItemSwitcher(new bool?(this._settings.ItemLanguageFallbackEnabled)))
            {
                ID result;
                if (this.TryResolveId(inputPathOrIdOrShortId, out result))
                {
                    item = database.GetItem(result, language2, version1);
                    return item != null;
                }
                item = database.GetItem(inputPathOrIdOrShortId, language2, version1);
                return item != null;
            }
        }

        public bool TryResolveId(string inputPathOrIdOrShortId, out ID result)
        {
            result = (ID)null;
            if (string.IsNullOrWhiteSpace(inputPathOrIdOrShortId))
                return false;
            ShortID result1;
            if (ShortID.TryParse(inputPathOrIdOrShortId, out result1))
            {
                result = result1.ToID();
                return true;
            }
            return ID.TryParse(inputPathOrIdOrShortId, out result);
        }

        public Item ResolveItemByPath(string path, Item rootItem)
        {
            return this._itemPathResolver.ResolveItem(path, rootItem);
        }

        public Item ResolveItemByPath(
          Database database,
          string path,
          Language language,
          Sitecore.Data.Version version)
        {
            if (database == null)
                return (Item)null;
            Sitecore.Data.Version version1 = version;
            if ((object)version1 == null)
                version1 = Sitecore.Data.Version.Latest;
            version = version1;
            Language language1 = language;
            if ((object)language1 == null)
                language1 = Context.ContentLanguage ?? Context.Language;
            language = language1;
            using (new LanguageFallbackItemSwitcher(new bool?(this._settings.ItemLanguageFallbackEnabled)))
                return database.GetItem(path, language, version);
        }

        public Uri ResolveUri(Item item, Language language)
        {
            SiteInfo siteInfo = this._multisiteService.ResolveSite(item);
            using (new SiteContextSwitcher(new SiteContext(siteInfo)))
                return this.IsMediaItem(item) ? this.ResolveMediaUri(item, language, siteInfo) : this.ResolveItemUri(item, language, siteInfo);
        }
        public string ResolveLinkedFieldUri(LinkField linkField, bool includeQueryString)
        {
            string itemUrl = string.Empty;
            Item targetItem = linkField.TargetItem;
            if (targetItem != null && !this.IsMediaLink(linkField) && !this.HasVersions(targetItem))
                return itemUrl;
            SiteInfo siteInfo = this._multisiteService.ResolveSite(linkField.InnerField.Item);
            SiteInfo info = this._multisiteService.ResolveSite(targetItem);
            using (new SiteContextSwitcher(new SiteContext(info)))
            {
                if (targetItem == null || this.IsMediaLink(linkField))
                {
                    itemUrl = linkField.GetFriendlyUrl();
                }
                else
                {
                    ItemUrlBuilderOptions urlBuilderOptions = new ItemUrlBuilderOptions();
                    urlBuilderOptions.AlwaysIncludeServerUrl = siteInfo != null ? new bool?(!siteInfo.Name.Equals(info?.Name, StringComparison.InvariantCultureIgnoreCase)) : new bool?();
                    urlBuilderOptions.Language = targetItem.Language;
                    urlBuilderOptions.Site = info != null ? new SiteContext(info) : (SiteContext)null;
                    urlBuilderOptions.SiteResolving = new bool?(false);
                    urlBuilderOptions.AddAspxExtension = new bool?(false);
                    urlBuilderOptions.ShortenUrls = new bool?(true);
                    ItemUrlBuilderOptions options = urlBuilderOptions;
                    itemUrl = this._baseLinkManager.GetItemUrl(targetItem, options);
                }
            }
            return !includeQueryString ? itemUrl : this.BuildUrlWithQueryString(itemUrl, linkField);
        }

        public bool HasVersions(Item item)
        {
            return item != null && (uint)item.Versions.Count > 0U;
        }

        private bool IsMediaLink(LinkField field)
        {
            return field.IsMediaLink || field.TargetItem.Paths.IsMediaItem;
        }

        private bool IsMediaItem(Item item)
        {
            return item.Paths.IsMediaItem && item.TemplateID != TemplateIDs.MediaFolder;
        }

        private string BuildUrlWithQueryString(string itemUrl, LinkField linkField)
        {
            UrlString urlString1 = new UrlString(itemUrl);
            urlString1.Hash = linkField.Anchor;
            UrlString urlString2 = urlString1;
            string queryString = this.RemoveHashSectionFromQueryString(linkField.QueryString);
            return this.AppendQueryString(urlString2.GetUrl(), queryString, urlString2.Parameters.Count > 0);
        }

        private string AppendQueryString(string itemUrl, string queryString, bool hasParameters)
        {
            string[] strArray = itemUrl.Split(new char[1] { '#' });
            string str = strArray.Length == 2 ? "#" + strArray[1] : "";
            return hasParameters ? strArray[0] + queryString + str : strArray[0] + this.GetQueryString(queryString) + str;
        }

        protected virtual string GetQueryString(string queryString)
        {
            Assert.ArgumentNotNull((object)queryString, nameof(queryString));
            return !string.IsNullOrEmpty(queryString) && !queryString.StartsWith("?", StringComparison.InvariantCultureIgnoreCase) ? "?" + queryString : queryString;
        }

        private string RemoveHashSectionFromQueryString(string queryString)
        {
            int length = queryString.IndexOf("#", StringComparison.Ordinal);
            return length >= 0 ? StringUtil.Left(queryString, length) : queryString;
        }

        private Uri ResolveMediaUri(Item item, Language language, SiteInfo site)
        {
            MediaUrlBuilderOptions urlBuilderOptions = new MediaUrlBuilderOptions();
            urlBuilderOptions.AlwaysIncludeServerUrl = new bool?(true);
            urlBuilderOptions.Language = language;
            urlBuilderOptions.LanguageEmbedding = new LanguageEmbedding?(LanguageEmbedding.Never);
            urlBuilderOptions.Site = site;
            MediaUrlBuilderOptions options = urlBuilderOptions;
            return new Uri(this._baseMediaManager.GetMediaUrl((MediaItem)item, options));
        }

        private Uri ResolveItemUri(Item item, Language language, SiteInfo site)
        {
            ItemUrlBuilderOptions urlBuilderOptions = new ItemUrlBuilderOptions();
            urlBuilderOptions.AlwaysIncludeServerUrl = new bool?(true);
            urlBuilderOptions.Language = language;
            urlBuilderOptions.Site = site != null ? new SiteContext(site) : (SiteContext)null;
            urlBuilderOptions.SiteResolving = new bool?(false);
            ItemUrlBuilderOptions options = urlBuilderOptions;
            return new Uri(this._baseLinkManager.GetItemUrl(item, options));
        }
    }
}