using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning.Generators
{
    /// <summary>
    /// المرحلة 2.6: إنشاء شبكة النقل العالمية
    /// Generates transport network (not actual roads, just planning)
    /// 
    /// ينشئ Transport Graph:
    /// - Nodes (المدن)
    /// - Edges (الطرق المخططة بين المدن)
    /// 
    /// مثال:
    ///   Capital → Family City → Industrial City
    /// </summary>
    public class TransportNetworkGenerator
    {
        private readonly System.Random random;

        public TransportNetworkGenerator(int seed)
        {
            this.random = new System.Random(seed);
        }

        /// <summary>
        /// توليد شبكة النقل
        /// </summary>
        public TransportNetworkData GenerateTransportNetwork(
            CapitalLocationData capital,
            List<CityMasterPlanData> cities)
        {
            var network = new TransportNetworkData();

            if (cities.Count == 0)
            {
                Debug.LogError("[TransportNetworkGenerator] No cities to connect!");
                return network;
            }

            // ============================================
            // 1. إضافة جميع المدن كـ Nodes
            // ============================================
            for (int i = 0; i < cities.Count; i++)
            {
                var node = new TransportNodeData(i, cities[i].position, cities[i].isCapital);
                network.AddNode(node);
            }

            Debug.Log($"[TransportNetworkGenerator] Added {network.nodes.Count} transport nodes");

            // ============================================
            // 2. إنشاء شبكة النقل الهرمية
            // ============================================
            // الخطوة الأولى: ربط كل مدينة بالعاصمة (Hub and Spoke)
            ConnectCapitalNetwork(network, cities, 0);

            // الخطوة الثانية: ربط المدن المتشابهة النوع
            ConnectSimilarCities(network, cities);

            // الخطوة الثالثة: ربط المدن الساحلية
            ConnectCoastalCities(network, cities);

            // ============================================
            // 3. حساب الإحصائيات
            // ============================================
            CalculateNetworkStatistics(network);

            Debug.Log($"[TransportNetworkGenerator] Transport network complete:");
            Debug.Log($"  Total Edges: {network.edges.Count}");
            Debug.Log($"  Total Planned Roads: {network.totalPlannedRoadsKm:F2} km");
            Debug.Log($"  Highways: {network.highwayCount}");
            Debug.Log($"  Secondary Roads: {network.secondaryRoadCount}");

            return network;
        }

        /// <summary>
        /// ربط شبكة المحور والحافة (Hub & Spoke)
        /// كل مدينة متصلة بالعاصمة
        /// </summary>
        private void ConnectCapitalNetwork(TransportNetworkData network, List<CityMasterPlanData> cities, int capitalIndex)
        {
            // ربط العاصمة بكل مدينة
            for (int i = 1; i < cities.Count; i++)
            {
                float distance = Vector2.Distance(cities[capitalIndex].position, cities[i].position) / 1000f;

                var edge = new TransportEdgeData(
                    capitalIndex,
                    i,
                    "Highway",  // الطرق السريعة تربط العاصمة بجميع المدن
                    distance,
                    priority: 10  // أولوية عالية
                );

                network.AddEdge(edge);
            }

            Debug.Log($"[TransportNetworkGenerator] Connected all cities to capital");
        }

        /// <summary>
        /// ربط المدن المتشابهة النوع
        /// مثلاً: المدن الصناعية مع بعضها
        /// </summary>
        private void ConnectSimilarCities(TransportNetworkData network, List<CityMasterPlanData> cities)
        {
            // تجميع المدن حسب النوع
            var citiesByType = new Dictionary<CityArchetype, List<int>>();

            for (int i = 0; i < cities.Count; i++)
            {
                if (!citiesByType.ContainsKey(cities[i].archetype))
                    citiesByType[cities[i].archetype] = new List<int>();

                citiesByType[cities[i].archetype].Add(i);
            }

            // ربط المدن من نفس النوع
            foreach (var typeGroup in citiesByType.Values)
            {
                // ربط كل مدينة بأقرب مدينة من نفس النوع
                for (int i = 0; i < typeGroup.Count; i++)
                {
                    int cityIndex = typeGroup[i];
                    float closestDistance = float.MaxValue;
                    int closestCityIndex = -1;

                    for (int j = 0; j < typeGroup.Count; j++)
                    {
                        if (i == j) continue;

                        int otherCityIndex = typeGroup[j];
                        float distance = Vector2.Distance(
                            cities[cityIndex].position,
                            cities[otherCityIndex].position
                        );

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCityIndex = otherCityIndex;
                        }
                    }

                    if (closestCityIndex >= 0)
                    {
                        float distance = closestDistance / 1000f;

                        var edge = new TransportEdgeData(
                            cityIndex,
                            closestCityIndex,
                            "Secondary",
                            distance,
                            priority: 5
                        );

                        // تحقق من عدم تكرار الحافة
                        if (!EdgeExists(network, cityIndex, closestCityIndex))
                        {
                            network.AddEdge(edge);
                        }
                    }
                }
            }

            Debug.Log($"[TransportNetworkGenerator] Connected similar city types");
        }

        /// <summary>
        /// ربط المدن الساحلية
        /// المدن الساحلية يجب أن تكون متصلة لتكوين خطوط تجارة بحرية
        /// </summary>
        private void ConnectCoastalCities(TransportNetworkData network, List<CityMasterPlanData> cities)
        {
            // اعثر على كل المدن الساحلية
            var coastalCities = new List<int>();
            for (int i = 0; i < cities.Count; i++)
            {
                // تحديد المدينة الساحلية بناءً على نوعها أو موقعها
                if (cities[i].archetype == CityArchetype.CoastalCity ||
                    cities[i].economicIdentity == EconomicIdentity.Tourism)
                {
                    coastalCities.Add(i);
                }
            }

            // ربط المدن الساحلية بسلسلة
            for (int i = 0; i < coastalCities.Count - 1; i++)
            {
                int city1 = coastalCities[i];
                int city2 = coastalCities[i + 1];

                float distance = Vector2.Distance(cities[city1].position, cities[city2].position) / 1000f;

                var edge = new TransportEdgeData(
                    city1,
                    city2,
                    "SecondaryRoad",
                    distance,
                    priority: 6
                );

                if (!EdgeExists(network, city1, city2))
                {
                    network.AddEdge(edge);
                }
            }

            Debug.Log($"[TransportNetworkGenerator] Connected {coastalCities.Count} coastal cities");
        }

        /// <summary>
        /// التحقق من وجود حافة بين مدينتين
        /// </summary>
        private bool EdgeExists(TransportNetworkData network, int fromIndex, int toIndex)
        {
            foreach (var edge in network.edges)
            {
                if ((edge.fromNodeIndex == fromIndex && edge.toNodeIndex == toIndex) ||
                    (edge.fromNodeIndex == toIndex && edge.toNodeIndex == fromIndex))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// حساب إحصائيات الشبكة
        /// </summary>
        private void CalculateNetworkStatistics(TransportNetworkData network)
        {
            network.totalPlannedRoadsKm = 0;
            network.highwayCount = 0;
            network.secondaryRoadCount = 0;

            foreach (var edge in network.edges)
            {
                network.totalPlannedRoadsKm += edge.lengthKm;

                if (edge.roadType == "Highway")
                    network.highwayCount++;
                else
                    network.secondaryRoadCount++;
            }
        }

        /// <summary>
        /// الحصول على جميع الطرق السريعة
        /// </summary>
        public List<TransportEdgeData> GetHighways(TransportNetworkData network)
        {
            var highways = new List<TransportEdgeData>();
            foreach (var edge in network.edges)
            {
                if (edge.roadType == "Highway")
                    highways.Add(edge);
            }
            return highways;
        }

        /// <summary>
        /// الحصول على جميع المدن المتصلة بمدينة معينة
        /// </summary>
        public List<int> GetConnectedCities(TransportNetworkData network, int cityIndex)
        {
            var connected = new List<int>();

            foreach (var edge in network.edges)
            {
                if (edge.fromNodeIndex == cityIndex)
                    connected.Add(edge.toNodeIndex);
                else if (edge.toNodeIndex == cityIndex)
                    connected.Add(edge.fromNodeIndex);
            }

            return connected;
        }
    }
}
