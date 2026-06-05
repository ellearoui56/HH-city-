using System;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// يحدد حدود العالم الرياضية الأساسية
    /// Defines the mathematical world boundaries
    /// </summary>
    [Serializable]
    public class WorldBoundsData
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;

        /// <summary>
        /// الحجم الكلي للعالم بالكيلومتر
        /// </summary>
        public float sizeKm => (maxX - minX) / 1000f;

        public float areaSquareKm => (maxX - minX) * (maxZ - minZ) / 1_000_000f;

        public Vector2 center => new Vector2(
            (minX + maxX) / 2f,
            (minZ + maxZ) / 2f
        );

        public WorldBoundsData() { }

        public WorldBoundsData(float minX, float maxX, float minZ, float maxZ)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minZ = minZ;
            this.maxZ = maxZ;
        }

        public bool Contains(Vector2 position)
        {
            return position.x >= minX && position.x <= maxX &&
                   position.y >= minZ && position.y <= maxZ;
        }

        public Vector2 GetRandomPositionInBounds(System.Random random)
        {
            float x = minX + (float)random.NextDouble() * (maxX - minX);
            float z = minZ + (float)random.NextDouble() * (maxZ - minZ);
            return new Vector2(x, z);
        }
    }
}
