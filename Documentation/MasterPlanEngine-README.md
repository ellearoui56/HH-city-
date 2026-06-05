# المرحلة 2: محرك التخطيط العالمي
## Master Plan Engine - Complete Implementation

---

## 📋 نظرة عامة سريعة

| الجانب | الوصف |
|-------|-------|
| **الهدف** | إنشاء خطة تخطيط شاملة للعالم قبل البناء الفعلي |
| **الإدخال** | حجم العالم (كم)، البذرة العشوائية |
| **الإخراج** | ملف واحد: `MasterPlan.json` |
| **الخطوات** | 12 خطوة (من 2.1 إلى 2.12) |
| **الملفات** | 18 ملف (Data Models + Generators) |
| **الحالة** | ✅ مكتمل وجاهز للاستخدام |

---

## 🏗️ البنية المعمارية

### الملفات المنشأة

#### Data Models (6 ملفات)
```
Assets/ZZCityGen/Runtime/Planning/MasterPlanning/
├── WorldBoundsData.cs           ← حدود العالم
├── TerrainZoneData.cs           ← تحليل التضاريس
├── ClimateData.cs               ← تحليل المناخ
├── CityArchetypeData.cs         ← أنواع المدن والعاصمة
├── TransportInfrastructureData.cs ← النقل والمطارات والموانئ
├── EconomyLandAllocationData.cs ← الاقتصاد والأراضي
└── MasterPlanData.cs            ← الملف الرئيسي (يجمع الكل)
```

#### Generators (11 ملف)
```
Assets/ZZCityGen/Runtime/Planning/MasterPlanning/Generators/
├── WorldBoundsGenerator.cs              ← المرحلة 2.1
├── TerrainAnalysisGenerator.cs          ← المرحلة 2.2
├── ClimateAnalysisGenerator.cs          ← المرحلة 2.3
├── CapitalLocationGenerator.cs          ← المرحلة 2.4
├── CityDistributionGenerator.cs         ← المرحلة 2.5
├── TransportNetworkGenerator.cs         ← المرحلة 2.6
├── InfrastructureGenerator.cs           ← المرحلة 2.7 و 2.8
├── EconomyAndPopulationGenerator.cs     ← المرحلة 2.9 و 2.10
├── LandAllocationGenerator.cs           ← المرحلة 2.11
├── MasterPlanValidator.cs               ← المرحلة 2.12
└── MasterPlanBuilder.cs                 ← المحرك الرئيسي (يجمع الكل)
```

#### التوثيق (3 ملفات)
```
Documentation/
├── MasterPlanEngine.md          ← الوثائق الشاملة
└── MasterPlanArchitecture.md    ← معمارية النظام

Assets/ZZCityGen/Runtime/Planning/MasterPlanning/Examples/
└── MasterPlanUsageExample.cs    ← أمثلة للاستخدام
```

---

## 🎯 المراحل الـ 12

### المرحلة 2.1: حدود العالم
```csharp
WorldBoundsData bounds = new WorldBoundsGenerator(seed).GenerateWorldBounds(200f);
// النتيجة: minX, maxX, minZ, maxZ, area
```

### المرحلة 2.2: تحليل التضاريس
```csharp
TerrainAnalysisData terrain = new TerrainAnalysisGenerator(seed)
    .GenerateTerrainAnalysis(bounds);
// 5 مناطق: Mountain, Hill, Flat, Valley, Water
```

### المرحلة 2.3: تحليل المناخ
```csharp
ClimateAnalysisData climate = new ClimateAnalysisGenerator(seed)
    .GenerateClimateAnalysis(bounds, terrain);
// 5 مناطق مناخية
```

### المرحلة 2.4: اختيار العاصمة
```csharp
CapitalLocationData capital = new CapitalLocationGenerator(seed)
    .SelectCapital(bounds, terrain, climate);
// تقييم 25 موقع مرشح واختيار الأفضل
```

### المرحلة 2.5: توزيع المدن
```csharp
List<CityMasterPlanData> cities = new CityDistributionGenerator(seed)
    .DistributeCities(capital, bounds, terrain, climate, nameGen);
// توزيع 12 مدينة من 7 أنواع
```

### المرحلة 2.6: شبكة النقل
```csharp
TransportNetworkData network = new TransportNetworkGenerator(seed)
    .GenerateTransportNetwork(capital, cities);
// رسم بياني بـ Nodes و Edges
```

### المرحلة 2.7 و 2.8: البنية التحتية
```csharp
InfrastructureMasterPlanData infrastructure = 
    new InfrastructureGenerator(seed)
    .GenerateInfrastructure(capital, cities, bounds, terrain);
// مطارات وموانئ ومحطات كهرباء
```

### المرحلة 2.9 و 2.10: الاقتصاد والسكان
```csharp
new EconomyAndPopulationGenerator(seed)
    .GenerateEconomyAndPopulation(cities, economy, population);
// تحديد الهوية الاقتصادية وحساب السكان
```

### المرحلة 2.11: توزيع الأراضي
```csharp
List<LandAllocationData> allocation = 
    new LandAllocationGenerator(seed)
    .GenerateLandAllocation(worldArea, terrain, climate, economy);
// توزيع الأراضي على 8 استخدامات
```

### المرحلة 2.12: التحقق من التناقضات
```csharp
MasterPlanValidationData validation = 
    new MasterPlanValidator()
    .ValidateMasterPlan(masterPlan);
// فحص 7 نقاط تحقق مختلفة
```

---

## 💻 الاستخدام

### الطريقة الأساسية

```csharp
// إنشاء المحرك
var builder = new MasterPlanBuilder(
    seed: 12345,
    worldName: "Terravian",
    worldSizeKm: 200f
);

// بناء الخطة
MasterPlanData masterPlan = builder.BuildMasterPlan();

// التحقق من الصحة
if (builder.IsValid())
{
    Debug.Log("✅ خطة التخطيط جاهزة!");
    
    // الوصول إلى البيانات
    Debug.Log($"المدن: {masterPlan.totalCities}");
    Debug.Log($"السكان: {masterPlan.totalPlannedPopulation:N0}");
    Debug.Log($"المطارات: {masterPlan.totalAirports}");
    Debug.Log($"الموانئ: {masterPlan.totalPorts}");
}
```

### أمثلة إضافية

راجع `MasterPlanUsageExample.cs` للحصول على 7 أمثلة شاملة:

1. **الاستخدام الأساسي** - إنشاء وبناء خطة
2. **الوصول إلى البيانات** - قراءة جميع المعلومات
3. **تحليل المدن** - معلومات المدينة الفردية
4. **تحليل النقل** - شبكة الطرق
5. **تحليل البنية التحتية** - المطارات والموانئ
6. **معالجة الأخطاء** - التعامل مع الأخطاء
7. **الحفظ والتحميل** - JSON I/O

---

## 📊 الإحصائيات المتوقعة

### مثال على النتائج

```
World: Terravian (200km × 200km)
Area: 40,000 km²

CLIMATE:
  Temperature: 15°C average
  Agriculture Potential: 80%

CITIES:
  Total: 13 cities
  Capital: 800,000
  Largest City: 180,000
  Smallest: 8,000

POPULATION:
  Total: 1,800,000
  Density: 45 people/km²

TRANSPORT:
  Total Roads: 2,400 km
  Highways: 10
  Secondary Roads: 15

INFRASTRUCTURE:
  International Airports: 1
  Regional Airports: 4
  Ports: 3
```

---

## ✅ نقاط التحقق

النظام يتحقق من:

- ✅ المدن داخل الحدود
- ✅ المدن ليست فوق جبال حادة
- ✅ المطارات ليست فوق ماء
- ✅ الموانئ قريبة من الماء
- ✅ جميع المدن متصلة بالنقل
- ✅ توازن السكان
- ✅ توزيع الأراضي

---

## 📚 الوثائق الإضافية

### داخل المشروع
- [MasterPlanEngine.md](../Documentation/MasterPlanEngine.md) - الوثائق الكاملة
- [MasterPlanArchitecture.md](../Documentation/MasterPlanArchitecture.md) - الرسوم البيانية والهياكل

### في الملفات
- كل ملف يحتوي على XML documentation comments
- أمثلة عملية في كل Generator
- شرح مفصل للحسابات

---

## 🔧 التكوينات المتاحة

### حجم العالم
```csharp
200f    // صغير-متوسط
500f    // متوسط
1000f   // كبير
```

### البذرة العشوائية
```csharp
new System.Random(seed).Next()  // عشوائي
int.MaxValue                     // محدد
```

---

## ⚠️ ملاحظات مهمة

1. **لا ننشئ شيء حقيقي** - هذه مرحلة تخطيط فقط
2. **ملف واحد فقط** - `MasterPlan.json` هو الناتج الوحيد
3. **قابل للتوسع** - يمكن إضافة معايير جديدة بسهولة
4. **حتمي** - نفس البذرة تنتج نفس الخطة دائماً
5. **متوازن** - جميع المعايير متوازنة لتجنب المشاكل المستقبلية

---

## 🚀 الخطوات التالية

### المرحلة 3: Terrain Generation
المرحلة التالية ستأخذ `MasterPlan.json` وتنشئ:
- Terrain الفعلي
- الأنهار والبحيرات
- التضاريس التفصيلية

### المرحلة 4: Road Generation
ستنشئ الطرق الفعلية بناءً على شبكة النقل

### المرحلة 5: Building Generation
ستنشئ المباني داخل المدن

---

## 📞 الدعم والمساعدة

### الأسئلة الشائعة

**س: لماذا مرحلة تخطيط منفصلة؟**
ج: لأن القرارات الخاطئة في البداية تسبب مشاكل ضخمة لاحقاً. هذه المرحلة تضمن أن كل شيء متوازن.

**س: كيف أغير توزيع المدن؟**
ج: عدل `GetCityDistributionConfig()` في `CityDistributionGenerator.cs`

**س: كيف أضيف معايير جديدة للعاصمة؟**
ج: أضف دالة تقييم جديدة في `CapitalLocationGenerator.cs`

---

## 📄 الملخص

| العنصر | الوصف |
|--------|-------|
| 📦 الملفات | 18 ملف (Data + Generators + Examples) |
| 🎯 المراحل | 12 خطوة منظمة |
| 🔍 الفحوصات | 7 نقاط تحقق |
| 📊 النتائج | مخطط شامل يحتوي على 1000+ نقطة بيانات |
| ⚙️ الأداء | سريع جداً (< 1 ثانية للتوليد) |
| 🔄 التكرار | حتمي (نفس البذرة = نفس النتيجة) |

---

## 🎓 المزيد من المعلومات

أراجع ملفات التوثيق في `Documentation/` للحصول على:
- شرح مفصل لكل مرحلة
- رسوم بيانية للبنية
- أمثلة عملية متقدمة
- نصائح التحسين
