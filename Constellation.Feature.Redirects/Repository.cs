﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Constellation.Feature.Redirects.Models;
using Constellation.Foundation.ModelMapping;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web;
using FieldIDs = Constellation.Feature.Redirects.Data.FieldIDs;
using ItemIDs = Constellation.Feature.Redirects.Data.ItemIDs;

namespace Constellation.Feature.Redirects
{
	/// <summary>
	/// Provides access to the SQL redirect database.
	/// </summary>
	public class Repository
	{
		/// <summary>
		/// Creates a new instance of Repository
		/// </summary>
		/// <param name="database">The database to use for storage</param>
		/// <param name="indexName">The index to use for lookups</param>
		public Repository(Database database, string indexName)
		{
			Database = database;
			Index = ContentSearchManager.GetIndex(indexName);
		}

		/// <summary>
		/// Creates a new instance of Repository
		/// </summary>
		/// <param name="database">The database to use for storage</param>
		/// <param name="index">The name of the index to use for lookups</param>
		public Repository(Database database, ISearchIndex index)
		{
			Database = database;
			Index = index;
		}

		/// <summary>
		/// Gets the Database to use for storage.
		/// </summary>
		protected Database Database { get; }



		/// <summary>
		/// Gets the current instance of ISearchIndex
		/// </summary>
		protected ISearchIndex Index { get; }


		/// <summary>
		/// Gets all redirects.
		/// </summary>
		/// <returns></returns>
		public List<MarketingRedirect> GetAll()
		{
			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				IQueryable<MarketingRedirect> query = context.GetQueryable<MarketingRedirect>();
				query = query.Filter(i => i.Paths.Contains(ItemIDs.MarketingRedirectBucketID))
					.Filter(i => i.TemplateId == ItemIDs.MarketingRedirectTemplateID);
				return query.ToList();
			}
		}

		/// <summary>
		/// Finds a redirect by id.
		/// </summary>
		public MarketingRedirect GetById(string id)
		{
			Item redirect = Database.GetItem(id);
			return MappingContext.Current.MapItemToNew<MarketingRedirect>(redirect);
		}

		/// <summary>
		/// Deletes a redirect by ID.
		/// </summary>
		public void Delete(string id)
		{
			Item redirect = Database.GetItem(id);

			if (redirect == null)
			{
				return;
			}

			if (Sitecore.Configuration.Settings.RecycleBinActive)
			{
				redirect.Recycle();
			}
			else
			{
				redirect.Delete();
			}
		}

		/// <summary>
		/// Deletes all redirects.
		/// </summary>
		public void DeleteAll()
		{
			Item redirectBucket = Database.GetItem(ItemIDs.MarketingRedirectBucketID);
			redirectBucket?.DeleteChildren();
		}

		/// <summary>
		/// Inserts a new redirect.
		/// </summary>
		public ID Insert(MarketingRedirect record)
		{
			return Insert(record.SiteName, record.OldUrl, record.NewUrl, record.IsPermanent);
		}

		/// <summary>
		/// Inserts a new redirect.
		/// </summary>
		public ID Insert(string siteName, string oldUrl, string newUrl, bool type)
		{
			if (string.IsNullOrWhiteSpace(siteName))
			{
				throw new ArgumentException("siteName");
			}

			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			Item redirectBucket = Database.GetItem(ItemIDs.MarketingRedirectBucketID);

			if (redirectBucket == null)
			{
				return null;
			}

			var itemName = siteName + Regex.Replace(oldUrl, "\\W", "-");

			if (itemName.Length > 100)
			{
				itemName = itemName.Remove(99);
			}

			Item newItem = redirectBucket.Add(itemName, new TemplateID(ItemIDs.MarketingRedirectTemplateID));
			newItem.Editing.BeginEdit();
			newItem.Fields[FieldIDs.SiteName].Value = siteName;
			newItem.Fields[FieldIDs.OldUrl].Value = oldUrl;
			newItem.Fields[FieldIDs.NewUrl].Value = newUrl;
			newItem.Fields[FieldIDs.IsPermanent].Value = Convert.ToInt32(type).ToString();
			newItem.Editing.EndEdit();

			return newItem.ID;
		}

		/// <summary>
		/// Updates an existing redirect.
		/// </summary>
		public void Update(MarketingRedirect changes)
		{
			Update(changes.ItemId, changes.SiteName, changes.OldUrl, changes.NewUrl, changes.IsPermanent);
		}

		/// <summary>
		/// Updates an existing redirect.
		/// </summary>
		public void Update(ID id, string siteName, string oldUrl, string newUrl, bool type)
		{
			if (string.IsNullOrWhiteSpace(oldUrl))
			{
				throw new ArgumentException("oldUrl");
			}

			if (string.IsNullOrWhiteSpace(newUrl))
			{
				throw new ArgumentException("newUrl");
			}

			if (string.IsNullOrWhiteSpace(siteName))
			{
				throw new ArgumentException("siteName");
			}

			Item redirect = Database.GetItem(id);

			if (redirect == null)
			{
				return;
			}

			redirect.Editing.BeginEdit();
			redirect.Fields[FieldIDs.SiteName].Value = siteName;
			redirect.Fields[FieldIDs.OldUrl].Value = oldUrl;
			redirect.Fields[FieldIDs.NewUrl].Value = newUrl;
			redirect.Fields[FieldIDs.IsPermanent].Value = Convert.ToInt32(type).ToString();
			redirect.Editing.EndEdit();
		}

		/// <summary>
		/// Returns true if the supplied Marketing Redirect has a value for SiteName that matches a known Site definition in this Sitecore instance.
		/// </summary>
		/// <param name="candidate">the marketing redirect to inspect.</param>
		/// <returns>True if the value of Site Name is the name of a Site in this Sitecore instance.</returns>
		public bool CandidateHasValidSiteName(MarketingRedirect candidate)
		{
			if (string.IsNullOrEmpty(candidate.SiteName))
			{
				return false;
			}

			var site = Sitecore.Configuration.Factory.GetSite(candidate.SiteName);

			if (site == null)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns true if the supplied Marketing Redirect's Old Url field contains an absolute URL.
		/// </summary>
		/// <param name="candidate">The Marketing Redirect to inspect</param>
		/// <returns>True if Old Url contains a fully qualified URL</returns>
		public bool CandidateOldUrlContainsHostname(MarketingRedirect candidate)
		{
			string hostNameRegex = @"^([a-zA-Z]+:\/\/)?([^\/]+)\/.*?$";

			return Regex.IsMatch(candidate.OldUrl, hostNameRegex);
		}

		/// <summary>
		/// Returns true if the supplied Marketing Redirect's Old Url field has a value that is unique for the Marketing Redirect's Site.
		/// </summary>
		/// <param name="candidate">The Marketing Redirect to inspect</param>
		/// <returns>True if the Site + Old Url value pair does not exist in the database</returns>
		public bool CandidateIsUnique(MarketingRedirect candidate)
		{
			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				IQueryable<MarketingRedirect> query = context.GetQueryable<MarketingRedirect>();
				query = query.Filter(i => i.Paths.Contains(ItemIDs.MarketingRedirectBucketID))
					.Filter(i => i.TemplateId == ItemIDs.MarketingRedirectTemplateID)
					.Filter(i => i.SiteName == candidate.SiteName);


				if (ID.IsNullOrEmpty(candidate.ItemId))
				{
					return !query.Any(i => i.OldUrl == candidate.OldUrl);
				}

				return !query.Any(i => i.OldUrl == candidate.OldUrl && i.ItemId != candidate.ItemId);
			}
		}

		/// <summary>
		/// Returns true if the value of the New Url property of the supplied Marketing Redirect is the value of an Old Url field on another Marketing Redirect.
		/// </summary>
		/// <param name="candidate">The Marketing Redirect to inspect.</param>
		/// <returns>True if New Url matches an Old Url on another record for a given Site Name.</returns>
		public bool CandidateTargetIsRedirect(MarketingRedirect candidate)
		{
			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				IQueryable<MarketingRedirect> query = context.GetQueryable<MarketingRedirect>();
				query = query.Filter(i => i.Paths.Contains(ItemIDs.MarketingRedirectBucketID))
					.Filter(i => i.TemplateId == ItemIDs.MarketingRedirectTemplateID)
					.Filter(i => i.SiteName == candidate.SiteName);

				return query.Any(i => i.OldUrl == candidate.NewUrl);
			}
		}

		/// <summary>
		/// Returns true if the value of the New Url property of the supplied Marketing Redirect returns an Http status code of 200 when requested.
		/// </summary>
		/// <param name="candidate">The Marketing Redirect to inspect</param>
		/// <param name="status">The response status of the Http Request using the New Url provided.</param>
		/// <returns>the value of the status argument's Successful property.</returns>
		public bool CandidateTargetReturnsHttpSuccessResponse(MarketingRedirect candidate, out LinkVerifier.Status status)
		{
			status = new LinkVerifier().CheckLink(candidate);

			return status.Successful;
		}

		/// <summary>
		/// Gets the mapped redirect (i..e. new URL) for the given old / request url.
		/// </summary>
		public MarketingRedirect GetNewUrl(SiteInfo site, string requestUrl)
		{
			Assert.ArgumentNotNull(site, "site");
			Assert.ArgumentNotNullOrEmpty(requestUrl, "requestUrl");

			using (IProviderSearchContext context = Index.CreateSearchContext())
			{
				IQueryable<MarketingRedirect> query = context.GetQueryable<MarketingRedirect>();
				query = query.Filter(i => i.Paths.Contains(ItemIDs.MarketingRedirectBucketID))
					.Filter(i => i.TemplateId == ItemIDs.MarketingRedirectTemplateID)
					.Filter(i => i.SiteName == site.Name);


				return query.FirstOrDefault(i => i.OldUrl == requestUrl);
			}
		}
	}
}

