USE [master]
GO

-- Tạo database với kích thước file nhỏ hơn và recovery model là SIMPLE
CREATE DATABASE [MangaReaderDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'MangaReaderDB', FILENAME = N'C:\Users\QQ\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\MangaReaderDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'MangaReaderDB_log', FILENAME = N'C:\Users\QQ\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\MangaReaderDB.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
GO

-- Chỉ giữ lại các cài đặt quan trọng
ALTER DATABASE [MangaReaderDB] SET COMPATIBILITY_LEVEL = 150
GO
ALTER DATABASE [MangaReaderDB] SET RECOVERY SIMPLE
GO
ALTER DATABASE [MangaReaderDB] SET ANSI_NULLS ON 
GO
ALTER DATABASE [MangaReaderDB] SET QUOTED_IDENTIFIER ON 
GO
ALTER DATABASE [MangaReaderDB] SET MULTI_USER 
GO

USE [MangaReaderDB]
GO

-- Cài đặt cơ bản
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Tạo các bảng
CREATE TABLE [dbo].[Users](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[AvatarUrl] [nvarchar](255) NULL,
	[IsAdmin] [bit] NOT NULL DEFAULT ((0)),
	[IsActive] [bit] NOT NULL DEFAULT ((1)),
	[CreatedAt] [datetime2](7) NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([UserId] ASC)
)
GO

CREATE TABLE [dbo].[Sources](
	[SourceId] [int] IDENTITY(1,1) NOT NULL,
	[SourceName] [nvarchar](50) NOT NULL,
	[SourceUrl] [nvarchar](255) NULL,
	[ApiBaseUrl] [nvarchar](255) NULL,
	[IsActive] [bit] NOT NULL DEFAULT ((1)),
PRIMARY KEY CLUSTERED ([SourceId] ASC)
)
GO

CREATE TABLE [dbo].[Genres](
	[GenreId] [int] IDENTITY(1,1) NOT NULL,
	[GenreName] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED ([GenreId] ASC)
)
GO

CREATE TABLE [dbo].[Groups](
	[GroupId] [int] IDENTITY(1,1) NOT NULL,
	[GroupName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([GroupId] ASC)
)
GO

CREATE TABLE [dbo].[Mangas](
	[MangaId] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](2055) NOT NULL,
	[AlternativeTitle] [nvarchar](2055) NULL,
	[Description] [nvarchar](max) NULL,
	[CoverUrl] [nvarchar](255) NULL,
	[Author] [nvarchar](100) NULL,
	[Artist] [nvarchar](100) NULL,
	[Status] [nvarchar](50) NULL,
	[PublicationYear] [int] NULL,
	[SourceId] [int] NOT NULL,
	[ExternalId] [nvarchar](100) NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (getdate()),
	[UpdatedAt] [datetime] NOT NULL DEFAULT (getdate()),
	[GroupId] [int] NULL,
	[ChapterCount] [int] NULL DEFAULT ((0)),
	[LastSyncAt] [datetime] NULL,
	[OriginalLanguage] [nvarchar](50) NULL,
	[ViewCount] [int] NULL DEFAULT ((0)),
PRIMARY KEY CLUSTERED ([MangaId] ASC)
)
GO

CREATE TABLE [dbo].[Chapters](
	[ChapterId] [int] IDENTITY(1,1) NOT NULL,
	[MangaId] [int] NULL,
	[ChapterNumber] [decimal](8, 2) NULL,
	[Title] [nvarchar](255) NULL,
	[LanguageCode] [nvarchar](10) NULL,
	[SourceId] [int] NULL,
	[ExternalId] [nvarchar](100) NULL,
	[UploadDate] [datetime] NOT NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([ChapterId] ASC)
)
GO

CREATE TABLE [dbo].[Pages](
	[PageId] [int] IDENTITY(1,1) NOT NULL,
	[ChapterId] [int] NULL,
	[PageNumber] [int] NULL,
	[ImageUrl] [nvarchar](255) NOT NULL,
	[ImageHash] [nvarchar](64) NULL,
	[Width] [int] NULL,
	[Height] [int] NULL,
	[FileSize] [int] NULL,
PRIMARY KEY CLUSTERED ([PageId] ASC)
)
GO

CREATE TABLE [dbo].[Comments](
	[CommentId] [int] IDENTITY(1,1) NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[UserId] [int] NOT NULL,
	[MangaId] [int] NOT NULL,
	[ChapterId] [int] NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED ([CommentId] ASC)
)
GO

CREATE TABLE [dbo].[CommentReplies](
	[ReplyId] [int] IDENTITY(1,1) NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[CommentId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_CommentReplies] PRIMARY KEY CLUSTERED ([ReplyId] ASC)
)
GO

CREATE TABLE [dbo].[CommentReactions](
	[ReactionId] [int] IDENTITY(1,1) NOT NULL,
	[IsLike] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UserId] [int] NOT NULL,
	[CommentId] [int] NULL,
	[ReplyId] [int] NULL,
 CONSTRAINT [PK_CommentReactions] PRIMARY KEY CLUSTERED ([ReactionId] ASC)
)
GO

CREATE TABLE [dbo].[ReplyReactions](
	[ReactionId] [int] IDENTITY(1,1) NOT NULL,
	[ReplyId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[IsLike] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([ReactionId] ASC),
CONSTRAINT [UQ_ReplyReaction] UNIQUE NONCLUSTERED ([ReplyId] ASC, [UserId] ASC)
)
GO

CREATE TABLE [dbo].[MangaGenres](
	[MangaId] [int] NOT NULL,
	[GenreId] [int] NOT NULL,
PRIMARY KEY CLUSTERED ([MangaId] ASC, [GenreId] ASC)
)
GO

CREATE TABLE [dbo].[MangaGroups](
	[MangaId] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
PRIMARY KEY CLUSTERED ([MangaId] ASC, [GroupId] ASC)
)
GO

CREATE TABLE [dbo].[Favorites](
	[UserId] [int] NOT NULL,
	[MangaId] [int] NOT NULL,
	[AddedAt] [datetime] NOT NULL DEFAULT (getdate()),
	[CreatedAt] [datetime2](7) NOT NULL DEFAULT (getutcdate()),
PRIMARY KEY CLUSTERED ([UserId] ASC, [MangaId] ASC)
)
GO

CREATE TABLE [dbo].[ReadingHistories](
	[HistoryId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[MangaId] [int] NOT NULL,
	[ChapterId] [int] NOT NULL,
	[ReadAt] [datetime2](7) NOT NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([HistoryId] ASC)
)
GO

CREATE TABLE [dbo].[ViewCounts](
	[ViewId] [int] IDENTITY(1,1) NOT NULL,
	[MangaId] [int] NOT NULL,
	[ChapterId] [int] NOT NULL,
	[UserId] [int] NULL,
	[ViewedAt] [datetime] NOT NULL DEFAULT (getutcdate()),
	[IpAddress] [nvarchar](45) NULL,
PRIMARY KEY CLUSTERED ([ViewId] ASC)
)
GO

CREATE TABLE [dbo].[ApiCache](
	[CacheKey] [nvarchar](255) NOT NULL,
	[CacheData] [nvarchar](max) NULL,
	[ExpireAt] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (getdate()),
PRIMARY KEY CLUSTERED ([CacheKey] ASC)
)
GO

CREATE TABLE [dbo].[MangaStatistics](
	[StatisticId] [int] IDENTITY(1,1) NOT NULL,
	[MangaId] [int] NOT NULL,
	[Date] [date] NOT NULL,
	[ViewCount] [int] NOT NULL DEFAULT ((0)),
	[FavoriteCount] [int] NOT NULL DEFAULT ((0)),
	[IsDaily] [bit] NOT NULL DEFAULT ((0)),
	[IsMonthly] [bit] NOT NULL DEFAULT ((0)),
	[IsYearly] [bit] NOT NULL DEFAULT ((0)),
PRIMARY KEY CLUSTERED ([StatisticId] ASC)
)
GO

-- Chèn dữ liệu mẫu
-- Thêm nguồn MangaDex
INSERT INTO [dbo].[Sources] ([SourceName], [SourceUrl], [ApiBaseUrl], [IsActive])
VALUES ('MangaDex', 'https://mangadex.org', 'https://api.mangadex.org/v2', 1);
GO

-- Tạo các chỉ mục chính
CREATE NONCLUSTERED INDEX [IX_Comments_MangaId] ON [dbo].[Comments] ([MangaId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Comments_UserId] ON [dbo].[Comments] ([UserId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Comments_ChapterId] ON [dbo].[Comments] ([ChapterId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_Comments_CreatedAt] ON [dbo].[Comments] ([CreatedAt] DESC)
GO

CREATE NONCLUSTERED INDEX [IX_CommentReplies_CommentId] ON [dbo].[CommentReplies] ([CommentId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_CommentReplies_UserId] ON [dbo].[CommentReplies] ([UserId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_CommentReplies_CreatedAt] ON [dbo].[CommentReplies] ([CreatedAt] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_CommentReactions_CommentId] ON [dbo].[CommentReactions] ([CommentId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_CommentReactions_ReplyId] ON [dbo].[CommentReactions] ([ReplyId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_CommentReactions_UserId] ON [dbo].[CommentReactions] ([UserId] ASC)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_CommentReactions_User_Comment] ON [dbo].[CommentReactions]
([UserId] ASC, [CommentId] ASC)
WHERE ([CommentId] IS NOT NULL AND [ReplyId] IS NULL)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_CommentReactions_User_Reply] ON [dbo].[CommentReactions]
([UserId] ASC, [ReplyId] ASC)
WHERE ([ReplyId] IS NOT NULL AND [CommentId] IS NULL)
GO

CREATE NONCLUSTERED INDEX [IX_ReplyReactions_ReplyId] ON [dbo].[ReplyReactions] ([ReplyId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_ReplyReactions_UserId] ON [dbo].[ReplyReactions] ([UserId] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_ReadingHistories_User_Manga_Chapter] ON [dbo].[ReadingHistories]
([UserId] ASC, [MangaId] ASC, [ChapterId] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_ViewCounts_MangaId] ON [dbo].[ViewCounts] ([MangaId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_ViewCounts_ChapterId] ON [dbo].[ViewCounts] ([ChapterId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_ViewCounts_ViewedAt] ON [dbo].[ViewCounts] ([ViewedAt] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_MangaStatistics_MangaId_Date] ON [dbo].[MangaStatistics]
([MangaId] ASC, [Date] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_MangaStatistics_Types] ON [dbo].[MangaStatistics]
([MangaId] ASC, [IsDaily] ASC, [IsMonthly] ASC, [IsYearly] ASC)
GO

-- Thiết lập khóa ngoại (sử dụng WITH NOCHECK để tăng tốc độ tạo)
ALTER TABLE [dbo].[Chapters] WITH NOCHECK ADD CONSTRAINT [FK_Chapters_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId])
GO
ALTER TABLE [dbo].[Chapters] WITH NOCHECK ADD CONSTRAINT [FK_Chapters_Sources] 
    FOREIGN KEY([SourceId]) REFERENCES [dbo].[Sources] ([SourceId])
GO

ALTER TABLE [dbo].[Pages] WITH NOCHECK ADD CONSTRAINT [FK_Pages_Chapters] 
    FOREIGN KEY([ChapterId]) REFERENCES [dbo].[Chapters] ([ChapterId])
GO

ALTER TABLE [dbo].[Comments] WITH NOCHECK ADD CONSTRAINT [FK_Comments_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Comments] WITH NOCHECK ADD CONSTRAINT [FK_Comments_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Comments] WITH NOCHECK ADD CONSTRAINT [FK_Comments_Chapters] 
    FOREIGN KEY([ChapterId]) REFERENCES [dbo].[Chapters] ([ChapterId]) ON DELETE SET NULL
GO

ALTER TABLE [dbo].[CommentReplies] WITH NOCHECK ADD CONSTRAINT [FK_CommentReplies_Comments] 
    FOREIGN KEY([CommentId]) REFERENCES [dbo].[Comments] ([CommentId]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CommentReplies] WITH NOCHECK ADD CONSTRAINT [FK_CommentReplies_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
GO

ALTER TABLE [dbo].[CommentReactions] WITH NOCHECK ADD CONSTRAINT [FK_CommentReactions_Comments] 
    FOREIGN KEY([CommentId]) REFERENCES [dbo].[Comments] ([CommentId])
GO
ALTER TABLE [dbo].[CommentReactions] WITH NOCHECK ADD CONSTRAINT [FK_CommentReactions_CommentReplies] 
    FOREIGN KEY([ReplyId]) REFERENCES [dbo].[CommentReplies] ([ReplyId])
GO
ALTER TABLE [dbo].[CommentReactions] WITH NOCHECK ADD CONSTRAINT [FK_CommentReactions_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[ReplyReactions] WITH NOCHECK ADD CONSTRAINT [FK_ReplyReactions_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
GO

ALTER TABLE [dbo].[MangaGenres] WITH NOCHECK ADD CONSTRAINT [FK_MangaGenres_Genres] 
    FOREIGN KEY([GenreId]) REFERENCES [dbo].[Genres] ([GenreId])
GO
ALTER TABLE [dbo].[MangaGenres] WITH NOCHECK ADD CONSTRAINT [FK_MangaGenres_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId])
GO

ALTER TABLE [dbo].[MangaGroups] WITH NOCHECK ADD CONSTRAINT [FK_MangaGroups_Groups] 
    FOREIGN KEY([GroupId]) REFERENCES [dbo].[Groups] ([GroupId])
GO
ALTER TABLE [dbo].[MangaGroups] WITH NOCHECK ADD CONSTRAINT [FK_MangaGroups_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId])
GO

ALTER TABLE [dbo].[Favorites] WITH NOCHECK ADD CONSTRAINT [FK_Favorites_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId])
GO
ALTER TABLE [dbo].[Favorites] WITH NOCHECK ADD CONSTRAINT [FK_Favorites_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
GO

ALTER TABLE [dbo].[ReadingHistories] WITH NOCHECK ADD CONSTRAINT [FK_ReadingHistories_Users] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
GO
ALTER TABLE [dbo].[ReadingHistories] WITH NOCHECK ADD CONSTRAINT [FK_ReadingHistories_Mangas] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId])
GO
ALTER TABLE [dbo].[ReadingHistories] WITH NOCHECK ADD CONSTRAINT [FK_ReadingHistories_Chapters] 
    FOREIGN KEY([ChapterId]) REFERENCES [dbo].[Chapters] ([ChapterId])
GO

ALTER TABLE [dbo].[ViewCounts] WITH NOCHECK ADD CONSTRAINT [FK_ViewCounts_Manga] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ViewCounts] WITH NOCHECK ADD CONSTRAINT [FK_ViewCounts_Chapter] 
    FOREIGN KEY([ChapterId]) REFERENCES [dbo].[Chapters] ([ChapterId])
GO
ALTER TABLE [dbo].[ViewCounts] WITH NOCHECK ADD CONSTRAINT [FK_ViewCounts_User] 
    FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([UserId])
GO

ALTER TABLE [dbo].[MangaStatistics] WITH NOCHECK ADD CONSTRAINT [FK_MangaStatistics_Manga] 
    FOREIGN KEY([MangaId]) REFERENCES [dbo].[Mangas] ([MangaId]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Mangas] WITH NOCHECK ADD CONSTRAINT [FK_Mangas_Sources] 
    FOREIGN KEY([SourceId]) REFERENCES [dbo].[Sources] ([SourceId])
GO

-- Tạo các stored procedures cần thiết
CREATE PROCEDURE [dbo].[UpdateMangaViewCounts]
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Mangas
    SET ViewCount = (
        SELECT COUNT(*) 
        FROM ViewCounts 
        WHERE ViewCounts.MangaId = Mangas.MangaId
    )
    WHERE EXISTS (
        SELECT 1 
        FROM ViewCounts 
        WHERE ViewCounts.MangaId = Mangas.MangaId
    );

    UPDATE Mangas
    SET ViewCount = 0
    WHERE ViewCount IS NULL;
END;
GO

-- Kết thúc script
USE [master]
GO
ALTER DATABASE [MangaReaderDB] SET READ_WRITE 
GO

-- Add UpdatedAt column to Pages table
ALTER TABLE Pages ADD UpdatedAt DATETIME2;
go
-- Set default value for existing records
UPDATE Pages SET UpdatedAt = GETDATE();
go
-- Make UpdatedAt column NOT NULL after populating with values
ALTER TABLE Pages ALTER COLUMN UpdatedAt DATETIME2 NOT NULL;
go
-- Add a default constraint for new records
ALTER TABLE Pages ADD CONSTRAINT DF_Pages_UpdatedAt DEFAULT GETDATE() FOR UpdatedAt;

-- Add ThemePreference column to Users table
ALTER TABLE Users
ADD ThemePreference NVARCHAR(10) NOT NULL DEFAULT 'light';

-- Update existing user records to have the default theme
UPDATE Users
SET ThemePreference = 'light'
WHERE ThemePreference IS NULL;

-- CREATE TRIGGERS

-- 1. Trigger to update manga ViewCount when a view is added
USE [MangaReaderDB]
GO
CREATE OR ALTER TRIGGER TR_ViewCounts_Insert
ON [dbo].[ViewCounts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update the ViewCount in the Mangas table for affected manga IDs
    UPDATE m
    SET m.ViewCount = (
        SELECT COUNT(*) 
        FROM ViewCounts 
        WHERE MangaId = m.MangaId
    )
    FROM [dbo].[Mangas] m
    INNER JOIN inserted i ON m.MangaId = i.MangaId;
END;
GO

-- 2. Trigger to update MangaStatistics when a view is added
CREATE OR ALTER TRIGGER TR_ViewCounts_Insert_Statistics
ON [dbo].[ViewCounts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get today's date
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    
    -- For each manga in the inserted records
    MERGE INTO [dbo].[MangaStatistics] AS target
    USING (
        SELECT 
            i.MangaId,
            @Today AS [Date],
            COUNT(*) AS ViewCount,
            1 AS IsDaily,
            0 AS IsMonthly,
            0 AS IsYearly
        FROM 
            inserted i
        GROUP BY 
            i.MangaId
    ) AS source
    ON (target.MangaId = source.MangaId AND target.Date = source.Date AND target.IsDaily = 1)
    
    -- If matching record exists, update it
    WHEN MATCHED THEN
        UPDATE SET 
            target.ViewCount = target.ViewCount + source.ViewCount
            
    -- If no matching record, insert a new one
    WHEN NOT MATCHED THEN
        INSERT (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (source.MangaId, source.Date, source.ViewCount, 0, 1, 0, 0);
END;
GO

-- 3. Trigger to update FavoriteCount in MangaStatistics when a favorite is added
CREATE OR ALTER TRIGGER TR_Favorites_Insert
ON [dbo].[Favorites]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get today's date
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    
    -- For each manga in the inserted records
    MERGE INTO [dbo].[MangaStatistics] AS target
    USING (
        SELECT 
            i.MangaId,
            @Today AS [Date],
            COUNT(*) AS FavoriteCount,
            1 AS IsDaily,
            0 AS IsMonthly,
            0 AS IsYearly
        FROM 
            inserted i
        GROUP BY 
            i.MangaId
    ) AS source
    ON (target.MangaId = source.MangaId AND target.Date = source.Date AND target.IsDaily = 1)
    
    -- If matching record exists, update it
    WHEN MATCHED THEN
        UPDATE SET 
            target.FavoriteCount = target.FavoriteCount + source.FavoriteCount
            
    -- If no matching record, insert a new one
    WHEN NOT MATCHED THEN
        INSERT (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (source.MangaId, source.Date, 0, source.FavoriteCount, 1, 0, 0);
END;
GO

-- 4. Trigger to update FavoriteCount in MangaStatistics when a favorite is removed
CREATE OR ALTER TRIGGER TR_Favorites_Delete
ON [dbo].[Favorites]
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get today's date
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    
    -- For each manga in the deleted records
    MERGE INTO [dbo].[MangaStatistics] AS target
    USING (
        SELECT 
            d.MangaId,
            @Today AS [Date],
            COUNT(*) AS FavoriteCount,
            1 AS IsDaily,
            0 AS IsMonthly,
            0 AS IsYearly
        FROM 
            deleted d
        GROUP BY 
            d.MangaId
    ) AS source
    ON (target.MangaId = source.MangaId AND target.Date = source.Date AND target.IsDaily = 1)
    
    -- If matching record exists, update it
    WHEN MATCHED THEN
        UPDATE SET 
            target.FavoriteCount = target.FavoriteCount - source.FavoriteCount
            
    -- If no matching record (shouldn't happen, but just in case)
    WHEN NOT MATCHED THEN
        INSERT (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (source.MangaId, source.Date, 0, -source.FavoriteCount, 1, 0, 0);
END;
GO

-- 5. Trigger to update ChapterCount in Mangas when a chapter is added
CREATE OR ALTER TRIGGER TR_Chapters_Insert
ON [dbo].[Chapters]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update the ChapterCount in the Mangas table for affected manga IDs
    UPDATE m
    SET m.ChapterCount = (
        SELECT COUNT(*) 
        FROM Chapters 
        WHERE MangaId = m.MangaId
    ),
    m.UpdatedAt = GETDATE()
    FROM [dbo].[Mangas] m
    INNER JOIN inserted i ON m.MangaId = i.MangaId;
END;
GO

-- 6. Trigger to update ChapterCount in Mangas when a chapter is deleted
CREATE OR ALTER TRIGGER TR_Chapters_Delete
ON [dbo].[Chapters]
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update the ChapterCount in the Mangas table for affected manga IDs
    UPDATE m
    SET m.ChapterCount = (
        SELECT COUNT(*) 
        FROM Chapters 
        WHERE MangaId = m.MangaId
    ),
    m.UpdatedAt = GETDATE()
    FROM [dbo].[Mangas] m
    INNER JOIN deleted d ON m.MangaId = d.MangaId;
END;
GO

-- 7. Auto-update UpdatedAt column in Pages when a page is modified
CREATE OR ALTER TRIGGER TR_Pages_Update
ON [dbo].[Pages]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only update timestamp if the content has changed but the timestamp hasn't
    UPDATE p
    SET p.UpdatedAt = GETDATE()
    FROM [dbo].[Pages] p
    INNER JOIN inserted i ON p.PageId = i.PageId
    WHERE 
        (p.ImageUrl <> i.ImageUrl OR
         p.ImageHash <> i.ImageHash OR
         p.Width <> i.Width OR
         p.Height <> i.Height OR
         p.FileSize <> i.FileSize)
        AND p.UpdatedAt = i.UpdatedAt;
END;
GO

-- 8. Trigger to auto-update UpdatedAt in Mangas when related records change
CREATE OR ALTER TRIGGER TR_MangaGenres_Change
ON [dbo].[MangaGenres]
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- For inserts
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE m
        SET m.UpdatedAt = GETDATE()
        FROM [dbo].[Mangas] m
        INNER JOIN inserted i ON m.MangaId = i.MangaId;
    END
    
    -- For deletes
    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        UPDATE m
        SET m.UpdatedAt = GETDATE()
        FROM [dbo].[Mangas] m
        INNER JOIN deleted d ON m.MangaId = d.MangaId;
    END
END;
GO

-- 9. Trigger to calculate monthly statistics at the end of each month
CREATE OR ALTER TRIGGER TR_ViewCounts_MonthlyRollup
ON [dbo].[ViewCounts]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only run this logic on the first day of the month
    IF DAY(GETDATE()) = 1
    BEGIN
        DECLARE @LastMonth DATE = DATEADD(DAY, -1, CAST(GETDATE() AS DATE));
        DECLARE @FirstDayLastMonth DATE = DATEADD(DAY, 1-DAY(@LastMonth), @LastMonth);
        
        -- Check if monthly stats for last month already exist
        IF NOT EXISTS (
            SELECT 1 
            FROM [dbo].[MangaStatistics] 
            WHERE Date = @FirstDayLastMonth AND IsMonthly = 1
        )
        BEGIN
            -- Insert monthly statistics for each manga
            INSERT INTO [dbo].[MangaStatistics] (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
            SELECT 
                m.MangaId,
                @FirstDayLastMonth,
                ISNULL((
                    SELECT COUNT(*) 
                    FROM ViewCounts v
                    WHERE v.MangaId = m.MangaId 
                        AND v.ViewedAt >= @FirstDayLastMonth 
                        AND v.ViewedAt < CAST(GETDATE() AS DATE)
                ), 0),
                0, -- FavoriteCount not tracked monthly yet
                0, -- Not a daily record
                1, -- Is a monthly record
                0  -- Not a yearly record
            FROM Mangas m;
        END;
    END;
END;
GO

-- Delete all cache entries
DELETE FROM ApiCache;

-- Optional: Reclaim storage space after deletion
-- DBCC SHRINKDATABASE (MangaReaderDB, 10);