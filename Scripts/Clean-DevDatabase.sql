-- ========================================
-- CLEAN DEVELOPMENT DATABASE
-- Removes 470 MB of GeoJSON boundary data
-- ========================================

-- Step 1: Check current size
EXEC sp_spaceused;

-- Step 2: Clear boundary data (keep structure)
UPDATE Jurisdictions 
SET BoundaryData = NULL 
WHERE BoundaryData IS NOT NULL;

PRINT 'Cleared boundary data from ' + CAST(@@ROWCOUNT AS VARCHAR) + ' jurisdictions';

-- Step 3: Check table size after cleanup
SELECT 
    t.NAME AS TableName,
    CAST(ROUND(((SUM(a.total_pages) * 8) / 1024.00), 2) AS NUMERIC(36, 2)) AS TotalSpaceMB
FROM 
    sys.tables t
INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE 
    t.NAME = 'Jurisdictions'
GROUP BY t.Name;

-- Step 4: Shrink database to reclaim space
DBCC SHRINKDATABASE('aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d', 10);

-- Step 5: Check new size
EXEC sp_spaceused;

PRINT 'Database cleanup complete!';
PRINT 'Backups will now be much smaller (~20-30 MB instead of 500+ MB)';
