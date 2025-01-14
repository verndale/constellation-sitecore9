﻿using System;
using System.Xml;
using Constellation.Foundation.Caching;
using Sitecore.Sites;
using Sitecore.Web;

namespace Constellation.Foundation.SitemapXml.Repositories
{
	/// <summary>
	/// Provides access to retrieving an XmlDocument for the supplied Site.
	/// </summary>
	public class SitemapRepository
	{
		/// <summary>
		/// Used to determine if the Site should be crawled and a sitemap.xml should be generated.
		/// </summary>
		/// <param name="site">The site to inspect.</param>
		/// <returns>true if the configuration files indicate this site should be ignored.</returns>
		public static bool IsOnIgnoreList(SiteContext site)
		{
			return IsOnIgnoreList(site.SiteInfo);
		}

		/// <summary>
		/// Used to determine if the Site should be crawled and a sitemap.xml should be generated.
		/// </summary>
		/// <param name="site">The site to inspect.</param>
		/// <returns>true if the configuration files indicate this site should be ignored.</returns>
		public static bool IsOnIgnoreList(SiteInfo site)
		{
			return SitemapXmlConfiguration.Current.SitesToIgnore.Contains(site.Name);
		}

		/// <summary>
		/// Returns a sitemap.xml XmlDocument for the supplied site.
		/// </summary>
		/// <param name="site">The site to crawl.</param>
		/// <param name="forceRegenerate">Whether the document can be retrieved from cache, or whether a fresh document should be generated.</param>
		/// <returns>An XML document representing the supplied site's crawl-able links</returns>
		public static XmlDocument GetSitemap(SiteContext site, bool forceRegenerate = false)
		{
			return GetSitemap(site.SiteInfo, forceRegenerate);
		}

		/// <summary>
		/// Returns a sitemap.xml XmlDocument for the supplied site.
		/// </summary>
		/// <param name="site">The site to crawl.</param>
		/// <param name="forceRegenerate">Whether the document can be retrieved from cache, or whether a fresh document should be generated.</param>
		/// <returns>An XML document representing the supplied site's crawl-able links</returns>
		public static XmlDocument GetSitemap(SiteInfo site, bool forceRegenerate = false)
		{
			var key = typeof(SitemapRepository).FullName + site.Name;

			XmlDocument document = null;

			if (SitemapXmlConfiguration.Current.CacheEnabled)
			{
				document = CachingContext.Current.Get<XmlDocument>(key);
			}

			if (document == null || forceRegenerate)
			{
				var generator = new SitemapGenerator(site);

				document = generator.Generate();

				if (SitemapXmlConfiguration.Current.CacheEnabled)
				{
					var cacheTimeout = site.GetSitemapXmlCacheTimeout();
					CachingContext.Current.Add(key, document, DateTime.Now.AddMinutes(cacheTimeout));
				}
			}

			return document;
		}
	}
}
