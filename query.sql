CREATE FUNCTION dbo.STRING_SPLIT_UDF
(
    @String NVARCHAR(MAX),
    @Delimiter NVARCHAR(255)
)
RETURNS TABLE
AS
RETURN
(
    WITH SplitCTE AS
    (
        SELECT 
            CAST('<' + REPLACE(@String, @Delimiter, '</><') + '</>' AS XML) AS SplitXML
    )
    SELECT 
        LTRIM(RTRIM(T.c.value('.', 'NVARCHAR(MAX)'))) AS Value
    FROM 
        SplitCTE
    CROSS APPLY 
        SplitXML.nodes('/root/row') T(c)
);
------------------------------------
-- Specify your table and columns
DECLARE @TableName NVARCHAR(128) = 'YourTableName';
DECLARE @IndexName NVARCHAR(128) = 'IX_NonClusteredIndex'; -- Specify the desired index name
DECLARE @IndexColumns NVARCHAR(MAX) = 'Column1, Column2'; -- Replace with your column names

-- Check if the index already exists
IF NOT EXISTS (
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
    -- AND c.name IN (SELECT value FROM dbo.STRING_SPLIT_UDF(@IndexColumns, ','))
    AND CHARINDEX(c.name, @IndexColumns) > 0
)
BEGIN
    -- Create the non-clustered index
    DECLARE @IndexCreationQuery NVARCHAR(MAX);
    SET @IndexCreationQuery = 
        'CREATE NONCLUSTERED INDEX ' + @IndexName +
        ' ON ' + @TableName + ' (' + @IndexColumns + ');';

    EXEC sp_executesql @IndexCreationQuery;

    PRINT 'Non-clustered index created successfully.';
END
ELSE
BEGIN
    PRINT 'The specified non-clustered index already exists on the table.';
END;


CREATE PROCEDURE GetFeedsWithPagination
    @PageNumber INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT YourColumns
    FROM YourTable
    ORDER BY YourOrderByColumns
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;


DECLARE @TableName NVARCHAR(128) = 'YourTableName' -- Change this to your table name
DECLARE @SchemaName NVARCHAR(128) = 'dbo'          -- Change this to your schema if needed
DECLARE @ColumnList NVARCHAR(MAX) = ''
DECLARE @ColumnConditions NVARCHAR(MAX) = ''
DECLARE @ParameterList NVARCHAR(MAX) = ''
DECLARE @InsertStatement NVARCHAR(MAX) = ''
DECLARE @GeneratedScript NVARCHAR(MAX) = ''

-- 1. Get the list of columns and parameters for the specified table
SELECT 
    @ColumnList = STRING_AGG('[' + COLUMN_NAME + ']', ', '),
    @ColumnConditions = STRING_AGG('[' + COLUMN_NAME + '] = @' + COLUMN_NAME, ' AND '),
    @ParameterList = STRING_AGG('@' + COLUMN_NAME, ', ')
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = @SchemaName

-- 2. Generate the dynamic INSERT statement with the column and parameter lists
SET @InsertStatement = 'INSERT INTO [' + @SchemaName + '].[' + @TableName + '] (' + @ColumnList + ') VALUES (' + @ParameterList + ')'

-- 3. Generate the entire script with the conditional check
SET @GeneratedScript = '
DECLARE ' + REPLACE(@ColumnList, ', ', ' NVARCHAR(MAX), ') + ' NVARCHAR(MAX);

-- Sample values for demonstration; replace with actual values or logic to pull these from a source
' + REPLACE('SET ' + REPLACE(@ColumnList, '[', '@'), '] = ''SampleValue'';', ', ', ' SET ') + '

IF NOT EXISTS (
    SELECT 1 FROM [' + @SchemaName + '].[' + @TableName + ']
    WHERE ' + @ColumnConditions + '
)
BEGIN
    ' + @InsertStatement + '
END
ELSE
BEGIN
    PRINT ''Record already exists in [' + @SchemaName + '].[' + @TableName + ']. No insert required.'';
END;
'

-- 4. Print or execute the generated script
PRINT @GeneratedScript

