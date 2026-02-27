-- Script để tạo bảng MonthlyStats
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MonthlyStats]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MonthlyStats] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Month] DATETIME2 NOT NULL,
        [NewUsers] INT NOT NULL DEFAULT 0,
        [NewManga] INT NOT NULL DEFAULT 0,
        [NewChapters] INT NOT NULL DEFAULT 0,
        [TotalViews] BIGINT NOT NULL DEFAULT 0,
        [UniqueVisitors] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [PK_MonthlyStats] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [IX_MonthlyStats_Month] UNIQUE NONCLUSTERED ([Month] ASC)
    );
    
    PRINT 'Bảng MonthlyStats đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng MonthlyStats đã tồn tại.';
END