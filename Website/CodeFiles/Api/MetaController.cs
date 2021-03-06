﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using GalleryServerPro.Business;
using GalleryServerPro.Business.Metadata;
using GalleryServerPro.Events.CustomExceptions;
using GalleryServerPro.Web.Controller;

namespace GalleryServerPro.Web.Api
{
	/// <summary>
	/// Contains methods for Web API access to metadata.
	/// </summary>
	public class MetaController : ApiController
	{
		#region Methods

		/// <summary>
		/// Gets a list of tags the current user can view. Guaranteed to not return null.
		/// </summary>
		/// <returns>IEnumerable{Tag}.</returns>
		/// <exception cref="System.Web.Http.HttpResponseException">Thrown when an error occurs.</exception>
		[ActionName("Tags")]
		public IEnumerable<Entity.Tag> GetTags()
		{
			try
			{
				return MetadataController.GetTags(GetTagSearchOptions(TagSearchType.TagsUserCanView));
			}
			catch (Exception ex)
			{
				AppEventController.LogError(ex);

				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = Utils.GetExStringContent(ex),
					ReasonPhrase = "Server Error"
				});
			}
		}

		/// <summary>
		/// Gets a list of people the current user can view. Guaranteed to not return null.
		/// </summary>
		/// <returns>IEnumerable{Tag}.</returns>
		/// <exception cref="System.Web.Http.HttpResponseException">Thrown when an error occurs.</exception>
		[ActionName("People")]
		public IEnumerable<Entity.Tag> GetPeople()
		{
			try
			{
				return MetadataController.GetTags(GetTagSearchOptions(TagSearchType.PeopleUserCanView));
			}
			catch (Exception ex)
			{
				AppEventController.LogError(ex);

				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = Utils.GetExStringContent(ex),
					ReasonPhrase = "Server Error"
				});
			}
		}

		/// <summary>
		/// Persists the metadata item to the data store. The current implementation requires that
		/// an existing item exist in the data store and only stores the contents of the
		/// <see cref="Entity.MetaItem.Value" /> property.
		/// </summary>
		/// <param name="metaItem">An instance of <see cref="Entity.MetaItem" /> to persist to the data
		/// store.</param>
		/// <returns>Entity.MetaItem.</returns>
		/// <exception cref="System.Web.Http.HttpResponseException">Thrown when an album or media object associated
		/// with the meta item doesn't exist or an error occurs.</exception>
		public Entity.MetaItem PutMetaItem(Entity.MetaItem metaItem)
		{
			try
			{
				return MetadataController.Save(metaItem);
			}
			catch (InvalidAlbumException)
			{
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
					                                {
						                                Content = new StringContent(String.Format("Could not find album with ID {0}", metaItem.MediaId)),
						                                ReasonPhrase = "Album Not Found"
					                                });
			}
			catch (InvalidMediaObjectException)
			{
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
					                                {
						                                Content = new StringContent(String.Format("One of the following errors occurred: (1) Could not find meta item with ID {0} (2) Could not find media object with ID {1} ", metaItem.Id, metaItem.MediaId)),
						                                ReasonPhrase = "Media Object/Metadata Item Not Found"
					                                });
			}
			catch (GallerySecurityException)
			{
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Forbidden));
			}
			catch (Exception ex)
			{
				AppEventController.LogError(ex);

				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = Utils.GetExStringContent(ex),
					ReasonPhrase = "Server Error"
				});
			}
		}

		/// <summary>
		/// Rebuilds the meta name having ID <paramref name="metaNameId" /> for all items in the gallery having ID 
		/// <paramref name="galleryId" />. The action is executed asyncronously and returns immediately.
		/// </summary>
		/// <param name="metaNameId">ID of the meta item. This must match the enumeration value of <see cref="MetadataItemName" />.</param>
		/// <param name="galleryId">The gallery ID.</param>
		/// <exception cref="System.Web.Http.HttpResponseException">Thrown when an error occurs.</exception>
		[HttpPost]
		[ActionName("RebuildMetaItem")]
		public void RebuildItemForGallery(int metaNameId, int galleryId)
		{
			try
			{
				if (Utils.IsCurrentUserGalleryAdministrator(galleryId))
				{
					var metaName = (MetadataItemName) metaNameId;
					if (MetadataItemNameEnumHelper.IsValidFormattedMetadataItemName(metaName))
					{
						MetadataController.RebuildItemForGalleryAsync(metaName, galleryId);
					}
				}
			}
			catch (Exception ex)
			{
				AppEventController.LogError(ex);

				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = Utils.GetExStringContent(ex),
					ReasonPhrase = "Server Error"
				});
			}
		}

		#endregion

		#region Functions

		private static TagSearchOptions GetTagSearchOptions(TagSearchType searchType)
		{
			return new TagSearchOptions
				       {
					       GalleryId = Utils.GetQueryStringParameterInt32("galleryId"),
					       SearchType = searchType,
								 SearchTerm = Utils.GetQueryStringParameterString("q"),
								 IsUserAuthenticated = Utils.IsAuthenticated,
					       Roles = RoleController.GetGalleryServerRolesForUser()
				       };
		}

		// WARNING: Given the current API, there is no way to verify the user has permission to 
		// view the specified meta ID, so we'll comment out this method to ensure it isn't used.
		///// <summary>
		///// Gets the meta item with the specified <paramref name="id" />.
		///// Example: api/meta/4/
		///// </summary>
		///// <param name="id">The value that uniquely identifies the metadata item.</param>
		///// <returns>An instance of <see cref="Entity.MetaItem" />.</returns>
		///// <exception cref="System.Web.Http.HttpResponseException"></exception>
		//public Entity.MetaItem Get(int id)
		//{
		//	try
		//	{
		//		return MetadataController.Get(id);
		//	}
		//	catch (InvalidMediaObjectException)
		//	{
		//		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
		//		{
		//			Content = new StringContent(String.Format("Could not find meta item with ID = {0}", id)),
		//			ReasonPhrase = "Media Object Not Found"
		//		});
		//	}
		//	catch (GallerySecurityException)
		//	{
		//		throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Forbidden));
		//	}
		//}

		#endregion
	}
}