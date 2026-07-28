# دليل مطور تطبيق العميل: نظام تفعيل التراخيص الأوفلاين (Client Integration Guide)

يقدم هذا الدليل الخطوات البرمجية الشاملة التي يحتاجها مطور **تطبيق العميل (Client Application)** لربط برنامجه بنظام **LicenseHub**، تفعيل الترخيص، والتحقق منه بشكل "أوفلاين".

---

## 📌 نظرة عامة على دورة التفعيل
النظام مصمم ليسمح للتطبيق بالعمل أوفلاين عن طريق الاعتماد على ملف ترخيص مشفر (`.lic`). لكن عملية "إصدار" هذا الملف لأول مرة تتطلب اتصالاً بالإنترنت عبر نقطة نهاية الـ API.

1. العميل يشتري البرنامج ويحصل على **مفتاح الترخيص (License Key)**.
2. تطبيق العميل يقرأ **بصمة الجهاز (Machine Fingerprint)**.
3. تطبيق العميل يتصل بـ **نقطة نهاية التفعيل (Activation Endpoint)** مع إرسال المفتاح والبصمة.
4. الخادم يرد ببيانات **ملف الترخيص المشفر** في حال نجاح التفعيل.
5. تطبيق العميل يحفظ هذا الملف كـ `license.lic` بجانب ملف التشغيل.
6. **العمل الأوفلاين:** تطبيق العميل يقرأ ملف `license.lic` محلياً ويتحقق من صحة التوقيع الرقمي، ولا يحتاج للاتصال بالإنترنت إلا للمزامنة الدورية.

---

## 🛠️ الخطوة 1: توليد بصمة الجهاز (Hardware Fingerprint)
يجب على تطبيق العميل توليد كود فريد وثابت يعتمد على قطع الجهاز (مثل اللوحة الأم والمعالج).

> [!TIP]
> لا تعتمد على أشياء متغيرة مثل عنوان الـ MAC أو الـ IP. اعتمد على `Motherboard Serial Number` أو `CPU ID`.

**مثال باستخدام مكتبة Management في C#:**
```csharp
public static string GetMachineFingerprint()
{
    var mbs = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard");
    var cpu = new ManagementObjectSearcher("Select ProcessorId From Win32_Processor");
    
    string boardSerial = mbs.Get().Cast<ManagementBaseObject>().First()["SerialNumber"].ToString();
    string cpuId = cpu.Get().Cast<ManagementBaseObject>().First()["ProcessorId"].ToString();

    // تشفير المعرفات لإنشاء بصمة ثابتة وقصيرة
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(boardSerial + cpuId));
    return Convert.ToBase64String(hashBytes).Substring(0, 32);
}
```

---

## 🚀 الخطوة 2: استدعاء نقطة التفعيل (Activation API)
للحصول على ملف الترخيص، يجب إرسال طلب `POST` إلى الخادم.

### تفاصيل الطلب:
- **المسار:** `POST /api/v1/client/activation`
- **حماية الـ Header:** يجب إرفاق `X-Api-Key` وهو المفتاح الخاص بشركتك (Organization API Key). هذا يمنع أي طلبات عشوائية من استهلاك الـ API.

### شكل بيانات الإرسال (Request Body):
```json
{
  "licenseKey": "ABCD-1234-WXYZ-5678",
  "machineFingerprint": "hw_9876543210abcdef",
  "machineName": "DESKTOP-JOHN-DOE"
}
```

### كود الاستدعاء باستخدام `HttpClient`:
```csharp
public async Task<bool> ActivateLicenseAsync(string licenseKey)
{
    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://api.your-licensehub.com/");
    client.DefaultRequestHeaders.Add("X-Api-Key", "ORG_API_KEY_HERE");

    var request = new
    {
        licenseKey = licenseKey,
        machineFingerprint = GetMachineFingerprint(),
        machineName = Environment.MachineName
    };

    var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
    var response = await client.PostAsync("api/v1/client/activation", content);

    if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        // الكود المرجع هو من نوع ServiceResult<byte[]>
        // قم باستخراج المصفوفة البايتية وحفظها
        var result = JsonSerializer.Deserialize<ApiResponse>(jsonResponse);
        
        File.WriteAllBytes("license.lic", result.Data);
        return true;
    }
    else
    {
        // عرض الخطأ للمستخدم (مثلاً: تم تجاوز الحد الأقصى للأجهزة)
        return false;
    }
}
```

---

## 🔒 الخطوة 3: التحقق الأوفلاين (Offline Verification)
في كل مرة يُفتح فيها التطبيق، لا تقم بالاتصال بالإنترنت! بدلاً من ذلك، اقرأ ملف `license.lic` وتحقق منه.

> [!CAUTION]
> **التوقيع الرقمي:** الملف محمي بتوقيع رقمي من نوع ECDSA. يجب تضمين **المفتاح العام (Public Key)** في كود تطبيق العميل الثنائي للتحقق من أن الملف صادر من خادمك ولم يتم التلاعب به.

**كيفية التحقق محلياً:**
1. اقرأ محتوى `license.lic`.
2. فك التشفير / التحقق من التوقيع الرقمي باستخدام المفتاح العام المدمج.
3. استخرج البيانات.
4. **تحقق من البصمة:** هل `HardwareId` داخل الملف يطابق جهاز العميل الحالي؟
5. **تحقق من الصلاحية:** هل تاريخ اليوم يتجاوز `ExpiryDate` المذكور في الملف؟

```csharp
public bool IsLicenseValidOffline()
{
    if (!File.Exists("license.lic")) return false;

    var licenseBytes = File.ReadAllBytes("license.lic");
    
    // 1. تحقق من التوقيع باستخدام المفتاح العام (PublicKey)
    bool isSignatureValid = CryptoService.VerifySignature(licenseBytes, "YOUR_PUBLIC_KEY");
    if (!isSignatureValid) return false;

    // 2. فك ضغط المحتوى
    var licenseData = CryptoService.DeserializeLicense(licenseBytes);

    // 3. مقارنة البصمة
    if (licenseData.MachineFingerprint != GetMachineFingerprint()) return false;

    // 4. التحقق من تاريخ الانتهاء
    if (DateTime.UtcNow > licenseData.ExpiryDate) return false;

    return true;
}
```

---

## 🔄 الخطوة 4: المزامنة الدورية (Background Sync)
للتأكد من أن صاحب النظام لم يقم بحظر العميل أو إيقاف ترخيصه عبر لوحة التحكم (الداشبورد)، يجب على تطبيق العميل الاتصال بالخادم **في الخلفية** بشكل دوري (مثلاً كل 3 أو 7 أيام) إذا كان الإنترنت متاحاً.

- **ماذا لو لم يكن هناك إنترنت؟** يستمر التطبيق بالعمل بناءً على تاريخ الصلاحية الموجود في ملف `.lic` المحلي.
- **ماذا لو اتصل بالإنترنت وكان الترخيص موقوفاً؟** يرد الخادم بالرفض، فيقوم التطبيق بمسح ملف `.lic` المحلي وإيقاف البرنامج.
- **ماذا لو اتصل بالإنترنت وكان الترخيص مجدداً؟** يقوم الخادم بإرسال ملف `.lic` جديد بتواريخ أطول، ويقوم التطبيق بكتابته فوق القديم.

> [!IMPORTANT]
> نقطة النهاية الخاصة بالمزامنة الدورية لم يتم بناؤها في هذه المرحلة، لكنها ستكون مشابهة لنقطة التفعيل وتستقبل البصمة ومفتاح الترخيص للتأكد من الحالة.
