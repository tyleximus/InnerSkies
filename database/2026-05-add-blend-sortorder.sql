/* InnerSky migration: add SortOrder to EmotionProfiles for blend reordering within a moment.
   Idempotent — safe to run against an existing database without rebuilding from innersky.sql.
   Existing rows are backfilled by CreatedUtc so the current display order is preserved. */
USE [InnerSky];
GO

IF COL_LENGTH(N'dbo.EmotionProfiles', N'SortOrder') IS NULL
BEGIN
    ALTER TABLE dbo.EmotionProfiles
    ADD SortOrder INT NOT NULL CONSTRAINT DF_EmotionProfiles_SortOrder DEFAULT (0);
END;
GO

/* Backfill: per moment, number blends from 0 in CreatedUtc order. */
WITH Ordered AS
(
    SELECT
        Id,
        ROW_NUMBER() OVER (PARTITION BY MomentId ORDER BY CreatedUtc, Id) - 1 AS NewSortOrder
    FROM dbo.EmotionProfiles
)
UPDATE p
SET p.SortOrder = o.NewSortOrder
FROM dbo.EmotionProfiles p
INNER JOIN Ordered o ON o.Id = p.Id;
GO
