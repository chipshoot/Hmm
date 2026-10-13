using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.Utility.Dal.Query;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;

using System.Text.Json;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Domain-to-DTO mappings for the cheatsheet area.
    ///
    /// Every JSON-carrying member is mapped with an explicit delegate rather
    /// than by convention: JsonElement is a struct AutoMapper would otherwise
    /// try to map member-by-member, and the two JSON stacks have no common
    /// representation. The delegates route through CheatsheetJsonInterop, which
    /// converts via raw text and therefore cannot lose anything.
    /// </summary>
    public class CheatsheetMappingProfile : Profile
    {
        public CheatsheetMappingProfile()
        {
            CreateMap<CheatsheetSource, ApiCheatsheetSource>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)));

            CreateMap<ApiCheatsheetSource, CheatsheetSource>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            CreateMap<CheatsheetRow, ApiCheatsheetRow>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)))
                .ForMember(d => d.Raw, opt => opt.MapFrom(
                    (src, dest) => src.RawJson.HasValue
                        ? CheatsheetJsonInterop.ToJToken(src.RawJson.Value)
                        : null))
                .AfterMap((src, dest) =>
                {
                    if (dest.Raw != null)
                    {
                        return;
                    }

                    if (dest.Emoji == null &&
                        dest.ExtraFields.TryGetValue(LegacyEmoji, out var legacy) &&
                        legacy.Type == JTokenType.String)
                    {
                        dest.Emoji = legacy.Value<string>();
                    }

                    if (dest.Emoji != null)
                    {
                        dest.ExtraFields.Remove(LegacyEmoji);
                        dest.ExtraFields.Remove(LegacyEmojiLower);
                    }
                });

            CreateMap<ApiCheatsheetRow, CheatsheetRow>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)))
                .ForMember(d => d.RawJson, opt => opt.MapFrom(
                    (src, dest) => src.Raw == null
                        ? (System.Text.Json.JsonElement?)null
                        : CheatsheetJsonInterop.ToJsonElement(src.Raw)));

            // Never a typed Style AND an extra keyed "Style" on the wire: the
            // response would carry the key twice and a last-key-wins decoder
            // (Dart's jsonDecode) would show the stale, extra one.
            CreateMap<CheatsheetCard, ApiCheatsheet>()
                .ForMember(d => d.Links, opt => opt.Ignore())
                .ForMember(d => d.Style, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokenOrNull(src.Style)))
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)))
                .AfterMap((src, dest) =>
                {
                    if (dest.Style == null &&
                        dest.ExtraFields.TryGetValue(LegacyStyle, out var legacy) &&
                        legacy.Type != JTokenType.Null)
                    {
                        dest.Style = legacy;
                    }

                    if (dest.Style != null)
                    {
                        dest.ExtraFields.Remove(LegacyStyle);
                        dest.ExtraFields.Remove(LegacyStyleLower);
                    }
                });

            CreateMap<ApiCheatsheet, CheatsheetCard>()
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.Style, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElementOrNull(src.Style)))
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            CreateMap<ApiCheatsheetForCreate, CheatsheetCard>()
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.Style, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElementOrNull(src.Style)))
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            // PUT replaces content only. Id, NoteId and AuthorId are identity:
            // they come from the route and the authenticated author, never from
            // a request body that could disagree with them. Style follows the
            // MergeExtras philosophy: a body that never mentions it (an older
            // client) keeps the stored one; an explicit null clears it.
            CreateMap<ApiCheatsheetForUpdate, CheatsheetCard>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.Style, opt => opt.MapFrom(
                    (src, dest) => src.StyleSpecified
                        ? CheatsheetJsonInterop.ToJsonElementOrNull(src.Style)
                        : dest.Style))
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => MergeExtras(dest.ExtraFields, src.ExtraFields)))
                .AfterMap((src, dest) => DropLegacyStyle(src.StyleSpecified, dest));

            CreateMap<PageList<CheatsheetCard>, PageList<ApiCheatsheet>>()
                .ConvertUsing(new PageListConverter<CheatsheetCard, ApiCheatsheet>());
        }

        // PascalCase keys an older server stored verbatim as unknown extras when
        // a styling client sent them before typed Style/Emoji existed. The
        // lower-case guards cover a camelCase writer's key surviving as extra.
        private const string LegacyStyle = "Style";
        private const string LegacyStyleLower = "style";
        private const string LegacyEmoji = "Emoji";
        private const string LegacyEmojiLower = "emoji";

        /// <summary>
        /// A stored card may still carry a legacy "Style" extra (written by the
        /// pre-Style server). MergeExtras would otherwise keep it forever, so the
        /// write that follows a PUT drops it: promoted into the typed Style when
        /// the request said nothing about Style and none is stored, replaced
        /// (dropped) when the request carried a Style - a value or a reset.
        /// </summary>
        private static void DropLegacyStyle(bool styleSpecified, CheatsheetCard dest)
        {
            if (dest.ExtraFields == null ||
                !dest.ExtraFields.TryGetValue(LegacyStyle, out var legacy))
            {
                return;
            }

            if (!styleSpecified && !dest.Style.HasValue)
            {
                if (legacy.ValueKind == JsonValueKind.Null)
                {
                    return;
                }

                dest.Style = legacy;
            }

            dest.ExtraFields.Remove(LegacyStyle);
        }

        /// <summary>
        /// Merges request extras over the stored ones instead of replacing them.
        /// This map runs ONTO a card already loaded from storage, so replacing
        /// meant any client that does not model card-level extras deleted them
        /// simply by not mentioning them - the exact silent loss the rest of
        /// this module works to prevent. The trade-off is that an unknown field
        /// cannot be removed through the API; keeping data the server cannot
        /// interpret beats deleting it on a client's silence.
        /// </summary>
        private static IDictionary<string, JsonElement> MergeExtras(
            IDictionary<string, JsonElement> stored,
            IDictionary<string, JToken> incoming)
        {
            var merged = stored == null
                ? new Dictionary<string, JsonElement>()
                : new Dictionary<string, JsonElement>(stored);

            foreach (var extra in CheatsheetJsonInterop.ToJsonElements(incoming))
            {
                merged[extra.Key] = extra.Value;
            }

            return merged;
        }

    }
}
