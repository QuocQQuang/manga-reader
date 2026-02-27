using Microsoft.EntityFrameworkCore;

namespace Mangareading.Models
{
    public class YourDbContext : DbContext
    {
        public YourDbContext(DbContextOptions<YourDbContext> options)
            : base(options)
        {
        }

        public DbSet<Source> Sources { get; set; }
        public DbSet<Manga> Mangas { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MangaGenre> MangaGenres { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ReadingProgress> ReadingProgresses { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<ReadingHistory> ReadingHistories { get; set; }
        public DbSet<ApiCache> ApiCache { get; set; }
        public DbSet<MangaView> MangaViews { get; set; }
        public DbSet<ViewCount> ViewCounts { get; set; }
        public DbSet<MonthlyStats> MonthlyStats { get; set; }

        public DbSet<Group> Groups { get; set; }
        public DbSet<MangaGroup> MangaGroups { get; set; }

        // Comment-related DbSets
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReply> CommentReplies { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
            });
            modelBuilder.Entity<MangaGenre>()
                .HasKey(cg => new { cg.MangaId, cg.GenreId });

            modelBuilder.Entity<ReadingProgress>()
                .HasKey(rp => new { rp.UserId, rp.MangaId, rp.ChapterId });

            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.MangaId });

            modelBuilder.Entity<MangaGroup>()
                .HasKey(cg => new { cg.MangaId, cg.GroupId });

            modelBuilder.Entity<MangaGenre>()
                .HasOne(mg => mg.Manga)
                .WithMany(m => m.MangaGenres)
                .HasForeignKey(mg => mg.MangaId);

            modelBuilder.Entity<MangaGenre>()
                .HasOne(mg => mg.Genre)
                .WithMany(g => g.MangaGenres)
                .HasForeignKey(mg => mg.GenreId);

            modelBuilder.Entity<ViewCount>()
                .HasKey(v => new { v.ChapterId, v.IpAddress, v.ViewedAt });

            modelBuilder.Entity<ViewCount>()
                .HasOne(v => v.Manga)
                .WithMany()
                .HasForeignKey(v => v.MangaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ViewCount>()
                .HasOne(v => v.Chapter)
                .WithMany()
                .HasForeignKey(v => v.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ViewCount>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Manga)
                .WithMany()
                .HasForeignKey(f => f.MangaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment relationships
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Manga)
                .WithMany(m => m.Comments)
                .HasForeignKey(c => c.MangaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Chapter)
                .WithMany(ch => ch.Comments)
                .HasForeignKey(c => c.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            // CommentReply relationships
            // NoAction to avoid multiple cascade paths: Users→Comments→CommentReplies AND Users→CommentReplies
            modelBuilder.Entity<CommentReply>()
                .HasOne(r => r.User)
                .WithMany(u => u.CommentReplies)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CommentReply>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Replies)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // CommentReaction relationships - all NoAction to avoid multiple cascade paths
            modelBuilder.Entity<CommentReaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.CommentReactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CommentReaction>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Reactions)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CommentReaction>()
                .HasOne(r => r.Reply)
                .WithMany(r => r.Reactions)
                .HasForeignKey(r => r.ReplyId)
                .OnDelete(DeleteBehavior.NoAction);

            // Add configuration for MonthlyStats
            modelBuilder.Entity<MonthlyStats>(entity =>
            {
                entity.ToTable("MonthlyStats");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Month).IsUnique();
            });
        }
    }
}