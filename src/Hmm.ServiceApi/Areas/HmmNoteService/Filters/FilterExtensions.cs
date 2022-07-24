using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}