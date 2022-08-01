using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters
{
    public static class FilterExtensions
    {
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddAuthor", null),
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddNoteCatalog", null),
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

        public static void CreateLinks(this ApiNoteRender render, ActionContext context, LinkGenerator linkGen)
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteRenderById", new { id = render.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddNoteRender",
                    Rel = "create_noteRender",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddNoteRender", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateNoteRender",
                    Rel = "update_noteRender",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateNoteRender", new {id=render.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchNoteRender",
                    Rel = "patch_noteRender",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchNoteRender", new {id=render.Id}),
                    Method = "PATCH"
                }
            };

            render.Links = links;
        }

        public static void CreateLinks(this ApiSubsystem system, ActionContext context, LinkGenerator linkGen)
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetSubsystemById", new { id = system.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddSubsystem",
                    Rel = "create_subsystem",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddSubsystem", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateSubsystem",
                    Rel = "update_subsystem",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateSubsystem", new { id = system.Id }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchSubsystem",
                    Rel = "patch_subsystem",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchSubsystem", new { id = system.Id }),
                    Method = "PATCH"
                }
            };

            // child
            var authorId = system.DefaultAuthorId;
            if (authorId != Guid.Empty)
            {
                links.Add(
                    new Link
                    {
                        Title = "Author",
                        Rel = "get_defaultAuthor",
                        Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAuthorById", new { id = authorId }),
                        Method = "Get"
                    });
            }

            if (system.NoteCatalogIds != null && system.NoteCatalogIds.Any())
            {
                var catalogList = system.NoteCatalogIds.ToList();
                links.AddRange(catalogList.Select(catId => new Link
                {
                    Title = "NoteCatalog",
                    Rel = "get_childNoteCatalog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteCatalogById", new { id = catId }),
                    Method = "Get"
                }));
            }

            system.Links = links;
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddNote", null),
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
            if (authorId != Guid.Empty)
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

        public static PageList<ExpandoObject> ShapeData<T>(this PageList<T> source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // create a list to hold our ExpandoObjects
            var expandoObjectList = new List<ExpandoObject>();

            // create a list with PropertyInfo objects on TSource.  Reflection is
            // expensive, so rather than doing it for each object in the list, we do
            // it once and reuse the results.  After all, part of the reflection is on the
            // type of the object (TSource), not on the instance
            var propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                // all public properties should be in the ExpandoObject
                var propertyInfos = typeof(T)
                        .GetProperties(BindingFlags.IgnoreCase
                        | BindingFlags.Public | BindingFlags.Instance);

                propertyInfoList.AddRange(propertyInfos);
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

                    // use reflection to get the property on the source object
                    // we need to include public and instance, b/c specifying a binding
                    // flag overwrites the already-existing binding flags.
                    var propertyInfo = typeof(T)
                        .GetProperty(propertyName, BindingFlags.IgnoreCase |
                        BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} wasn't found on {typeof(T)}");
                    }

                    // add propertyInfo to list
                    propertyInfoList.Add(propertyInfo);
                }

                // check if source contains links property
                var linksInfo = typeof(T).GetProperty(nameof(ApiEntity.Links), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
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
            return new PageList<ExpandoObject>(expandoObjectList.AsEnumerable(), source.Count, source.CurrentPage, source.PageSize);
        }

        public static ExpandoObject ShapeData<T>(this T source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var dataShapedObject = new ExpandoObject();

            if (string.IsNullOrWhiteSpace(fields))
            {
                // all public properties should be in the ExpandoObject
                var propertyInfos = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                foreach (var propertyInfo in propertyInfos)
                {
                    // remove links property, because it's will showing as links section
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

            // the field are separated by ",", so we split it.
            var fieldsAfterSplit = fields.Split(',');

            foreach (var field in fieldsAfterSplit)
            {
                // trim each field, as it might contain leading
                // or trailing spaces. Can't trim the var in for each,
                // so use another var.
                var propertyName = field.Trim();

                // use reflection to get the property on the source object
                // we need to include public and instance, b/c specifying a
                // binding flag overwrites the already-existing binding flags.
                var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    throw new Exception($"Property {propertyName} wasn't found on {typeof(T)}");
                }

                // get the value of the property on the source object
                var propertyValue = propertyInfo.GetValue(source);

                // add the field to the ExpandoObject
                ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            // return the list
            return dataShapedObject;
        }
    }
}