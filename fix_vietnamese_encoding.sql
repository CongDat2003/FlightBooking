-- ============================================
-- Script để sửa lỗi encoding tiếng Việt
-- ============================================

-- 1. Kiểm tra collation hiện tại của database
SELECT 
    DATABASEPROPERTYEX('FlightBooking', 'Collation') AS DatabaseCollation,
    DATABASEPROPERTYEX('FlightBooking', 'SQLSortOrder') AS SQLSortOrder;

-- 2. Kiểm tra collation của các cột trong bảng addons (nếu có)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'addons')
BEGIN
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        COLLATION_NAME
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'addons' 
        AND COLLATION_NAME IS NOT NULL
    ORDER BY ORDINAL_POSITION;
END

-- 3. Cập nhật collation cho database (nếu cần)
-- Lưu ý: Chỉ chạy lệnh này nếu database chưa có collation phù hợp
-- ALTER DATABASE FlightBooking COLLATE SQL_Latin1_General_CP1_CI_AS;

-- 4. Cập nhật collation cho các cột trong bảng addons (nếu bảng tồn tại)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'addons')
BEGIN
    -- Kiểm tra và cập nhật cột name
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'name')
    BEGIN
        -- Đảm bảo cột là NVARCHAR (Unicode)
        DECLARE @nameType NVARCHAR(100);
        SELECT @nameType = DATA_TYPE + 
            CASE 
                WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(MAX)'
                WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                ELSE ''
            END
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'name';
        
        IF @nameType LIKE 'VARCHAR%'
        BEGIN
            -- Chuyển từ VARCHAR sang NVARCHAR
            ALTER TABLE addons 
            ALTER COLUMN name NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật cột name từ VARCHAR sang NVARCHAR';
        END
        ELSE IF @nameType LIKE 'NVARCHAR%'
        BEGIN
            -- Chỉ cập nhật collation nếu cần
            ALTER TABLE addons 
            ALTER COLUMN name NVARCHAR(255) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật collation cho cột name';
        END
    END
    
    -- Kiểm tra và cập nhật cột description
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'description')
    BEGIN
        DECLARE @descType NVARCHAR(100);
        SELECT @descType = DATA_TYPE + 
            CASE 
                WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(MAX)'
                WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                ELSE ''
            END
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'description';
        
        IF @descType LIKE 'VARCHAR%'
        BEGIN
            ALTER TABLE addons 
            ALTER COLUMN description NVARCHAR(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật cột description từ VARCHAR sang NVARCHAR';
        END
        ELSE IF @descType LIKE 'NVARCHAR%'
        BEGIN
            ALTER TABLE addons 
            ALTER COLUMN description NVARCHAR(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật collation cho cột description';
        END
    END
    
    -- Kiểm tra và cập nhật cột conditions (nếu có)
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'conditions')
    BEGIN
        DECLARE @condType NVARCHAR(100);
        SELECT @condType = DATA_TYPE + 
            CASE 
                WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN '(MAX)'
                WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                ELSE ''
            END
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'addons' AND COLUMN_NAME = 'conditions';
        
        IF @condType LIKE 'VARCHAR%'
        BEGIN
            ALTER TABLE addons 
            ALTER COLUMN conditions NVARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật cột conditions từ VARCHAR sang NVARCHAR';
        END
        ELSE IF @condType LIKE 'NVARCHAR%'
        BEGIN
            ALTER TABLE addons 
            ALTER COLUMN conditions NVARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS;
            PRINT 'Đã cập nhật collation cho cột conditions';
        END
    END
END
ELSE
BEGIN
    PRINT 'Bảng addons không tồn tại trong database';
END

-- 5. Kiểm tra lại collation sau khi cập nhật
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'addons')
BEGIN
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        COLLATION_NAME
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'addons' 
        AND COLLATION_NAME IS NOT NULL
    ORDER BY ORDINAL_POSITION;
END

PRINT 'Hoàn tất kiểm tra và cập nhật collation!';




