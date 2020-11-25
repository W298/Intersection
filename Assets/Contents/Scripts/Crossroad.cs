using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Crossroad
{
    public static Dictionary<int, float> road_offset
        = new Dictionary<int, float>
        {
            {0, 0},
            {1, 0.65f},
            {2, 0.65f * 3}
        };
    
    private CreatePathManager pathManager;
    
    private Vector3 position;
    private List<SplineComputer> roads = new List<SplineComputer>();

    private int lastRoadCount;
    private int currentRoadCount;

    public Crossroad()
    {
        lastRoadCount = 0;
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
    }

    public Crossroad(List<SplineComputer> _roads)
    {
        SetRoads(_roads);
    }

    public Crossroad(List<SplineComputer> _roads, Vector3 _position)
    {
        SetRoads(_roads);
        position = _position;
    }

    public bool ConnectRoad()
    {
        foreach (var departRoad in roads)
        {
            foreach (var arrivRoad in roads)
            {
                if (departRoad != arrivRoad)
                {
                    Vector3 departDir;
                    Vector3 departOffsetDir;
                    if (departRoad.GetPoints().Last().position == this.GetPosition())
                    {
                        int lastIndex = departRoad.GetPoints().Length - 1;
                        departDir = departRoad.GetPoint(lastIndex).position -
                                    departRoad.GetPoint(lastIndex - 1).position;
                        departOffsetDir = Quaternion.AngleAxis(90, Vector3.up) * departDir;
                    }
                    else
                    {
                        departDir = departRoad.GetPoint(0).position - departRoad.GetPoint(1).position;
                        departOffsetDir = Quaternion.AngleAxis(90, Vector3.up) * departDir;
                    }

                    Vector3 arrivDir;
                    Vector3 arrivOffsetDir;
                    if (arrivRoad.GetPoints().Last().position == GetPosition())
                    {
                        int lastIndex = arrivRoad.GetPoints().Length - 1;
                        arrivDir = arrivRoad.GetPoint(lastIndex - 1).position -
                                   arrivRoad.GetPoint(lastIndex).position;
                        arrivOffsetDir = Quaternion.AngleAxis(90, Vector3.up) * arrivDir;
                    }
                    else
                    {
                        arrivDir = arrivRoad.GetPoint(1).position - arrivRoad.GetPoint(0).position;
                        arrivOffsetDir = Quaternion.AngleAxis(90, Vector3.up) * arrivDir;
                    }

                    
                    float depart_divider = 0;
                    float arriv_divider = 0;

                    switch (departRoad.roadLane)
                    {
                        case CreatePathManager.ROADLANE.RL05:
                            depart_divider = 1.5f;
                            break;
                        case CreatePathManager.ROADLANE.RL1:
                            depart_divider = 3f;
                            break;
                        case CreatePathManager.ROADLANE.RL2:
                            depart_divider = 3f;
                            break;
                    }
                    
                    switch (arrivRoad.roadLane)
                    {
                        case CreatePathManager.ROADLANE.RL05:
                            arriv_divider = 1.5f;
                            break;
                        case CreatePathManager.ROADLANE.RL1:
                            arriv_divider = 3f;
                            break;
                        case CreatePathManager.ROADLANE.RL2:
                            arriv_divider = 3f;
                            break;
                    }


                    var per_depart = departRoad.Project(GetPosition() - departDir / depart_divider).percent;
                    var departPoint = departRoad.EvaluatePosition(per_depart);

                    var per_arriv = arrivRoad.Project(GetPosition() + arrivDir / arriv_divider).percent;
                    var arrivPoint = arrivRoad.EvaluatePosition(per_arriv);

                    
                    // If failed to get Point Value, Re-try
                    if (Vector3.Distance(departPoint, arrivPoint) >= 20)
                    {
                        ConnectRoad();
                        return false;
                    }
                    
                    
                    // Normalize Offset Vectors
                    departOffsetDir.Normalize();
                    arrivOffsetDir.Normalize();

                    
                    // Offset List
                    var departOffsetList = new List<float>();
                    var arrivOffsetList = new List<float>();
                    
                    switch (departRoad.roadLane)
                    {
                        case CreatePathManager.ROADLANE.RL05:
                            departOffsetList.Add(road_offset[0]);
                            break;
                        case CreatePathManager.ROADLANE.RL1:
                            departOffsetList.Add(road_offset[1]);
                            break;
                        case CreatePathManager.ROADLANE.RL2:
                            departOffsetList.Add(road_offset[1]);
                            departOffsetList.Add(road_offset[2]);
                            break;
                    }

                    switch (arrivRoad.roadLane)
                    {
                        case CreatePathManager.ROADLANE.RL05:
                            arrivOffsetList.Add(road_offset[0]);
                            break;
                        case CreatePathManager.ROADLANE.RL1:
                            arrivOffsetList.Add(road_offset[1]);
                            break;
                        case CreatePathManager.ROADLANE.RL2:
                            arrivOffsetList.Add(road_offset[1]);
                            arrivOffsetList.Add(road_offset[2]);
                            break;
                    }

                    foreach (var dRightOffset in departOffsetList)
                    {
                        foreach (var aRightOffset in arrivOffsetList)
                        {
                            var start_offset = road_offset.FirstOrDefault(x => x.Value == dRightOffset).Key;
                            var end_offset = road_offset.FirstOrDefault(x => x.Value == aRightOffset).Key;
                            
                            // Set Point Position by Offset
                            var departPointOA = departPoint + departOffsetDir * dRightOffset;
                            var arrivPointOA = arrivPoint + arrivOffsetDir * aRightOffset;

                            // Calc Inter Point
                            Vector3 interPoint;
                            if (CreatePathManager.isVectorParallel(departDir, arrivDir))
                            {
                                interPoint = (departPointOA + arrivPointOA) / 2;
                            }
                            else
                            {
                                LineLineIntersection(out interPoint, departPointOA, departDir, arrivPointOA, -arrivDir);
                            }

                            if (Vector3.Distance(departPointOA, arrivPointOA) >= 20 ||
                                Vector3.Distance(departPointOA, interPoint) >= 20 ||
                                Vector3.Distance(interPoint, arrivPointOA) >= 20)
                            {
                                ConnectRoad();
                                return false;
                            }

                            var connectingSpline = pathManager.InsSpline(GetPosition());
                            connectingSpline.name = departRoad.name + " - " + arrivRoad.name + " / " +
                                                    start_offset + " - " + end_offset;
                            
                            // Spawn Depart Point
                            connectingSpline.SetPointNormal(0, CreatePathManager.def_normal);
                            connectingSpline.SetPointSize(0, 1);
                            connectingSpline.SetPointPosition(0, departPointOA);

                            // Spawn Inter Point
                            connectingSpline.SetPointNormal(1, CreatePathManager.def_normal);
                            connectingSpline.SetPointSize(1, 1);
                            connectingSpline.SetPointPosition(1, interPoint);
                            
                            // Spawn Arriv Point
                            connectingSpline.SetPointNormal(2, CreatePathManager.def_normal);
                            connectingSpline.SetPointSize(2, 1);
                            connectingSpline.SetPointPosition(2, arrivPointOA);

                            var roadConnection = departRoad.roadConnectionList.FirstOrDefault(rc => rc.GetconnectedRoad() == arrivRoad);
                            if (roadConnection != null)
                            {
                                roadConnection.AddConnector(connectingSpline, start_offset, end_offset);
                            }
                            else
                            {
                                var rc = new RoadConnection(arrivRoad);
                                rc.AddConnector(connectingSpline, start_offset, end_offset);
                                departRoad.roadConnectionList.Add(rc);
                            }
                        }
                    }
                }
            }
        }

        return true;
    }
    
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parallel
        if( Mathf.Approximately(planarFactor, 0f) && 
            !Mathf.Approximately(crossVec1and2.sqrMagnitude, 0f))
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    public List<SplineComputer> GetRoads()
    {
        return roads;
    }

    public Vector3 GetPosition()
    {
        return position;
    }

    public void AddRoad(SplineComputer road)
    {
        if (!roads.Contains(road))
            roads.Add(road);
    }

    public void RemoveRoad(SplineComputer road)
    {
        roads.Remove(road);
    }

    public void SetRoads(List<SplineComputer> list)
    {
        if (list != null)
        {
            roads.Clear();
            roads = list;
        }
    }

    public void SetPosition(Vector3 pos)
    {
        position = pos;
    }

    public void OnRoadUpdate()
    {
        foreach (var road in roads)
        {
            road.crossroad = this;
        }
        
        ConnectRoad();
    }

    public void Update()
    {
        currentRoadCount = roads.Count;

        if (currentRoadCount != lastRoadCount)
        {
            OnRoadUpdate();
            lastRoadCount = currentRoadCount;
        }
    }
}
