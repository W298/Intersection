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

    public void ConnectRoad()
    {
        foreach (var departRoad in roads)
        {
            foreach (var arrivRoad in roads)
            {
                if (departRoad != arrivRoad)
                {
                    var spline = pathManager.InsSpline(GetPosition());

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

                    departOffsetDir.Normalize();
                    arrivOffsetDir.Normalize();

                    spline.SetPointNormal(0, pathManager.def_normal);
                    spline.SetPointSize(0, 1);
                    spline.SetPointPosition(0, departPoint + departOffsetDir * 0.65f);
                    pathManager.debugPointPer(departPoint + departOffsetDir * 0.65f);

                    Vector3 interPoint;
                    LineLineIntersection(out interPoint, departPoint + departOffsetDir * 0.65f, departDir,
                        arrivPoint + arrivOffsetDir * 0.65f, -arrivDir);
                    
                    spline.SetPointNormal(1, pathManager.def_normal);
                    spline.SetPointSize(1, 1);
                    spline.SetPointPosition(1, interPoint);
                    pathManager.debugPointPer(interPoint);
                    
                    spline.SetPointNormal(2, pathManager.def_normal);
                    spline.SetPointSize(2, 1);
                    spline.SetPointPosition(2, arrivPoint + arrivOffsetDir * 0.65f);
                    pathManager.debugPointPer(arrivPoint + arrivOffsetDir * 0.65f);
                }
            }
        }
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
