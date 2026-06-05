# معمارية محرك التخطيط العالمي - رسم بياني

## رسم البيانات الكامل

```
┌─────────────────────────────────────────────────────────────┐
│          MasterPlanBuilder (المحرك الرئيسي)               │
│                  يضبط تسلسل الخطوات                       │
└────────┬────────────────────────────────────────────────────┘
         │
         ├──[2.1]──→ WorldBoundsGenerator
         │          └──→ WorldBoundsData (حدود الكون)
         │
         ├──[2.2]──→ TerrainAnalysisGenerator
         │          └──→ TerrainAnalysisData (تحليل التضاريس)
         │             • Mountain Zones (جبال)
         │             • Hill Zones (تلال)
         │             • Flat Zones (سهول)
         │             • Water Zones (مياه)
         │
         ├──[2.3]──→ ClimateAnalysisGenerator
         │          └──→ ClimateAnalysisData (تحليل المناخ)
         │             • Temperate (معتدل)
         │             • Tropical (استوائي)
         │             • Mediterranean (متوسطي)
         │             • Alpine (جبلي)
         │             • Arid (صحراوي)
         │
         ├──[2.4]──→ CapitalLocationGenerator
         │          └──→ CapitalLocationData (موقع العاصمة)
         │             • Best location score
         │             • Target population
         │
         ├──[2.5]──→ CityDistributionGenerator
         │          └──→ List<CityMasterPlanData> (المدن)
         │             • Capital × 1 (800k)
         │             • Family Cities × 4 (120k each)
         │             • Industrial × 2 (180k each)
         │             • Rural × 3 (8k each)
         │             • Coastal × 2 (95k each)
         │             • University × 1 (50k)
         │
         ├──[2.6]──→ TransportNetworkGenerator
         │          └──→ TransportNetworkData (شبكة النقل)
         │             • Nodes (المدن)
         │             • Edges (الطرق المخططة)
         │             • Hub & Spoke topology
         │
         ├──[2.7]──→ InfrastructureGenerator
         │   [2.8]  └──→ InfrastructureMasterPlanData
         │             • Airports (مطارات)
         │               - International (عاصمة)
         │               - Regional (مدن كبرى)
         │               - Local (مدن صغرى)
         │             • Ports (موانئ)
         │               - Commercial
         │               - Fishing
         │               - Tourism
         │
         ├──[2.9]──→ EconomyAndPopulationGenerator
         │   [2.10] └──→ GlobalEconomyPlanData
         │             └──→ PopulationDistributionData
         │             • Economic identities
         │             • Population per city
         │
         ├──[2.11]─→ LandAllocationGenerator
         │          └──→ List<LandAllocationData>
         │             • Residential (20%)
         │             • Commercial (8%)
         │             • Industrial (10%)
         │             • Agricultural (30%)
         │             • Parks (20%)
         │             • Government (2%)
         │             • Reserved (10%)
         │
         └──[2.12]─→ MasterPlanValidator
                    └──→ MasterPlanValidationData
                       • Errors (أخطاء حرجة)
                       • Warnings (تحذيرات)
                       • isValid flag
```

## هرم البيانات

```
                         MasterPlanData
                              │
                ┌──────────────┼──────────────┐
                │              │              │
        ┌─────────────┐  ┌──────────┐  ┌───────────────┐
        │   Metadata  │  │  Layers  │  │  Validation   │
        ├─────────────┤  ├──────────┤  ├───────────────┤
        │ • seed      │  │          │  │ • errors      │
        │ • worldName │  │ 1. World │  │ • warnings    │
        │ • timestamp │  │    Layer │  │ • isValid     │
        └─────────────┘  │          │  └───────────────┘
                         │ 2. Terrain
        ┌─────────────┐  │    Layer
        │ Geographical│  │          │
        ├─────────────┤  │ 3. Climate
        │ • bounds    │  │    Layer
        │ • terrain   │  │          │
        │ • climate   │  │ 4. Transport
        └─────────────┘  │    Layer
                         │          │
        ┌─────────────┐  │ 5. City
        │   Cities    │  │    Layer
        ├─────────────┤  │          │
        │ • capital   │  │ 6. Infrastructure
        │ • cities[]  │  │    Layer
        │ • indices   │  │          │
        └─────────────┘  │ 7. District
                         │    Layer
        ┌─────────────┐  │          │
        │Infrastructure│ │ ...and more
        ├─────────────┤  └──────────┘
        │ • airports  │
        │ • ports     │
        │ • plants    │
        └─────────────┘
```

## تدفق البيانات

```
الخطوة 2.1: الحدود
    ↓
    [Input: Size=200km]
    [Process: Create bounds]
    [Output: Bounds(min,max)]

الخطوة 2.2: التضاريس
    ↓
    [Input: Bounds]
    [Process: Analyze and divide]
    [Output: 5 Terrain Zones]

الخطوة 2.3: المناخ
    ↓
    [Input: Bounds + Terrain]
    [Process: Distribute climate]
    [Output: 5 Climate Regions]

الخطوة 2.4: العاصمة
    ↓
    [Input: Bounds + Terrain + Climate]
    [Process: Score candidates]
    [Output: Best Capital Location]

الخطوة 2.5: المدن
    ↓
    [Input: Capital + Bounds + Terrain]
    [Process: Distribute 12 cities]
    [Output: List of Cities with constraints]

الخطوة 2.6: النقل
    ↓
    [Input: Capital + Cities]
    [Process: Create graph]
    [Output: Transport Graph]

الخطوة 2.7-2.8: البنية التحتية
    ↓
    [Input: Cities + Terrain]
    [Process: Place airports & ports]
    [Output: Airports + Ports]

الخطوة 2.9-2.10: الاقتصاد والسكان
    ↓
    [Input: Cities]
    [Process: Assign economy + calculate population]
    [Output: Economy + Population]

الخطوة 2.11: الأراضي
    ↓
    [Input: World area + Terrain]
    [Process: Allocate land use]
    [Output: Land allocation percentages]

الخطوة 2.12: التحقق
    ↓
    [Input: Complete MasterPlan]
    [Process: Validate all data]
    [Output: Validation results]
```

## مثال على النموذج الهرمي للمدن

```
مدينة واحدة (CityMasterPlanData)
    ├── Basic Info
    │   ├── name: "Harbor City"
    │   ├── archetype: CoastalCity
    │   └── position: Vector2(45000, -50000)
    │
    ├── Size & Scope
    │   ├── radiusMeters: 6000
    │   ├── targetPopulation: 120000
    │   └── economicIdentity: Tourism
    │
    ├── Status
    │   ├── isCapital: false
    │   ├── isCoastal: true
    │   └── distanceFromCapitalKm: 78
    │
    └── Derived Properties
        └── growthRate: 7% annually
```

## مثال على تحليل التضاريس

```
World Size: 200km × 200km = 40,000 km²

├── Mountain Zone (الشمال)
│   ├── Area: 15% (6,000 km²)
│   ├── Avg Height: 2,500m
│   ├── Slope: 0.6 (حاد جداً)
│   └── Buildable: No ❌
│
├── Hill Zone (شمال-وسط)
│   ├── Area: 20% (8,000 km²)
│   ├── Avg Height: 1,200m
│   ├── Slope: 0.35
│   └── Buildable: Partial ⚠️
│
├── Flat Zone (الوسط) ← أفضل للبناء
│   ├── Area: 35% (14,000 km²)
│   ├── Avg Height: 150m
│   ├── Slope: 0.05
│   └── Buildable: Yes ✅
│
├── Valley Zone (جنوب-وسط)
│   ├── Area: 15% (6,000 km²)
│   ├── Avg Height: 50m
│   ├── Slope: 0.15
│   └── Buildable: Yes ✅
│
└── Water Zone (الجنوب)
    ├── Area: 15% (6,000 km²)
    ├── Avg Height: -50m
    ├── Slope: 0
    └── Buildable: No ❌
```

## نموذج توزيع السكان

```
Total Population: 1,800,000

Capital City (العاصمة)
    800,000 (44%)
    └── 1 Capital

Family Cities
    480,000 (27%)
    └── 4 cities × 120,000 each

Industrial Cities
    360,000 (20%)
    └── 2 cities × 180,000 each

Coastal Cities
    190,000 (11%)
    └── 2 cities × 95,000 each

University City
    50,000 (3%)
    └── 1 city

Rural Towns
    24,000 (1%)
    └── 3 cities × 8,000 each
```

## خريطة التوصيلية (Connectivity Map)

```
                    Capital City
                   (العاصمة)
                        │
        ┌───────┬───────┼───────┬───────┐
        │       │       │       │       │
    Family  Family  Industrial Industrial  University
    City1   City2    City1     City2      City
        │       │       │       │       │
        └─┬─────┴───┬───┴───┬───┴─────┬─┘
          │         │       │         │
      Coastal   Coastal   Family   Rural
      City1     City2     City3   Town1
                           │        │
                        Family    Rural
                        City4     Town2
                                   │
                                Rural
                                Town3
```

## مثال على توزيع الأراضي العالمية

```
40,000 km² إجمالياً

Agricultural (الزراعي)
    12,000 km² (30%)
    └── For farmlands and forests

Residential (السكني)
    8,000 km² (20%)
    └── Urban and suburban areas

Parks (الحدائق والطبيعة)
    8,000 km² (20%)
    └── Protected areas and recreation

Industrial (الصناعي)
    4,000 km² (10%)
    └── Manufacturing and factories

Commercial (التجاري)
    3,200 km² (8%)
    └── Trade and business districts

Government (الحكومي)
    800 km² (2%)
    └── Administrative zones

Water & Unbuildable (مياه وغير صالحة)
    3,200 km² (8%)
    └── Seas and mountains

Reserved (محفوظة للمستقبل)
    800 km² (2%)
    └── Future development
```

---

## الملفات المرتبطة

- [MasterPlanEngine.md](MasterPlanEngine.md) - الوثائق الكاملة
- [Architecture.md](Architecture.md) - معمارية النظام الكاملة
