# مراجعة النظام — 15–16 أكتوبر 2026

> تحديث 16 أكتوبر: نُفذت إصلاحات النتائج. انظر [سجل المعالجة والتحقق ومتطلبات الترحيل](C:/Users/Masoud/source/repos/Restaurant.API/docs/audit-fixes-2026-10-16.md). ما يلي يوثق الحالة وقت المراجعة قبل الإصلاح.

## النطاق وطريقة التحقق

شملت المراجعة `Restaurant.API` و`Restaurant.Services` و`Restaurant.DataAccess` و`Restaurant.Models` و`Restaurant.Models.DTOs` و`restaurant.web` والاختبارات والترحيلات وإعدادات التشغيل. استُبعد مشروع `Restaurant.Desktop` بالكامل من التحليل والبناء والاختبارات.

راجعت الحالة الحالية للملفات، بما فيها التعديلات الموجودة مسبقاً والملفات غير المتتبعة في Git. لم أعدّل شيفرة التطبيق، ولم أشغّل الترحيلات أو سكربت البيانات على قاعدة المطعم. الملف الحالي هو مخرج المراجعة داخل المستودع.

للتحقق من المشكلات استُخدم برنامج منفصل خارج المستودع، مع SQLite في الذاكرة وASP.NET TestServer ورموز JWT فعلية. يستخدم البرنامج المتحكمات والخدمات الحقيقية وسياسات مكافئة للسياسات الموجودة في `Program.cs`؛ لا يشغّل تهيئة الإنتاج أو Seeder على قاعدة تشغيلية. واستخدمت اختبارات متصفح Edge مع طلبات API مستبدلة للتحقق من سلوك الواجهة دون إرسال أعمال إلى الطابعة.

الأولوية P1: مشكلة تؤثر بصورة كبيرة في الصلاحيات أو المال أو سلامة البيانات. الأولوية P2: مشكلة وظيفية أو دقة أو موثوقية أو خطر مشروط يحتاج إصلاحاً. ليست الأولوية تقديراً لاحتمال الاستغلال، وتُذكر شروط المخاطر المشروطة صراحةً.

النتيجة: **30 ملاحظة قابلة للمعالجة: 11 بأولوية P1 و19 بأولوية P2**. بعض النتائج مثبتة بالتنفيذ، وبعضها مثبت مباشرة من مسار الكود، وبعض المخاطر مرتبط بظروف تشغيل أو صيانة محددة. نجاح الاختبارات الحالية لا يغطي هذه السيناريوهات جميعاً.

## نتائج الفحوص

| الفحص | النتيجة | الدلالة والحدود |
| --- | --- | --- |
| `dotnet test Restaurant.Services.Tests/Restaurant.Services.Tests.csproj` | نجح 36/36 | اختبارات الخدمات والخزينة وHTTP الحالية |
| `npm run build` داخل الويب | ناجح | TypeScript وVite؛ تحذير حزمة JavaScript بحجم 588.44 kB قبل gzip |
| `npm run lint` | فشل: 16 خطأ، صفر تحذيرات | تشمل قواعد hooks وpurity وFast Refresh و`any`؛ ليست جميعها أخطاء تشغيل |
| `node --test tests/printing.browser.test.cjs` | نجح 7/7 | Edge دون طابعة فعلية؛ اسم المطعم، CSV، المعاينة، ترتيب الطباعة، وتوليد PDF |
| برنامج التحقق المعزول للخادم | نجح تشغيله وأعاد الأدلة الواردة أدناه | قاعدة SQLite مؤقتة؛ لا يثبت سلوك أقفال SQL Server تحت الحمل |
| اختبارات المتصفح الإضافية | أكدت 3 مشكلات | تصدير بيانات الفترة السابقة، صيغة CSV غير محيدة، وطلب DELETE عند الإلغاء |
| `Database.HasPendingModelChanges()` باستخدام موفر SQL Server | `false` | النموذج الحالي متوافق مع آخر snapshot |
| توليد SQL للترحيلات | نجح: 7 ترحيلات، 16,579 حرفاً | توليد دون اتصال بقاعدة؛ لا يعني نجاح الترحيل على بيانات حقيقية |
| `npm audit --omit=dev --json` | صفر حزم مبلغ عنها | ضمن قاعدة بيانات advisories المتاحة وقت الفحص |
| `npm audit --json` | حزمة واحدة بتصنيف High: Vite | حزمة تطوير؛ تفاصيل F29 |
| `dotnet list Restaurant.API/Restaurant.API.csproj package --vulnerable --include-transitive --no-restore --format json` | `System.Security.Cryptography.Pkcs 9.0.4`، تنبيهان High وModerate | تفاصيل F28؛ المحاولة الأولى فشلت شبكياً، والثانية اكتملت |

## النتائج ذات الأولوية العالية

### F01 — P1 — تنفيذ البيع وتعديل الطلبات لا يفرضان صلاحية العملية

**المواضع:** [OrdersController.cs:78](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/OrdersController.cs:78)، [OrdersController.cs:110](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/OrdersController.cs:110)، [OrderService.cs:25](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:25).

`checkout` وقراءة الطلبات وتحديثها تعتمد السياسة العامة التي تشترط تسجيل الدخول فقط. لا يوجد تحقق من `Permission.NewOrder` للبيع أو `Permission.Orders` للقراءة والتعديل. توجد فحوص للخصم والإلغاء، لكنها لا تحمي أصل العملية.

**الإثبات:** حساب يحمل فقط المطالبات غير القياسية `POS, Reports, Orders`، دون الأذونات المعتمدة، حصل على `201` عند الدفع و`200` عند قراءة جميع الطلبات و`204` عند تعديل طلب. إخفاء الشاشة في React لا يمنع طلب API مباشر. الاستجابة كشفت أيضاً تكلفة الطلب وربحه وحركات دفعه، ما يضعف حجب المعلومات المالية للمستخدم محدود الصلاحية.

**المعالجة:** سياسات صريحة لكل عملية، وفصل بيانات التشغيل اللازمة للكاشير عن التكاليف والأرباح والإجماليات المقيدة. إضافة اختبارات مصفوفة أدوار وأذونات للمتحكمات، تشمل الحساب المسجل بلا أذونات.

### F02 — P1 — إدارة المستخدمين تحفظ مفاتيح صلاحيات لا يفهمها النظام

**المواضع:** [SettingsPage.tsx:493](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/settings/pages/SettingsPage.tsx:493)، [SettingsPage.tsx:521](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/settings/pages/SettingsPage.tsx:521)، [AuthService.cs:88](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/AuthService.cs:88)، [App.tsx:78](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/App.tsx:78).

الإعدادات تستخدم `Dashboard`, `POS`, `Reports`, `Treasury` وغيرها، وترسلها دون تحويل. التحقق في الويب والخادم يستخدم `Permission.Dashboard`, `Permission.NewOrder`, `Permission.Reports`, `Permission.Treasury`. إضافة البادئة وحدها لا تكفي؛ `POS` يجب تحويله إلى `Permission.NewOrder` تحديداً.

**الإثبات:** التسجيل أعاد المطالبات كما أُرسلت؛ حساب يحمل `Reports` حصل على `403` من تقرير الخادم. وعند تحرير مستخدم لديه المطالبات الصحيحة لا تتطابق معها مربعات الاختيار؛ لذلك تعجز الواجهة عن عرض هذه الأذونات وسحبها بصورة موثوقة، ويمكن أن تضيف مطالبات غير صالحة إلى جانبها.

**المعالجة:** تعريف مركزي موحد للأذونات، والتحقق من القائمة المسموحة على الخادم، وتحويل القيم القديمة بترحيل مدروس. اختبارات عقد بين الإعدادات وسياسات الخادم وحراس المسارات.

### F03 — P1 — سحب دور المدير وتغيير كلمة المرور لا يبطلان JWT الصادر

**المواضع:** [Program.cs:118](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Program.cs:118)، [AuthService.cs:170](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/AuthService.cs:170)، [UserService.cs:157](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/UserService.cs:157).

المطالبات والأدوار داخل الرمز تُقبل حتى انتهاء صلاحيته. لا يوجد تحقق من نسخة جلسة أو `SecurityStamp` أو حالة الحساب عند استقبال الرمز. تحديث Identity لا يغيّر الرمز الموجود عند العميل.

**الإثبات:** أُصدر رمز لحساب مدير، ثم أُزيل دوره من قاعدة الاختبار؛ بقي `POST /api/products` يعيد `201` بالرمز القديم. وبعد إعادة تعيين كلمة المرور بقي الطلب نفسه ناجحاً. المدة الحالية 60 دقيقة، ويمكن ضبطها حتى 24 ساعة وفق التحقق الموجود.

**المعالجة:** آلية إبطال جلسات مرتبطة بالمستخدم، مع فحص نسخة أو ختم أمني عند التحقق من JWT وتحديثه عند تغيير الدور أو الأذونات أو كلمة المرور. المدة القصيرة تساعد لكنها لا تعالج الإبطال الفوري وحدها.

### F04 — P1 — زر إلغاء الفاتورة يستخدم مسار الحذف الذي يرفض الفواتير المدفوعة

**المواضع:** [OrdersPage.tsx:95](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/orders/pages/OrdersPage.tsx:95)، [OrdersController.cs:158](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/OrdersController.cs:158)، [OrderService.cs:349](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:349).

الواجهة ترسل `DELETE /api/orders/{id}` عند الإلغاء. الخادم يقصر الحذف على المدير ويرفض أي طلب له حركة مالية، ويطلب استخدام الإلغاء. المسار الصحيح الموجود هو تحديث الحالة إلى `Cancelled`، والذي ينشئ الاسترداد داخل المعاملة.

**الإثبات:** اختبار متصفح أكد إرسال DELETE وإظهار رسالة رفض الفاتورة المدفوعة. المستخدم صاحب `Permission.Orders.VoidOrder` دون دور Admin يصطدم أيضاً بقيد الدور على DELETE. أزرار تغيير الحالة الأخرى لا تقدم إجراء إلغاء بديل.

**المعالجة:** إجراء إلغاء واضح يحدّث الحالة عبر مسار الاسترداد ويحتفظ بالفاتورة والسجل المالي. إبقاء الحذف الإداري للطلبات غير المالية فقط، وتسميات منفصلة للإجراءين.

### F05 — P1 — تقارير المنتجات والتصنيفات تضخّم الربح عند خصم الطلب

**المواضع:** [ReportService.cs:98](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:98)، [ReportService.cs:174](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:174)، [ReportService.cs:206](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:206).

التقرير اليومي يجمع `Order.Total` بعد خصم الطلب. تقارير المنتجات والتصنيفات تجمع `OrderItem.Total` دون توزيع `Order.Discount`، فتحسب إيراداً وربحاً أكبر من المبيعات الفعلية. و`TotalDiscount` في التقرير الشامل يجمع خصم الطلب فقط دون خصومات الوحدات.

**الإثبات:** في قاعدة الاختبار بيعان بإجمالي أصناف 200 وتكلفة 80 وخصم طلب 20: التقرير الشامل أعاد مبيعات 180 وربحاً 100؛ تقرير المنتجات أعاد إيراداً 200 وربحاً 120.

**المعالجة:** توزيع خصم الطلب على البنود بسياسة ثابتة مع معالجة بواقي التقريب، واستخدام المبالغ الصافية في جميع التجميعات. فصل خصم البند وخصم الطلب وإجمالي الخصم في العقد المحاسبي.

### F06 — P1 — الاسترداد في يوم لاحق يغيّر التقرير السابق ولا يظهر في يوم الاسترداد

**المواضع:** [ReportService.cs:41](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:41)، [ReportService.cs:250](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:250)، [OrderService.cs:320](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:320).

التقارير تستبعد الطلب بناءً على حالته الحالية، حتى عند طلب فترة قديمة. تقرير وسائل الدفع يختار `SalePayment` فقط، ويستبعد الدفعة إذا أصبح طلبها ملغى؛ لا يجمع `Refund` بتاريخ حدوثه.

**الإثبات:** بيع أمس بقيمة 80 أعطى تقرير أمس 80. بعد إلغائه اليوم صار تقرير أمس صفراً. كان هناك بيع آخر اليوم بقيمة 100؛ بقي تقرير وسائل الدفع لليوم 100 مع وجود استرداد اليوم بقيمة 80، بينما صافي التحصيل ينبغي أن يوضح 20 إذا كان التقرير يمثل حركة التحصيل.

**المعالجة:** فصل تقارير تشغيل الطلبات عن المبيعات والاستردادات والتحصيل بتاريخ القيد، مع أعمدة إجمالي البيع والاسترداد والصافي. الاحتفاظ بتوقيت الإلغاء وفاعله وسببه، وعدم إعادة تفسير الأيام المغلقة بالحالة الحالية وحدها.

### F07 — P1 — حذف طريقة دفع يُسقط مبيعاتها من التقارير التاريخية

**المواضع:** [PaymentMethodService.cs:64](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/PaymentMethodService.cs:64)، [RestaurantDbContext.cs:92](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/RestaurantDbContext.cs:92)، [ReportService.cs:250](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:250).

الحذف الفعلي يضع `PaymentMethodId = null` على القيود المرتبطة، بينما تقرير وسائل الدفع يشترط معرفاً غير فارغ. حفظ `AffectsCash` يحمي رصيد النقد لكنه لا يحمي تصنيف التقارير أو اسم وسيلة الدفع في إعادة طباعة الفاتورة.

**الإثبات:** مجموع تقرير وسيلة الدفع انخفض من 100 إلى صفر بمجرد حذف الطريقة، مع بقاء الطلب وحركة البيع.

**المعالجة:** تعطيل طريقة الدفع المستخدمة بدلاً من حذفها، وحفظ هويتها/اسمها التاريخي على القيد. كحد أدنى يجب ألا تختفي القيود ذات الطريقة المحذوفة من الإجماليات.

### F08 — P1 — طريقة «بطاقة» تُنشأ كدفع نقدي في قاعدة جديدة

**المواضع:** [DataSeeder.cs:165](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/DataSeeder.cs:165)، [PaymentMethod.cs:30](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models/PaymentMethod.cs:30)، [OrderService.cs:121](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:121).

القيمة الابتدائية لـ`PaymentMethod.IsCash` هي `true`. Seeder ينشئ «نقدي» و«بطاقة» دون ضبطها. الترحيل يصحح أسماء طرق موجودة وقت تنفيذه، لكن على قاعدة فارغة تُنشأ بيانات Seeder بعد انتهاء الترحيلات، فتفوت البطاقة مرحلة التصحيح.

**الإثبات:** إنشاء الكائن بنفس قيم Seeder أعاد `IsCash=true` للبطاقة؛ checkout ينسخ هذه القيمة إلى `AffectsCash`. النتيجة زيادة نقدية وهمية عند الدفع بالبطاقة ثم فروق ورديات خاطئة.

**المعالجة:** قيم صريحة لكل طريقة في Seeder، واختبار تهيئة قاعدة جديدة مستقلاً عن اختبار ترقية قاعدة قديمة.

### F09 — P1 — حذف تصنيف يحذف منتجاته غير المباعة تلقائياً

**المواضع:** [CategoryService.cs:56](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/CategoryService.cs:56)، [RestaurantDbContext.cs:125](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/RestaurantDbContext.cs:125).

العلاقة من التصنيف إلى المنتج تستخدم Cascade، والخدمة لا تفحص وجود منتجات. إجراء حذف تصنيف يمكن أن يمحو كتالوج أصنافه. إذا كانت له أصناف مستخدمة في طلبات، يمنع قيد آخر الحذف بدلاً من تقديم معالجة مفهومة.

**الإثبات:** أُنشئ تصنيف ومنتج غير مباع ثم حُذف التصنيف عبر الخدمة؛ اختفى المنتج أيضاً.

**المعالجة:** منع حذف تصنيف غير فارغ أو تقديم نقل صريح للمنتجات؛ استخدام التعطيل عندما تكون هناك بيانات تاريخية. لا يكفي منع حذف المنتجات المستخدمة وحدها.

### F10 — P1 — يمكن تصدير أرقام فترة سابقة تحت عنوان فترة جديدة

**المواضع:** [ReportsPage.tsx:93](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:93)، [ReportsPage.tsx:188](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:188)، [ReportsPage.tsx:445](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:445).

تغيير الفترة يترك البيانات القديمة في state حتى اكتمال الطلب الجديد، لكن التصدير والطباعة يستخدمان قيم حقول التواريخ الجديدة فوراً. الأزرار لا ترتبط باكتمال تحميل التقرير. وعند فشل الطلب تبقى البيانات السابقة أيضاً، ولا توجد حماية من وصول الردود بترتيب مخالف لترتيب الطلبات.

**الإثبات:** اختبار Edge حمّل مبيعات يوم 15 بقيمة 100، ثم غيّر نهاية الفترة إلى 14 وأبقى الطلب الجديد معلقاً؛ ملف CSV الناتج حمل العنوان الجديد مع مبيعات 100 القديمة.

**المعالجة:** حفظ نتيجة التقرير مع الفترة التي أنتجتها، وإلغاء/تجاهل الردود القديمة، ومنع التصدير والطباعة قبل وجود نتيجة ناجحة تطابق الفلاتر المعروضة.

## النتائج الوظيفية وموثوقية البيانات

### F11 — P2 — اختيار دور مستخدم جديد لا يُحفظ

**المواضع:** [SettingsPage.tsx:537](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/settings/pages/SettingsPage.tsx:537)، [AuthService.cs:68](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/AuthService.cs:68).

شاشة الإنشاء تسمح باختيار Cashier أو Admin، لكن طلب التسجيل لا يرسل الأدوار، و`RegisterRequestDto` لا يتضمنها، والخدمة لا تسند أي دور. **مثبت:** التسجيل الناجح أعاد `Roles=[]`. حساب اختير كمدير لا يصبح مديراً، وقد يفتقد أيضاً أذونات صحيحة بسبب F02. يلزم إنشاء المستخدم والدور والأذونات كعملية واحدة مخولة إدارياً، والتحقق من النتائج قبل إعلان النجاح.

### F12 — P2 — صلاحية Dashboard وحدها لا تكفي لتشغيل لوحتها

**المواضع:** [DashboardPage.tsx:62](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/dashboard/pages/DashboardPage.tsx:62)، [ReportsController.cs:15](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/ReportsController.cs:15)، [Program.cs:142](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Program.cs:142).

المسار يسمح بـ`Permission.Dashboard`، لكن البيانات تأتي من `/api/reports/comprehensive` المحمي بـ`Permission.Reports`. حساب لوحة القيادة فقط يفتح الصفحة وتفشل بياناتها بـ403. يلزم تعريف البيانات المسموحة للوحة وسياسة مناسبة، أو إعلان الاعتماد على Reports وفرضه في إدارة الأذونات.

### F13 — P2 — صفحات المنتجات والتصنيفات تعرض إجراءات إدارة لا تدعمها صلاحياتها

**المواضع:** [ProductsPage.tsx:143](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/products/pages/ProductsPage.tsx:143)، [CategoriesPage.tsx:105](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/categories/pages/CategoriesPage.tsx:105)، [ProductsController.cs:47](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/ProductsController.cs:47).

بعد منح الصلاحية القياسية للوصول، يرى غير المدير أزرار إنشاء وتعديل وحذف، بينما جميع عمليات الكتابة تتطلب Admin. لا يوجد ربط في الويب لصلاحيات `Permission.Products.Manage` و`Permission.Categories.Manage` الموجودة في بيانات التهيئة. يلزم تحديد نموذج التفويض: عرض فقط مع إخفاء الكتابة، أو إدارة مفوضة مدعومة بسياسات على الخادم.

### F14 — P2 — حالة توفر المنتج في الويب لا تُحفظ ولا تُفرض

**المواضع:** [ProductsPage.tsx:167](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/products/pages/ProductsPage.tsx:167)، [ProductDtos.cs:68](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models.DTOs/ProductDtos.cs:68)، [Product.cs:13](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models/Product.cs:13)، [PosPage.tsx:132](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/pos/pages/PosPage.tsx:132).

الواجهة ترسل `isAvailable` وتستخدمه لمنع إضافة الصنف، لكن حقل التوفر غير موجود في النموذج أو DTOs أو قاعدة البيانات. **مثبت:** إزالة/قراءة JSON عبر DTO الخادم تتجاهل الحقل. حفظ «غير متوفر» يعلن النجاح، ثم يعود الصنف متوفراً عند التحميل. يلزم إضافة الحقل والترحيل وفحصه أثناء checkout.

### F15 — P2 — تكرار معرف الدفع مع جسم مختلف يعيد نجاح العملية القديمة

**الموضع:** [OrderService.cs:66](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:66).

عند وجود `ClientRequestId`، تعيد الخدمة الطلب السابق قبل التحقق من مطابقة الأصناف والكميات والخصم والخزينة ووسيلة الدفع. **مثبت:** إعادة المعرف بعد تغيير الكمية ووسيلة الدفع إلى معرف غير موجود أعادت الطلب السابق بقيمة 80 دون رفض. يلزم تخزين بصمة الطلب الأصلي ومقارنتها، وإرجاع Conflict إذا اختلف الجسم، مع بقاء إعادة الطلب المطابق آمنة.

### F16 — P2 — نطاق مفتاح منع التكرار لا يطابق قيده الفريد

**المواضع:** [OrderService.cs:67](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:67)، [RestaurantDbContext.cs:60](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/RestaurantDbContext.cs:60)، [OrdersController.cs:92](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/OrdersController.cs:92).

البحث يقيد المعرف بالمستخدم، بينما فهرس Orders يجعل المعرف فريداً على مستوى الجميع. **مثبت:** مستخدم ثانٍ أرسل نفس المفتاح فتجاوز البحث ثم اصطدم بـ`DbUpdateException`. مسار checkout لا يحول هذا الاستثناء إلى استجابة تعارض مفهومة. يلزم توحيد النطاق بين البحث والفهرس، ومعالجة تعارض الإدخال المتزامن وإعادة قراءة النتيجة عند اللزوم.

### F17 — P2 — نتيجة دفع مبهمة قد تتبعها فاتورة ثانية عند تعديل السلة

**الموضع:** [PosPage.tsx:271](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/pos/pages/PosPage.tsx:271).

المفتاح المعلق موجود في `useRef` فقط، ويتغير عند تغير توقيع السلة أو طريقة الدفع. إذا حفظ الخادم الدفع وضاع الرد، يستطيع الكاشير تغيير الطريقة أو الكمية وإعادة الإرسال بمفتاح جديد؛ لا توجد خطوة لاسترجاع نتيجة العملية السابقة أولاً. إعادة تحميل الصفحة تفقد أيضاً المفتاح والسلة. الحماية الحالية تغطي إعادة المحاولة المطابقة داخل نفس المكوّن، ولا تغطي استعادة العمل بعد رد مبهم.

يلزم حفظ عملية البيع المعلقة محلياً واسترجاع نتيجتها من الخادم قبل السماح باعتبارها بيعاً جديداً. وينطبق مبدأ الاستعادة نفسه على الحركة اليدوية إذا أُغلقت نافذتها وفُتحت بعد ضياع الرد.

### F18 — P2 — تعديل بيانات المنتج يغيّر الفواتير والتصنيفات التاريخية

**المواضع:** [MappingProfile.cs:70](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models.DTOs/MappingProfile.cs:70)، [ReportService.cs:193](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:193).

اسم البند وتصنيفه يُقرآن من المنتج الحالي؛ لا توجد لقطة لهما عند البيع. **مثبت:** تغيير اسم المنتج جعل قراءة الطلب القديم تعيد الاسم الجديد. نقل المنتج لتصنيف آخر ينقل مبيعاته القديمة في تقرير التصنيفات أيضاً. يلزم حفظ الاسم والتصنيف والبيانات التجارية اللازمة على بند البيع عند إنشائه، مع سياسة واضحة لتقارير «التصنيف وقت البيع» و«التصنيف الحالي».

### F19 — P2 — تحويل التاريخ المحلي إلى ISO يختار يوماً خاطئاً في ليبيا

**المواضع:** [ReportsPage.tsx:50](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:50)، [DashboardPage.tsx:54](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/dashboard/pages/DashboardPage.tsx:54)، [OrdersPage.tsx:33](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/orders/pages/OrdersPage.tsx:33).

`toISOString().split('T')[0]` يستخدم يوم UTC، بينما الفلاتر تمثل يوم العمل المحلي. **مثبت مع `TZ=Africa/Tripoli`:** الساعة 00:30 يوم 15 أكتوبر أعطت الفلاتر 14 أكتوبر؛ بداية الشهر المحلية 1 أكتوبر تحولت إلى 30 سبتمبر. خطأ بداية الشهر يحدث عند إنشاء منتصف الليل المحلي حتى خارج أول ساعتين من اليوم.

يلزم تنسيق التاريخ من مكوناته المحلية، وتوحيد تعريف منطقة عمل المطعم. الخادم يستخدم `DateTime.Now` وتواريخ بلا منطقة أيضاً؛ نشره على خادم UTC سيضيف اختلافاً آخر إن لم تُضبط السياسة صراحةً.

### F20 — P2 — CSV لا يحيّد الصيغ ولا يهرب الاقتباسات لجميع الحقول

**المواضع:** [ReportsPage.tsx:206](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:206)، [ReportsPage.tsx:225](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/reports/pages/ReportsPage.tsx:225).

الأسماء والملاحظات تُحاط باقتباسين دون تحويل الاقتباس الداخلي إلى اقتباسين، باستثناء اسم المطعم في المقدمة. هذا يفسد بنية CSV عند وجود اقتباسات وفواصل. كذلك الحقل النصي الذي يبدأ بعلامة صيغة لا يُحيَّد؛ الاقتباس وحده لا يضمن قراءته كنص في برامج الجداول.

**مثبت:** ملاحظة طلب ملغى `=1+1` خرجت من المتصفح كما هي داخل `"=1+1"`. لم يُفتح الملف في Excel أو يُنفذ أي محتوى. يلزم دالة تصدير موحدة تضمن escaping وتحييد الحقول النصية القابلة لتفسير الصيغ وفق القارئ المستهدف. [مرجع OWASP](https://owasp.org/www-community/attacks/CSV_Injection).

### F21 — P2 — التحقق من المال لا يطابق دقة التخزين في كل المسارات

**المواضع:** [ProductDtos.cs:94](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models.DTOs/ProductDtos.cs:94)، [OrderService.cs:194](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:194)، [Product.cs:41](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models/Product.cs:41).

أسعار المنتجات وحدود الخصم تقبل نطاقاً أوسع من `decimal(18,2)` ولا تفرض خانتين عشريتين. فحص المال في checkout يفحص الإجمالي فقط. مثال: خصم وحدة `0.005` لكمية 2 يعطي إجمالياً صالحاً بخانتين، لكن تقريب خصم الوحدة عند التخزين يجعل البند المخزن غير قابل لإعادة الحساب بنفس الإجمالي. القيم الضخمة قد تمر من DTO ثم تفشل في SQL Server.

يلزم سياسة نقدية مشتركة للمدخلات والبنود والإجماليات، وحدود مطابقة للتخزين ومعالجة بواقي التقريب. لم يُنفذ مثال التقريب على SQL Server في هذه المراجعة؛ الخلل في عقد التحقق مثبت من الكود. توضح [وثائق EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties#precision-and-scale) أن EF لا يتحقق بنفسه من الدقة والمقياس قبل تمرير القيمة إلى الموفر.

### F22 — P2 — الخادم يقبل حالات وأنواع طلب غير معرفة

**المواضع:** [OrderDtos.cs:164](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models.DTOs/OrderDtos.cs:164)، [OrderService.cs:287](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:287).

لا يوجد `EnumDataType` أو فحص `Enum.IsDefined` للقيم الواردة. **مثبت:** إرسال `orderStatus=999` و`orderType=999` أعاد 204، ثم أعادت القراءة القيمتين 999. ويُعد الطلب فعّالاً في التقارير لأنه ليس Cancelled. يلزم رفض القيم غير المعرفة وتحديد انتقالات الحالة المسموحة بدلاً من نسخ أي قيمة واردة.

### F23 — P2 — أخطاء أعمال متوقعة تتسرب كأخطاء خادم

**المواضع:** [ProductsController.cs:54](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.API/Controllers/ProductsController.cs:54)، [ProductService.cs:68](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ProductService.cs:68)، [ReportService.cs:31](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:31)، [CashDrawerEntryService.cs:28](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/CashDrawerEntryService.cs:28).

إنشاء منتج بتصنيف غير موجود أو حذف منتج مستخدم يعتمد مباشرة على رفض FK، دون استجابة أعمال واضحة. لا يوجد معالج استثناءات مركزي في `Program.cs`. والتاريخ الصحيح شكلياً `9999-12-31` يؤدي إلى تجاوز نطاق `AddDays(1)` في التقارير ونهاية فترة الخزينة.

يلزم تحقق مراجع وحدود تواريخ، واستجابات ProblemDetails موحدة، ومعالجة قيود قاعدة البيانات المتوقعة كـ400/409 مع تسجيل السبب الداخلي. لا يجوز إرجاع SQL أو stack trace إلى العميل في بيئة الإنتاج.

### F24 — P2 — التحميل الكامل والتجميع في الذاكرة لا يتوسع مع سجل المطعم

**المواضع:** [GenericRepository.cs:48](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/Repositories/GenericRepository.cs:48)، [OrderService.cs:27](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/OrderService.cs:27)، [ReportService.cs:35](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/ReportService.cs:35)، [CashShiftService.cs:17](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Services/CashShiftService.cs:17).

`GetAllAsync` ينفذ `ToListAsync` مع التتبع. قائمة الطلبات تحمل جميع الفواتير والبنود والمدفوعات، ثم تصفيها الواجهة. التقارير تحمل الطلبات وعلاقاتها قبل GroupBy. رصيد الدرج وسياق الوردية يعيدان قراءة تاريخ الحركات كاملاً، وسياق الوردية يفعل ذلك داخل Serializable؛ طول القراءة يرفع كلفة القفل أيضاً. لا يوجد paging أو حد لنطاق التقرير، ولا فهرس تاريخ مخصص لهذه الاستعلامات.

هذه مشكلة بنيوية مؤكدة، أما حجم التأخير تحت الحمل فلم يُقَس. يلزم ترقيم صفحات وتصفية على الخادم، وإسقاط DTOs وAsNoTracking للقراءة، وتجميعات SQL وفهارس مبنية على خطة الاستعلام. ينبغي الحفاظ على اتساق العمليات المالية عند تحسين المعاملات، لا إزالة العزل دون بديل.

### F25 — P2 — صور المنتجات والشعارات تتعطل عندما يكون API على أصل مستقل

**المواضع:** [imageUtils.ts:1](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/utils/imageUtils.ts:1)، [env.ts:7](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/config/env.ts:7)، [vite.config.ts:14](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/vite.config.ts:14).

يمكن ضبط `VITE_API_BASE_URL` لخادم مختلف، لكن `getFullImageUrl('/images/...')` يعيد المسار نفسه، فيطلب المتصفح الصورة من أصل الواجهة. يعمل ذلك محلياً بسبب proxy الصور في Vite، ويظهر الخلل عند نشر الواجهة وAPI منفصلين دون reverse proxy مماثل. يلزم بناء الروابط النسبية بالنسبة لأصل الملفات/الخادم، مع الحفاظ على الروابط المطلقة الصحيحة.

### F26 — P2 — السلة تدمج نسختين مختلفتين من تخصيص الصنف

**الموضع:** [PosPage.tsx:138](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/pos/pages/PosPage.tsx:138).

المطابقة تعتمد `productId` فقط. إذا أضاف الكاشير وجبة وعدّلها إلى «بدون بصل»، ثم أضاف وجبة ثانية عادية من نفس المنتج، تصبح كمية البند المخصص 2. يصل للمطبخ أن الوجبتين لهما نفس الملاحظة. يلزم تمييز البنود بحسب المنتج والتخصيص أو إضافة بند جديد عند اختلافه، وإتاحة فصل البنود وتعديلها بصورة مستقلة.

### F27 — P2 — مؤشرات الخزينة لا تجمع جميع أنواع التدفق النقدي

**الموضع:** [TreasuryPage.tsx:171](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/src/features/treasury/pages/TreasuryPage.tsx:171)، مقارنةً بـ[CashLedger.cs:7](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.Models/CashLedger.cs:7).

بطاقة التدفقات الداخلة تجمع Inflow فقط وتتجاهل TransferIn وCashSurplus، بينما الخارجة تجمع TransferOut وCashShortage وغيرها. عند اختيار خزينة رئيسية استلمت توريداً تظهر الحركة في الجدول ويتغير الرصيد، لكن التدفق الداخل يبقى صفراً. يلزم جعل المؤشرات متناظرة مع تعريف دفتر النقد، أو تسميتها صراحةً «إيداع يدوي» إذا كان هذا هو المقصود، وفصل التحويلات الداخلية عن الصافي المجمع لجميع الخزائن.

## الاعتماديات والصيانة

### F28 — P2 — اعتماد .NET انتقالي لديه تنبيهات أمنية مؤكدة

**المواضع:** [Restaurant.DataAccess.csproj:19](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/Restaurant.DataAccess.csproj:19)، [project.assets.json:460](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/obj/project.assets.json:460).

الفحص المكتمل وجد `System.Security.Cryptography.Pkcs 9.0.4` عبر `Microsoft.Data.SqlClient 6.1.1`، مع [GHSA-846f-f764-76mx](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx) و[GHSA-2fmv-rv2c-59x7](https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7). الأول مصنف High ومصحح في فرع 9 عند 9.0.21 بحسب نشر Microsoft وقت المراجعة.

وجود الحزمة المتأثرة مثبت، لكن لم يُعثر في شيفرة المطعم على استدعاء مباشر لمسار SignedCms المتأثر، ولم يُثبت استغلال عبر API. يلزم تحديث شجرة الاعتماد إلى إصدار مصحح متوافق وإعادة الفحص. لا يكفي الاعتماد على نسخة runtime المثبتة لتصحيح حزمة تطبيق مقيدة بإصدار قديم.

### F29 — P2 — Vite 6.2.0 متأثر بتنبيهات خادم التطوير

**الموضع:** [package.json:34](C:/Users/Masoud/source/repos/Restaurant.API/restaurant.web/package.json:34) وملف القفل. البناء الفعلي أظهر 6.2.0، و`npm audit` صنفه High ضمن حزمة واحدة تحتوي عدة advisories.

يتضمن ذلك [تنبيه Vite الرسمي لمسارات Windows](https://github.com/vitejs/vite/security/advisories/GHSA-fx2h-pf6j-xcff)، الذي يصحح فرع 6 في 6.4.3. الاستغلال الموصوف في هذا التنبيه يشترط إتاحة خادم التطوير للشبكة ووجود ملف حساس ضمن النطاق المسموح. الإعداد الحالي لا يحتوي `server.host` لإتاحته للشبكة؛ لذلك هذه ليست ثغرة مثبتة في الملفات الثابتة الناتجة عن البناء أو في API.

يلزم تحديث إصدار Vite والقفل إلى نسخة مصححة ومتوافقة، وعدم استخدام خادم التطوير كخادم نشر. فحص حزم الإنتاج وحدها أعاد صفر تنبيهات.

### F30 — P1 مشروط بالرجوع إلى ترحيل قديم — Down يمحو جميع الاستردادات

**الموضع:** [SecureCheckoutAndRefunds.cs:72](C:/Users/Masoud/source/repos/Restaurant.API/Restaurant.DataAccess/Migrations/20260926221218_SecureCheckoutAndRefunds.cs:72).

`Down` ينفذ `DELETE FROM CashDrawerEntries WHERE EntryType = 4` دون تمييز الاستردادات التي أضافها الترحيل عن الاستردادات الحقيقية المسجلة بعد تشغيل النظام. الرجوع عبر هذا الترحيل يحذف السجل المالي للاستردادات ويرفع الأرصدة المحسوبة. هذا المسار لم يُنفذ أثناء المراجعة.

يلزم استراتيجية rollback تحفظ القيود، أو قصر إزالة بيانات الترحيل على سجلات موسومة له تحديداً، أو إعلان عدم قابلية الرجوع الآلي مع مسار استعادة معتمد. كما أن Down الخاص بحماية سجل المستخدم يعيد Cascade لحذف طلباته، ويستحق اختبار صيانة منفصلاً قبل أي downgrade.

## فجوات تصميم وتشغيل تحتاج قرارات واضحة

هذه البنود لا تدخل في عدّ النتائج الثلاثين، لأن أثر بعضها يعتمد على متطلبات النشر أو سياسة العمل:

- **تعدد المطاعم:** الارتباط بالمطعم موجود للمستخدم ولعرض اسمه، لكن Orders وProducts وCashboxes وغيرها ليست مقيدة بمعرف مطعم. النظام الحالي لا يحقق عزلاً لبيانات مطاعم مستقلة. عند دعم أكثر من مطعم يلزم تصميم tenancy كامل؛ وجود جدول Restaurants لا يكفي.
- **التتبع والمراجعة المالية:** تُحذف الحركات اليدوية غير المغلقة فعلياً؛ لا يوجد سجل تدقيق مستقل لتعديلات الأسعار والأذونات والحذف والإلغاء. يلزم تحديد ما إذا كان التصحيح المالي يجب أن يكون بقيد عكسي مع فاعل وسبب وتوقيت. كما لا يوجد وقت/فاعل إلغاء مستقل على Order.
- **حماية آخر مدير:** منع حذف/خفض آخر Admin يعتمد عدّاً ثم كتابة دون قفل أو معاملة مشتركة، ما يترك سباقاً إذا نُفذت عمليتان إداريتان بالتوازي. لم يُحاكَ هذا السباق على SQL Server.
- **اكتمال تحديث المستخدم:** تعديل البيانات والأذونات ثم الأدوار يتم عبر عمليات متعددة غير ذرية. فشل مرحلة متأخرة قد يترك التعديل جزئياً؛ إنشاء Claims يتجاهل نتائج بعض عمليات Identity أيضاً.
- **تغيّر السعر أثناء تجهيز السلة:** الخادم يحسب السعر من قاعدة البيانات، وهو يحميه من التلاعب، لكن الواجهة لا تعيد تأكيد الإجمالي إذا تغيّر السعر منذ تحميل الكتالوج. يلزم تحديد سياسة تثبيت السعر أو إعادة عرض السعر الجديد قبل اعتبار الدفع مؤكداً.
- **طرق الدفع الإلكترونية:** checkout يسجل SalePayment لطريقة مختارة دون اتصال ببوابة دفع أو مرجع تحقق خارجي. هذا مناسب لتسجيل دفع مؤكد خارج النظام، لكنه لا يثبت تحصيل البطاقة بذاته. يجب توضيح طبيعة التكامل إن كان التحصيل الإلكتروني مطلباً.
- **الأقفال والاسترداد من الفشل:** اختبارات SQLite الحالية لا تثبت سلوك Serializable وإعادة المحاولة وتعارض الإدخال على SQL Server. يلزم سيناريوهات متزامنة للدفع والسحب والإلغاء وإغلاق الدرج على قاعدة اختبار مستقلة من نفس الموفر.
- **تسجيل الدخول خلف proxy:** تحديد المعدل يعتمد RemoteIpAddress، ولا يظهر إعداد ForwardedHeaders. إذا جُمعت الطلبات خلف proxy فقد تشترك في حد خمس محاولات في الدقيقة. كذلك `AllowedForNewUsers=false` يعطل قفل الحسابات الجديدة؛ حد IP وحده لا يساوي حماية لكل حساب.
- **النشر والمراقبة:** لا توجد داخل المستودع منظومة CI ظاهرة لهذه الفحوص، ولا health/readiness endpoint أو سياسة نسخ احتياطي واستعادة موثقة. مستوى Logging الأساسي موجود، لكن لا يوجد قياس مخصص لزمن الدفع وفشل المعاملات. لا يمكن استنتاج إعدادات البنية الخارجية من المستودع.
- **الترحيل أثناء الإقلاع:** التطبيق يطبق migrations قبل بدء الخدمة. يلزم سياسة إصدار واستعادة على نسخة اختبار من بيانات التشغيل، وحساب صلاحيات قاعدة مناسب. لم يُختبر bootstrap والتحديث الفعليان على SQL Server هنا.
- **سكربت البيانات التجريبية:** `SeedData.sql` موصوف كتعبئة تجريبية ويعيد حذف جميع روابط CategoryPrintStations في السطر 216. ينبغي تقييد استخدامه ببيئة اختبار أو إضافة حارس بيئة صريح قبل إدخاله في أي إجراء نشر؛ لم يُشغّل أثناء المراجعة.
- **توجيه المطبخ:** إعدادات محطات الطباعة وربط التصنيفات موجودة، لكن `printKitchenTicket` في الويب يطبع جميع البنود في تذكرة واحدة ولا يقرأ الربط. يجب حسم هل التوجيه المنفصل لكل محطة مطلوب في الويب. اختيار طابعة Windows من المتصفح له قيود مستقلة موثقة في المشروع.
- **اختبارات وعقد الواجهة:** الاختبارات الحالية تركز الطلبات والخزينة والطباعة. لا تغطي دورة إنشاء مدير من الويب، أو توافق جميع مفاتيح الأذونات، أو توفر المنتج، أو التقارير بعد الخصم والاسترداد والحذف. فشل lint يحتاج إصلاحاً أو ضبط قواعد مقصوداً، دون مساواة جميع مخالفاته بأخطاء أعمال.

## جوانب سليمة تحققت منها

- توجد سياسة مصادقة عامة، والتسجيل محصور في Admin، والدخول له تحديد معدل. لا يوجد دليل هنا على وصول مجهول عام إلى API.
- الأسعار والتكاليف الفعلية للبنود تُقرأ من قاعدة البيانات، وهوية منفذ الطلب والحركة تأتي من الرمز في المسارات الأساسية.
- checkout وتعديل الإلغاء والإغلاق المالي يستخدمان معاملات؛ إغلاق الوردية يضم المطابقة وطرفي التحويل. الاختبارات الحالية تشمل التراجع عند فشل مصطنع.
- توجد حماية من السحب والاسترداد النقدي فوق الرصيد، ومن تعديل بنود طلب مالي أو حذفه، ومن حذف بعض السجلات المرجعية المرتبطة بالتاريخ.
- حفظ `AffectsCash` على القيد يحمي الأثر النقدي التاريخي من تغيير طريقة الدفع، رغم مشكلة تصنيف التقارير الموضحة في F07.
- رفع الصور يقيد الحجم ويفحص ترويسات JPEG/PNG ويسمي الملفات عشوائياً، ومخرجات HTML للطباعة تستخدم escaping للحقول النصية الأساسية التي راجعتها.
- اختبارات الطباعة المتاحة نجحت، وتوافق نموذج EF مع snapshot الحالي تحقق دون تعديل قاعدة المطعم.

## ترتيب المعالجة المقترح

1. **سلامة التفويض ودورة المستخدم:** F01–F03 وF11–F13، مع تعريف موحد للأذونات واختبارات للحسابات محدودة الصلاحية وإبطال الجلسات.
2. **الفواتير والأرقام المالية:** F04–F08 وF10 وF18 وF19 وF27. ينبغي إضافة اختبارات مصالحة تثبت أن صافي التقارير يطابق القيود والخصومات والاستردادات عبر الأيام.
3. **حماية البيانات وتكرار العمليات:** F09 وF15–F17 وF21–F23 وF30، مع اختبار ترقية وrollback على نسخة اختبار من البيانات واختبارات تزامن SQL Server.
4. **تكامل واجهة الويب والتوسع:** F14 وF20 وF24–F26، وإغلاق أخطاء lint وضبط اختبارات العقد والتحميل وحالات فشل الشبكة.
5. **تحديث الاعتماديات ومتابعتها:** F28 وF29، ثم إدراج فحوص الأمن والبناء والاختبارات في CI دون اعتبار نتيجة audit وحدها ضماناً للأمان.

## إعادة إنتاج الأدلة وحدود الاستنتاج

مصادر التحقق المعزولة محفوظة خارج المستودع:

- [برنامج الخادم](C:/Users/Masoud/AppData/Local/Temp/restaurant-system-audit-20261015/Program.cs) و[مشروع تشغيله](C:/Users/Masoud/AppData/Local/Temp/restaurant-system-audit-20261015/Audit.csproj).
- [اختبارات الواجهة الإضافية](C:/Users/Masoud/AppData/Local/Temp/restaurant-system-audit-20261015/frontend-audit.cjs)؛ تتطلب Vite على `127.0.0.1:5193` وEdge ومكتبة Playwright المشار إليها في الملف. تستبدل طلبات API ولا تتصل بقاعدة المطعم.

لم تشمل المراجعة اختبار اختراق لخادم منشور، أو اختبار حمل، أو تشغيل SQL Server على بيانات حقيقية، أو جهاز طباعة، أو فحص بنية الاستضافة والنسخ الاحتياطية خارج المستودع. نتائج SQLite وTestServer تؤكد السيناريوهات المذكورة ضمن هذا النطاق. لم يُجرَ أي إصلاح ضمن هذه المهمة؛ يلزم التحقق مجدداً بعد المعالجة، خاصةً لتوافق الصلاحيات وتوازن السجلات المالية.
