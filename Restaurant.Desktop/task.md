# مهام الانتقال إلى نظام طباعة WPF

---

## المرحلة 1 — تصميم UserControls للطباعة

### 1.1 فاتورة الزبون (ReceiptPrintControl)

- `[x]` إنشاء مجلد `Controls/Printing/` في مشروع `Restaurant.Desktop`
- `[x]` إنشاء `Controls/Printing/ReceiptPrintControl.xaml`
  - `[x]` ضبط `FlowDirection="RightToLeft"` على مستوى الـ UserControl
  - `[x]` **رأس الفاتورة**: اسم المطعم (نص ثابت `const`) بخط Bold كبير + سطر فاصل
  - `[x]` **بيانات الطلب**: رقم الطلب، التاريخ والوقت، نوع الطلب (داخلي/سفري/توصيل)، اسم الكاشير
  - `[x]` **طريقة الدفع**
  - `[x]` **سطر فاصل** (خط أفقي)
  - `[x]` **رأس جدول المنتجات**: (المنتج / الكمية / السعر / الإجمالي)
  - `[x]` **ItemsControl** للمنتجات: اسم المنتج، الكمية، سعر الوحدة، الإجمالي
  - `[x]` عرض **ملاحظة كل صنف** إن وُجدت (بخط مائل أصغر)
  - `[x]` **سطر فاصل**
  - `[x]` **قسم الإجمالي**: المجموع الفرعي، الخصم (إن وُجد)، **الصافي بخط عريض**
  - `[x]` **رسالة الشكر** في الأسفل
- `[x]` إنشاء `Controls/Printing/ReceiptPrintControl.xaml.cs` (Code-Behind بسيط)

### 1.2 تذكرة المطبخ (KitchenTicketPrintControl)

- `[x]` إنشاء `Controls/Printing/KitchenTicketPrintControl.xaml`
  - `[x]` ضبط `FlowDirection="RightToLeft"` على مستوى الـ UserControl
  - `[x]` **رأس التذكرة**: اسم محطة الطباعة بخط كبير + خط فاصل سميك
  - `[x]` **رقم الطلب**: بخط ضخم جداً (40-48pt) — مرئي من بُعد
  - `[x]` **نوع الطلب** بخط 22pt وتلوين مميز:
    - داخلي → خلفية خضراء فاتحة
    - سفري → خلفية صفراء فاتحة
    - توصيل → خلفية حمراء فاتحة
  - `[x]` **الوقت** بخط متوسط
  - `[x]` **سطر فاصل**
  - `[x]` **قائمة المنتجات** بخط كبير (18-20pt): اسم المنتج × الكمية
  - `[x]` ملاحظة كل صنف بخط 14pt مائل
  - `[x]` **ملاحظة الطلب العامة** في الأسفل (إن وُجدت)
- `[x]` إنشاء `Controls/Printing/KitchenTicketPrintControl.xaml.cs`

---

## المرحلة 2 — إنشاء خدمة الطباعة WPF

### 2.1 واجهة الخدمة

- `[x]` إنشاء `Services/IServices/IWpfPrintingService.cs`
  - `[x]` تعريف `Task PrintOrderAsync(OrderDto order)`
  - `[x]` تعريف `Task PrintReceiptAsync(OrderDto order)`
  - `[x]` تعريف `Task PrintKitchenTicketsAsync(OrderDto order)`

### 2.2 تنفيذ الخدمة `WpfPrintingService`

- `[x]` إنشاء `Services/WpfPrintingService.cs`
- `[x]` حقن التبعيات: `IPrinterApiService` + `IPrintStationApiService`
- `[x]` تنفيذ `PrintReceiptAsync`
- `[x]` تنفيذ `PrintKitchenTicketsAsync`
- `[x]` تنفيذ `PrintOrderAsync` (يستدعي `PrintReceiptAsync` ثم `PrintKitchenTicketsAsync`)

---

## المرحلة 3 — تعليق الكود القديم

- `[x]` تعليق ملفات `ReceiptBuilder.cs` و `KitchenTicketBuilder.cs` و `WindowsPrintService.cs` و `PrintingService.cs` و `IPrintingService.cs`

---

## المرحلة 4 — ربط الخدمة في DI وتعديل الـ UI

### 4.1 تحديث الـ DI والـ ViewModels
- `[x]` تسجيل `WpfPrintingService` in DI Container بـ `App.xaml.cs`.
- `[x]` ربط الـ `IWpfPrintingService` بـ `NewOrderViewModel.cs` واستبدال الطباعة القديمة.

### 4.2 إضافة ميزة ربط التصنيفات بمحطات الطباعة (Category-PrintStation Link)
- `[x]` إضافة واجهة `LinkCategoryToStationAsync` و `UnlinkCategoryFromStationAsync` في `IPrintStationApiService` و `PrintStationApiService`.
- `[x]` تعديل `CategoriesViewModel.cs` ليقوم بجلب محطات الطباعة المتاحة وتحميل المحطة المرتبطة بالتصنيف المحدد ومزامنتها عند الحفظ (ربط/إلغاء ربط).
- `[x]` إضافة ComboBox في `CategoriesPage.xaml` تحت حقل اسم التصنيف لاختيار محطة الطباعة المرتبطة.

---

## المرحلة 5 — التحقق والاختبار

- `[x]` بناء المشروع بدون أخطاء (`Build`)
- `[x]` تشغيل التطبيق وإنشاء طلب تجريبي
- `[x]` التحقق من أن فاتورة الزبون تطبع بشكل صحيح
- `[x]` ربط أحد التصنيفات بمحطة طباعة (مثال: محطة المطبخ) عبر شاشة التصنيفات الجديدة.
- `[x]` التأكد من أن الأصناف التابعة لهذا التصنيف تطبع تذكرة في طابعة المطبخ المحددة.
