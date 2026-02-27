-- File path: c:\Users\QQ\Desktop\Git\Mangarea\Mangareading\SQL\trigger.sql
USE [MangaReaderDB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	Cập nhật Manga.UpdatedAt khi thông tin manga thay đổi
-- =============================================
CREATE TRIGGER [dbo].[TRG_Mangas_UpdateTimestamp]
   ON  [dbo].[Mangas]
   AFTER UPDATE
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Chỉ cập nhật nếu các cột không phải là UpdatedAt thay đổi
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE m
        SET UpdatedAt = GETDATE()
        FROM [dbo].[Mangas] m
        INNER JOIN inserted i ON m.MangaId = i.MangaId;
    END
END
GO

ALTER TABLE [dbo].[Mangas] ENABLE TRIGGER [TRG_Mangas_UpdateTimestamp]
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	Cập nhật số lượng chapter trong bảng Mangas khi thêm/xóa chapter
-- =============================================
CREATE TRIGGER [dbo].[TRG_Chapters_UpdateMangaChapterCount]
   ON  [dbo].[Chapters]
   AFTER INSERT, DELETE
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @MangaId INT;

    -- Lấy MangaId từ bảng inserted (khi INSERT) hoặc deleted (khi DELETE)
    IF EXISTS (SELECT 1 FROM inserted)
        SELECT @MangaId = MangaId FROM inserted;
    ELSE
        SELECT @MangaId = MangaId FROM deleted;

    -- Cập nhật ChapterCount trong bảng Mangas
    IF @MangaId IS NOT NULL
    BEGIN
        UPDATE m
        SET ChapterCount = (SELECT COUNT(*) FROM [dbo].[Chapters] c WHERE c.MangaId = m.MangaId)
        FROM [dbo].[Mangas] m
        WHERE m.MangaId = @MangaId;
    END
END
GO

ALTER TABLE [dbo].[Chapters] ENABLE TRIGGER [TRG_Chapters_UpdateMangaChapterCount]
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	Cập nhật ViewCount trong bảng Mangas khi có lượt xem mới
-- =============================================
CREATE TRIGGER [dbo].[TRG_ViewCounts_UpdateMangaViewCount]
   ON  [dbo].[ViewCounts]
   AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;

    UPDATE m
    SET ViewCount = ISNULL(m.ViewCount, 0) + 1 -- Tăng ViewCount lên 1
    FROM [dbo].[Mangas] m
    INNER JOIN inserted i ON m.MangaId = i.MangaId;

END
GO

ALTER TABLE [dbo].[ViewCounts] ENABLE TRIGGER [TRG_ViewCounts_UpdateMangaViewCount]
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	Cập nhật thống kê lượt xem hàng ngày trong MangaStatistics
-- =============================================
CREATE TRIGGER [dbo].[TRG_ViewCounts_UpdateDailyStats]
   ON  [dbo].[ViewCounts]
   AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MangaId INT;
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    SELECT @MangaId = MangaId FROM inserted;

    -- Kiểm tra xem đã có bản ghi thống kê cho manga này vào ngày hôm nay chưa
    IF EXISTS (SELECT 1 FROM [dbo].[MangaStatistics] WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1)
    BEGIN
        -- Nếu có, cập nhật ViewCount
        UPDATE [dbo].[MangaStatistics]
        SET ViewCount = ViewCount + 1
        WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1;
    END
    ELSE
    BEGIN
        -- Nếu chưa có, tạo bản ghi mới
        INSERT INTO [dbo].[MangaStatistics] (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
        VALUES (@MangaId, @Today, 1, 0, 1, 0, 0); -- Giả sử FavoriteCount không thay đổi ở đây
    END
END
GO

ALTER TABLE [dbo].[ViewCounts] ENABLE TRIGGER [TRG_ViewCounts_UpdateDailyStats]
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	Cập nhật thống kê lượt yêu thích hàng ngày trong MangaStatistics
-- =============================================
CREATE TRIGGER [dbo].[TRG_Favorites_UpdateDailyStats]
   ON  [dbo].[Favorites]
   AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MangaId INT;
    DECLARE @Change INT;
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    -- Xác định MangaId và sự thay đổi (+1 khi thêm, -1 khi xóa)
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        SELECT @MangaId = MangaId FROM inserted;
        SET @Change = 1;
    END
    ELSE
    BEGIN
        SELECT @MangaId = MangaId FROM deleted;
        SET @Change = -1;
    END

    -- Kiểm tra xem đã có bản ghi thống kê cho manga này vào ngày hôm nay chưa
    IF EXISTS (SELECT 1 FROM [dbo].[MangaStatistics] WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1)
    BEGIN
        -- Nếu có, cập nhật FavoriteCount
        UPDATE [dbo].[MangaStatistics]
        SET FavoriteCount = FavoriteCount + @Change
        WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1;
    END
    ELSE
    BEGIN
        -- Nếu chưa có (chỉ xảy ra khi INSERT), tạo bản ghi mới
        IF @Change = 1
        BEGIN
            INSERT INTO [dbo].[MangaStatistics] (MangaId, Date, ViewCount, FavoriteCount, IsDaily, IsMonthly, IsYearly)
            VALUES (@MangaId, @Today, 0, 1, 1, 0, 0); -- Giả sử ViewCount không thay đổi ở đây
        END
        -- Trường hợp DELETE mà không có bản ghi thống kê thì không cần làm gì
    END

    -- Đảm bảo FavoriteCount không âm
    UPDATE [dbo].[MangaStatistics]
    SET FavoriteCount = 0
    WHERE MangaId = @MangaId AND Date = @Today AND IsDaily = 1 AND FavoriteCount < 0;

END
GO

ALTER TABLE [dbo].[Favorites] ENABLE TRIGGER [TRG_Favorites_UpdateDailyStats]
GO
