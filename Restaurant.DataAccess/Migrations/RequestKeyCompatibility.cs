namespace Restaurant.DataAccess.Migrations;

internal static class RequestKeyCompatibility
{
    // The old schema cannot store per-user duplicate keys. Keep a lossless mapping for the next upgrade.
    internal const string PrepareDowngrade = """
        IF OBJECT_ID(N'[__OrderRequestKeyBackups]', N'U') IS NULL
            CREATE TABLE [__OrderRequestKeyBackups] (
                [OrderId] int NOT NULL PRIMARY KEY, [UserId] nvarchar(450) NOT NULL,
                [OriginalKey] nvarchar(64) NOT NULL, [DowngradeKey] nvarchar(64) NOT NULL);

        DECLARE @OrderId int, @UserId nvarchar(450), @OriginalKey nvarchar(64), @DowngradeKey nvarchar(64);
        DECLARE duplicates CURSOR LOCAL FAST_FORWARD FOR
            SELECT [Id], [UserId], [ClientRequestId] FROM (
                SELECT [Id], [UserId], [ClientRequestId],
                    ROW_NUMBER() OVER (PARTITION BY [ClientRequestId] ORDER BY [Id]) AS rn
                FROM [Orders] WHERE [ClientRequestId] IS NOT NULL
            ) ranked WHERE rn > 1;
        OPEN duplicates;
        FETCH NEXT FROM duplicates INTO @OrderId, @UserId, @OriginalKey;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @DowngradeKey = N'legacy:' + CONVERT(nvarchar(36), NEWID());
            WHILE EXISTS (SELECT 1 FROM [Orders] WHERE [ClientRequestId] = @DowngradeKey)
                SET @DowngradeKey = N'legacy:' + CONVERT(nvarchar(36), NEWID());
            IF EXISTS (SELECT 1 FROM [__OrderRequestKeyBackups] WHERE [OrderId] = @OrderId)
                UPDATE [__OrderRequestKeyBackups] SET [UserId] = @UserId,
                    [OriginalKey] = @OriginalKey, [DowngradeKey] = @DowngradeKey WHERE [OrderId] = @OrderId;
            ELSE
                INSERT INTO [__OrderRequestKeyBackups] VALUES (@OrderId, @UserId, @OriginalKey, @DowngradeKey);
            UPDATE [Orders] SET [ClientRequestId] = @DowngradeKey WHERE [Id] = @OrderId;
            FETCH NEXT FROM duplicates INTO @OrderId, @UserId, @OriginalKey;
        END;
        CLOSE duplicates;
        DEALLOCATE duplicates;
        """;

    internal const string RestoreOnUpgrade = """
        IF OBJECT_ID(N'[__OrderRequestKeyBackups]', N'U') IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1 FROM [__OrderRequestKeyBackups] b
                JOIN [Orders] o ON o.[Id] = b.[OrderId] AND o.[UserId] = b.[UserId]
                    AND o.[ClientRequestId] = b.[DowngradeKey]
                JOIN [Orders] other ON other.[Id] <> o.[Id] AND other.[UserId] = b.[UserId]
                    AND other.[ClientRequestId] = b.[OriginalKey]
            )
                THROW 51001, N'An original request key was reused during downgrade. Review __OrderRequestKeyBackups before upgrading; no keys were discarded.', 1;
            UPDATE o SET [ClientRequestId] = b.[OriginalKey]
            FROM [Orders] o JOIN [__OrderRequestKeyBackups] b ON b.[OrderId] = o.[Id]
                AND b.[UserId] = o.[UserId] AND o.[ClientRequestId] = b.[DowngradeKey];
        END;
        """;
}
