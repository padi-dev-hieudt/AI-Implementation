using ForumWebsite.Data.Context;
using ForumWebsite.Models.Entities;

namespace ForumWebsite.Data.Seed
{
    /// <summary>
    /// Seeds the database with realistic fake users, posts and comments.
    /// Only runs when the Posts table is empty — safe to call on every startup.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            // ── Default category — always ensure it exists ────────────────────────
            // Uses AnyAsync to avoid the synchronous .Any() on a thread-pool thread.
            if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AnyAsync(db.Categories))
            {
                db.Categories.AddRange(new List<Category>
                {
                    new() { Name = "Uncategorized", Description = "Default category for all posts.",
                            IsDefault = true,  SortOrder = 0, CreatedAt = DateTime.UtcNow },
                    new() { Name = "General",        Description = "General discussion.",
                            IsDefault = false, SortOrder = 1, CreatedAt = DateTime.UtcNow },
                    new() { Name = "Q&A",            Description = "Questions and answers.",
                            IsDefault = false, SortOrder = 2, CreatedAt = DateTime.UtcNow },
                    new() { Name = "Tutorial",       Description = "Guides and tutorials.",
                            IsDefault = false, SortOrder = 3, CreatedAt = DateTime.UtcNow },
                    new() { Name = "Announcement",   Description = "Official announcements.",
                            IsDefault = false, SortOrder = 4, CreatedAt = DateTime.UtcNow },
                });
                await db.SaveChangesAsync();
            }

            if (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AnyAsync(db.Posts)) return;

            // Resolve seeded category IDs by name
            var catGeneral  = db.Categories.First(c => c.Name == "General");
            var catQnA      = db.Categories.First(c => c.Name == "Q&A");
            var catTutorial = db.Categories.First(c => c.Name == "Tutorial");
            var catAnnounce = db.Categories.First(c => c.Name == "Announcement");

            // ── Users ────────────────────────────────────────────────────────────
            var users = new List<User>
            {
                new() {
                    Username     = "admin",
                    Email        = "admin@forum.dev",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                    Role         = UserRoles.Admin,
                    IsActive     = true,
                    CreatedAt    = Ago(days: 180)
                },
                new() {
                    Username     = "minh_dev",
                    Email        = "minh.dev@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                    Role         = UserRoles.User,
                    IsActive     = true,
                    CreatedAt    = Ago(days: 120)
                },
                new() {
                    Username     = "tuan_backend",
                    Email        = "tuan.backend@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                    Role         = UserRoles.User,
                    IsActive     = true,
                    CreatedAt    = Ago(days: 90)
                },
                new() {
                    Username     = "linh_fullstack",
                    Email        = "linh.fullstack@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                    Role         = UserRoles.User,
                    IsActive     = true,
                    CreatedAt    = Ago(days: 60)
                },
                new() {
                    Username     = "hung_junior",
                    Email        = "hung.junior@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 12),
                    Role         = UserRoles.User,
                    IsActive     = true,
                    CreatedAt    = Ago(days: 30)
                }
            };

            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            var adminUser   = users[0];
            var minhDev     = users[1];
            var tuanBack    = users[2];
            var linhFull    = users[3];
            var hungJunior  = users[4];

            // ── Posts ─────────────────────────────────────────────────────────────
            var posts = new List<Post>
            {
                new() {
                    Title      = "Giới thiệu bản thân và hành trình học lập trình của mình",
                    Content    = "Xin chào mọi người! Mình là Minh, hiện đang làm .NET Developer được 3 năm. " +
                                 "Mình bắt đầu học code từ năm 2020 với C# và ASP.NET Core. " +
                                 "Hành trình đầu tiên khá gian nan nhưng cộng đồng daynhauhoc đã giúp mình rất nhiều. " +
                                 "Hy vọng sẽ được học hỏi và chia sẻ nhiều hơn ở đây!",
                    UserId     = minhDev.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 312,
                    CreatedAt  = Ago(days: 100)
                },
                new() {
                    Title      = "[Hỏi] ASP.NET Core 6 vs .NET 7 - nên chọn cái nào cho dự án mới?",
                    Content    = "Mình đang bắt đầu một dự án web API mới cho công ty. " +
                                 "Sếp hỏi nên dùng .NET 6 LTS hay .NET 7/8 cho dự án production. " +
                                 "Các bạn có kinh nghiệm thực tế không? Dự án dự kiến maintain 3-5 năm. " +
                                 "Cảm ơn mọi người trước!",
                    UserId     = hungJunior.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 489,
                    CreatedAt  = Ago(days: 85)
                },
                new() {
                    Title     = "Chia sẻ: Cách mình tối ưu Entity Framework Core query tăng performance 10x",
                    Content   = "Sau nhiều tháng đau khổ với slow query trong EF Core, mình đã tìm ra một số trick hay:\n\n" +
                                "1. Dùng AsNoTracking() cho read-only queries\n" +
                                "2. Select chỉ những cột cần thiết thay vì load cả entity\n" +
                                "3. Dùng projection với anonymous type\n" +
                                "4. Tránh N+1 bằng cách dùng Include() đúng chỗ\n" +
                                "5. Sử dụng compiled queries cho hot paths\n\n" +
                                "Chi tiết từng điểm mình sẽ giải thích trong comment bên dưới.",
                    UserId     = minhDev.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 1024,
                    CreatedAt  = Ago(days: 75)
                },
                new() {
                    Title      = "[Thảo luận] Clean Architecture vs Layered Architecture - bạn chọn gì?",
                    Content    = "Mình thấy có nhiều tranh luận về việc dùng Clean Architecture vs traditional N-tier. " +
                                 "Ở công ty mình đang dùng 3-layer (Controller > Service > Repository) và khá ổn. " +
                                 "Nhưng mình tò mò liệu Clean Architecture có thực sự worth it không " +
                                 "hay chỉ là over-engineering? " +
                                 "Mọi người đang dùng pattern gì trong dự án thực tế?",
                    UserId     = tuanBack.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 756,
                    CreatedAt  = Ago(days: 70)
                },
                new() {
                    Title      = "JWT Authentication trong ASP.NET Core - Hướng dẫn từ A đến Z",
                    Content    = "Bài viết này mình sẽ hướng dẫn chi tiết cách implement JWT Authentication:\n\n" +
                                 "**Setup cơ bản:**\n" +
                                 "- Cài package Microsoft.AspNetCore.Authentication.JwtBearer\n" +
                                 "- Configure trong Program.cs\n" +
                                 "- Tạo JwtService để generate token\n\n" +
                                 "**Bảo mật nâng cao:**\n" +
                                 "- Dùng HttpOnly cookie thay vì localStorage\n" +
                                 "- Set ClockSkew = Zero\n" +
                                 "- Implement refresh token\n\n" +
                                 "Mọi người có câu hỏi gì cứ hỏi nhé!",
                    UserId     = linhFull.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 2341,
                    CreatedAt  = Ago(days: 65)
                },
                new() {
                    Title      = "[Hỏi] Lỗi CORS khi call API từ React - đã thử nhiều cách vẫn không được",
                    Content    = "Mình bị lỗi này khi fetch từ React (localhost:3000) sang API .NET (localhost:5001):\n\n" +
                                 "Access to fetch at 'http://localhost:5001/api/posts' from origin " +
                                 "'http://localhost:3000' has been blocked by CORS policy\n\n" +
                                 "Mình đã thêm UseCors() trong Program.cs rồi nhưng vẫn lỗi. " +
                                 "Config của mình:\n" +
                                 "builder.Services.AddCors(o => o.AddPolicy(\"AllowAll\", b => b.AllowAnyOrigin()...));\n\n" +
                                 "Mọi người help mình với!",
                    UserId     = hungJunior.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 203,
                    CreatedAt  = Ago(days: 55)
                },
                new() {
                    Title      = "So sánh PostgreSQL vs SQL Server cho ứng dụng .NET - Kinh nghiệm thực tế",
                    Content    = "Mình đã làm việc với cả hai hệ quản trị CSDL này trong nhiều năm. " +
                                 "Tóm tắt nhanh:\n\n" +
                                 "**SQL Server:** Tích hợp tốt với .NET ecosystem, SSMS ngon, nhưng tốn tiền license production.\n\n" +
                                 "**PostgreSQL:** Free, hiệu năng cao, JSON support mạnh, community lớn. " +
                                 "Cần thêm Npgsql package nhưng EF Core hỗ trợ tốt.\n\n" +
                                 "Với startup/personal project thì PostgreSQL là lựa chọn tốt hơn về chi phí.",
                    UserId     = tuanBack.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 891,
                    CreatedAt  = Ago(days: 50)
                },
                new() {
                    Title      = "Mình vừa deploy .NET app lên Azure App Service lần đầu - Chia sẻ kinh nghiệm",
                    Content    = "Sau 2 ngày vật lộn, cuối cùng mình cũng deploy được app .NET 6 lên Azure. " +
                                 "Những lỗi mình gặp phải:\n\n" +
                                 "1. Connection string format khác với local\n" +
                                 "2. Quên set ASPNETCORE_ENVIRONMENT = Production\n" +
                                 "3. App settings cần configure trong Azure portal\n" +
                                 "4. EF Core migration cần chạy riêng (dùng startup task)\n\n" +
                                 "Nếu ai cần hỏi chi tiết bước nào cứ comment nhé!",
                    UserId     = minhDev.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 445,
                    CreatedAt  = Ago(days: 45)
                },
                new() {
                    Title      = "[Thông báo] Quy tắc cộng đồng và cách đặt câu hỏi hiệu quả",
                    Content    = "Chào mừng tất cả thành viên mới!\n\n" +
                                 "Để diễn đàn hoạt động hiệu quả, mọi người vui lòng:\n\n" +
                                 "1. **Tìm kiếm trước khi hỏi** - câu hỏi của bạn có thể đã được trả lời\n" +
                                 "2. **Đặt tiêu đề rõ ràng** - mô tả ngắn gọn vấn đề\n" +
                                 "3. **Cung cấp context** - OS, phiên bản framework, error message đầy đủ\n" +
                                 "4. **Format code** - dùng code block cho snippet\n" +
                                 "5. **Mark solved** - khi có câu trả lời hãy đánh dấu để giúp người khác\n\n" +
                                 "Chúc mọi người học vui!",
                    UserId     = adminUser.Id,
                    CategoryId = catAnnounce.Id,
                    ViewCount  = 1567,
                    CreatedAt  = Ago(days: 160)
                },
                new() {
                    Title      = "[Hỏi] Cách implement Pagination đúng chuẩn trong REST API?",
                    Content    = "Mình đang build API và cần implement pagination cho endpoint GET /api/posts. " +
                                 "Có 2 cách mình đang cân nhắc:\n\n" +
                                 "**Offset-based:** ?page=1&pageSize=10 - đơn giản nhưng inconsistent khi data thay đổi\n\n" +
                                 "**Cursor-based:** ?after=cursorId - consistent hơn nhưng phức tạp hơn\n\n" +
                                 "Response nên trả về gì? Chỉ items hay cả totalCount, totalPages?\n\n" +
                                 "Mọi người có best practice gì không chia sẻ với mình nhé!",
                    UserId     = linhFull.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 334,
                    CreatedAt  = Ago(days: 40)
                },
                new() {
                    Title      = "FluentValidation trong ASP.NET Core - Tại sao bạn nên bỏ DataAnnotations",
                    Content    = "Sau khi thử cả DataAnnotations và FluentValidation, mình thấy FluentValidation win rõ ràng:\n\n" +
                                 "- Logic validate tách riêng khỏi model → cleaner code\n" +
                                 "- Support complex validation rules (cross-field, async)\n" +
                                 "- Unit test validator độc lập, không cần mock controller\n" +
                                 "- Error message linh hoạt, i18n dễ dàng\n\n" +
                                 "Ví dụ rule mình hay dùng: phải là email hợp lệ, độ dài 6-50 ký tự, không có ký tự đặc biệt.",
                    UserId     = tuanBack.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 612,
                    CreatedAt  = Ago(days: 35)
                },
                new() {
                    Title      = "[Hỏi] BCrypt vs Argon2 để hash password - 2025 nên dùng gì?",
                    Content    = "Mình đang implement authentication cho project mới. " +
                                 "Hiện tại đang dùng BCrypt.Net-Next với work factor 12. " +
                                 "Nhưng mình đọc thấy Argon2 mới hơn và được khuyến nghị hơn.\n\n" +
                                 "Các bạn đang dùng gì trong production? " +
                                 "BCrypt work factor 12 có đủ secure không hay cần nâng lên?\n\n" +
                                 "Server của mình: 4 vCPU, 8GB RAM.",
                    UserId     = hungJunior.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 178,
                    CreatedAt  = Ago(days: 28)
                },
                new() {
                    Title      = "Xây dựng CRUD API hoàn chỉnh với .NET 6 + EF Core + Repository Pattern",
                    Content    = "Series tutorial mình viết cho các bạn mới:\n\n" +
                                 "**Part 1:** Setup project, cấu trúc folder theo 3-layer\n" +
                                 "**Part 2:** DbContext, Entity, Migration\n" +
                                 "**Part 3:** Generic Repository + Unit of Work\n" +
                                 "**Part 4:** Service layer + AutoMapper\n" +
                                 "**Part 5:** Controller + FluentValidation\n" +
                                 "**Part 6:** JWT Authentication\n" +
                                 "**Part 7:** Global Error Handling Middleware\n\n" +
                                 "Source code đầy đủ mình để link GitHub trong comment đầu tiên.",
                    UserId     = linhFull.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 3102,
                    CreatedAt  = Ago(days: 110)
                },
                new() {
                    Title      = "[Hỏi] AutoMapper hay Mapster - Bạn đang dùng thư viện nào?",
                    Content    = "Dự án mình đang dùng AutoMapper nhưng nghe nói Mapster nhanh hơn đáng kể. " +
                                 "Mình có nên migrate không? Dự án có khoảng 50 mapping profile.\n\n" +
                                 "Các bạn có benchmark thực tế không? Với CRUD API thông thường thì difference có đáng kể không?",
                    UserId     = minhDev.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 267,
                    CreatedAt  = Ago(days: 20)
                },
                new() {
                    Title      = "Middleware pipeline trong ASP.NET Core - Thứ tự quan trọng hơn bạn nghĩ",
                    Content    = "Một lỗi hay gặp của senior lẫn junior: đặt middleware sai thứ tự trong pipeline.\n\n" +
                                 "Thứ tự đúng:\n" +
                                 "1. Exception handling (phải là đầu tiên để catch mọi lỗi)\n" +
                                 "2. HTTPS Redirection\n" +
                                 "3. Static Files\n" +
                                 "4. Routing\n" +
                                 "5. CORS (phải sau Routing, trước Authentication)\n" +
                                 "6. Authentication\n" +
                                 "7. Authorization\n" +
                                 "8. Endpoints\n\n" +
                                 "Sai thứ tự Auth ↔ CORS là bug kinh điển nhất mình từng thấy!",
                    UserId     = tuanBack.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 889,
                    CreatedAt  = Ago(days: 15)
                },
                new() {
                    Title      = "[Tìm kiếm] Mentor .NET cho fresher, học online 2-3 buổi/tuần",
                    Content    = "Mình là sinh viên năm 3 CNTT, đang học .NET Core để chuẩn bị đi thực tập. " +
                                 "Mình muốn tìm một anh/chị mentor có thể review code và hướng dẫn best practice.\n\n" +
                                 "Hiện tại mình đã biết: C# cơ bản, OOP, SQL cơ bản, đang học ASP.NET Core MVC.\n\n" +
                                 "Mình sẵn sàng học online qua Discord/Teams. " +
                                 "Ai có thể mentor hoặc biết nguồn nào tốt xin giới thiệu mình với!",
                    UserId     = hungJunior.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 156,
                    CreatedAt  = Ago(days: 8)
                },
                new() {
                    Title      = "Rate Limiting trong ASP.NET Core - Protect API khỏi abuse",
                    Content    = ".NET 7+ có built-in RateLimiter middleware rất xịn. Nhưng với .NET 6 thì sao?\n\n" +
                                 "Mình implement bằng IMemoryCache + custom ActionFilter:\n" +
                                 "- Track số request per IP trong sliding window\n" +
                                 "- Login: 5 requests/15 phút\n" +
                                 "- Register: 10 requests/giờ\n" +
                                 "- Return 429 Too Many Requests khi vượt limit\n\n" +
                                 "Approach này đủ cho MVP nhưng production nên dùng Redis để share state giữa multiple instances.",
                    UserId     = minhDev.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 423,
                    CreatedAt  = Ago(days: 5)
                },
                new() {
                    Title      = "[Hỏi] Làm sao test Controller trong ASP.NET Core mà không cần chạy server?",
                    Content    = "Mình mới bắt đầu viết unit test. Muốn test PostController nhưng không biết mock gì.\n\n" +
                                 "Controller của mình inject IPostService. " +
                                 "Mình đã thử dùng Moq nhưng bị lỗi khi setup mock:\n\n" +
                                 "mockService.Setup(s => s.GetPagedAsync(...)).ReturnsAsync(...);\n\n" +
                                 "Lỗi: Expression cannot be null. Mình đang bị kẹt ở đây. Help!",
                    UserId     = hungJunior.Id,
                    CategoryId = catQnA.Id,
                    ViewCount  = 98,
                    CreatedAt  = Ago(days: 2)
                },
                new() {
                    Title      = "Soft Delete trong EF Core - Pattern và cạm bẫy cần tránh",
                    Content    = "Soft delete nghe đơn giản nhưng có nhiều gotcha:\n\n" +
                                 "**Cách naive:** Thêm IsDeleted = true và filter thủ công mọi chỗ → dễ quên, bug ẩn\n\n" +
                                 "**Cách tốt hơn:** Global query filter\n" +
                                 "modelBuilder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);\n\n" +
                                 "**Cạm bẫy:** Include() bỏ qua global filter của related entity → cần IgnoreQueryFilters() khi cần\n\n" +
                                 "Dự án mình filter thủ công trong Repository vì cần control rõ hơn.",
                    UserId     = linhFull.Id,
                    CategoryId = catTutorial.Id,
                    ViewCount  = 534,
                    CreatedAt  = Ago(days: 1)
                },
                new() {
                    Title      = "Tổng hợp resources học .NET Backend 2025 - Từ zero đến có việc làm",
                    Content    = "Mình tổng hợp lộ trình và tài nguyên mình đã dùng:\n\n" +
                                 "**Giai đoạn 1 - C# cơ bản (1-2 tháng):**\n" +
                                 "- C# Yellow Book (miễn phí)\n" +
                                 "- .NET documentation trên Microsoft Learn\n\n" +
                                 "**Giai đoạn 2 - ASP.NET Core (2-3 tháng):**\n" +
                                 "- Docs chính thức + Pluralsight\n" +
                                 "- YouTube: Nick Chapsas, Tim Corey\n\n" +
                                 "**Giai đoạn 3 - Build project thực tế:**\n" +
                                 "- Clone một app đơn giản: todo, blog, forum\n" +
                                 "- Deploy lên cloud\n\n" +
                                 "**Giai đoạn 4 - Apply việc:**\n" +
                                 "- LeetCode medium (SQL + basic algo)\n" +
                                 "- System design cơ bản",
                    UserId     = adminUser.Id,
                    CategoryId = catGeneral.Id,
                    ViewCount  = 4215,
                    CreatedAt  = Ago(days: 130)
                }
            };

            db.Posts.AddRange(posts);
            await db.SaveChangesAsync();

            // ── Comments ──────────────────────────────────────────────────────────
            var comments = new List<Comment>
            {
                // On post: "ASP.NET Core 6 vs .NET 7"
                new() {
                    PostId    = posts[1].Id,
                    UserId    = tuanBack.Id,
                    Content   = ".NET 6 LTS là lựa chọn an toàn nhất cho production dài hạn. " +
                                "Hỗ trợ đến tháng 11/2024, đủ thời gian migrate lên LTS tiếp theo.",
                    CreatedAt = Ago(days: 84)
                },
                new() {
                    PostId    = posts[1].Id,
                    UserId    = linhFull.Id,
                    Content   = "Công ty mình dùng .NET 8 (LTS) cho dự án mới từ 2024. " +
                                "Nếu bắt đầu fresh thì nhảy thẳng .NET 8 luôn, đừng dùng 6 nữa.",
                    CreatedAt = Ago(days: 83)
                },
                new() {
                    PostId    = posts[1].Id,
                    UserId    = hungJunior.Id,
                    Content   = "Cảm ơn anh chị! Mình sẽ dùng .NET 8 LTS. Vậy migration từ 6 lên 8 có khó không?",
                    CreatedAt = Ago(days: 82)
                },

                // On post: "EF Core performance"
                new() {
                    PostId    = posts[2].Id,
                    UserId    = tuanBack.Id,
                    Content   = "AsNoTracking() là cái mình hay quên nhất. Dùng cho mọi GET query là tăng perf rõ rệt.",
                    CreatedAt = Ago(days: 74)
                },
                new() {
                    PostId    = posts[2].Id,
                    UserId    = linhFull.Id,
                    Content   = "Thêm Split Query cho Include nhiều cấp:\n" +
                                ".AsSplitQuery() - tránh Cartesian explosion khi join nhiều bảng.",
                    CreatedAt = Ago(days: 73)
                },

                // On post: "Clean Architecture vs Layered"
                new() {
                    PostId    = posts[3].Id,
                    UserId    = minhDev.Id,
                    Content   = "3-layer đủ cho 80% dự án vừa và nhỏ. " +
                                "Clean Architecture hay khi team lớn, domain phức tạp. Đừng over-engineer sớm.",
                    CreatedAt = Ago(days: 69)
                },
                new() {
                    PostId    = posts[3].Id,
                    UserId    = adminUser.Id,
                    Content   = "Đồng ý với bạn trên. Bắt đầu đơn giản, refactor khi cần. " +
                                "Premature optimization is the root of all evil - kể cả architecture.",
                    CreatedAt = Ago(days: 68)
                },

                // On post: "CORS error"
                new() {
                    PostId    = posts[5].Id,
                    UserId    = minhDev.Id,
                    Content   = "Lỗi này mình từng gặp. Nguyên nhân hay gặp nhất: " +
                                "UseCors() phải gọi TRƯỚC UseAuthorization(). " +
                                "Bạn check lại thứ tự middleware trong Program.cs nhé.",
                    CreatedAt = Ago(days: 54)
                },
                new() {
                    PostId    = posts[5].Id,
                    UserId    = tuanBack.Id,
                    Content   = "Ngoài ra kiểm tra xem bạn đang gọi đúng tên policy không: " +
                                "app.UseCors(\"TênPolicy\") phải khớp với tên trong AddCors().",
                    CreatedAt = Ago(days: 54)
                },
                new() {
                    PostId    = posts[5].Id,
                    UserId    = hungJunior.Id,
                    Content   = "Fixed rồi mọi người ơi! Đúng là thứ tự middleware. Cảm ơn anh Minh và anh Tuấn!",
                    CreatedAt = Ago(days: 53)
                },

                // On post: "JWT Authentication guide"
                new() {
                    PostId    = posts[4].Id,
                    UserId    = minhDev.Id,
                    Content   = "Bài viết rất hay! Bạn có thể nói thêm về refresh token rotation không? " +
                                "Specifically là cách revoke old refresh token khi issue cái mới.",
                    CreatedAt = Ago(days: 63)
                },
                new() {
                    PostId    = posts[4].Id,
                    UserId    = hungJunior.Id,
                    Content   = "Tại sao không dùng localStorage mà phải dùng HttpOnly cookie? " +
                                "Mình thấy nhiều tutorial dùng localStorage.",
                    CreatedAt = Ago(days: 62)
                },
                new() {
                    PostId    = posts[4].Id,
                    UserId    = linhFull.Id,
                    Content   = "LocalStorage dễ bị XSS attack đọc được. " +
                                "HttpOnly cookie không accessible từ JavaScript nên an toàn hơn nhiều. " +
                                "Đây là best practice bảo mật.",
                    CreatedAt = Ago(days: 62)
                },

                // On post: "BCrypt vs Argon2"
                new() {
                    PostId    = posts[11].Id,
                    UserId    = tuanBack.Id,
                    Content   = "BCrypt work factor 12 vẫn ổn trong 2025. " +
                                "Argon2 được khuyến nghị bởi OWASP nhưng BCrypt cũng không phải deprecated. " +
                                "Quan trọng là work factor phải đủ cao để hash mất ít nhất 100ms.",
                    CreatedAt = Ago(days: 27)
                },
                new() {
                    PostId    = posts[11].Id,
                    UserId    = adminUser.Id,
                    Content   = "Với server 4 vCPU của bạn, work factor 12 là hợp lý. " +
                                "Bạn có thể benchmark bằng cách đo thời gian hash trên máy production. " +
                                "Mục tiêu: 250ms-1000ms per hash là sweet spot.",
                    CreatedAt = Ago(days: 26)
                },

                // On post: "Tìm mentor"
                new() {
                    PostId    = posts[15].Id,
                    UserId    = minhDev.Id,
                    Content   = "Mình có thể support một chút. " +
                                "Bạn join Discord daynhauhoc.com đi, có channel riêng cho .NET và nhiều người sẵn sàng giúp.",
                    CreatedAt = Ago(days: 7)
                },
                new() {
                    PostId    = posts[15].Id,
                    UserId    = linhFull.Id,
                    Content   = "Microsoft Learn có learning path .NET miễn phí và có structured path rõ ràng. " +
                                "Kết hợp với làm project thực tế sẽ tiến bộ nhanh hơn.",
                    CreatedAt = Ago(days: 7)
                }
            };

            db.Comments.AddRange(comments);
            await db.SaveChangesAsync();
        }

        private static DateTime Ago(int days) =>
            DateTime.UtcNow.AddDays(-days);
    }
}
