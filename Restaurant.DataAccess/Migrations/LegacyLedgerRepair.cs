namespace Restaurant.DataAccess.Migrations;

internal static class LegacyLedgerRepair
{
    // This audit table intentionally survives downgrades. Never discard the original financial values.
    internal const string Sql = """
        IF OBJECT_ID(N'[__LegacyLedgerRepairs]', N'U') IS NULL
            CREATE TABLE [__LegacyLedgerRepairs] (
                [Id] bigint IDENTITY PRIMARY KEY, [BatchId] uniqueidentifier NOT NULL,
                [EntryId] int NOT NULL, [OldAmount] decimal(18,2) NOT NULL, [NewAmount] decimal(18,2) NOT NULL,
                [OldAffectsCash] bit NOT NULL, [NewAffectsCash] bit NOT NULL,
                [Reason] nvarchar(100) NOT NULL, [RepairedAtUtc] datetime2 NOT NULL,
                [CashAdjustmentEntryId] int NULL);
        DECLARE @BatchId uniqueidentifier = NEWID();

        -- Only pre-fingerprint checkouts of the known legacy default are eligible.
        -- Renaming/changing other payment methods must never rewrite their historical snapshots.
        UPDATE e SET [AffectsCash] = 0
        OUTPUT @BatchId, inserted.[Id], deleted.[Amount], inserted.[Amount], deleted.[AffectsCash],
            inserted.[AffectsCash], N'Legacy default card classification', SYSUTCDATETIME()
        INTO [__LegacyLedgerRepairs]
            ([BatchId], [EntryId], [OldAmount], [NewAmount], [OldAffectsCash], [NewAffectsCash], [Reason], [RepairedAtUtc])
        FROM [CashDrawerEntries] e JOIN [PaymentMethods] p ON p.[Id] = e.[PaymentMethodId]
        JOIN [Orders] o ON o.[Id] = e.[OrderId]
        WHERE p.[Name] = N'بطاقة' AND p.[IsCash] = 0 AND e.[AffectsCash] = 1
            AND e.[EntryType] IN (1, 4) AND o.[RequestFingerprint] IS NULL;

        -- Correct only the machine-generated backfill; manually posted refunds are never reduced.
        ;WITH Totals AS (
            SELECT e.[OrderId], e.[CashboxId], e.[PaymentMethodId], e.[AffectsCash],
                SUM(CASE WHEN e.[EntryType] = 1 THEN e.[Amount]
                    WHEN e.[Notes] = N'استرداد تلقائي لطلب ملغى سابقاً' THEN 0 ELSE -e.[Amount] END) AS Due
            FROM [CashDrawerEntries] e JOIN [Orders] o ON o.[Id] = e.[OrderId]
            WHERE o.[OrderStatus] = 3 AND e.[EntryType] IN (1, 4)
            GROUP BY e.[OrderId], e.[CashboxId], e.[PaymentMethodId], e.[AffectsCash]
        ), AutomaticRefunds AS (
            SELECT e.[Id], e.[Amount], t.Due,
                SUM(e.[Amount]) OVER (PARTITION BY e.[OrderId], e.[CashboxId], e.[PaymentMethodId], e.[AffectsCash]
                    ORDER BY e.[Id] ROWS UNBOUNDED PRECEDING) - e.[Amount] AS PriorAmount
            FROM [CashDrawerEntries] e JOIN Totals t ON t.[OrderId] = e.[OrderId] AND t.[CashboxId] = e.[CashboxId]
                AND (t.[PaymentMethodId] = e.[PaymentMethodId] OR (t.[PaymentMethodId] IS NULL AND e.[PaymentMethodId] IS NULL))
                AND t.[AffectsCash] = e.[AffectsCash]
            WHERE e.[EntryType] = 4 AND e.[Notes] = N'استرداد تلقائي لطلب ملغى سابقاً'
        ), Targets AS (
            SELECT [Id], CASE WHEN Due <= PriorAmount THEN 0 WHEN Due - PriorAmount < [Amount]
                THEN Due - PriorAmount ELSE [Amount] END AS CorrectAmount FROM AutomaticRefunds
        )
        UPDATE e SET [Amount] = t.CorrectAmount
        OUTPUT @BatchId, inserted.[Id], deleted.[Amount], inserted.[Amount], deleted.[AffectsCash],
            inserted.[AffectsCash], N'Automatic refund overpayment', SYSUTCDATETIME()
        INTO [__LegacyLedgerRepairs]
            ([BatchId], [EntryId], [OldAmount], [NewAmount], [OldAffectsCash], [NewAffectsCash], [Reason], [RepairedAtUtc])
        FROM [CashDrawerEntries] e JOIN Targets t ON t.[Id] = e.[Id] WHERE e.[Amount] <> t.CorrectAmount;

        ;WITH Remaining AS (
            SELECT e.[OrderId], e.[CashboxId], e.[PaymentMethodId], e.[AffectsCash], MIN(e.[UserId]) AS [UserId],
                SUM(CASE WHEN e.[EntryType] = 1 THEN e.[Amount] ELSE -e.[Amount] END) AS Due
            FROM [CashDrawerEntries] e JOIN [Orders] o ON o.[Id] = e.[OrderId]
            WHERE o.[OrderStatus] = 3 AND e.[EntryType] IN (1, 4)
            GROUP BY e.[OrderId], e.[CashboxId], e.[PaymentMethodId], e.[AffectsCash]
        )
        INSERT INTO [CashDrawerEntries]
            ([Date], [Amount], [EntryType], [Notes], [CashboxId], [PaymentMethodId], [OrderId], [UserId], [AffectsCash], [PaymentMethodNameSnapshot])
        OUTPUT @BatchId, inserted.[Id], 0, inserted.[Amount], inserted.[AffectsCash], inserted.[AffectsCash],
            N'Missing automatic refund', SYSUTCDATETIME()
        INTO [__LegacyLedgerRepairs]
            ([BatchId], [EntryId], [OldAmount], [NewAmount], [OldAffectsCash], [NewAffectsCash], [Reason], [RepairedAtUtc])
        SELECT SYSDATETIME(), r.Due, 4, N'استرداد تلقائي لطلب ملغى سابقاً', r.[CashboxId],
            r.[PaymentMethodId], r.[OrderId], r.[UserId], r.[AffectsCash], p.[Name]
        FROM Remaining r LEFT JOIN [PaymentMethods] p ON p.[Id] = r.[PaymentMethodId] WHERE r.Due > 0;

        -- A past physical count/transfer is a fact. Offset changes to its closed ledger in the current period.
        DECLARE @ClosedRepairs TABLE ([RepairId] bigint PRIMARY KEY);
        INSERT INTO @ClosedRepairs
        SELECT r.[Id] FROM [__LegacyLedgerRepairs] r JOIN [CashDrawerEntries] e ON e.[Id] = r.[EntryId]
        LEFT JOIN [Orders] o ON o.[Id] = e.[OrderId]
        WHERE r.[BatchId] = @BatchId AND EXISTS (
            SELECT 1 FROM [CashShifts] s WHERE s.[SourceCashboxId] = e.[CashboxId] AND (
                s.[LastEntryId] >= e.[Id] OR (
                    r.[Reason] = N'Missing automatic refund' AND COALESCE(o.[CancelledAt], o.[Date]) <= s.[ClosedAt]
                    AND EXISTS (SELECT 1 FROM [CashDrawerEntries] payment WHERE payment.[OrderId] = e.[OrderId]
                        AND payment.[CashboxId] = e.[CashboxId] AND payment.[EntryType] = 1
                        AND payment.[Id] <= s.[LastEntryId])
                )
            ));
        DECLARE @CashboxId int, @Delta decimal(18,2), @UserId nvarchar(450), @AdjustmentId int;
        DECLARE closedCorrections CURSOR LOCAL FAST_FORWARD FOR
            SELECT e.[CashboxId], SUM((CASE WHEN e.[EntryType] = 1 THEN 1 ELSE -1 END) *
                (CASE WHEN r.[NewAffectsCash] = 1 THEN r.[NewAmount] ELSE 0 END
                 - CASE WHEN r.[OldAffectsCash] = 1 THEN r.[OldAmount] ELSE 0 END)), MIN(e.[UserId])
            FROM [__LegacyLedgerRepairs] r JOIN [CashDrawerEntries] e ON e.[Id] = r.[EntryId]
            JOIN @ClosedRepairs closed ON closed.[RepairId] = r.[Id]
            GROUP BY e.[CashboxId];
        OPEN closedCorrections;
        FETCH NEXT FROM closedCorrections INTO @CashboxId, @Delta, @UserId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @Delta <> 0
            BEGIN
                INSERT INTO [CashDrawerEntries] ([Date], [Amount], [EntryType], [Notes], [CashboxId], [UserId], [AffectsCash])
                    VALUES (SYSDATETIME(), ABS(@Delta), CASE WHEN @Delta > 0 THEN 7 ELSE 8 END,
                        N'تسوية تصحيح بيانات قديمة مع حفظ الجرد الفعلي للورديات المغلقة', @CashboxId, @UserId, 1);
                SET @AdjustmentId = CAST(SCOPE_IDENTITY() AS int);
                UPDATE r SET [CashAdjustmentEntryId] = @AdjustmentId
                FROM [__LegacyLedgerRepairs] r JOIN [CashDrawerEntries] e ON e.[Id] = r.[EntryId]
                JOIN @ClosedRepairs closed ON closed.[RepairId] = r.[Id]
                WHERE e.[CashboxId] = @CashboxId;
            END;
            FETCH NEXT FROM closedCorrections INTO @CashboxId, @Delta, @UserId;
        END;
        CLOSE closedCorrections;
        DEALLOCATE closedCorrections;
        """;
}
