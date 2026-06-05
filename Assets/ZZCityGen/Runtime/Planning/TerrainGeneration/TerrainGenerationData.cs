using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.TerrainGeneration
{
    /// <summary>
    /// أنواع البيانات الأساسية للتضاريس
    /// </summary>
    public enum TerrainLayerType
    {
        BaseHeight,      // طبقة الارتفاع الأساسي
        Mountain,        // الجبال
        Hill,            // التلال
        River,           // الأنهار
        Lake,            // البحيرات
        Coast,           // السواحل
        Forest,          // الغابات
        Detail           // التفاصيل
    }

    /// <summary>
    /// نقطة واحدة في HeightMap
    /// </summary>
    [Serializable]
    public class HeightMapPixel
    {
        /// <summary>
        /// الارتفاع بالمتر
        /// </summary>
        public float height;

        /// <summary>
        /// نسبة قابلية البناء (0-1)
        /// </summary>
        public float buildability;

        /// <summary>
        /// نسبة صلاحية الطريق (0-1)
        /// </summary>
        public float roadFriendliness;

        /// <summary>
        /// الانحدار (درجات)
        /// </summary>
        public float slope;

        /// <summary>
        /// نوع التضاريس الأساسي
        /// </summary>
        public TerrainLayerType primaryLayer;

        /// <summary>
        /// هل هذه النقطة مائية؟
        /// </summary>
        public bool isWater;

        /// <summary>
        /// هل هذه النقطة في غابة؟
        /// </summary>
        public bool isForest;

        /// <summary>
        /// معرف المدينة القريبة (إن وجدت)
        /// </summary>
        public int nearestCityIndex = -1;

        public HeightMapPixel()
        {
            height = 0;
            buildability = 0.5f;
            roadFriendliness = 0.5f;
            slope = 0;
            primaryLayer = TerrainLayerType.BaseHeight;
            isWater = false;
            isForest = false;
        }
    }

    /// <summary>
    /// HeightMap الكامل
    /// </summary>
    [Serializable]
    public class HeightMapData
    {
        /// <summary>
        /// عرض الخريطة بالبكسل
        /// </summary>
        public int width;

        /// <summary>
        /// ارتفاع الخريطة بالبكسل
        /// </summary>
        public int height;

        /// <summary>
        /// حجم البكسل الواحد بالمتر
        /// </summary>
        public float pixelSizeMeters;

        /// <summary>
        /// بيانات الارتفاع (2D array stored as 1D)
        /// </summary>
        public List<float> heightData = new List<float>();

        /// <summary>
        /// أقل ارتفاع في الخريطة
        /// </summary>
        public float minElevation;

        /// <summary>
        /// أعلى ارتفاع في الخريطة
        /// </summary>
        public float maxElevation;

        public HeightMapData() { }

        public HeightMapData(int width, int height, float pixelSize)
        {
            this.width = width;
            this.height = height;
            this.pixelSizeMeters = pixelSize;
            this.heightData = new List<float>(width * height);

            // ملء البيانات بقيم افتراضية
            for (int i = 0; i < width * height; i++)
            {
                heightData.Add(0);
            }
        }

        /// <summary>
        /// الحصول على قيمة الارتفاع في نقطة محددة
        /// </summary>
        public float GetHeight(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return 0;

            return heightData[y * width + x];
        }

        /// <summary>
        /// تعيين قيمة الارتفاع في نقطة محددة
        /// </summary>
        public void SetHeight(int x, int y, float value)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            heightData[y * width + x] = value;
        }

        /// <summary>
        /// الحصول على الارتفاع بالاستيفاء الثنائي (Bilinear Interpolation)
        /// </summary>
        public float GetHeightInterpolated(float x, float y)
        {
            // تحويل الإحداثيات العالمية إلى إحداثيات الخريطة
            x = Mathf.Clamp(x, 0, width - 1.001f);
            y = Mathf.Clamp(y, 0, height - 1.001f);

            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);

            float fx = x - x0;
            float fy = y - y0;

            float h00 = GetHeight(x0, y0);
            float h10 = GetHeight(x1, y0);
            float h01 = GetHeight(x0, y1);
            float h11 = GetHeight(x1, y1);

            float h0 = Mathf.Lerp(h00, h10, fx);
            float h1 = Mathf.Lerp(h01, h11, fx);

            return Mathf.Lerp(h0, h1, fy);
        }

        /// <summary>
        /// تحديث إحصائيات الارتفاع
        /// </summary>
        public void UpdateStats()
        {
            if (heightData.Count == 0)
                return;

            minElevation = float.MaxValue;
            maxElevation = float.MinValue;

            foreach (var h in heightData)
            {
                minElevation = Mathf.Min(minElevation, h);
                maxElevation = Mathf.Max(maxElevation, h);
            }
        }
    }

    /// <summary>
    /// منطقة محمية حول المدينة
    /// </summary>
    [Serializable]
    public class ProtectedZoneData
    {
        public int cityIndex;
        public Vector2 center;
        public float radiusMeters;
        public float maxAllowedSlope;

        /// <summary>
        /// المنطقة الداخلية (مستقيمة تماماً)
        /// </summary>
        public float innerRadiusMeters;

        public ProtectedZoneData() { }

        public ProtectedZoneData(int cityIndex, Vector2 center, float radius, float maxSlope)
        {
            this.cityIndex = cityIndex;
            this.center = center;
            this.radiusMeters = radius;
            this.maxAllowedSlope = maxSlope;
            this.innerRadiusMeters = radius * 0.6f;  // 60% من النطاق الداخلي
        }

        /// <summary>
        /// هل النقطة داخل المنطقة المحمية؟
        /// </summary>
        public bool Contains(Vector2 position)
        {
            return Vector2.Distance(position, center) <= radiusMeters;
        }

        /// <summary>
        /// هل النقطة في المنطقة الداخلية المستقيمة؟
        /// </summary>
        public bool ContainsInner(Vector2 position)
        {
            return Vector2.Distance(position, center) <= innerRadiusMeters;
        }
    }

    /// <summary>
    /// طبقة تضاريس واحدة (جبال، تلال، إلخ)
    /// </summary>
    [Serializable]
    public class TerrainLayerData
    {
        public TerrainLayerType type;

        /// <summary>
        /// الارتفاع الأساسي لهذه الطبقة
        /// </summary>
        public float baseHeight;

        /// <summary>
        /// الارتفاع الأقصى لهذه الطبقة
        /// </summary>
        public float maxHeight;

        /// <summary>
        /// الانحدار المتوسط
        /// </summary>
        public float averageSlope;

        /// <summary>
        /// النسبة المئوية من الخريطة
        /// </summary>
        public float coveragePercentage;

        /// <summary>
        /// المناطق المخصصة لهذه الطبقة
        /// </summary>
        public List<Vector2> regionCenters = new List<Vector2>();

        public TerrainLayerData() { }

        public TerrainLayerData(TerrainLayerType type, float baseHeight, float maxHeight, float slope)
        {
            this.type = type;
            this.baseHeight = baseHeight;
            this.maxHeight = maxHeight;
            this.averageSlope = slope;
            this.regionCenters = new List<Vector2>();
        }
    }

    /// <summary>
    /// بيانات النهر الواحد
    /// </summary>
    [Serializable]
    public class RiverData
    {
        public string name;

        /// <summary>
        /// نقاط مسار النهر
        /// </summary>
        public List<Vector2> pathPoints = new List<Vector2>();

        /// <summary>
        /// عرض النهر بالمتر
        /// </summary>
        public float width;

        /// <summary>
        /// عمق النهر بالمتر
        /// </summary>
        public float depth;

        /// <summary>
        /// منسوب المياه
        /// </summary>
        public float waterLevel;

        public RiverData() { }

        public RiverData(string name, float width, float depth)
        {
            this.name = name;
            this.width = width;
            this.depth = depth;
            this.pathPoints = new List<Vector2>();
        }
    }

    /// <summary>
    /// بيانات البحيرة الواحدة
    /// </summary>
    [Serializable]
    public class LakeData
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;
        public float waterLevel;

        /// <summary>
        /// نوع البحيرة
        /// </summary>
        public string lakeType;  // Mountain, Forest, Urban

        public LakeData() { }

        public LakeData(string name, Vector2 center, float radius, float waterLevel, string type)
        {
            this.name = name;
            this.center = center;
            this.radiusMeters = radius;
            this.waterLevel = waterLevel;
            this.lakeType = type;
        }
    }

    /// <summary>
    /// منطقة غابة واحدة
    /// </summary>
    [Serializable]
    public class ForestRegionData
    {
        public string name;
        public Vector2 center;
        public float radiusMeters;

        /// <summary>
        /// نسبة الكثافة (0-1)
        /// </summary>
        public float density;

        public ForestRegionData() { }

        public ForestRegionData(string name, Vector2 center, float radius, float density)
        {
            this.name = name;
            this.center = center;
            this.radiusMeters = radius;
            this.density = density;
        }
    }

    /// <summary>
    /// بيانات الساحل
    /// </summary>
    [Serializable]
    public class CoastlineData
    {
        /// <summary>
        /// نقاط خط الساحل
        /// </summary>
        public List<Vector2> coastlinePoints = new List<Vector2>();

        /// <summary>
        /// مناطق الشاطئ (مستقيمة)
        /// </summary>
        public List<Vector2> beachCenters = new List<Vector2>();

        /// <summary>
        /// مناطق الجروف (عالية ومرتفعة)
        /// </summary>
        public List<Vector2> cliffCenters = new List<Vector2>();

        /// <summary>
        /// مناطق الموانئ (محمية وآمنة)
        /// </summary>
        public List<Vector2> harborZones = new List<Vector2>();

        public CoastlineData()
        {
            coastlinePoints = new List<Vector2>();
            beachCenters = new List<Vector2>();
            cliffCenters = new List<Vector2>();
            harborZones = new List<Vector2>();
        }
    }

    /// <summary>
    /// الملف الرئيسي: بيانات التضاريس الكاملة
    /// </summary>
    [Serializable]
    public class TerrainGenerationData
    {
        /// <summary>
        /// HeightMap الأساسي
        /// </summary>
        public HeightMapData heightMap;

        /// <summary>
        /// المناطق المحمية حول المدن
        /// </summary>
        public List<ProtectedZoneData> protectedZones = new List<ProtectedZoneData>();

        /// <summary>
        /// طبقات التضاريس
        /// </summary>
        public List<TerrainLayerData> layers = new List<TerrainLayerData>();

        /// <summary>
        /// الأنهار
        /// </summary>
        public List<RiverData> rivers = new List<RiverData>();

        /// <summary>
        /// البحيرات
        /// </summary>
        public List<LakeData> lakes = new List<LakeData>();

        /// <summary>
        /// مناطق الغابات
        /// </summary>
        public List<ForestRegionData> forests = new List<ForestRegionData>();

        /// <summary>
        /// بيانات الساحل
        /// </summary>
        public CoastlineData coastline;

        /// <summary>
        /// إحصائيات عامة
        /// </summary>
        public float avgBuildability;
        public float avgRoadFriendliness;
        public float waterCoverage;
        public float forestCoverage;
        public float mountainCoverage;

        public TerrainGenerationData()
        {
            protectedZones = new List<ProtectedZoneData>();
            layers = new List<TerrainLayerData>();
            rivers = new List<RiverData>();
            lakes = new List<LakeData>();
            forests = new List<ForestRegionData>();
            coastline = new CoastlineData();
        }
    }
}
