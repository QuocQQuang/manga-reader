using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mangareading.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiCache",
                columns: table => new
                {
                    CacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CacheData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCache", x => x.CacheKey);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    GenreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GenreName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.GenreId);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewUsers = table.Column<int>(type: "int", nullable: false),
                    NewManga = table.Column<int>(type: "int", nullable: false),
                    NewChapters = table.Column<int>(type: "int", nullable: false),
                    TotalViews = table.Column<long>(type: "bigint", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    SourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.SourceId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThemePreference = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Mangas",
                columns: table => new
                {
                    MangaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlternativeTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Artist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationYear = table.Column<int>(type: "int", nullable: true),
                    OriginalLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ChapterCount = table.Column<int>(type: "int", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mangas", x => x.MangaId);
                    table.ForeignKey(
                        name: "FK_Mangas_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                    table.ForeignKey(
                        name: "FK_Mangas_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mangas_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MangaId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChapterNumber = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId");
                    table.ForeignKey(
                        name: "FK_Chapters_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId");
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => new { x.UserId, x.MangaId });
                    table.ForeignKey(
                        name: "FK_Favorites_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaGenres",
                columns: table => new
                {
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaGenres", x => new { x.MangaId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MangaGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "GenreId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaGenres_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaGroups",
                columns: table => new
                {
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaGroups", x => new { x.MangaId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_MangaGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaGroups_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comments_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MangaViews",
                columns: table => new
                {
                    ViewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaViews", x => x.ViewId);
                    table.ForeignKey(
                        name: "FK_MangaViews_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaViews_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MangaViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSize = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.PageId);
                    table.ForeignKey(
                        name: "FK_Pages_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_ReadingHistories_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingHistories_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingProgresses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingProgresses", x => new { x.UserId, x.MangaId, x.ChapterId });
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewCounts",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewCounts", x => new { x.ChapterId, x.IpAddress, x.ViewedAt });
                    table.ForeignKey(
                        name: "FK_ViewCounts_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViewCounts_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "MangaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewCounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CommentReplies",
                columns: table => new
                {
                    ReplyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReplies", x => x.ReplyId);
                    table.ForeignKey(
                        name: "FK_CommentReplies_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "CommentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentReplies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CommentReactions",
                columns: table => new
                {
                    ReactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsLike = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CommentId = table.Column<int>(type: "int", nullable: true),
                    ReplyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReactions", x => x.ReactionId);
                    table.ForeignKey(
                        name: "FK_CommentReactions_CommentReplies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "CommentReplies",
                        principalColumn: "ReplyId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CommentReactions_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "CommentId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CommentReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId",
                table: "Chapters",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SourceId",
                table: "Chapters",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_CommentId",
                table: "CommentReactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_ReplyId",
                table: "CommentReactions",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_UserId",
                table: "CommentReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReplies_CommentId",
                table: "CommentReplies",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReplies_UserId",
                table: "CommentReplies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChapterId",
                table: "Comments",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_MangaId",
                table: "Comments",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_MangaId",
                table: "Favorites",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGenres_GenreId",
                table: "MangaGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaGroups_GroupId",
                table: "MangaGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_GroupId",
                table: "Mangas",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_SourceId",
                table: "Mangas",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Mangas_UploadedByUserId",
                table: "Mangas",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaViews_ChapterId",
                table: "MangaViews",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaViews_MangaId",
                table: "MangaViews",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_MangaViews_UserId",
                table: "MangaViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStats_Month",
                table: "MonthlyStats",
                column: "Month",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ChapterId",
                table: "Pages",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_ChapterId",
                table: "ReadingHistories",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_MangaId",
                table: "ReadingHistories",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_UserId",
                table: "ReadingHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_ChapterId",
                table: "ReadingProgresses",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_MangaId",
                table: "ReadingProgresses",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewCounts_MangaId",
                table: "ViewCounts",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewCounts_UserId",
                table: "ViewCounts",
                column: "UserId");

            // Triggers
            migrationBuilder.Sql(@"
CREATE TRIGGER [dbo].[TRG_Mangas_UpdateTimestamp]
ON [dbo].[Mangas] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE m SET UpdatedAt = GETDATE()
        FROM [dbo].[Mangas] m INNER JOIN inserted i ON m.MangaId = i.MangaId;
    END
END");

            migrationBuilder.Sql(@"
CREATE TRIGGER [dbo].[TRG_Chapters_UpdateMangaChapterCount]
ON [dbo].[Chapters] AFTER INSERT, DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MangaId INT;
    IF EXISTS (SELECT 1 FROM inserted) SELECT @MangaId = MangaId FROM inserted;
    ELSE SELECT @MangaId = MangaId FROM deleted;
    IF @MangaId IS NOT NULL
        UPDATE m SET ChapterCount = (SELECT COUNT(*) FROM [dbo].[Chapters] c WHERE c.MangaId = m.MangaId)
        FROM [dbo].[Mangas] m WHERE m.MangaId = @MangaId;
END");

            migrationBuilder.Sql(@"
CREATE TRIGGER [dbo].[TRG_ViewCounts_UpdateMangaViewCount]
ON [dbo].[ViewCounts] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    UPDATE m SET ViewCount = ISNULL(m.ViewCount, 0) + 1
    FROM [dbo].[Mangas] m INNER JOIN inserted i ON m.MangaId = i.MangaId;
END");

            migrationBuilder.Sql(@"
CREATE TRIGGER [dbo].[TRG_ViewCounts_UpdateDailyStats]
ON [dbo].[ViewCounts] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MangaId INT;
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    SELECT @MangaId = MangaId FROM inserted;
    IF EXISTS (SELECT 1 FROM [dbo].[MangaStatistics] WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1)
        UPDATE [dbo].[MangaStatistics] SET ViewCount = ViewCount + 1
        WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1;
    ELSE
        INSERT INTO [dbo].[MangaStatistics] (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (@MangaId, @Today, 1, 0, 1, 0, 0);
END");

            migrationBuilder.Sql(@"
CREATE TRIGGER [dbo].[TRG_Favorites_UpdateDailyStats]
ON [dbo].[Favorites] AFTER INSERT, DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MangaId INT; DECLARE @Change INT;
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    IF EXISTS (SELECT 1 FROM inserted) BEGIN SELECT @MangaId = MangaId FROM inserted; SET @Change = 1; END
    ELSE BEGIN SELECT @MangaId = MangaId FROM deleted; SET @Change = -1; END
    IF EXISTS (SELECT 1 FROM [dbo].[MangaStatistics] WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1)
        UPDATE [dbo].[MangaStatistics] SET FavoriteCount = FavoriteCount + @Change
        WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1;
    ELSE IF @Change = 1
        INSERT INTO [dbo].[MangaStatistics] (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (@MangaId, @Today, 0, 1, 1, 0, 0);
    UPDATE [dbo].[MangaStatistics] SET FavoriteCount = 0
    WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1 AND FavoriteCount < 0;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TRG_Favorites_UpdateDailyStats]");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TRG_ViewCounts_UpdateDailyStats]");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TRG_ViewCounts_UpdateMangaViewCount]");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TRG_Chapters_UpdateMangaChapterCount]");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TRG_Mangas_UpdateTimestamp]");

            migrationBuilder.DropTable(
                name: "ApiCache");

            migrationBuilder.DropTable(
                name: "CommentReactions");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "MangaGenres");

            migrationBuilder.DropTable(
                name: "MangaGroups");

            migrationBuilder.DropTable(
                name: "MangaViews");

            migrationBuilder.DropTable(
                name: "MonthlyStats");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "ReadingHistories");

            migrationBuilder.DropTable(
                name: "ReadingProgresses");

            migrationBuilder.DropTable(
                name: "ViewCounts");

            migrationBuilder.DropTable(
                name: "CommentReplies");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Mangas");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
