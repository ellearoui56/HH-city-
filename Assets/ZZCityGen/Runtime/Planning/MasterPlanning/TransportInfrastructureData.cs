using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Planning.MasterPlanning
{
    /// <summary>
    /// رسم بياني للنقل يربط المدن
    /// Transport graph connecting cities
    /// </summary>
    [Serializable]
    public class TransportNodeData
    {
        public int cityIndex;
        public Vector2 position;
        public bool isHub;  // عقدة محورية

        public TransportNodeData() { }

        public TransportNodeData(int cityIndex, Vector2 position, bool isHub = false)
        {
            this.cityIndex = cityIndex;
            this.position = position;
            this.isHub = isHub;
        }
    }

    [Serializable]
    public class TransportEdgeData
    {
        public int fromNodeIndex;
        public int toNodeIndex;
        
        /// <summary>
        /// نوع الطريق: Highway, Secondary, Local
        /// </summary>
        public string roadType;
        
        /// <summary>
        /// الطول بالكيلومتر
        /// </summary>
        public float lengthKm;
        
        /// <summary>
        /// الأولوية (1-10)
        /// </summary>
        public int priority;

        public TransportEdgeData() { }

        public TransportEdgeData(int from, int to, string roadType, float length, int priority = 5)
        {
            this.fromNodeIndex = from;
            this.toNodeIndex = to;
            this.roadType = roadType;
            this.lengthKm = length;
            this.priority = priority;
        }
    }

    /// <summary>
    /// شبكة النقل الكاملة
    /// Complete transport network
    /// </summary>
    [Serializable]
    public class TransportNetworkData
    {
        public List<TransportNodeData> nodes = new List<TransportNodeData>();
        public List<TransportEdgeData> edges = new List<TransportEdgeData>();
        
        /// <summary>
        /// إجمالي طول الطرق المخطط (كم)
        /// </summary>
        public float totalPlannedRoadsKm;
        
        /// <summary>
        /// عدد الطرق السريعة
        /// </summary>
        public int highwayCount;
        
        /// <summary>
        /// عدد الطرق الثانوية
        /// </summary>
        public int secondaryRoadCount;

        public TransportNetworkData()
        {
            nodes = new List<TransportNodeData>();
            edges = new List<TransportEdgeData>();
        }

        public void AddNode(TransportNodeData node)
        {
            nodes.Add(node);
        }

        public void AddEdge(TransportEdgeData edge)
        {
            edges.Add(edge);
            totalPlannedRoadsKm += edge.lengthKm;

            if (edge.roadType == "Highway")
                highwayCount++;
            else if (edge.roadType == "Secondary")
                secondaryRoadCount++;
        }
    }

    /// <summary>
    /// بيانات المطار
    /// Airport data
    /// </summary>
    public enum AirportType
    {
        International,   // دولي
        Regional,        // إقليمي
        Local            // محلي
    }

    [Serializable]
    public class AirportPlanData
    {
        public string name;
        public Vector2 position;
        public AirportType type;
        public int cityIndex;  // المدينة التي تخدمها
        
        /// <summary>
        /// السعة المخطط لها (الركاب السنويين)
        /// </summary>
        public long plannedCapacity;
        
        /// <summary>
        /// طول المدرج (متر)
        /// </summary>
        public float runwayLengthMeters;
        
        /// <summary>
        /// عدد بوابات المغادرة
        /// </summary>
        public int gateCount;

        public AirportPlanData() { }

        public AirportPlanData(string name, Vector2 position, AirportType type, int cityIndex)
        {
            this.name = name;
            this.position = position;
            this.type = type;
            this.cityIndex = cityIndex;
            
            // تحديد الخصائص حسب النوع
            switch (type)
            {
                case AirportType.International:
                    plannedCapacity = 50_000_000;
                    runwayLengthMeters = 3500;
                    gateCount = 40;
                    break;
                case AirportType.Regional:
                    plannedCapacity = 5_000_000;
                    runwayLengthMeters = 2500;
                    gateCount = 15;
                    break;
                case AirportType.Local:
                    plannedCapacity = 500_000;
                    runwayLengthMeters = 1500;
                    gateCount = 5;
                    break;
            }
        }
    }

    /// <summary>
    /// بيانات الميناء
    /// Port data
    /// </summary>
    public enum PortType
    {
        Commercial,      // تجاري
        Fishing,         // صيد
        Tourism,         // سياحي
        Military         // عسكري
    }

    [Serializable]
    public class PortPlanData
    {
        public string name;
        public Vector2 position;
        public PortType type;
        public int cityIndex;
        
        /// <summary>
        /// قدرة معالجة الحاويات (TEU سنويًا)
        /// </summary>
        public long containerCapacity;
        
        /// <summary>
        /// عدد الأرصفة المخطط لها
        /// </summary>
        public int plannedBerths;
        
        /// <summary>
        /// أقصى عمق للسفن (متر)
        /// </summary>
        public float maxShipDraftMeters;

        public PortPlanData() { }

        public PortPlanData(string name, Vector2 position, PortType type, int cityIndex)
        {
            this.name = name;
            this.position = position;
            this.type = type;
            this.cityIndex = cityIndex;

            switch (type)
            {
                case PortType.Commercial:
                    containerCapacity = 5_000_000;
                    plannedBerths = 8;
                    maxShipDraftMeters = 14;
                    break;
                case PortType.Fishing:
                    containerCapacity = 500_000;
                    plannedBerths = 20;
                    maxShipDraftMeters = 6;
                    break;
                case PortType.Tourism:
                    containerCapacity = 200_000;
                    plannedBerths = 4;
                    maxShipDraftMeters = 8;
                    break;
                case PortType.Military:
                    containerCapacity = 1_000_000;
                    plannedBerths = 6;
                    maxShipDraftMeters = 12;
                    break;
            }
        }
    }

    /// <summary>
    /// بيانات البنية التحتية الكاملة
    /// Complete infrastructure data
    /// </summary>
    [Serializable]
    public class InfrastructureMasterPlanData
    {
        public List<AirportPlanData> airports = new List<AirportPlanData>();
        public List<PortPlanData> ports = new List<PortPlanData>();
        
        /// <summary>
        /// محطات الكهرباء الرئيسية المخطط لها
        /// </summary>
        public int powerPlantCount;
        
        /// <summary>
        /// محطات معالجة المياه
        /// </summary>
        public int waterTreatmentPlantCount;
        
        /// <summary>
        /// محطات الصرف الصحي
        /// </summary>
        public int sewagePlantCount;

        public InfrastructureMasterPlanData()
        {
            airports = new List<AirportPlanData>();
            ports = new List<PortPlanData>();
        }

        public void AddAirport(AirportPlanData airport)
        {
            airports.Add(airport);
        }

        public void AddPort(PortPlanData port)
        {
            ports.Add(port);
        }
    }
}
