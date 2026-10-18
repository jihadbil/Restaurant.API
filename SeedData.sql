-- ==========================================================================================
-- اسم السكربت: SeedData.sql
-- الوصف: سكربت SQL شامل لتعبئة قاعدة بيانات نظام المطعم (RestaurantDb) ببيانات تجريبية وواقعية
-- يشمل: المطعم، المستخدمين والصلاحيات، الأقسام، المنتجات، الإضافات، طرق الدفع، الخزائن،
--        محطات الطباعة، الطابعات، والطلبات وحركات الخزينة والورديات.
-- ==========================================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

BEGIN TRY
    -- --------------------------------------------------------------------------------------
    -- 1. الأدوار (Roles)
    -- --------------------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
    BEGIN
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    END

    IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Cashier')
    BEGIN
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (NEWID(), 'Cashier', 'CASHIER', NEWID());
    END

    -- --------------------------------------------------------------------------------------
    -- 2. معلومات المطعم (RestaurantInfo)
    -- --------------------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Restaurants)
    BEGIN
        INSERT INTO Restaurants (Name, Address, PhoneNumber, TaxNumber)
        VALUES (N'مقهى ومطعم الجردينة', N'غريان - شارع الحرية', N'0919713563', N'123456789012345');
    END
    DECLARE @RestaurantId INT = (SELECT TOP 1 Id FROM Restaurants);

    -- --------------------------------------------------------------------------------------
    -- 3. المستخدمون والصلاحيات (Users & Permissions)
    -- --------------------------------------------------------------------------------------
    DECLARE @AdminId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = 'admin');
    DECLARE @CashierId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = 'cashier');

    -- تنشيط وفك القفل عن حساب الكاشير
    IF @CashierId IS NOT NULL
    BEGIN
        UPDATE AspNetUsers
        SET LockoutEnd = NULL,
            LockoutEnabled = 0,
            AccessFailedCount = 0,
            RestaurantId = @RestaurantId
        WHERE Id = @CashierId;
    END

    -- منح كامل صلاحيات النظام لحساب المدير (admin)
    IF @AdminId IS NOT NULL
    BEGIN
        DECLARE @AllPermissions TABLE (Permission NVARCHAR(100));
        INSERT INTO @AllPermissions (Permission) VALUES
            ('Permission.Dashboard'), ('Permission.NewOrder'), ('Permission.Orders'),
            ('Permission.Products'), ('Permission.Categories'), ('Permission.Reports'),
            ('Permission.Settings'), ('Permission.Treasury'), ('Permission.POS.ApplyDiscount'),
            ('Permission.POS.VoidItem'), ('Permission.Orders.VoidOrder'), ('Permission.Shift.ViewTotals'),
            ('Permission.Products.Manage'), ('Permission.Categories.Manage');

        INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)
        SELECT @AdminId, 'Permission', p.Permission
        FROM @AllPermissions p
        WHERE NOT EXISTS (
            SELECT 1 FROM AspNetUserClaims uc
            WHERE uc.UserId = @AdminId AND uc.ClaimType = 'Permission' AND uc.ClaimValue = p.Permission
        );
    END

    -- منح صلاحيات نقطة البيع والورديات لحساب الكاشير (cashier)
    IF @CashierId IS NOT NULL
    BEGIN
        DECLARE @CashierPermissions TABLE (Permission NVARCHAR(100));
        INSERT INTO @CashierPermissions (Permission) VALUES
            ('Permission.NewOrder'), ('Permission.Orders'),
            ('Permission.POS.ApplyDiscount'), ('Permission.Shift.ViewTotals'),
            ('Permission.Categories');

        INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)
        SELECT @CashierId, 'Permission', cp.Permission
        FROM @CashierPermissions cp
        WHERE NOT EXISTS (
            SELECT 1 FROM AspNetUserClaims uc
            WHERE uc.UserId = @CashierId AND uc.ClaimType = 'Permission' AND uc.ClaimValue = cp.Permission
        );
    END

    -- --------------------------------------------------------------------------------------
    -- 4. طرق الدفع (Payment Methods)
    -- --------------------------------------------------------------------------------------
    DECLARE @PaymentMethods TABLE (Name NVARCHAR(50), IsTaxFree BIT, IsCash BIT);
    INSERT INTO @PaymentMethods (Name, IsTaxFree, IsCash) VALUES
        (N'نقدي', 0, 1),
        (N'بطاقة خصم محلي', 0, 1),
        (N'سداد', 0, 0),
        (N'موبي كاش', 0, 1),
        (N'تداول', 0, 0),
        (N'ادفع لي', 0, 1);

    MERGE INTO PaymentMethods AS target
    USING @PaymentMethods AS source
    ON target.Name = source.Name
    WHEN NOT MATCHED THEN
        INSERT (Name, IsTaxFree, IsCash)
        VALUES (source.Name, source.IsTaxFree, source.IsCash);

    -- --------------------------------------------------------------------------------------
    -- 5. الخزائن وأدراج النقدية (Cashboxes & Drawers)
    -- --------------------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Cashboxes WHERE Name = N'الخزينة الرئيسية')
    BEGIN
        INSERT INTO Cashboxes (Name, Description, InitialBalance, IsActive, IsDrawer)
        VALUES (N'الخزينة الرئيسية', N'الخزينة المركزية للمطعم للإيداعات العامة والتحويلات', 10000.00, 1, 0);
    END

    IF NOT EXISTS (SELECT 1 FROM Cashboxes WHERE Name = N'خزينة درج نقطة البيع')
    BEGIN
        INSERT INTO Cashboxes (Name, Description, InitialBalance, IsActive, IsDrawer)
        VALUES (N'خزينة درج نقطة البيع', N'درج النقدية المخصص لمبيعات نقطة البيع والكاشير الرئيسي', 500.00, 1, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM Cashboxes WHERE Name = N'خزينة درج نقطة بيع 2')
    BEGIN
        INSERT INTO Cashboxes (Name, Description, InitialBalance, IsActive, IsDrawer)
        VALUES (N'خزينة درج نقطة بيع 2', N'درج النقدية لنقطة البيع الإضافية للكاشير الثاني', 300.00, 1, 1);
    END

    -- --------------------------------------------------------------------------------------
    -- 6. محطات الطباعة (Print Stations)
    -- --------------------------------------------------------------------------------------
    DECLARE @PrintStations TABLE (Name NVARCHAR(100));
    INSERT INTO @PrintStations (Name) VALUES
        (N'المطبخ الرئيسي'),
        (N'قسم الشاورما والمشاوي'),
        (N'قسم المشروبات والبار'),
        (N'طابعة الكاشير');

    MERGE INTO PrintStations AS target
    USING @PrintStations AS source
    ON target.Name = source.Name
    WHEN NOT MATCHED THEN
        INSERT (Name) VALUES (source.Name);

    DECLARE @StationKitchen INT = (SELECT TOP 1 Id FROM PrintStations WHERE Name = N'المطبخ الرئيسي');
    DECLARE @StationGrill INT = (SELECT TOP 1 Id FROM PrintStations WHERE Name = N'قسم الشاورما والمشاوي');
    DECLARE @StationBar INT = (SELECT TOP 1 Id FROM PrintStations WHERE Name = N'قسم المشروبات والبار');
    DECLARE @StationCashier INT = (SELECT TOP 1 Id FROM PrintStations WHERE Name = N'طابعة الكاشير');

    IF @StationKitchen IS NULL SET @StationKitchen = (SELECT TOP 1 Id FROM PrintStations);
    IF @StationGrill IS NULL SET @StationGrill = @StationKitchen;
    IF @StationBar IS NULL SET @StationBar = @StationKitchen;
    IF @StationCashier IS NULL SET @StationCashier = @StationKitchen;

    -- --------------------------------------------------------------------------------------
    -- 7. الطابعات (Printers)
    -- --------------------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Printers WHERE Name = N'طابعة ريسيت الكاشير')
    BEGIN
        INSERT INTO Printers (Name, PrinterName, PrinterType, PrintStationId)
        VALUES (N'طابعة ريسيت الكاشير', N'POS-80-Receipt', 0, @StationCashier);
    END

    IF NOT EXISTS (SELECT 1 FROM Printers WHERE Name = N'طابعة مطبخ الوجبات')
    BEGIN
        INSERT INTO Printers (Name, PrinterName, PrinterType, PrintStationId)
        VALUES (N'طابعة مطبخ الوجبات', N'Kitchen-Printer-01', 1, @StationKitchen);
    END

    IF NOT EXISTS (SELECT 1 FROM Printers WHERE Name = N'طابعة قسم الشاورما')
    BEGIN
        INSERT INTO Printers (Name, PrinterName, PrinterType, PrintStationId)
        VALUES (N'طابعة قسم الشاورما', N'Grill-Printer-02', 1, @StationGrill);
    END

    IF NOT EXISTS (SELECT 1 FROM Printers WHERE Name = N'طابعة البار والمشروبات')
    BEGIN
        INSERT INTO Printers (Name, PrinterName, PrinterType, PrintStationId)
        VALUES (N'طابعة البار والمشروبات', N'Bar-Printer-03', 1, @StationBar);
    END

    -- --------------------------------------------------------------------------------------
    -- 8. التصنيفات (Categories)
    -- --------------------------------------------------------------------------------------
    DECLARE @Categories TABLE (Name NVARCHAR(100));
    INSERT INTO @Categories (Name) VALUES
        (N'وجبات رئيسية'),
        (N'برجر'),
        (N'بيتزا'),
        (N'شاورما'),
        (N'مشويات'),
        (N'سندوتشات'),
        (N'مقبلات وسلطات'),
        (N'مطبخ لبناني'),
        (N'حلويات'),
        (N'مشروبات وعصائر');

    MERGE INTO Categories AS target
    USING @Categories AS source
    ON target.Name = source.Name
    WHEN NOT MATCHED THEN
        INSERT (Name) VALUES (source.Name);

    -- ربط التصنيفات بمحطات الطباعة
    DELETE FROM CategoryPrintStations;
    INSERT INTO CategoryPrintStations (CategoryId, PrintStationId)
    SELECT c.Id,
        CASE
            WHEN c.Name IN (N'وجبات رئيسية', N'بيتزا', N'برجر', N'سندوتشات') THEN @StationKitchen
            WHEN c.Name IN (N'شاورما', N'مشويات', N'مطبخ لبناني') THEN @StationGrill
            WHEN c.Name IN (N'مشروبات وعصائر', N'حلويات') THEN @StationBar
            ELSE @StationKitchen
        END
    FROM Categories c;

    -- --------------------------------------------------------------------------------------
    -- 9. الإضافات (Addons)
    -- --------------------------------------------------------------------------------------
    DECLARE @Addons TABLE (Name NVARCHAR(100));
    INSERT INTO @Addons (Name) VALUES
        (N'كاتشب'),
        (N'هريسة حارة'),
        (N'صوص تومية'),
        (N'جبنة شيدر سائلة'),
        (N'جبنة موزاريلا إضافية'),
        (N'صوص باربيكيو'),
        (N'صوص رانش'),
        (N'صوص سبايسي حار'),
        (N'بطاطس مقلية إضافية'),
        (N'مخلل إضافي'),
        (N'خبز صاج إضافي'),
        (N'بدون بصل'),
        (N'بدون طماطم'),
        (N'بدون مايونيز'),
        (N'بدون مخلل'),
        (N'حجم كبير (L)');

    MERGE INTO Addons AS target
    USING @Addons AS source
    ON target.Name = source.Name
    WHEN NOT MATCHED THEN
        INSERT (Name) VALUES (source.Name);

    -- --------------------------------------------------------------------------------------
    -- 10. المنتجات (Products)
    -- --------------------------------------------------------------------------------------
    DECLARE @CatMain INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'وجبات رئيسية');
    DECLARE @CatBurger INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'برجر');
    DECLARE @CatPizza INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'بيتزا');
    DECLARE @CatShawarma INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'شاورما');
    DECLARE @CatGrill INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'مشويات');
    DECLARE @CatSandwich INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'سندوتشات');
    DECLARE @CatAppetizer INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'مقبلات وسلطات');
    DECLARE @CatLebanese INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'مطبخ لبناني');
    DECLARE @CatDessert INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'حلويات');
    DECLARE @CatDrinks INT = (SELECT TOP 1 Id FROM Categories WHERE Name = N'مشروبات وعصائر');

    DECLARE @Products TABLE (
        BarCode NVARCHAR(50),
        Name NVARCHAR(150),
        Description NVARCHAR(500),
        CostPrice DECIMAL(18,2),
        SalePrice DECIMAL(18,2),
        CategoryId INT
    );

    INSERT INTO @Products (BarCode, Name, Description, CostPrice, SalePrice, CategoryId) VALUES
    -- وجبات رئيسية
    ('1001', N'صحن كباب مشكل ملوكي', N'أسياخ كباب لحم وشيش طاووق مع أرز بسمتي وسلطة وخبز', 22.00, 35.00, @CatMain),
    ('1002', N'نصف دجاجة شواية مع أرز', N'نصف دجاجة متبلة ومحمرة على الفحم مع أرز مبهر وصوص ثومية', 15.00, 25.00, @CatMain),
    ('1003', N'إسكالوب دجاج بانيه', N'صدر دجاج مقلي مقرمش مع بطاطس بوريه وخضار سوتيه', 17.00, 28.00, @CatMain),
    ('1004', N'طاجين لحم بالبرقوق واللوز', N'لحم بتلو مطهو على الطريقة المغاربية بصوص البرقوق واللوز', 26.00, 42.00, @CatMain),
    ('1005', N'برياني دجاج حيدر آبادي', N'أرز برياني معطر بالتوابل الهندية مع قطع الدجاج وصوص الرايتا', 16.00, 27.00, @CatMain),

    -- برجر
    ('2001', N'برجر لحم كلاسيك سنجل', N'شريحة لحم بقري 150 جم مشوية مع الخس والطماطم وصوص البرجر', 10.00, 18.00, @CatBurger),
    ('2002', N'برجر لحم دبل تشيز', N'شريحتان لحم بقري 300 جم مع طبقتين جبن شيدر وصوص خاص', 15.00, 27.00, @CatBurger),
    ('2003', N'برجر دجاج مقرمش كريسبي', N'قطعة دجاج مقرمشة مع الخس والمايونيز والجبن الأمريكي', 11.00, 20.00, @CatBurger),
    ('2004', N'برجر مشروم سويسري', N'برجر لحم مع فطر طازج مطهو بصلصة الكريمة والجبنة السويسرية', 14.00, 24.00, @CatBurger),
    ('2005', N'زنجر دجاج حار سوبريم', N'فيليه دجاج حار ومقرمش مع شرائح الهالبينو وصلصة الرانش الحارة', 12.00, 22.00, @CatBurger),

    -- بيتزا
    ('3001', N'بيتزا مارجريتا إيطالية', N'صلصة طماطم إيطالية مع جبنة موزاريلا طازجة وريحان وزيت زيتون', 11.00, 20.00, @CatPizza),
    ('3002', N'بيتزا بيبروني لحم بقري', N'شرائح بيبروني بقري مدخن مع جبنة موزاريلا وصلصة الطماطم', 14.00, 26.00, @CatPizza),
    ('3003', N'بيتزا دجاج باربيكيو مدخن', N'قطع دجاج مشوي مع صوص الباربيكيو والبصل المكرمل والجبن', 15.00, 27.00, @CatPizza),
    ('3004', N'بيتزا سوبر سوبريم مشكلة', N'بيبروني ولحم مفروم وفلفل رومي وزيتون ومشروم وجبنة موزاريلا', 17.00, 32.00, @CatPizza),
    ('3005', N'بيتزا خضار مشكلة', N'مشروم وفلفل وزيتون أسود وطماطم وذرة حلوة مع الموزاريلا', 12.00, 22.00, @CatPizza),
    ('3006', N'بيتزا فور تشيز (أربعة أجبان)', N'موزاريلا وجبنة شيدر وجبنة بارميزان وجبنة ريكوتا', 16.00, 29.00, @CatPizza),

    -- شاورما
    ('4001', N'شاورما دجاج صاج كبير', N'شاورما دجاج بخبز الصاج المحمص مع الثومية ومخلل الخيار', 7.50, 14.00, @CatShawarma),
    ('4002', N'شاورما لحم صاج كبير', N'شاورما لحم عجل بخبز الصاج مع صوص الطحينة والبقدونس والبصل', 10.00, 18.00, @CatShawarma),
    ('4003', N'صحن شاورما عربي دجاج', N'قطع شاورما دجاج مقطعة مع بطاطس مقلية وثومية ومخللات مشكلة', 12.50, 23.00, @CatShawarma),
    ('4004', N'صحن شاورما عربي لحم', N'قطع شاورما لحم عجل مقطعة مع بطاطس وطحينة ومخللات وسلطة', 15.50, 28.00, @CatShawarma),

    -- مشويات
    ('5001', N'كباب لحم غنم بلدي مشوي', N'3 أسياخ كباب غنم مفروم مع البصل والبقدونس والمشويات على الفحم', 24.00, 39.00, @CatGrill),
    ('5002', N'شيش طاووق على الفحم', N'3 أسياخ صدور دجاج متبلة بالزبادي والليمون مشوية على الفحم', 16.00, 29.00, @CatGrill),
    ('5003', N'ريش لحم ضأن مشوية', N'أربع قطع ريش ضأن بتتبيلة الأعشاب الخاصة مشوية بدرجة ممتازة', 32.00, 52.00, @CatGrill),
    ('5004', N'أوصال لحم عجل مشوي', N'مكعبات لحم عجل طري مشوي مع البصل والطماطم والفلفل الأخضر', 23.00, 38.00, @CatGrill),

    -- سندوتشات
    ('6001', N'سندوتش فاهيتا دجاج مكسيكي', N'دجاج متبل مع شرائح الفلفل الملون والبصل والجبن في خبز باجيت', 9.00, 16.00, @CatSandwich),
    ('6002', N'سندوتش كفتة مشوية بلدي', N'أصابع كفتة مشوية مع سلطة طحينة ومخلل في خبز طازج', 8.50, 15.00, @CatSandwich),
    ('6003', N'سندوتش كلوب هاوس تريبل', N'توست محمص ثلاث طبقات مع تركي مدخن وجبن وبيض وخس وطماطم', 9.50, 17.00, @CatSandwich),
    ('6004', N'سندوتش فيليه لحم ستيك', N'شرائح لحم بقري رقيقة مع مشروم وجبنة بروفولون وبصل مكرمل', 12.00, 21.00, @CatSandwich),

    -- مقبلات وسلطات
    ('7001', N'بطاطس مقلية مقرمشة', N'أصابع بطاطس ذهبية ومملحة مع كاتشب ومايونيز', 3.50, 8.00, @CatAppetizer),
    ('7002', N'بطاطس بالجبنة والبيكون', N'بطاطس مغطاة بصوص جبنة شيدر السائل مع قطع البيكون المقرمش', 7.00, 15.00, @CatAppetizer),
    ('7003', N'أصابع موزاريلا مقلية (5 قطع)', N'أصابع جبنة موزاريلا ذائبة ومقرمشة مع صلصة المارينارا', 6.00, 13.00, @CatAppetizer),
    ('7004', N'حلقات بصل مقرمشة (8 قطع)', N'حلقات بصل ذهبية مغلفة بخلطة مقرمشة مع صوص الرانش', 4.50, 11.00, @CatAppetizer),
    ('7005', N'سلطة سيزر دجاج', N'خس روماني مع قطع دجاج مشوي وخبز محمص وجبن بارميزان وصوص سيزر', 9.50, 19.00, @CatAppetizer),
    ('7006', N'سلطة فتوش لبنانية', N'خضار طازجة مع خبز مقرمش وسماق ودبس رمان وزيت زيتون بكر', 5.50, 13.00, @CatAppetizer),
    ('7007', N'حمص بيروتي بالصنوبر', N'حمص ناعم بالطحينة وزيت الزيتون البكر ومزين بالصنوبر المحمص', 4.50, 11.00, @CatAppetizer),

    -- مطبخ لبناني
    ('8001', N'كبة مقلية باللحم (4 حبات)', N'كبة برغل مقرمشة محشوة باللحم المفروم والمكسرات والبصل', 8.00, 16.00, @CatLebanese),
    ('8002', N'صحن فلافل لبناني مشكل', N'أقراص فلافل مقرمشة مع مخلل لفت وطماطم وطحينة وخبز', 4.50, 11.00, @CatLebanese),
    ('8003', N'تبولة لبنانية أصلية', N'بقدونس مفروم ناعم مع برغل وطماطم ونعناع وعصير ليمون وزيت زيتون', 5.00, 12.00, @CatLebanese),
    ('8004', N'متبل باذنجان مدخن', N'باذنجان مشوي على الفحم ومهروس مع الطحينة والثوم وزيت الزيتون', 4.50, 11.00, @CatLebanese),

    -- حلويات
    ('9001', N'كنافة نابلسية بالجبن الفاخر', N'كنافة خشنة محشوة بالجبن العكاوي الذائب ومسقية بالقطر والفستق', 8.50, 17.00, @CatDessert),
    ('9002', N'كيك شوكولاتة فادج مع آيسكريم', N'كيك شوكولاتة غني بصوص الشوكولاتة الساخن وبولة آيس كريم فانيليا', 7.00, 15.00, @CatDessert),
    ('9003', N'تشيز كيك توت أزرق نيو يورك', N'تشيز كيك كريمي مخبوز بطبقة بسكويت مقرمشة وتوت بري طازج', 7.50, 16.00, @CatDessert),
    ('9004', N'أم علي ملوكية بالمكسرات', N'رقائق العجين بالحليب المكثف والقشطة والمكسرات المحمصة في الفرن', 6.00, 13.00, @CatDessert),

    -- مشروبات وعصائر
    ('9501', N'عصير برتقال فريش طازج', N'عصير برتقال طبيعي معصور طازج بدون أي إضافات سكر', 3.00, 8.00, @CatDrinks),
    ('9502', N'ليموناضة بالنعناع المثلجة', N'عصير ليمون طازج مثلج مع أوراق النعناع المنعشة', 2.50, 7.00, @CatDrinks),
    ('9503', N'موهيتو فراولة وسفن أب', N'مشروب منعش مع صوص الفراولة والنعناع والليمون ومشروب غازي', 3.50, 11.00, @CatDrinks),
    ('9504', N'كوكاكولا / بيبسي كانز 330 مل', N'مشروب غازي مثلج', 1.50, 3.50, @CatDrinks),
    ('9505', N'مياه معدنية طبيعية 500 مل', N'مياه شرب نقية باردة', 0.50, 1.50, @CatDrinks),
    ('9506', N'إسبريسو دبل إيطالي', N'قهوة إسبريسو مركزة ومحمصة بعناية من حبوب الأرابيكا', 1.50, 5.00, @CatDrinks),
    ('9507', N'كابتشينو برغوة الحليب', N'إسبريسو مع حليب مبخر ورغوة حليب كريمية ورشة كاكاو', 2.50, 8.00, @CatDrinks),
    ('9508', N'شاي كرك عدني بالهيل والزعفران', N'شاي بالحليب المركز والتوابل العطرية الأصيلة', 1.50, 5.00, @CatDrinks);

    MERGE INTO Products AS target
    USING @Products AS source
    ON target.Name = source.Name
    WHEN MATCHED THEN
        UPDATE SET
            target.BarCode = source.BarCode,
            target.Description = source.Description,
            target.CostPrice = source.CostPrice,
            target.SalePrice = source.SalePrice,
            target.CategoryId = source.CategoryId
    WHEN NOT MATCHED THEN
        INSERT (BarCode, Name, Description, CostPrice, SalePrice, CategoryId)
        VALUES (source.BarCode, source.Name, source.Description, source.CostPrice, source.SalePrice, source.CategoryId);

    -- --------------------------------------------------------------------------------------
    -- 11. الورديات والطلبات وحركات الخزينة التجريبية (Shifts, Orders & Cash Ledger)
    -- --------------------------------------------------------------------------------------
    DECLARE @UserId NVARCHAR(450) = COALESCE(@CashierId, @AdminId, (SELECT TOP 1 Id FROM AspNetUsers));
    DECLARE @CashboxDrawerId INT = (SELECT TOP 1 Id FROM Cashboxes WHERE IsDrawer = 1 AND IsActive = 1);
    IF @CashboxDrawerId IS NULL SET @CashboxDrawerId = (SELECT TOP 1 Id FROM Cashboxes WHERE Name LIKE N'%درج%');
    IF @CashboxDrawerId IS NULL SET @CashboxDrawerId = (SELECT TOP 1 Id FROM Cashboxes);

    DECLARE @SafeCashboxId INT = (SELECT TOP 1 Id FROM Cashboxes WHERE IsDrawer = 0);
    IF @SafeCashboxId IS NULL SET @SafeCashboxId = @CashboxDrawerId;

    DECLARE @PayCashId INT = (SELECT TOP 1 Id FROM PaymentMethods WHERE IsCash = 1);
    DECLARE @PayCardId INT = (SELECT TOP 1 Id FROM PaymentMethods WHERE IsCash = 0);
    IF @PayCashId IS NULL SET @PayCashId = (SELECT TOP 1 Id FROM PaymentMethods);
    IF @PayCardId IS NULL SET @PayCardId = @PayCashId;

    -- إضافة وردية مغلقة سابقة للتجربة
    IF NOT EXISTS (SELECT 1 FROM CashShifts WHERE ClientRequestId = 'seed-shift-yesterday')
    BEGIN
        INSERT INTO CashShifts (
            SourceCashboxId, TargetCashboxId, PreviousShiftId, LastEntryId,
            StartedAt, ClosedAt, ExpectedCash, ActualCash, Difference,
            Notes, ClientRequestId, UserId
        )
        VALUES (
            @CashboxDrawerId, @SafeCashboxId, 0, 0,
            DATEADD(HOUR, -24, GETDATE()), DATEADD(HOUR, -16, GETDATE()),
            1250.00, 1250.00, 0.00,
            N'وردية مسائية تجريبية مغلقة بنجاح ومطابقة', 'seed-shift-yesterday', @UserId
        );
    END
    DECLARE @ShiftId INT = (SELECT TOP 1 Id FROM CashShifts WHERE ClientRequestId = 'seed-shift-yesterday');

    -- إضافة طلبات واقعية متنوعة لاختبار شاشة الطلبات وتقارير المبيعات
    DECLARE @OrderCount INT = (SELECT COUNT(*) FROM Orders WHERE ClientRequestId LIKE 'seed-order-%');
    IF @OrderCount = 0
    BEGIN
        DECLARE @P_Burger INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%برجر لحم كلاسيك%');
        DECLARE @P_DblBurger INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%برجر لحم دبل%');
        DECLARE @P_Fries INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%بطاطس مقلية مقرمشة%');
        DECLARE @P_Cola INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%كوكاكولا%');
        DECLARE @P_Water INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%مياه معدنية%');
        DECLARE @P_Kebab INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%صحن كباب مشكل%');
        DECLARE @P_Pizza INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%بيتزا بيبروني%');
        DECLARE @P_Shawarma INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%شاورما دجاج صاج%');
        DECLARE @P_Juice INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%عصير برتقال%');
        DECLARE @P_Dessert INT = (SELECT TOP 1 Id FROM Products WHERE Name LIKE N'%كنافة نابلسية%');

        -- طلب 1: صالة - تم التسليم (نقدي)
        DECLARE @NextNum INT = 200101;
        INSERT INTO Orders (OrderNumber, ClientRequestId, Total, Cost, Profit, Discount, Date, OrderStatus, OrderType, Notes, UserId)
        VALUES (@NextNum, 'seed-order-1', 48.00, 25.50, 22.50, 0.00, DATEADD(HOUR, -2, GETDATE()), 2, 0, N'طلب صالة طاولة رقم 4', @UserId);
        DECLARE @O1 INT = SCOPE_IDENTITY();
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitSalePrice, UnitCostPrice, UnitDiscount, Total, Notes) VALUES
        (@O1, @P_Burger, 2, 18.00, 10.00, 0.00, 36.00, N'بدون بصل'),
        (@O1, @P_Fries, 1, 8.00, 3.50, 0.00, 8.00, N'كاتشب إضافي'),
        (@O1, @P_Cola, 2, 2.00, 1.00, 0.00, 4.00, N'بارد');
        INSERT INTO CashDrawerEntries (Date, Amount, AffectsCash, ClientRequestId, CashShiftId, EntryType, Notes, CashboxId, PaymentMethodId, OrderId, UserId)
        VALUES (DATEADD(HOUR, -2, GETDATE()), 48.00, 1, 'seed-cde-1', @ShiftId, 1, N'سداد طلب رقم ' + CAST(@NextNum AS NVARCHAR(20)), @CashboxDrawerId, @PayCashId, @O1, @UserId);

        -- طلب 2: سفري - تم التسليم (بطاقة)
        SET @NextNum = 200102;
        INSERT INTO Orders (OrderNumber, ClientRequestId, Total, Cost, Profit, Discount, Date, OrderStatus, OrderType, Notes, UserId)
        VALUES (@NextNum, 'seed-order-2', 41.00, 20.00, 21.00, 0.00, DATEADD(HOUR, -4, GETDATE()), 2, 1, N'سفري مع أكياس إضافية', @UserId);
        DECLARE @O2 INT = SCOPE_IDENTITY();
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitSalePrice, UnitCostPrice, UnitDiscount, Total, Notes) VALUES
        (@O2, @P_Shawarma, 2, 14.00, 7.50, 0.00, 28.00, N'ثومية زيادة'),
        (@O2, @P_Fries, 1, 8.00, 3.50, 0.00, 8.00, N'لا يوجد ملاحظات'),
        (@O2, @P_Cola, 1, 3.00, 1.50, 0.00, 3.00, N'لا يوجد ملاحظات'),
        (@O2, @P_Water, 1, 2.00, 0.50, 0.00, 2.00, N'لا يوجد ملاحظات');
        INSERT INTO CashDrawerEntries (Date, Amount, AffectsCash, ClientRequestId, CashShiftId, EntryType, Notes, CashboxId, PaymentMethodId, OrderId, UserId)
        VALUES (DATEADD(HOUR, -4, GETDATE()), 41.00, 0, 'seed-cde-2', @ShiftId, 1, N'سداد إلكتروني طلب رقم ' + CAST(@NextNum AS NVARCHAR(20)), @CashboxDrawerId, @PayCardId, @O2, @UserId);

        -- طلب 3: توصيل - تم التسليم (نقدي مع خصم)
        SET @NextNum = 200103;
        INSERT INTO Orders (OrderNumber, ClientRequestId, Total, Cost, Profit, Discount, Date, OrderStatus, OrderType, Notes, UserId)
        VALUES (@NextNum, 'seed-order-3', 75.00, 40.00, 35.00, 5.00, DATEADD(HOUR, -6, GETDATE()), 2, 2, N'توصيل حي الأندلس هاتف 0912345678', @UserId);
        DECLARE @O3 INT = SCOPE_IDENTITY();
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitSalePrice, UnitCostPrice, UnitDiscount, Total, Notes) VALUES
        (@O3, @P_Pizza, 2, 26.00, 14.00, 2.50, 47.00, N'كاتشب وصوص حار'),
        (@O3, @P_Dessert, 1, 17.00, 8.50, 0.00, 17.00, N'ساخنة'),
        (@O3, @P_Juice, 2, 8.00, 3.00, 0.00, 16.00, N'طازج بدون سكر');
        INSERT INTO CashDrawerEntries (Date, Amount, AffectsCash, ClientRequestId, CashShiftId, EntryType, Notes, CashboxId, PaymentMethodId, OrderId, UserId)
        VALUES (DATEADD(HOUR, -6, GETDATE()), 75.00, 1, 'seed-cde-3', @ShiftId, 1, N'سداد نقدي طلب توصيل رقم ' + CAST(@NextNum AS NVARCHAR(20)), @CashboxDrawerId, @PayCashId, @O3, @UserId);

        -- طلب 4: صالة - جاهز للاستلام
        SET @NextNum = 200104;
        INSERT INTO Orders (OrderNumber, ClientRequestId, Total, Cost, Profit, Discount, Date, OrderStatus, OrderType, Notes, UserId)
        VALUES (@NextNum, 'seed-order-4', 35.00, 18.00, 17.00, 0.00, DATEADD(MINUTE, -25, GETDATE()), 1, 0, N'جاهز طاولة رقم 2', @UserId);
        DECLARE @O4 INT = SCOPE_IDENTITY();
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitSalePrice, UnitCostPrice, UnitDiscount, Total, Notes) VALUES
        (@O4, @P_DblBurger, 1, 27.00, 15.00, 0.00, 27.00, N'سبايسي'),
        (@O4, @P_Fries, 1, 8.00, 3.50, 0.00, 8.00, N'لا يوجد ملاحظات');
        INSERT INTO CashDrawerEntries (Date, Amount, AffectsCash, ClientRequestId, CashShiftId, EntryType, Notes, CashboxId, PaymentMethodId, OrderId, UserId)
        VALUES (DATEADD(MINUTE, -25, GETDATE()), 35.00, 1, 'seed-cde-4', @ShiftId, 1, N'سداد طلب رقم ' + CAST(@NextNum AS NVARCHAR(20)), @CashboxDrawerId, @PayCashId, @O4, @UserId);

        -- طلب 5: سفري - قيد التحضير في المطبخ
        SET @NextNum = 200105;
        INSERT INTO Orders (OrderNumber, ClientRequestId, Total, Cost, Profit, Discount, Date, OrderStatus, OrderType, Notes, UserId)
        VALUES (@NextNum, 'seed-order-5', 56.00, 31.00, 25.00, 0.00, DATEADD(MINUTE, -10, GETDATE()), 0, 1, N'قيد التحضير في المطبخ سفري', @UserId);
        DECLARE @O5 INT = SCOPE_IDENTITY();
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitSalePrice, UnitCostPrice, UnitDiscount, Total, Notes) VALUES
        (@O5, @P_Shawarma, 2, 14.00, 7.50, 0.00, 28.00, N'طحينة زيادة'),
        (@O5, @P_Shawarma, 2, 14.00, 7.50, 0.00, 28.00, N'بدون مخلل');
        INSERT INTO CashDrawerEntries (Date, Amount, AffectsCash, ClientRequestId, CashShiftId, EntryType, Notes, CashboxId, PaymentMethodId, OrderId, UserId)
        VALUES (DATEADD(MINUTE, -10, GETDATE()), 56.00, 1, 'seed-cde-5', @ShiftId, 1, N'سداد طلب رقم ' + CAST(@NextNum AS NVARCHAR(20)), @CashboxDrawerId, @PayCashId, @O5, @UserId);
    END

    COMMIT TRANSACTION;
    PRINT N'تم تنفيذ سكربت SeedData.sql بنجاح وتعبئة قاعدة البيانات بالكامل!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
