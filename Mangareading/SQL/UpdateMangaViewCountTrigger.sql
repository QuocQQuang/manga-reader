-- Kiểm tra xem trigger đã tồn tại chưa, nếu có thì xóa đi để tạo lại
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[trg_UpdateMangaViewCount]'))
DROP TRIGGER [dbo].[trg_UpdateMangaViewCount]
GO

-- Tạo trigger cho Insert vào bảng ViewCounts
CREATE TRIGGER [dbo].[trg_UpdateMangaViewCount]
ON [dbo].[ViewCounts]
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Tạo bảng tạm lưu trữ MangaId cần cập nhật
    DECLARE @UpdatedMangas TABLE (MangaId INT);
    
    -- Lấy danh sách MangaId từ các hàng được thêm vào
    INSERT INTO @UpdatedMangas (MangaId)
    SELECT DISTINCT MangaId FROM inserted
    UNION
    SELECT DISTINCT MangaId FROM deleted;
    
    -- Cập nhật ViewCount trong bảng Mangas dựa trên số lượng thực tế trong ViewCounts
    UPDATE m
    SET m.ViewCount = (
        SELECT COUNT(*)
        FROM ViewCounts vc
        WHERE vc.MangaId = m.MangaId
    )
    FROM Mangas m
    INNER JOIN @UpdatedMangas um ON m.MangaId = um.MangaId;
    
    -- Log thông tin về việc cập nhật (tùy chọn)
    DECLARE @UpdatedCount INT = (SELECT COUNT(*) FROM @UpdatedMangas);
    
    -- Có thể thêm log vào bảng SystemLogs nếu cần
    -- INSERT INTO SystemLogs (LogType, Message, CreatedAt)
    -- VALUES ('TriggerExecution', 'Updated ViewCount for ' + CAST(@UpdatedCount AS VARCHAR) + ' manga(s)', GETDATE());
END
GO

-- Tạo thêm trigger cho Update (nếu có thay đổi MangaId)
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[trg_UpdateViewCountOnChange]'))
DROP TRIGGER [dbo].[trg_UpdateViewCountOnChange]
GO

CREATE TRIGGER [dbo].[trg_UpdateViewCountOnChange]
ON [dbo].[ViewCounts]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ xử lý khi có sự thay đổi MangaId
    IF UPDATE(MangaId)
    BEGIN
        -- Tạo bảng tạm lưu trữ MangaId cần cập nhật
        DECLARE @UpdatedMangas TABLE (MangaId INT);
        
        -- Lấy danh sách MangaId từ cả giá trị cũ và mới
        INSERT INTO @UpdatedMangas (MangaId)
        SELECT DISTINCT MangaId FROM inserted
        UNION
        SELECT DISTINCT MangaId FROM deleted;
        
        -- Cập nhật ViewCount trong bảng Mangas
        UPDATE m
        SET m.ViewCount = (
            SELECT COUNT(*)
            FROM ViewCounts vc
            WHERE vc.MangaId = m.MangaId
        )
        FROM Mangas m
        INNER JOIN @UpdatedMangas um ON m.MangaId = um.MangaId;
    END
END
GO

-- Thông báo hoàn thành
PRINT 'Triggers for updating Manga ViewCount have been created successfully.';