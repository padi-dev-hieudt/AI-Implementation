using AutoMapper;
using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Models.Entities;

namespace ForumWebsite.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ─── Post → PostDto ───────────────────────────────────────────────────
            // Username is denormalised from the User navigation property.
            // CommentCount counts only non-deleted comments in the loaded collection.
            CreateMap<Post, PostDto>()
                .ForMember(d => d.Username,
                    opt => opt.MapFrom(s => s.User != null ? s.User.Username : string.Empty))
                .ForMember(d => d.CommentCount,
                    opt => opt.MapFrom(s => s.Comments.Count(c => !c.IsDeleted)));

            // ─── Post → PostDetailDto ─────────────────────────────────────────────
            // Inherits the base mappings above and additionally maps the Comments list.
            // AutoMapper will use the Comment → CommentDto map for items in the list.
            CreateMap<Post, PostDetailDto>()
                .ForMember(d => d.Username,
                    opt => opt.MapFrom(s => s.User != null ? s.User.Username : string.Empty))
                .ForMember(d => d.CommentCount,
                    opt => opt.MapFrom(s => s.Comments.Count(c => !c.IsDeleted)))
                .ForMember(d => d.Comments,
                    opt => opt.MapFrom(s => s.Comments.Where(c => !c.IsDeleted)));

            // ─── Comment → CommentDto ─────────────────────────────────────────────
            CreateMap<Comment, CommentDto>()
                .ForMember(d => d.Username,
                    opt => opt.MapFrom(s => s.User != null ? s.User.Username : string.Empty));
        }
    }
}
