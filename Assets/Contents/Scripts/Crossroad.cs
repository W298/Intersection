using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Crossroad
{
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
                    Vector3 departPoint;
                    if (departRoad.GetPoints().Last().position == GetPosition())
                    {
                        departPoint = departRoad.EvaluatePosition(0.8f);
                    }
                    else
                    {
                        departPoint = departRoad.EvaluatePosition(0.2f);
                    }

                    Vector3 arrivPoint;
                    if (arrivRoad.GetPoints().Last().position == GetPosition())
                    {
                        arrivPoint = arrivRoad.EvaluatePosition(0.8f);
                    }
                    else
                    {
                        arrivPoint = arrivRoad.EvaluatePosition(0.2f);
                    }

                    // If failed to get Point Value, Re-try
                    if (Vector3.Distance(departPoint, arrivPoint) >= 20)
                    {
                        ConnectRoad();
                        return false;
                    }

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

                    
                    // Normalize Offset Vectors
                    departOffsetDir.Normalize();
                    arrivOffsetDir.Normalize();

                    
                    // Offset List
                    var rightOffsetList = new List<float>();
                    
                    switch (roads[0].roadLane)
                    {
                        case CreatePathManager.ROADLANE.RL1:
                            rightOffsetList.Add(0.65f);
                            break;
                        case CreatePathManager.ROADLANE.RL2:
                            rightOffsetList.Add(0.65f);
                            rightOffsetList.Add(0.65f * 3);
                            break;
                    }

                    foreach (var dRightOffset in rightOffsetList)
                    {
                        foreach (var aRightOffset in rightOffsetList)
                        {
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
                            
                            var connectingSpline = pathManager.InsSpline(GetPosition());
                            connectingSpline.name = departRoad.name + " - " + arrivRoad.name;
                            
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
                                roadConnection.AddConnectingSpline(connectingSpline);
                            }
                            else
                            {
                                var rc = new RoadConnection(arrivRoad);
                                rc.AddConnectingSpline(connectingSpline);
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
