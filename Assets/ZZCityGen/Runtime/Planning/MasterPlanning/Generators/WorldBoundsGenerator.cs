using System;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.1: إنشاء حدود العالم
    /// Generates world boundaries based on world size configuration
    /// </summary>
    public class WorldBoundsGenerator
    {
        private readonly System.Random random;

        public WorldBoundsGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد حدود العالم
        /// مثال: 200km × 200km
        /// </summary>
        public WorldBoundsData GenerateWorldBounds(float worldSizeKm)
        {
            // تحويل من الكيلومتر إلى المتر
            float sizeMeters = worldSizeKm * 1000;

            // إنشاء عالم متمركز حول (0, 0)
            float min = -sizeMeters / 2f;
            float max = sizeMeters / 2f;

            var bounds = new WorldBoundsData(
                minX: min,
                maxX: max,
                minZ: min,
                maxZ: max
            );

            Debug.Log($"[WorldBoundsGenerator] Generated world bounds: {worldSizeKm}km × {worldSizeKm}km");
            Debug.Log($"  Area: {bounds.areaSquareKm:F2} km²");
            Debug.Log($"  Bounds: X[{bounds.minX}-{bounds.maxX}], Z[{bounds.minZ}-{bounds.maxZ}]");

            return bounds;
        }

        /// <summary>
        /// توليد حدود مخصصة (مستطيل)
        /// </summary>
        public WorldBoundsData GenerateCustomWorldBounds(float widthKm, float lengthKm)
        {
            float widthMeters = widthKm * 1000;
            float lengthMeters = lengthKm * 1000;

            float minX = -widthMeters / 2f;
            float maxX = widthMeters / 2f;
            float minZ = -lengthMeters / 2f;
            float maxZ = lengthMeters / 2f;

            var bounds = new WorldBoundsData(minX, maxX, minZ, maxZ);

            Debug.Log($"[WorldBoundsGenerator] Generated custom bounds: {widthKm}km × {lengthKm}km");
            Debug.Log($"  Area: {bounds.areaSquareKm:F2} km²");

            return bounds;
        }

        /// <summary>
        /// يتحقق من صحة الحدود
        /// </summary>
        public bool ValidateWorldBounds(WorldBoundsData bounds)
        {
            if (bounds == null)
            {
                Debug.LogError("[WorldBoundsGenerator] Bounds are null");
                return false;
            }

            if (bounds.minX >= bounds.maxX)
            {
                Debug.LogError("[WorldBoundsGenerator] Invalid X bounds");
                return false;
            }

            if (bounds.minZ >= bounds.maxZ)
            {
                Debug.LogError("[WorldBoundsGenerator] Invalid Z bounds");
                return false;
            }

            if (bounds.areaSquareKm < 100)
            {
                Debug.LogWarning("[WorldBoundsGenerator] World is very small (< 100 km²)");
            }

            if (bounds.areaSquareKm > 1_000_000)
            {
                Debug.LogWarning("[WorldBoundsGenerator] World is very large (> 1,000,000 km²)");
            }

            return true;
        }
    }
}
