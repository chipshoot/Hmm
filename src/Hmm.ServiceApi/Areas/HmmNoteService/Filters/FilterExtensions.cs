using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
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

        public static (string prevPageLink, string nextPageLink) CreatePaginationLinks(this PageList<ApiAuthor> authors, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return (null, null);
            }

            var prevPageLink = authors.HasPrevPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetAuthors", new
            {
                pageNumber = authors.CurrentPage - 1,
                pageSize = authors.PageSize,
            }) : null;
            var nextPageLink = authors.HasNextPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetAuthors", new
            {
                pageNumber = authors.CurrentPage + 1,
                pageSize = authors.PageSize,
            }) : null;

            return (prevPageLink, nextPageLink);
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

        public static (string prevPageLink, string nextPageLink) CreatePaginationLinks(this PageList<ApiNoteCatalog> catalogs, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return (null, null);
            }

            var prevPageLink = catalogs.HasPrevPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteCatalogs", new
            {
                pageNumber = catalogs.CurrentPage - 1,
                pageSize = catalogs.PageSize,
            }) : null;
            var nextPageLink = catalogs.HasNextPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteCatalogs", new
            {
                pageNumber = catalogs.CurrentPage + 1,
                pageSize = catalogs.PageSize,
            }) : null;

            return (prevPageLink, nextPageLink);
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

        public static (string prevPageLink, string nextPageLink) CreatePaginationLinks(this PageList<ApiNoteRender> renders, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return (null, null);
            }

            var prevPageLink = renders.HasPrevPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteRenders", new
            {
                pageNumber = renders.CurrentPage - 1,
                pageSize = renders.PageSize,
            }) : null;
            var nextPageLink = renders.HasNextPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNoteRenders", new
            {
                pageNumber = renders.CurrentPage + 1,
                pageSize = renders.PageSize,
            }) : null;

            return (prevPageLink, nextPageLink);
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

        public static (string prevPageLink, string nextPageLink) CreatePaginationLinks(this PageList<ApiSubsystem> systems, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return (null, null);
            }

            var prevPageLink = systems.HasPrevPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetSubsystems", new
            {
                pageNumber = systems.CurrentPage - 1,
                pageSize = systems.PageSize,
            }) : null;
            var nextPageLink = systems.HasNextPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetSubsystems", new
            {
                pageNumber = systems.CurrentPage + 1,
                pageSize = systems.PageSize,
            }) : null;

            return (prevPageLink, nextPageLink);
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

        public static (string prevPageLink, string nextPageLink) CreatePaginationLinks(this PageList<ApiNote> notes, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return (null, null);
            }

            var prevPageLink = notes.HasPrevPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNotes", new
            {
                pageNumber = notes.CurrentPage - 1,
                pageSize = notes.PageSize,
            }) : null;
            var nextPageLink = notes.HasNextPage ? linkGen.GetUriByRouteValues(context.HttpContext, "GetNotes", new
            {
                pageNumber = notes.CurrentPage + 1,
                pageSize = notes.PageSize,
            }) : null;

            return (prevPageLink, nextPageLink);
        }
    }
}