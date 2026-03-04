using AutoMapper;
using ForumWebsite.Models.DTOs.Category;
using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Models.DTOs.Tag;
using ForumWebsite.Models.Entities;

namespace ForumWebsite.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ─── Category → CategoryDto ───────────────────────────────────────────
            CreateMap<Category, CategoryDto>()
                .ForMember(d => d.PostCount,
                    opt => opt.MapFrom(s => s.Posts.Count(p => !p.IsDeleted)));

            // ─── Tag → TagDto ─────────────────────────────────────────────────────
            // Posts is only populated when explicitly included; otherwise Count() = 0.
            CreateMap<Tag, TagDto>()
                .ForMember(d => d.PostCount,
                    opt => opt.MapFrom(s => s.Posts.Count(p => !p.IsDeleted)));

            // ─── Post → PostDto ───────────────────────────────────────────────────
            CreateMap<Post, PostDto>()
                .ForMember(d => d.Username,
                    opt => opt.MapFrom(s => s.User != null ? s.User.Username : string.Empty))
                .ForMember(d => d.CategoryName,
                    opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
                .ForMember(d => d.Tags,
                    opt => opt.MapFrom(s => s.Tags))
                .ForMember(d => d.CommentCount,
                    opt => opt.MapFrom(s => s.Comments.Count(c => !c.IsDeleted)));

            // ─── Post → PostDetailDto ─────────────────────────────────────────────
            CreateMap<Post, PostDetailDto>()
                .ForMember(d => d.Username,
                    opt => opt.MapFrom(s => s.User != null ? s.User.Username : string.Empty))
                .ForMember(d => d.CategoryName,
                    opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
                .ForMember(d => d.Tags,
                    opt => opt.MapFrom(s => s.Tags))
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
