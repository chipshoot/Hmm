using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters
{
    public static class FilterExtensions
    {
        /// <summary>
        /// Cache for all public properties per type. Used by ShapeData methods.
        /// Key: Type, Value: Array of all public instance PropertyInfo
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> AllPropertiesCache = new();

        /// <summary>
        /// Cache for individual property lookups per type.
        /// Key: (Type, PropertyName lowercase), Value: PropertyInfo or null if not found
        /// </summary>
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache = new();

        /// <summary>
        /// Gets all public instance properties for a type, using cache for performance.
        /// </summary>
        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            return AllPropertiesCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Gets a specific property by name for a type, using cache for performance.
        /// </summary>
        private static PropertyInfo GetCachedProperty(Type type, string propertyName)
        {
            var key = (type, propertyName.ToLowerInvariant());
            return PropertyCache.GetOrAdd(key, k =>
                k.Item1.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
        }

        public static void CreateLinks(this ApiAuthor author, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAuthorById", new { id = author.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddAuthor",
                    Rel = "create_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddAuthor"),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateAuthor",
                    Rel = "update_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAuthor", new {id=author.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchAuthor",
                    Rel = "patch_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAuthor", new {id=author.Id}),
                    Method = "PATCH"
                }
            };

            author.Links = links;
        }

        public static void CreateLinks(this ApiContact contact, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetContactById", new { id = contact.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddContact",
                    Rel = "create_contact",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddContact"),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateContact",
                    Rel = "update_contact",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateContact", new {id=contact.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchContact",
                    Rel = "patch_contact",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchContact", new {id=contact.Id}),
                    Method = "PATCH"
                }
            };

            contact.Links = links;
        }

        public static void CreateLinks(this ApiNoteCatalog catalog, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteCatalogById", new { id = catalog.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddNoteCatalog",
                    Rel = "create_noteCatalog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddNoteCatalog"),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateNoteCatalog",
                    Rel = "update_noteCatalog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateNoteCatalog", new {id=catalog.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchNoteCatalog",
                    Rel = "patch_noteCatalog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchNoteCatalog", new {id=catalog.Id}),
                    Method = "PATCH"
                }
            };

            catalog.Links = links;
        }

        public static void CreateLinks(this ApiNote note, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteById", new { id = note.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddNote",
                    Rel = "create_note",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddNote"),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateNote",
                    Rel = "update_note",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateNote", new { id = note.Id }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchNote",
                    Rel = "patch_note",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchNote", new { id = note.Id }),
                    Method = "PATCH"
                }
            };

            // child
            var authorId = note.AuthorId;
            if (authorId > 0)
            {
                links.Add(
                    new Link
                    {
                        Title = "Author",
                        Rel = "get_author",
                        Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAuthorById", new { id = authorId }),
                        Method = "Get"
                    });
            }

            var catalogId = note.CatalogId;
            if (catalogId > 0)
            {
                links.Add(
                    new Link
                    {
                        Title = "NoteCatalog",
                        Rel = "get_noteCatalog",
                        Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteCatalogById", new { id = catalogId }),
                        Method = "Get"
                    });
            }

            note.Links = links;
        }

        public static void CreateLinks(this ApiTag tag, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetTagById", new { id = tag.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddTag",
                    Rel = "create_tag",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddTag"),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateTaq",
                    Rel = "update_Tag",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateTag", new { id = tag.Id }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchTag",
                    Rel = "patch_Tag",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchTag", new { id = tag.Id }),
                    Method = "PATCH"
                }
            };

            tag.Links = links;
        }

        public static PageList<ExpandoObject> ShapeData<T>(this PageList<T> source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // create a list to hold our ExpandoObjects
            var expandoObjectList = new List<ExpandoObject>();

            // Get PropertyInfo objects using cached reflection for performance
            var propertyInfoList = new List<PropertyInfo>();
            var sourceType = typeof(T);

            if (string.IsNullOrWhiteSpace(fields))
            {
                // all public properties should be in the ExpandoObject (using cache)
                propertyInfoList.AddRange(GetCachedProperties(sourceType));
            }
            else
            {
                // the field are separated by ",", so we split it.
                var fieldsAfterSplit = fields.Split(',');

                foreach (var field in fieldsAfterSplit)
                {
                    // trim each field, as it might contain leading
                    // or trailing spaces. Can't trim the var in for each,
                    // so use another var.
                    var propertyName = field.Trim();

                    // use cached reflection to get the property on the source object
                    var propertyInfo = GetCachedProperty(sourceType, propertyName);

                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} wasn't found on {sourceType}");
                    }

                    // add propertyInfo to list
                    propertyInfoList.Add(propertyInfo);
                }

                // check if source contains links property (using cache)
                var linksInfo = GetCachedProperty(sourceType, nameof(ApiEntity.Links));
                if (linksInfo != null)
                {
                    propertyInfoList.Add(linksInfo);
                }
            }

            // run through the source objects
            foreach (var sourceObject in source)
            {
                // create an ExpandoObject that will hold the
                // selected properties & values
                var dataShapedObject = new ExpandoObject();

                // Get the value of each property we have to return.  For that,
                // we run through the list
                foreach (var propertyInfo in propertyInfoList)
                {
                    // GetValue returns the value of the property on the source object
                    var propertyValue = propertyInfo.GetValue(sourceObject);

                    // add the field to the ExpandoObject
                    ((IDictionary<string, object>)dataShapedObject)
                        .Add(propertyInfo.Name, propertyValue);
                }

                // add the ExpandoObject to the list
                expandoObjectList.Add(dataShapedObject);
            }

            // return the Page list
            // Use TotalCount (not Count) to preserve pagination metadata correctly
            return new PageList<ExpandoObject>(expandoObjectList.AsEnumerable(), source.TotalCount, source.CurrentPage, source.PageSize);
        }

        public static ExpandoObject ShapeData<T>(this T source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var dataShapedObject = new ExpandoObject();
            var sourceType = typeof(T);

            if (string.IsNullOrWhiteSpace(fields))
            {
                // all public properties should be in the ExpandoObject (using cache)
                var propertyInfos = GetCachedProperties(sourceType);

                foreach (var propertyInfo in propertyInfos)
                {
                    // remove links property, because it's will show as links section
                    if (propertyInfo.Name == "Links")
                    {
                        continue;
                    }

                    // get the value of the property on the source object
                    var propertyValue = propertyInfo.GetValue(source);

                    // add the field to the ExpandoObject
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                return dataShapedObject;
            }

            // the field is separated by ",", so we split it.
            var fieldsAfterSplit = fields.Split(',');

            foreach (var field in fieldsAfterSplit)
            {
                // trim each field, as it might contain leading
                // or trailing spaces. Can't trim the var in for each,
                // so use another var.
                var propertyName = field.Trim();

                // use cached reflection to get the property on the source object
                var propertyInfo = GetCachedProperty(sourceType, propertyName);

                if (propertyInfo == null)
                {
                    throw new Exception($"Property {propertyName} wasn't found on {sourceType}");
                }

                // get the value of the property on the source object
                var propertyValue = propertyInfo.GetValue(source);

                // add the field to the ExpandoObject
                ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            // return the list
            return dataShapedObject;
        }

        public static List<Link> CreatePaginationLinks<T>(this PageList<T> records, string routName, ActionContext context, LinkGenerator linkGen, ResourceCollectionParameters resourceCollectionParameters)
        {
            var links = new List<Link>();
            if (context == null || linkGen == null)
            {
                return links;
            }

            var orderBy = resourceCollectionParameters != null ? resourceCollectionParameters.OrderBy : string.Empty;
            var fields = resourceCollectionParameters != null ? resourceCollectionParameters.Fields : string.Empty;

            links.Add(new Link
            {
                Title = routName,
                Rel = "self",
                Href = linkGen.GetUriByRouteValues(context.HttpContext, routName, new { pageNumber = records.CurrentPage, records.PageSize, orderBy, fields }),
                Method = "Get"
            });

            if (records.HasPrevPage)
            {
                links.Add(new Link
                {
                    Title = routName,
                    Rel = "prev_page",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, routName, new { pageNumber = records.CurrentPage - 1, pageSize = records.PageSize, orderBy, fields }),
                    Method = "Get"
                });
            }

            if (records.HasNextPage)
            {
                links.Add(new Link
                {
                    Title = routName,
                    Rel = "next_page",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, routName, new { pageNumber = records.CurrentPage + 1, pageSize = records.PageSize, orderBy, fields }),
                    Method = "Get"
                });
            }

            return links;
        }
    }
}