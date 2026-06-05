# محرك توليد التضاريس - دليل شامل
# Terrain Generation Engine - Complete Guide

## نظرة عامة

محرك التضاريس **المرحلة 3** من نظام توليد العالم. يأخذ خطة العالم الكاملة (من المرحلة 2) ويحولها إلى تضاريس جسدية متماسكة وذات معنى.

### السلسلة الهرمية:
```
Master Plan (Stage 2)
    ↓
    [Terrain Generation Engine] (Stage 3) ← أنت هنا
    ↓
Terrain Data (TerrainData.json)
    ↓
    [Road Network Engine] (Stage 4)
    ↓
    [Building Placement Engine] (Stage 5)
```

---

## المراحل الـ 13

### المرحلة 1: HeightMap الأساسي
**الملف:** `BaseHeightMapGenerator.cs`

ننشئ شبكة ضخمة من قيم الارتفاع (2048x2048 أو 4096x4096 بكسل).

```csharp
var heightMap = heightMapGenerator.GenerateBaseHeightMap(masterPlan, 2048);
heightMapGenerator.ApplyPerlinNoise(heightMap, scale: 100f, amplitude: 50f);
heightMapGenerator.SmoothHeightMap(heightMap, iterations: 2);
```

**النتيجة:**
- خريطة ارتفاعات أساسية بقيم من -50 متر (ماء) إلى 2500 متر (جبال عالية)
- تفاصيل طبيعية باستخدام Perlin Noise
- ممسحة لتجنب الانتقالات الحادة

### المرحلة 2: المناطق المحمية
**الملف:** `CityProtectionZoneGenerator.cs`

حماية المدن من الانحدارات الحادة جداً.

```csharp
var protectedZones = protectionGenerator.GenerateProtectedZones(masterPlan);
protectionGenerator.ApplyProtectionToHeightMap(heightMap, protectedZones, worldMin, pixelSize);
```

**الميزات:**
- منطقة داخلية (مستقيمة تماماً)
- منطقة خارجية (تنحدر تدريجياً)
- ضمان أن كل مدينة تحصل على أرض مستقيمة للبناء

### المرحلة 3: السهول
**الملف:** `PlateauMountainHillGenerator.cs`

إنشاء مناطق سهول مسطحة حول المدن.

```csharp
plateauGenerator.GeneratePlains(heightMap, masterPlan, worldMin, pixelSize);
```

### المرحلة 4: الجبال
**الملف:** `PlateauMountainHillGenerator.cs`

وضع الجبال في مناطقها المحددة من Master Plan.

```csharp
plateauGenerator.GenerateMountains(heightMap, masterPlan, worldMin, pixelSize);
```

**الخصائص:**
- شكل هرمي: الأعلى في المركز، تنحدر تدريجياً للخارج
- تفاصيل صخرية عشوائية
- ترتفع فوق الارتفاعات الحالية فقط

### المرحلة 5: التلال
**الملف:** `PlateauMountainHillGenerator.cs`

انتقالات سلسة بين السهول والجبال.

```csharp
plateauGenerator.GenerateHills(heightMap, masterPlan, worldMin, pixelSize);
```

### المرحلة 6: السواحل
**الملف:** `RiverLakeCoastGenerator.cs`

تحديد وتصنيف خطوط الساحل.

```csharp
var coastlineData = waterGenerator.GenerateCoastline(heightMap, masterPlan, worldMin, pixelSize);
```

**التصنيفات:**
- شواطئ (Beaches): منحدرة لطيفة
- جروف (Cliffs): منحدرات حادة
- موانئ (Harbors): مناطق محمية

### المرحلة 7: الأنهار
**الملف:** `RiverLakeCoastGenerator.cs`

تتبع الأنهار بذكاء حسب الانحدار الطبيعي.

```csharp
var rivers = waterGenerator.GenerateRivers(heightMap, masterPlan, worldMin, pixelSize);
```

**الخوارزمية:**
1. ابدأ من أعلى نقطة في الجبل
2. في كل خطوة، انتقل إلى أقل نقطة مجاورة
3. استمر حتى تصل إلى البحر أو تفقد الحافزية

### المرحلة 8: البحيرات
**الملف:** `RiverLakeCoastGenerator.cs`

وضع البحيرات في الوديان.

```csharp
var lakes = waterGenerator.GenerateLakes(heightMap, masterPlan, worldMin, pixelSize);
```

### المرحلة 9: مناطق الغابات
**الملف:** `ForestGenerator.cs`

تحديد مناطق الغابات.

```csharp
var forests = forestGenerator.GenerateForests(masterPlan, worldMin, worldMax);
```

**المعايير:**
- بعيدة عن المدن (5 كم على الأقل)
- بعيدة عن الغابات الأخرى (3 كم على الأقل)
- توزع عشوائي بكثافة متغيرة

### المرحلة 10: قابلية البناء
**الملف:** `BuildabilityAndSlopeAnalyzer.cs`

حساب نسبة قابلية البناء في كل بكسل (0-1).

```csharp
analysisGenerator.CalculateBuildability(
    heightMap, protectedZones, forests, lakes, rivers,
    worldMin, pixelSize, pixelData
);
```

**العوامل:**
- الماء = 0 (غير قابل للبناء)
- جبال عالية = 0.3
- تلال = 0.6
- سهول = 1.0
- الغابات = تقليل بناءً على الكثافة
- المناطق المحمية = 1.0

### المرحلة 11: خريطة الانحدارات
**الملف:** `BuildabilityAndSlopeAnalyzer.cs`

حساب الانحدار في كل بكسل (بالدرجات).

```csharp
analysisGenerator.CalculateSlopes(heightMap, pixelSize, pixelData);
```

**الاستخدام:**
- تخطيط الطرق: نحب الانحدار < 3 درجات
- جودة الطريق (Road Friendliness):
  - < 3°: 1.0 (ممتاز)
  - < 6°: 0.8 (جيد)
  - < 10°: 0.5 (متوسط)
  - < 15°: 0.2 (سيء)
  - ≥ 15°: 0.0 (مستحيل)

### المرحلة 12: التحقق من الصحة
**الملف:** `TerrainValidatorAndCorrector.cs`

التحقق من أن التضاريس منطقية وسليمة.

```csharp
var validation = validatorCorrector.ValidateTerrainData(
    terrainData, masterPlan, worldMin, pixelSize
);
```

**الفحوصات:**
1. وجود HeightMap
2. عدد المناطق المحمية صحيح
3. ارتفاع المدن معقول
4. الأنهار متصلة
5. البحيرات بحجم معقول
6. الإحصائيات صحيحة

### المرحلة 13: التصحيح التلقائي
**الملف:** `TerrainValidatorAndCorrector.cs`

إصلاح المشاكل المكتشفة تلقائياً.

```csharp
validatorCorrector.CorrectTerrainIssues(
    terrainData, heightMap, worldMin, pixelSize
);
```

**الإصلاحات:**
- حذف الأنهار القصيرة جداً
- ملء الفجوات في الأنهار بنقاط وسيطة
- إزالة البحيرات المتداخلة
- إعادة حساب الإحصائيات

---

## ملفات البيانات

### TerrainGenerationData.cs
**الفئات الرئيسية:**

```csharp
// نقطة واحدة في الخريطة
public class HeightMapPixel {
    public float height;               // الارتفاع (متر)
    public float buildability;         // قابلية البناء (0-1)
    public float roadFriendliness;     // صلاحية الطريق (0-1)
    public float slope;                // الانحدار (درجات)
    public TerrainLayerType primaryLayer;
    public bool isWater;
    public bool isForest;
}

// الخريطة الكاملة
public class HeightMapData {
    public int width;                  // 2048 أو 4096
    public int height;
    public float pixelSizeMeters;
    public List<float> heightData;     // مصفوفة 2D كـ 1D
    public float minElevation;
    public float maxElevation;
}

// المنطقة المحمية حول مدينة
public class ProtectedZoneData {
    public Vector2 center;
    public float radiusMeters;
    public float innerRadiusMeters;    // المنطقة المستقيمة تماماً
    public float maxAllowedSlope;
}

// النهر
public class RiverData {
    public string name;
    public List<Vector2> pathPoints;   // مسار النهر
    public float width;
    public float depth;
    public float waterLevel;
}

// البحيرة
public class LakeData {
    public string name;
    public Vector2 center;
    public float radiusMeters;
    public float waterLevel;
    public string lakeType;            // Mountain, Forest, Urban
}

// منطقة الغابة
public class ForestRegionData {
    public string name;
    public Vector2 center;
    public float radiusMeters;
    public float density;              // 0-1
}

// الملف الرئيسي
public class TerrainGenerationData {
    public HeightMapData heightMap;
    public List<ProtectedZoneData> protectedZones;
    public List<RiverData> rivers;
    public List<LakeData> lakes;
    public List<ForestRegionData> forests;
    public CoastlineData coastline;
    
    // الإحصائيات
    public float avgBuildability;
    public float avgRoadFriendliness;
    public float waterCoverage;
    public float forestCoverage;
    public float mountainCoverage;
}
```

### TerrainAnalysisData.cs
بيانات إضافية للتحليل المتقدم (اختياري، مفيد للمراحل التالية):

```csharp
public class TerrainAnalysisData {
    public List<ExpansionAreaData> bestExpansionAreas;
    public List<HighwayCorridorData> highwayCorridors;
    public List<FloodRiskData> floodRisks;
    public List<LandslideRiskData> landslideRisks;
    public List<FutureAirportAreaData> futureAirportAreas;
    public List<Vector2> bestIndustrialZones;
    public List<Vector2> bestResidentialZones;
    public List<Vector2> bestCommercialZones;
    public List<Vector2> conservationAreas;
}
```

---

## حفظ وتحميل

### الحفظ
```csharp
TerrainSaveLoadUtility.SaveTerrainData(terrainData, Application.persistentDataPath);
TerrainSaveLoadUtility.SaveTerrainAnalysis(analysisData, Application.persistentDataPath);
```

**الملفات:**
- `TerrainData.json` - بيانات التضاريس الأساسية
- `TerrainAnalysis.json` - التحليل الإضافي (اختياري)

### التحميل
```csharp
var terrainData = TerrainSaveLoadUtility.LoadTerrainData(path);
var analysisData = TerrainSaveLoadUtility.LoadTerrainAnalysis(path);
```

---

## الأمثلة العملية

### مثال 1: الاستخدام الأساسي
```csharp
var masterPlan = MasterPlanSaveLoadUtility.LoadMasterPlanData("MasterPlanData.json");
var builder = new TerrainGenerationBuilder(seed: 12345, masterPlan);
var terrainData = builder.BuildTerrain(resolution: 2048);
TerrainSaveLoadUtility.SaveTerrainData(terrainData, ".");
```

### مثال 2: تحليل البيانات
```csharp
var terrainData = TerrainSaveLoadUtility.LoadTerrainData("TerrainData.json");

Debug.Log($"Elevation: {terrainData.heightMap.minElevation}m to {terrainData.heightMap.maxElevation}m");
Debug.Log($"Water: {terrainData.waterCoverage:P}");
Debug.Log($"Rivers: {terrainData.rivers.Count}");
Debug.Log($"Lakes: {terrainData.lakes.Count}");
Debug.Log($"Forests: {terrainData.forests.Count}");
```

### مثال 3: الوصول إلى الارتفاع في نقطة معينة
```csharp
float height = terrainData.heightMap.GetHeight(x, y);
float heightInterpolated = terrainData.heightMap.GetHeightInterpolated(xFloat, yFloat);
```

---

## الإحصائيات والمقاييس

| الإحصائية | النطاق | الوصف |
|---------|--------|-------|
| Elevation | -50 إلى 2500 متر | ارتفاع الأرض |
| Buildability | 0-1 | مدى ملاءمة الأرض للبناء |
| Road Friendliness | 0-1 | مدى ملاءمة الأرض للطرق |
| Slope | 0-90 درجة | الانحدار |
| Water Coverage | 0-1 | نسبة الماء من الخريطة |
| Forest Coverage | 0-1 | نسبة الغابات |
| Mountain Coverage | 0-1 | نسبة الجبال |

---

## التوصيات

### للأداء:
- استخدم دقة 2048 للاختبار السريع
- استخدم دقة 4096 للمنتج النهائي
- استخدم Seed محدد للتطابق

### للدقة:
- دقق المناطق المحمية أولاً
- تحقق من الأنهار يدوياً
- اختبر قابلية البناء على مناطق العينة

### للتوسع:
- احفظ TerrainAnalysisData لتسريع المرحلة التالية
- لا تعيد حسابات يمكنك تخزينها
- استخدم Threading للخريطات الكبيرة (اختياري)

---

## الأخطاء الشائعة

| الخطأ | السبب | الحل |
|-------|--------|------|
| HeightMap فارغة | لم يتم تحديث الإحصائيات | استدعِ `UpdateStats()` |
| أنهار متقطعة | فجوات كبيرة بين النقاط | يصحح تلقائياً في المرحلة 13 |
| مدن على جبال | فشل حماية المناطق | تحقق من `protectedZones` |
| بيانات تضاريس فارغة | Master Plan غير كامل | تحقق من صحة MasterPlanData |

---

## الملفات المطلوبة

```
Assets/ZZCityGen/Runtime/Planning/TerrainGeneration/
├── TerrainGenerationData.cs              # بيانات التضاريس
├── TerrainAnalysisData.cs                # بيانات التحليل (اختياري)
├── TerrainGenerationBuilder.cs           # المحرك الرئيسي
├── TerrainSaveLoadUtility.cs             # حفظ/تحميل
├── TerrainGenerationUsageExample.cs      # الأمثلة (7 أمثلة)
├── Generators/
│   ├── BaseHeightMapGenerator.cs         # المرحلة 1
│   ├── CityProtectionZoneGenerator.cs    # المرحلة 2
│   ├── PlateauMountainHillGenerator.cs   # المراحل 3-5
│   ├── RiverLakeCoastGenerator.cs        # المراحل 6-8
│   ├── ForestGenerator.cs                # المرحلة 9
│   ├── BuildabilityAndSlopeAnalyzer.cs   # المراحل 10-11
│   └── TerrainValidatorAndCorrector.cs   # المراحل 12-13
└── README.md                             # هذا الملف
```

---

## الخطوات التالية

بعد إكمال المرحلة 3 (توليد التضاريس)، الانتقال إلى:

**المرحلة 4: Road Network Engine**
- استخدم `TerrainAnalysisData` لتخطيط الطرق الذكية
- اتبع ممرات Buildability العالية
- تجنب الانحدارات الحادة

**المرحلة 5: Building Placement Engine**
- استخدم Buildability scores لوضع المباني
- تجنب المناطق المائية
- احترم حدود الغابات

---

## الملاحظات النهائية

- **حتمي:** كل بكسل في HeightMap يحتاج ارتفاع محدد
- **ذكي:** الأنهار تتبع الانحدار الطبيعي
- **مرن:** يمكنك تخصيص كل معامل في كل مرحلة
- **حتمي:** نفس Seed = نفس التضاريس (reproducible)
- **قابل للتحقق:** نظام التحقق يضمن جودة المخرجات
