-- =============================================
-- SmartTour Dummy Data Generation Script (T-SQL)
-- Target: SQL Server
-- =============================================

BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Generate SELLERS (Role 2)
    PRINT 'Generating Sellers...';
    DECLARE @j INT = 1;
    WHILE @j <= 20
    BEGIN
        INSERT INTO Users (Username, FullName, Email, PasswordHash, RoleId, CreatedAt, IsActive, AuthProvider)
        VALUES (
            'seller' + CAST(@j AS VARCHAR), 
            N'Đối tác Seller ' + CAST(@j AS VARCHAR), 
            'seller' + CAST(@j AS VARCHAR) + '@smttest.com', 
            'AQAAAAEAACcQAAAAEPv...', -- Sample Hash (123456)
            2, 
            DATEADD(DAY, -(@j * 2), GETUTCDATE()), 
            1, 
            'Local'
        );
        SET @j = @j + 1;
    END

    -- 2. Generate VISITORS (Role 3)
    PRINT 'Generating Visitors...';
    SET @j = 1;
    WHILE @j <= 50
    BEGIN
        INSERT INTO Users (Username, FullName, Email, PasswordHash, RoleId, CreatedAt, IsActive, AuthProvider)
        VALUES (
            'visitor' + CAST(@j AS VARCHAR), 
            N'Khách du lịch ' + CAST(@j AS VARCHAR), 
            'visitor' + CAST(@j AS VARCHAR) + '@smttest.com', 
            'AQAAAAEAACcQAAAAEPv...', 
            3, 
            DATEADD(DAY, -@j, GETUTCDATE()), 
            1, 
            'Local'
        );
        SET @j = @j + 1;
    END

    -- 3. Generate POIs (Points of Interest)
    PRINT 'Generating POIs...';
    DECLARE @SellerList TABLE (ID INT, RowNum INT);
    INSERT INTO @SellerList SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM Users WHERE RoleId = 2;

    DECLARE @CatList TABLE (ID INT, RowNum INT);
    INSERT INTO @CatList SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM Categories;

    DECLARE @SCount INT = (SELECT COUNT(*) FROM @SellerList);
    DECLARE @CCount INT = (SELECT COUNT(*) FROM @CatList);

    SET @j = 1;
    WHILE @j <= 100
    BEGIN
        DECLARE @Sid INT = (SELECT ID FROM @SellerList WHERE RowNum = (@j % @SCount) + 1);
        DECLARE @Cid INT = (SELECT ID FROM @CatList WHERE RowNum = (@j % @CCount) + 1);
        
        INSERT INTO Pois (Name, Latitude, Longitude, GeofenceRadius, CategoryId, CreatedById, UpdatedById, IsActive, IsFeature, CreatedAt)
        VALUES (
            N'Điểm du lịch ' + CAST(@j AS VARCHAR),
            10.762 + (RAND() * 0.04 - 0.02),
            106.660 + (RAND() * 0.04 - 0.02),
            100 + (ABS(CHECKSUM(NEWID())) % 400),
            @Cid,
            @Sid,
            @Sid,
            1, -- IsActive
            0, -- IsFeature (Default to false)
            DATEADD(DAY, -(@j/2), GETUTCDATE())
        );
        SET @j = @j + 1;
    END

    -- 4. Generate PAYMENTS & SUBSCRIPTIONS
    PRINT 'Generating Payments and Subscriptions...';
    DECLARE @PkgList TABLE (ID INT, Code NVARCHAR(50), Price DECIMAL(18,2), RowNum INT);
    INSERT INTO @PkgList SELECT Id, Code, Price, ROW_NUMBER() OVER (ORDER BY Id) FROM ServicePackages WHERE Price > 0;
    DECLARE @PCount INT = (SELECT COUNT(*) FROM @PkgList);

    SET @j = 1;
    WHILE @j <= 150
    BEGIN
        DECLARE @Uid INT = (SELECT ID FROM @SellerList WHERE RowNum = (@j % @SCount) + 1);
        DECLARE @Prid INT, @Pcode NVARCHAR(50), @PriceVal DECIMAL(18,2);
        
        DECLARE @RandomPkgIndex INT = (ABS(CHECKSUM(NEWID())) % @PCount) + 1;
        SELECT @Prid = ID, @Pcode = Code, @PriceVal = Price FROM @PkgList WHERE RowNum = @RandomPkgIndex;

        DECLARE @PStatus NVARCHAR(20) = CASE WHEN (ABS(CHECKSUM(NEWID())) % 10) < 8 THEN 'Success' ELSE 'Failed' END;
        DECLARE @PDate DATETIME = DATEADD(DAY, - (ABS(CHECKSUM(NEWID())) % 60), GETUTCDATE());

        INSERT INTO Payments (UserId, PackageCode, Amount, ExternalTransactionNo, Status, Type, CreatedAt)
        VALUES (@Uid, @Pcode, @PriceVal, 'TEST' + LEFT(REPLACE(CAST(NEWID() AS VARCHAR(50)), '-', ''), 8), @PStatus, 'New', @PDate);

        IF @PStatus = 'Success'
        BEGIN
            DECLARE @Pid INT = SCOPE_IDENTITY();
            IF EXISTS (SELECT 1 FROM Subscriptions WHERE UserId = @Uid)
                UPDATE Subscriptions SET PackageId = @Prid, LastPaymentId = @Pid, StartDate = @PDate, EndDate = DATEADD(MONTH, 1, @PDate) WHERE UserId = @Uid;
            ELSE
                INSERT INTO Subscriptions (UserId, PackageId, LastPaymentId, PriceAtPurchase, StartDate, EndDate)
                VALUES (@Uid, @Prid, @Pid, @PriceVal, @PDate, DATEADD(MONTH, 1, @PDate));
        END
        SET @j = @j + 1;
    END

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Dummy data generated!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
END CATCH
