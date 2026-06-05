using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.4: اختيار موقع العاصمة
    /// Selects the capital location based on multiple criteria
    /// 
    /// يجب أن تكون العاصمة:
    /// ✅ بعيدة عن الجبال الحادة
    /// ✅ قريبة من السهول
    /// ✅ قابلة للتوسع
    /// ✅ مركزية
    /// </summary>
    public class CapitalLocationGenerator
    {
        private readonly System.Random random;

        public CapitalLocationGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// اختيار موقع العاصمة الأمثل
        /// يحسب Accessibility Score لكل موقع مرشح
        /// </summary>
        public CapitalLocationData SelectCapital(
            WorldBoundsData bounds,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate)
        {
            // ============================================
            // 1. إنشاء شبكة من الموقع المرشحة
            // ============================================
            var candidates = GenerateCandidates(bounds, terrain, climate);

            if (candidates.Count == 0)
            {
                Debug.LogError("[CapitalLocationGenerator] No valid capital candidates found!");
                return null;
            }

            // ============================================
            // 2. حساب نقاط لكل مرشح
            // ============================================
            foreach (var candidate in candidates)
            {
                CalculateCandidateScore(candidate, bounds, terrain, climate);
            }

            // ============================================
            // 3. اختيار أفضل مرشح
            // ============================================
            CityCandidateData best = candidates[0];
            foreach (var candidate in candidates)
            {
                if (candidate.totalScore > best.totalScore)
                    best = candidate;
            }

            Debug.Log($"[CapitalLocationGenerator] Selected capital at {best.position}");
            Debug.Log($"  Total Score: {best.totalScore:F1}");
            Debug.Log($"  Accessibility: {best.accessibilityScore:F1}");
            Debug.Log($"  Expansibility: {best.expansibilityScore:F1}");
            Debug.Log($"  Resource: {best.resourceScore:F1}");
            Debug.Log($"  Strategic: {best.strategicScore:F1}");

            // ============================================
            // 4. تحويل إلى CapitalLocationData
            // ============================================
            var capital = new CapitalLocationData(
                best.position,
                best.totalScore,
                targetPopulation: 800_000,  // العاصمة يجب أن تكون كبيرة
                radiusMeters: 15_000  // نطاق العاصمة
            );

            return capital;
        }

        /// <summary>
        /// توليد شبكة من الموقع المرشحة
        /// </summary>
        private List<CityCandidateData> GenerateCandidates(
            WorldBoundsData bounds,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate)
        {
            var candidates = new List<CityCandidateData>();

            // إنشاء شبكة بـ 5×5 = 25 مرشح
            int gridSize = 5;
            float cellWidth = (bounds.maxX - bounds.minX) / gridSize;
            float cellHeight = (bounds.maxZ - bounds.minZ) / gridSize;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    float posX = bounds.minX + cellWidth * (x + 0.5f);
                    float posZ = bounds.minZ + cellHeight * (z + 0.5f);
                    var position = new Vector2(posX, posZ);

                    // التحقق من أن الموقع داخل الحدود
                    if (bounds.Contains(position))
                    {
                        var candidate = new CityCandidateData(position, 0, 0, 0, 0);
                        candidates.Add(candidate);
                    }
                }
            }

            Debug.Log($"[CapitalLocationGenerator] Generated {candidates.Count} candidates");
            return candidates;
        }

        /// <summary>
        /// حساب نقاط المرشح
        /// </summary>
        private void CalculateCandidateScore(
            CityCandidateData candidate,
            WorldBoundsData bounds,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate)
        {
            // ============================================
            // 1. نقاط الوصولية (Accessibility Score)
            // ============================================
            // تقيس المسافة من المناطق الصالحة للبناء
            candidate.accessibilityScore = CalculateAccessibilityScore(candidate.position, terrain, bounds);

            // ============================================
            // 2. نقاط التوسعية (Expansibility Score)
            // ============================================
            // تقيس المساحة المتاحة للنمو المستقبلي
            candidate.expansibilityScore = CalculateExpansibilityScore(candidate.position, bounds, terrain);

            // ============================================
            // 3. نقاط الموارد (Resource Score)
            // ============================================
            // تقيس وجود الموارد الطبيعية والزراعية
            candidate.resourceScore = CalculateResourceScore(candidate.position, terrain, climate);

            // ============================================
            // 4. نقاط الموقع الاستراتيجي (Strategic Score)
            // ============================================
            // تقيس المركزية والأهمية الجغرافية
            candidate.strategicScore = CalculateStrategicScore(candidate.position, bounds);
        }

        /// <summary>
        /// حساب نقاط الوصولية
        /// النقاط العالية = الموقع قريب من مناطق قابلة للبناء
        /// </summary>
        private float CalculateAccessibilityScore(Vector2 position, TerrainAnalysisData terrain, WorldBoundsData bounds)
        {
            float maxScore = 100;
            float minDistance = float.MaxValue;

            // البحث عن أقرب منطقة قابلة للبناء
            foreach (var zone in terrain.zones)
            {
                if (!zone.isBuildable) continue;

                float distance = Vector2.Distance(position, zone.center);
                if (distance < minDistance)
                    minDistance = distance;
            }

            // تحويل المسافة إلى نقاط (أقرب = نقاط أعلى)
            float normalizedDistance = Mathf.Clamp01(minDistance / (bounds.sizeKm * 500));
            return maxScore * (1 - normalizedDistance);
        }

        /// <summary>
        /// حساب نقاط التوسعية
        /// النقاط العالية = مساحة كبيرة حول الموقع صالحة للبناء
        /// </summary>
        private float CalculateExpansibilityScore(Vector2 position, WorldBoundsData bounds, TerrainAnalysisData terrain)
        {
            float maxScore = 100;
            float buildableArea = 0;
            float checkRadius = bounds.sizeKm * 1000 / 4;  // 25% من حجم العالم

            // عد المساحات القابلة للبناء حول الموقع
            foreach (var zone in terrain.zones)
            {
                if (!zone.isBuildable) continue;

                float distance = Vector2.Distance(position, zone.center);
                if (distance < checkRadius)
                {
                    // كلما اقترب الموقع من الجزء المركزي للمنطقة، كلما زادت النقاط
                    float proximity = 1 - (distance / checkRadius);
                    buildableArea += zone.percentageOfWorld * proximity;
                }
            }

            return maxScore * Mathf.Clamp01(buildableArea);
        }

        /// <summary>
        /// حساب نقاط الموارد
        /// النقاط العالية = مناخ جيد وموارد طبيعية
        /// </summary>
        private float CalculateResourceScore(Vector2 position, TerrainAnalysisData terrain, ClimateAnalysisData climate)
        {
            float score = 0;

            // ============================================
            // 1. الموقع يجب أن يكون في منطقة مناخ جيدة
            // ============================================
            var bestClimate = climate.GetMostAgriculturalRegion();
            if (bestClimate != null)
            {
                float climateDist = Vector2.Distance(position, bestClimate.center);
                float maxDist = bestClimate.radiusMeters;
                if (climateDist < maxDist)
                {
                    float proximity = 1 - (climateDist / maxDist);
                    score += proximity * 50;  // 50 نقطة من المناخ
                }
            }

            // ============================================
            // 2. قرب مناطق مسطحة جيدة
            // ============================================
            var bestFlat = terrain.GetMostBuildableZone();
            if (bestFlat != null)
            {
                float flatDist = Vector2.Distance(position, bestFlat.center);
                float maxDist = bestFlat.radiusMeters;
                if (flatDist < maxDist)
                {
                    float proximity = 1 - (flatDist / maxDist);
                    score += proximity * 50;  // 50 نقطة من التضاريس
                }
            }

            return Mathf.Clamp(score, 0, 100);
        }

        /// <summary>
        /// حساب النقاط الاستراتيجية
        /// النقاط العالية = الموقع مركزي وسهل الدفاع
        /// </summary>
        private float CalculateStrategicScore(Vector2 position, WorldBoundsData bounds)
        {
            float maxScore = 100;

            // المسافة من مركز العالم (يجب أن تكون قريبة من المركز)
            Vector2 worldCenter = bounds.center;
            float distanceFromCenter = Vector2.Distance(position, worldCenter);
            float maxRadius = Mathf.Max(bounds.maxX - bounds.minX, bounds.maxZ - bounds.minZ) / 2f;

            // كلما اقتربنا من المركز، كلما زادت النقاط (الموقع المركزي أفضل)
            float centralityScore = (1 - (distanceFromCenter / maxRadius)) * maxScore;

            return Mathf.Clamp(centralityScore, 0, maxScore);
        }

        /// <summary>
        /// الحصول على قائمة بأفضل 5 مرشحين
        /// </summary>
        public List<CityCandidateData> GetTopCandidates(
            WorldBoundsData bounds,
            TerrainAnalysisData terrain,
            ClimateAnalysisData climate,
            int count = 5)
        {
            var candidates = GenerateCandidates(bounds, terrain, climate);

            foreach (var candidate in candidates)
            {
                CalculateCandidateScore(candidate, bounds, terrain, climate);
            }

            candidates.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));

            return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
        }
    }
}
