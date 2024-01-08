-- Specify your table and columns
DECLARE @TableName NVARCHAR(128) = 'YourTableName';
DECLARE @IndexColumns NVARCHAR(MAX) = 'Column1, Column2'; -- Replace with your column names

-- Check if the index already exists
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE t.name = @TableName
    AND i.type_desc = 'NONCLUSTERED'
    AND i.is_primary_key = 0 -- Exclude primary key indexes
    AND i.is_unique = 0 -- Exclude unique indexes
    AND i.name IS NOT NULL -- Exclude heap (table without clustered index)
    AND c.name IN (SELECT value FROM STRING_SPLIT(@IndexColumns, ','))
)
BEGIN
    PRINT 'The specified non-clustered index already exists on the table.';
END
ELSE
BEGIN
    PRINT 'The specified non-clustered index does not exist on the table.';
END;