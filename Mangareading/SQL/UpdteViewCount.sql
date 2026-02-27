-- Cập nhật ViewCount trong bảng Manga dựa trên số lượng thực tế trong ViewCounts
UPDATE Mangas
SET ViewCount = (
    SELECT COUNT(*)
    FROM ViewCounts
    WHERE ViewCounts.MangaId = Mangas.MangaId
)