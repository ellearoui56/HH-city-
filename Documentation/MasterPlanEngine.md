# المرحلة 2: محرك التخطيط العالمي (Master Plan Engine)

## نظرة عامة

المرحلة 2 هي **أهم مرحلة في المشروع كله**. وهي عبارة عن "مهندس مدن افتراضي" يرسم العالم بالكامل على الورق قبل البناء.

### الهدف الرئيسي

عند الضغط على **"Generate Master Plan"**، يتم إنشاء ملف واحد فقط:

```
MasterPlan.json
```

يحتوي هذا الملف على **جميع القرارات المستقبلية** التي ستوجه جميع المراحل التالية.

---

## معمارية النظام

### 1. بنية البيانات الأساسية

يتم تخزين كل شيء في `MasterPlanData` والذي يحتوي على:

```
MasterPlanData
├── WorldBoundsData (حدود العالم)
├── TerrainAnalysisData (تحليل التضاريس)
├── ClimateAnalysisData (تحليل المناخ)
├── CapitalLocationData (موقع العاصمة)
├── List<CityMasterPlanData> (قائمة المدن)
├── TransportNetworkData (شبكة النقل)
├── InfrastructureMasterPlanData (المطارات والموانئ)
├── GlobalEconomyPlanData (الاقتصاد العالمي)
├── PopulationDistributionData (توزيع السكان)
├── List<LandAllocationData> (توزيع الأراضي)
├── MasterPlanValidationData (نتائج التحقق)
└── List<PlanningLayerData> (حالات الطبقات)
```

### 2. المحركات (Generators)

كل محرك مسؤول عن خطوة واحدة من خطوات التخطيط:

#### 2.1 WorldBoundsGenerator
- **الهدف**: تحديد حدود العالم الرياضية
- **الإدخال**: حجم العالم بالكيلومتر
- **الإخراج**: `WorldBoundsData` (minX, maxX, minZ, maxZ)
- **مثال**: 200km × 200km

#### 2.2 TerrainAnalysisGenerator
- **الهدف**: تحليل وتقسيم التضاريس المستقبلية
- **الإدخال**: `WorldBoundsData`
- **الإخراج**: `TerrainAnalysisData`
- **يقسم العالم إلى**:
  - Mountains (جبال، 15% من العالم)
  - Hills (تلال، 20%)
  - Flat (سهول، 35%)
  - Valleys (أودية، 15%)
  - Water (مياه، 15%)

#### 2.3 ClimateAnalysisGenerator
- **الهدف**: تحديد المناطق المناخية
- **الإدخال**: `WorldBoundsData`, `TerrainAnalysisData`
- **الإخراج**: `ClimateAnalysisData`
- **أنواع المناخ**:
  - Temperate (معتدل)
  - Mediterranean (متوسطي)
  - Tropical (استوائي)
  - Alpine (جبلي)
  - Arid (صحراوي)

#### 2.4 CapitalLocationGenerator
- **الهدف**: اختيار موقع العاصمة الأمثل
- **الإدخال**: `WorldBoundsData`, `TerrainAnalysisData`, `ClimateAnalysisData`
- **الإخراج**: `CapitalLocationData`
- **المعايير المستخدمة**:
  - Accessibility Score (الوصولية)
  - Expansibility Score (التوسعية)
  - Resource Score (الموارد)
  - Strategic Score (الموقع الاستراتيجي)

#### 2.5 CityDistributionGenerator
- **الهدف**: توزيع المدن من أنواع مختلفة
- **الإدخال**: `CapitalLocationData`, `WorldBoundsData`, `TerrainAnalysisData`
- **الإخراج**: `List<CityMasterPlanData>`
- **أنواع المدن**:
  - Capital (عاصمة) × 1
  - FamilyCity (مدينة عائلية) × 4
  - RuralTown (مدينة ريفية) × 3
  - IndustrialCity (مدينة صناعية) × 2
  - CoastalCity (مدينة ساحلية) × 2
  - UniversityCity (مدينة جامعية) × 1
  - TourismDestination (مقصد سياحي)
- **القاعدة المهمة**: الحد الأدنى للمسافة بين المدن = 10 كم

#### 2.6 TransportNetworkGenerator
- **الهدف**: إنشاء شبكة النقل (ليست طرق حقيقية)
- **الإدخال**: `CapitalLocationData`, `List<CityMasterPlanData>`
- **الإخراج**: `TransportNetworkData`
- **النموذج**:
  - Hub & Spoke: كل مدينة متصلة بالعاصمة
  - Similar Types: المدن من نفس النوع متصلة ببعضها
  - Coastal Chain: المدن الساحلية متصلة بسلسلة

#### 2.7 InfrastructureGenerator
- **الهدف**: توليد المطارات والموانئ والبنية التحتية
- **الإدخال**: `CapitalLocationData`, `List<CityMasterPlanData>`
- **الإخراج**: `InfrastructureMasterPlanData`
- **المطارات**:
  - International Airport (في العاصمة)
  - Regional Airports (في المدن الكبيرة)
  - Local Airports (في المدن الصغيرة)
- **الموانئ** (للمدن الساحلية فقط):
  - Commercial Port
  - Fishing Port
  - Tourism Port

#### 2.8 EconomyAndPopulationGenerator
- **الهدف**: تحديد الهوية الاقتصادية وحساب السكان
- **الإدخال**: `List<CityMasterPlanData>`
- **الإخراج**: `GlobalEconomyPlanData`, `PopulationDistributionData`
- **الهويات الاقتصادية**:
  - Technology
  - Manufacturing
  - Tourism
  - Agriculture
  - Services
  - Finance
- **توزيع السكان** (حسب نوع المدينة):
  - Capital: 800,000
  - Family City: 120,000
  - Industrial City: 180,000
  - Rural Town: 8,000
  - Coastal City: 95,000

#### 2.9 LandAllocationGenerator
- **الهدف**: توزيع الأراضي عالميًا
- **الإدخال**: `float worldAreaSquareKm`
- **الإخراج**: `List<LandAllocationData>`
- **توزيع الأراضي**:
  - Agricultural (زراعي): 30%
  - Residential (سكني): 20%
  - Parks (حدائق): 20%
  - Industrial (صناعي): 10%
  - Commercial (تجاري): 8%
  - Government (حكومي): 2%
  - Water (مياه): 15% (حسب التضاريس)
  - Reserved (محفوظ): بقية النسبة

#### 2.10 MasterPlanValidator
- **الهدف**: فحص التناقضات والأخطاء
- **الإدخال**: `MasterPlanData`
- **الإخراج**: `MasterPlanValidationData`
- **الفحوصات**:
  - ✅ مدينة داخل بحيرة؟
  - ✅ مدينة فوق جبل حاد؟
  - ✅ مطار فوق نهر؟
  - ✅ ميناء بعيد عن الساحل؟
  - ✅ توازن السكان
  - ✅ توزيع الأراضي
  - ✅ اتصال شبكة النقل

### 3. محرك البناء الرئيسي (MasterPlanBuilder)

`MasterPlanBuilder` هو المحرك الرئيسي الذي:

1. **ينظم جميع المحركات**
2. **ينفذ الخطوات بالترتيب الصحيح**
3. **يتعامل مع الأخطاء**
4. **ينتج `MasterPlanData` النهائي**

```csharp
var builder = new MasterPlanBuilder(seed: 12345, "New World", 200f);
MasterPlanData masterPlan = builder.BuildMasterPlan();
```

---

## طبقات التخطيط (Planning Layers)

النظام ينظم العمل في 7 طبقات:

```
1. World Layer (حدود العالم)
   ↓
2. Terrain Layer (تحليل التضاريس)
   ↓
3. Climate Layer (تحليل المناخ) - ملخص: يعتمد على Terrain
   ↓
4. Transport Layer (شبكة النقل)
   ↓
5. City Layer (توزيع المدن)
   ↓
6. Infrastructure Layer (المطارات والموانئ)
   ↓
7. District Layer (توزيع الأراضي)
```

كل طبقة تعتمد على التي قبلها.

---

## مثال عملي

### المدخلات:
```csharp
seed: 12345
worldName: "Terravian"
worldSize: 200 km
```

### المخرجات (MasterPlan.json):
```json
{
  "seed": 12345,
  "worldName": "Terravian",
  "worldAreaSquareKm": 40000,
  "totalCities": 13,
  "totalAirports": 5,
  "totalPorts": 3,
  "totalPlannedPopulation": 1800000,
  "totalPlannedRoadsKm": 2400,
  
  "capital": {
    "name": "Central City",
    "position": [0, 0],
    "targetPopulation": 800000,
    "economicIdentity": "Finance"
  },
  
  "cities": [
    {
      "name": "Harbor City",
      "archetype": "CoastalCity",
      "targetPopulation": 120000,
      "economicIdentity": "Tourism",
      "isCoastal": true
    },
    // ...more cities
  ],
  
  "infrastructure": {
    "airports": 5,
    "ports": 3,
    "powerPlants": 4
  },
  
  "economy": {
    "technologyHubs": 1,
    "majorIndustrialZones": 2,
    "agriculturalZones": 4,
    "tourismCenters": 2
  },
  
  "populationDistribution": {
    "capitalPopulation": 800000,
    "totalPopulation": 1800000
  }
}
```

---

## الملفات الرئيسية

### Data Models:
```
Assets/ZZCityGen/Runtime/Planning/MasterPlanning/
├── WorldBoundsData.cs
├── TerrainZoneData.cs
├── ClimateData.cs
├── CityArchetypeData.cs
├── TransportInfrastructureData.cs
├── EconomyLandAllocationData.cs
└── MasterPlanData.cs (الملف الرئيسي)
```

### Generators:
```
Assets/ZZCityGen/Runtime/Planning/MasterPlanning/Generators/
├── WorldBoundsGenerator.cs
├── TerrainAnalysisGenerator.cs
├── ClimateAnalysisGenerator.cs
├── CapitalLocationGenerator.cs
├── CityDistributionGenerator.cs
├── TransportNetworkGenerator.cs
├── InfrastructureGenerator.cs
├── EconomyAndPopulationGenerator.cs
├── LandAllocationGenerator.cs
├── MasterPlanValidator.cs
└── MasterPlanBuilder.cs (المحرك الرئيسي)
```

---

## كيفية الاستخدام

### الطريقة 1: استخدام المحرك مباشرة

```csharp
var builder = new MasterPlanBuilder(
    seed: 12345,
    worldName: "New World",
    worldSizeKm: 200f
);

MasterPlanData masterPlan = builder.BuildMasterPlan();

if (builder.IsValid())
{
    // حفظ الخطة
    SaveMasterPlanToJson(masterPlan);
}
```

### الطريقة 2: من واجهة المحرر

(سيتم إضافة واجهة المحرر في خطوة لاحقة)

---

## الخطوات التالية

**⚠️ توقف هنا. لا تبدأ التضاريس أو الطرق بعد.**

المرحلة التالية ستكون:
- **المرحلة 3**: Terrain Planning & Generation (توليد التضاريس الفعلي)

هذه المرحلة ستأخذ `MasterPlanData` وتبدأ في الإنشاء الفعلي.

---

## معالجة الأخطاء

إذا حدث خطأ أثناء البناء:

1. يتم التقاط الاستثناء
2. يتم تسجيل الخطأ
3. يتم إرجاع `null`
4. يمكن إعادة المحاولة بـ seed مختلف

```csharp
MasterPlanData plan = builder.BuildMasterPlan();

if (plan == null)
{
    Debug.LogError("Failed to build master plan");
    // Try again with different seed
}
```

---

## ملاحظات مهمة

1. **الملف الوحيد للمخرجات**: `MasterPlan.json` فقط - لا توجد ملفات إضافية
2. **لا ننشئ شيء حقيقي**: هذه مرحلة تخطيط فقط - لا terrain أو roads أو buildings
3. **جميع القرارات مسبقة**: بعد هذه المرحلة، المراحل التالية ستعرف بالضبط ماذا تفعل
4. **قابل للتوسع**: يمكن إضافة معايير جديدة أو خطوات جديدة بسهولة
