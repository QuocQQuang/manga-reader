-- Trigger khi xóa favorite
CREATE TRIGGER TR_Favorites_Delete
ON Favorites
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Cập nhật FavoriteCount trong bảng Manga
    UPDATE m
    SET m.FavoriteCount = CASE 
                           WHEN ISNULL(m.FavoriteCount, 0) > 0 THEN m.FavoriteCount - 1
                           ELSE 0
                          END
    FROM Mangas m
    INNER JOIN deleted d ON m.MangaId = d.MangaId;
END;