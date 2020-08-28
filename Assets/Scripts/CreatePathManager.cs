﻿using System;
using System.Collections;
using Dreamteck.Splines;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using ArrayUtility = Dreamteck.ArrayUtility;
using Object = System.Object;

public class Crossroad
{
    private Vector3 position;
    private List<SplineComputer> roads = new List<SplineComputer>();

    public Crossroad() {}

    public Crossroad(List<SplineComputer> _roads)
    {
        SetRoads(_roads);
    }

    public Crossroad(List<SplineComputer> _roads, Vector3 _position)
    {
        SetRoads(_roads);
        position = _position;
    }

    public List<SplineComputer> getRoads()
    {
        return roads;
    }

    public Vector3 getPosition()
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

    public void SetRoads(SplineComputer[] list)
    {
        if (list != null)
        {
            roads.Clear();
            roads = list.ToList();
        }
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

    public void Update()
    {
        
    }
}

public class CreatePathManager : MonoBehaviour
{
    public enum MODE
    {
        BUILD,
        APPEND,
        REMOVE,
        NONE
    };

    public enum JOINMODE
    {
        TO3,
        TO4,
        HEAD,
        TO3_SPLIT,
        NONE,
        TO3_NOSPLIT
    };

    public enum ROADLANE
    {
        RL1,
        RL2,
        RL3,
        RL4,
        RL05
    };

    private enum MERGEMODE
    {
        LL,
        FF,
        LF,
        FL,
        LOOP,
        NONE
    };

    public int height = 0;

    public SplineComputer[] roadPrefabs;
    public GameObject debugobj;
    public GameObject debugobj2;
    public GameObject textObj;
    public GameObject Pillar;
    
    private Camera cm;
    private SplineComputer SplinePrefab;
    private ItemManager itemManager;
    
    public int snapsize = 10;
    
    public float[] dividerList = new float[5]
    {
        7.22f,
        3.6f,
        0.0f,
        0.0f,
        12.0f
    };

    private Vector3 def_normal = new Vector3(0, 1, 0);

    public SplineComputer currentSpline;
    public MODE currentMode = MODE.NONE;
    public JOINMODE joinMode = JOINMODE.NONE;
    public int newIndex = 0;
    public ROADLANE currentRoadLane = ROADLANE.RL1;

    public float lastX;
    public float lastZ;
    public Vector3 lastPos;
    public Vector3 pos;
    public Vector3 snapPos;
    public Vector3 snapPosWithY;

    private SplineComputer selectedSpline;
    private List<SplineComputer> selectedSplines;
    private int selectedIndex = 0;
    private Crossroad selectedCrossroad;
    private SplineComputer crossOldSpline;
    private SplineComputer crossNewSpline;
    
    private Crossroad crossroadRef;
    public REMOVEMODE removeMode = REMOVEMODE.STANDBY;
    private int removePointIndex = -1;
    private SplinePoint removePoint;
    private bool _rebuildLateBool = false;
    private SplineComputer _rebuildLateSpline = null;
    
    public bool needDebug = false;

    private List<GameObject> texts = new List<GameObject>();
    public List<Crossroad> crossroads = new List<Crossroad>();

    private void SetMeshClip(SplineComputer spline, int direction, bool isTo, double per)
    {
        var mesh = spline.GetComponent<SplineMesh>();
        
        switch (direction)
        {
            case 0:
                if (isTo)
                {
                    mesh.GetChannel(2).clipTo = 1;
                    mesh.GetChannel(4).clipTo = 1;
                    
                    mesh.GetChannel(3).clipTo = per;
                    mesh.GetChannel(5).clipTo = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipTo = per;
                        mesh.GetChannel(7).clipTo = per;
                    }
                }
                else
                {
                    mesh.GetChannel(2).clipFrom = 0;
                    mesh.GetChannel(4).clipFrom = 0;
                    
                    mesh.GetChannel(3).clipFrom = per;
                    mesh.GetChannel(5).clipFrom = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipFrom = per;
                        mesh.GetChannel(7).clipFrom = per;
                    }
                }
                break;
            case 1:
                if (isTo)
                {
                    mesh.GetChannel(3).clipTo = 1;
                    mesh.GetChannel(5).clipTo = 1;
                    
                    mesh.GetChannel(2).clipTo = per;
                    mesh.GetChannel(4).clipTo = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipTo = per;
                        mesh.GetChannel(7).clipTo = per;
                    }
                }
                else
                {
                    mesh.GetChannel(3).clipFrom = 0;
                    mesh.GetChannel(5).clipFrom = 0;
                    
                    mesh.GetChannel(2).clipFrom = per;
                    mesh.GetChannel(4).clipFrom = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipFrom = per;
                        mesh.GetChannel(7).clipFrom = per;
                    }
                }
                break;
            case 2:
                if (isTo)
                {
                    mesh.GetChannel(2).clipTo = per;
                    mesh.GetChannel(4).clipTo = per;
                    mesh.GetChannel(3).clipTo = per;
                    mesh.GetChannel(5).clipTo = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipTo = per;
                        mesh.GetChannel(7).clipTo = per;
                    }
                }
                else
                {
                    mesh.GetChannel(2).clipFrom = per;
                    mesh.GetChannel(4).clipFrom = per;
                    mesh.GetChannel(3).clipFrom = per;
                    mesh.GetChannel(5).clipFrom = per;
                    if (spline.roadLane == ROADLANE.RL2)
                    {
                        mesh.GetChannel(6).clipFrom = per;
                        mesh.GetChannel(7).clipFrom = per;
                    }
                }
                break;
        }
    }
    
    public void LogTextOnPos(string text, Vector3 onPos)
    {
        GameObject obj;
        if (texts.FirstOrDefault(o => (o.transform.position == onPos) && (o.GetComponent<TextMesh>().text != text)) !=
            null)
        {
            obj = Instantiate(textObj, onPos - new Vector3(0, 0, 1), Quaternion.Euler(90, 0, 0));
        }
        else
        {
            obj = Instantiate(textObj, onPos, Quaternion.Euler(90, 0, 0));
        }

        obj.GetComponent<TextMesh>().text = text;
        obj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        texts.Add(obj);

        StartCoroutine(Stop());

        IEnumerator Stop()
        {
            yield return 0;
            texts.Remove(obj);
            Destroy(obj);
        }
    }

    float SnapGrid(float value, int snapsize)
    {
        if (value < 0)
        {
            return Mathf.Round(Mathf.Abs(value / snapsize)) * snapsize * -1;
        }
        else
        {
            return Mathf.Round(value / snapsize) * snapsize;
        }
    }

    Vector3 SnapToGridPoint(Vector3 pos, int _snapsize)
    {
        var snapsize = (float) _snapsize;

        if (!isVectorInXZArea(pos, -snapsize + lastPos.x, snapsize + lastPos.x,
            -snapsize + lastPos.z, snapsize + +lastPos.z))
        {
            UnityEngine.Debug.LogWarning("Out of range!");
        }
        else
        {
            if (isVectorInXZArea(pos, snapsize / 2 + lastPos.x, snapsize + lastPos.x,
                lastPos.z - snapsize / 4, lastPos.z + snapsize / 4))
            {
                lastPos = lastPos + new Vector3(snapsize, 0, 0);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x - snapsize, lastPos.x - snapsize / 2,
                lastPos.z - snapsize / 4, lastPos.z + snapsize / 4))
            {
                lastPos = lastPos - new Vector3(snapsize, 0, 0);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x - snapsize / 4, lastPos.x + snapsize / 4,
                lastPos.z + snapsize / 2, lastPos.z + snapsize))
            {
                lastPos = lastPos + new Vector3(0, 0, snapsize);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x - snapsize / 4, lastPos.x + snapsize / 4,
                lastPos.z - snapsize, lastPos.z - snapsize / 2))
            {
                lastPos = lastPos - new Vector3(0, 0, snapsize);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x + snapsize / 2, lastPos.x + snapsize,
                lastPos.z + snapsize / 2, lastPos.z + snapsize))
            {
                lastPos = lastPos + new Vector3(snapsize, 0, snapsize);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x - snapsize, lastPos.x - snapsize / 2,
                lastPos.z + snapsize / 2, lastPos.z + snapsize))
            {
                lastPos = lastPos + new Vector3(-snapsize, 0, snapsize);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x + snapsize / 2, lastPos.x + snapsize,
                lastPos.z - snapsize, lastPos.z - snapsize / 2))
            {
                lastPos = lastPos + new Vector3(snapsize, 0, -snapsize);
                return lastPos;
            }
            else if (isVectorInXZArea(pos, lastPos.x - snapsize, lastPos.x - snapsize / 2,
                lastPos.z - snapsize, lastPos.z - snapsize / 2))
            {
                lastPos = lastPos - new Vector3(snapsize, 0, snapsize);
                return lastPos;
            }
        }

        return lastPos;
    }

    bool CheckAppendVaild(Vector3 lastPoint, Vector3 currentPoint, Vector3 addPoint)
    {
        var dir = currentPoint - lastPoint;
        var dirAppend = addPoint - currentPoint;
        
        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                return (Vector3.Angle(dir, dirAppend) <= 90);
            case ROADLANE.RL2:
                return (Vector3.Angle(dir, dirAppend) <= 45);
            default:
                return true;
        }
    }

    bool isVectorInXZArea(Vector3 pos, float x_from, float x_to, float z_from, float z_to)
    {
        var cond_1 = x_from <= pos.x && pos.x <= x_to;
        var cond_2 = z_from <= pos.z && pos.z <= z_to;

        return cond_1 && cond_2;
    }

    // Spawn SplineComputer and Apply to spline_computer variable.
    void SpawnPath()
    {
        if (currentSpline)
        {
            currentSpline = null;
            newIndex = 0;
        }
        
        UnityEngine.Debug.LogWarning((int) currentRoadLane);
        SplinePrefab = roadPrefabs[(int) currentRoadLane];

        currentSpline = Instantiate(SplinePrefab, pos, Quaternion.identity);
        currentSpline.roadLane = currentRoadLane;

        meshReform(currentSpline);
    }

    // Spawn SplineComputer independently.
    SplineComputer InsPath(Vector3 pos)
    {
        SplinePrefab = roadPrefabs[(int) currentRoadLane];

        var spline = Instantiate(SplinePrefab, pos, Quaternion.identity);
        spline.roadLane = currentRoadLane;

        meshReform(spline);

        return spline;
    }
    
    // TODO - 2차로 90도 굽어지는거 완전 삭제

    SplineComputer InsPath(Vector3 pos, ROADLANE roadlane)
    {
        var prefab = roadPrefabs[(int) currentRoadLane];
        
        var spline = Instantiate(prefab, pos, Quaternion.identity);
        spline.roadLane = roadlane;
        meshReform(spline);

        return spline;
    }

    void meshReform(SplineComputer spline)
    {
        switch (currentRoadLane)
        {
            case ROADLANE.RL1:
                for (var i = 0; i < 6; i++)
                {
                    spline.GetComponent<SplineMesh>().meshReduce(i, 1);
                }

                break;
            case ROADLANE.RL2:
                spline.GetComponent<SplineMesh>().meshReduce(6, 3);
                spline.GetComponent<SplineMesh>().meshReduce(7, 3);
                break;
        }
    }

    // Append path when snapping event on. Return true when snapping event on.
    bool AppendPath(bool countRoad = true)
    {
        if (lastX != SnapToGridPoint(pos, snapsize).x || lastZ != SnapToGridPoint(pos, snapsize).z)
        {
            var x = SnapToGridPoint(pos, snapsize).x;
            var z = SnapToGridPoint(pos, snapsize).z;

            var last_index = currentSpline.GetPoints().Length - 1;

            var cond = CheckAppendVaild(
                currentSpline.GetPoint(last_index - 1).position,
                currentSpline.GetPoint(last_index).position,
                new Vector3(x, 0, z));

            if (cond || currentSpline.GetPoints().Length == 1)
            {
                if (itemManager.remainRoadList[currentRoadLane] > 0)
                {
                    currentSpline.SetPointNormal(newIndex, def_normal);
                    currentSpline.SetPointSize(newIndex, 1);
                    currentSpline.SetPointPosition(newIndex, new Vector3(x, height * 2, z));

                    lastX = x;
                    lastZ = z;

                    if (countRoad)
                    {
                        --itemManager.remainRoadList[currentRoadLane];
                    }

                    // Spawn Pillar
                    var points = currentSpline.GetPoints();
                    if (newIndex != 0)
                    {
                        var spawnPos = (points[newIndex - 1].position + points[newIndex].position) / 2;

                        var dir = points[newIndex].position - points[newIndex - 1].position;
                        spawnPos -= new Vector3(0, 4.5f, 0);
                        
                        Instantiate(Pillar, spawnPos, Quaternion.LookRotation(dir));
                    }

                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No Remaining Roads!");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Point is out of range!");
            }
        }

        return false;
    }

    // Return true when snapping event on.
    // Same feature with AppendPath()
    public bool CheckSnap()
    {
        if (lastX != SnapToGridPoint(pos, snapsize).x || lastZ != SnapToGridPoint(pos, snapsize).z)
        {
            lastX = SnapToGridPoint(pos, snapsize).x;
            lastZ = SnapToGridPoint(pos, snapsize).z;

            return true;
        }

        return false;
    }

    // WARNING - To make this function work, I changed below thing.
    // CHANGED - Changed `spline` variable in SplineComputer to public (from private)
    // Body of this function referred DeletePointModule.cs
    private void RemovePoint(SplineComputer spline, int index)
    {
        var points = spline.spline.points;

        if (index < points.Length && index >= 0)
        {
            ArrayUtility.RemoveAt(ref points, index);
            spline.spline.points = points;
        }
        else
        {
            UnityEngine.Debug.LogError("Out of Index! (RemovePoint)");
        }

        if (spline)
        {
            spline.Rebuild();
        }
    }

    // Remove point with point ref.
    private void RemovePoint(SplineComputer spline, SplinePoint point, bool countRoad = true)
    {
        var points = spline.spline.points;

        ArrayUtility.RemoveAt(ref points, ArrayUtility.IndexOf(points, point));
        spline.spline.points = points;

        if (countRoad)
        {
            ++itemManager.remainRoadList[currentRoadLane];
        }
        
        if (spline)
        {
            spline.Rebuild(true);
        }
    }

    // Simply Ray-trace and Set mouse position.
    void RayTrace()
    {
        var ray = cm.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitData;

        if (Physics.Raycast(ray, out hitData, 10000))
        {
            pos = hitData.point;
        }
    }

    // Spawn SplineComputer and Change mode to Append.
    void runBuildMode()
    {
        UnityEngine.Debug.LogWarning("RunBuild!");

        newIndex = 0;

        if (itemManager.remainRoadList[currentRoadLane] > 0)
        {
            SpawnPath();
            
            if (currentSpline)
            {
                currentSpline.Rebuild(true);
            }
            
            AppendPath(false);
            newIndex++;

            currentMode = MODE.APPEND;
        }
    }

    // Append Point when snapping event on. Also Handle Cleaning Joined Path.
    void runAppendModeGrid()
    {
        if (Input.GetMouseButton(0))
        {
            // -------------------------------------------------------------------
            // HEAD
            if (joinMode == JOINMODE.HEAD)
            {
                if (CheckSnap())
                {
                    // APPEND CODE (HEAD)
                    var cond = CheckAppendVaild(
                        selectedSpline.GetPoint(1).position,
                        selectedSpline.GetPoint(0).position,
                        snapPos);

                    if (cond || currentSpline.GetPoints().Length == 1)
                    {
                        var points = selectedSpline.GetPoints();

                        for (var i = 0; i < points.Length; i++)
                        {
                            selectedSpline.SetPoint(i + 1, points[i]);
                        }

                        selectedSpline.SetPointNormal(0, def_normal);
                        selectedSpline.SetPointSize(newIndex, 1);
                        selectedSpline.SetPointPosition(0, snapPos);
                        
                        // Spawn Pillar (Head)
                        var selectedSplinePoints = selectedSpline.GetPoints();
                        if (selectedSplinePoints.Length > 1)
                        {
                            var spawnPos = (selectedSplinePoints[0].position + selectedSplinePoints[1].position) / 2;

                            var dir = selectedSplinePoints[1].position - selectedSplinePoints[0].position;
                            spawnPos -= new Vector3(0, 4.5f, 0);
                            
                            Instantiate(Pillar, spawnPos, Quaternion.LookRotation(dir));
                        }

                        // CHECK JOIN DURING APPEND (HEAD)
                        SplineComputer check_spline = null;
                        foreach (var spline in GetSplineComputers(snapPos))
                        {
                            if (spline != selectedSpline)
                            {
                                check_spline = spline;
                            }
                        }

                        if (check_spline != null)
                        {
                            selectedIndex = getSplinePointIndex(check_spline, getSplinePoint(snapPos, check_spline));
                        }

                        var isPosCrossroad = false;
                        foreach (var cros in GetRefCrossroads(check_spline))
                        {
                            if (cros.getPosition() == snapPos)
                                isPosCrossroad = true;
                        }

                        if (check_spline != null && check_spline != selectedSpline)
                        {
                            if ((check_spline.GetPoints().First().position == snapPos ||
                                 check_spline.GetPoints().Last().position == snapPos) && !isPosCrossroad)
                            {
                                UnityEngine.Debug.LogWarning("Join 2-crossroad (HEAD)");

                                var haveSameCrossroad = GetRefCrossroads(selectedSpline)
                                    .Any(cros => GetRefCrossroads(check_spline).Contains(cros));

                                if (haveSameCrossroad)
                                {
                                    // CROSSROAD LOOP (HEAD APPEND)
                                    UnityEngine.Debug.LogWarning("LOOP");
                                    var spline = MergeSplines(check_spline, selectedSpline);
                                    spline.isLoop = true;
                                }
                                else
                                {
                                    MergeSplines(check_spline, selectedSpline);
                                }
                            }
                            else if (!isPosCrossroad)
                            {
                                if (check_spline.isClosed)
                                {
                                    var lane = check_spline.roadLane;
                                    check_spline.Break();

                                    var pointList = new List<SplinePoint>();
                                    var points_check = new List<SplinePoint>(check_spline.GetPoints());

                                    for (int i = selectedIndex; i >= 0; i--)
                                    {
                                        pointList.Add(points_check[i]);
                                    }

                                    for (int i = points_check.Count - 2; i >= selectedIndex; i--)
                                    {
                                        pointList.Add(points_check[i]);
                                    }

                                    Destroy(check_spline.gameObject);

                                    var spline = InsPath(pointList[0].position, lane);
                                    spline.SetPoints(pointList.ToArray());
                                    spline.isLoop = true;

                                    var crossroad = new Crossroad();
                                    crossroad.AddRoad(spline);
                                    crossroad.AddRoad(selectedSpline);
                                    crossroad.SetPosition(selectedSpline.GetPoint(0).position);

                                    crossroads.Add(crossroad);
                                }
                                else
                                {
                                    UnityEngine.Debug.LogWarning("Join 3-crossroad (HEAD)");

                                    var index = getSplinePointIndex(check_spline,
                                        getSplinePoint(snapPos, check_spline));

                                    var new_spline = SplitSpline(index, check_spline);

                                    var crossroad = new Crossroad();
                                    crossroad.AddRoad(check_spline);
                                    crossroad.AddRoad(new_spline);
                                    crossroad.AddRoad(selectedSpline);
                                    crossroad.SetPosition(selectedSpline.GetPoint(0).position);

                                    crossroads.Add(crossroad);
                                }
                            }
                            else if (isPosCrossroad)
                            {
                                UnityEngine.Debug.LogWarning("Join 4-crossroad (APPEND)");

                                foreach (var cros in GetRefCrossroads(check_spline))
                                {
                                    cros.AddRoad(selectedSpline);
                                }
                            }
                        }

                        // Check Appending Spline is Closed.
                        if (check_spline == null)
                        {
                            if (selectedSpline.GetPoints().First().position ==
                                selectedSpline.GetPoints().Last().position)
                            {
                                UnityEngine.Debug.LogWarning("LOOP");
                                selectedSpline.Close();
                            }
                            else
                            {
                                var splinePoints = selectedSpline.GetPoints();

                                var overlappingPoint = new SplinePoint();
                                var overlappingPointIndex = -1;

                                for (var i = 0; i < splinePoints.Length; i++)
                                {
                                    if (splinePoints[i].position == splinePoints[0].position && i != 0)
                                    {
                                        overlappingPoint = splinePoints[i];
                                        overlappingPointIndex = i;
                                        break;
                                    }
                                }

                                if (overlappingPointIndex != -1)
                                {
                                    var restSpline = SplitSpline(overlappingPointIndex, selectedSpline);
                                    selectedSpline.isLoop = true;
                                    
                                    crossroads.Add(new Crossroad(
                                        new List<SplineComputer>() { selectedSpline, restSpline },
                                        overlappingPoint.position));
                                }
                            }
                        }
                    }
                }

                // -------------------------------------------------------------------
            }
            else
            {
                // -------------------------------------------------------------------
                // TAIL
                // APPEND CODE (TAIL)
                if (AppendPath())
                {
                    // CHECK JOIN DURING APPEND (TAIL)
                    SplineComputer check_spline = null;

                    foreach (var spline in GetSplineComputers(snapPos))
                    {
                        if (spline != currentSpline)
                        {
                            check_spline = spline;
                        }
                    }

                    if (check_spline != null)
                    {
                        selectedIndex = getSplinePointIndex(check_spline, getSplinePoint(snapPos, check_spline));
                    }

                    var isPosCrossroad = false;
                    foreach (var cros in GetRefCrossroads(check_spline))
                    {
                        if (cros.getPosition() == snapPos)
                            isPosCrossroad = true;
                    }

                    if (check_spline != null && check_spline != currentSpline)
                    {
                        if ((check_spline.GetPoints().First().position == snapPos ||
                             check_spline.GetPoints().Last().position == snapPos) && !isPosCrossroad)
                        {
                            UnityEngine.Debug.LogWarning("Join 2-roads (APPEND)");

                            var haveSameCrossroad = GetRefCrossroads(currentSpline)
                                .Any(cros => GetRefCrossroads(check_spline).Contains(cros));

                            if (haveSameCrossroad)
                            {
                                // CROSSROAD LOOP (TAIL APPEND)
                                var spline = MergeSplines(check_spline, currentSpline);
                                spline.isLoop = true;
                            }
                            else
                            {
                                var spline = MergeSplines(check_spline, currentSpline);
                            }
                        }
                        else if (!isPosCrossroad)
                        {
                            if (check_spline.isClosed)
                            {
                                var lane = check_spline.roadLane;
                                check_spline.Break();

                                var pointList = new List<SplinePoint>();
                                var points = new List<SplinePoint>(check_spline.GetPoints());

                                for (int i = selectedIndex; i >= 0; i--)
                                {
                                    pointList.Add(points[i]);
                                }

                                for (int i = points.Count - 2; i >= selectedIndex; i--)
                                {
                                    pointList.Add(points[i]);
                                }

                                Destroy(check_spline.gameObject);

                                var spline = InsPath(pointList[0].position, lane);
                                spline.SetPoints(pointList.ToArray());
                                spline.isLoop = true;

                                var crossroad = new Crossroad();
                                crossroad.AddRoad(spline);
                                crossroad.AddRoad(currentSpline);
                                crossroad.SetPosition(currentSpline.GetPoints().Last().position);

                                crossroads.Add(crossroad);
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Join 3-crossroad (APPEND)");

                                var points = check_spline.GetPoints();
                                var index = 0;

                                for (var i = 0; i < points.Length; i++)
                                {
                                    if (points[i].position == snapPos)
                                    {
                                        index = i;
                                    }
                                }

                                var new_spline = SplitSpline(index, check_spline);

                                var crossroad = new Crossroad();
                                crossroad.AddRoad(new_spline);
                                crossroad.AddRoad(check_spline);
                                crossroad.AddRoad(currentSpline);
                                crossroad.SetPosition(currentSpline.GetPoints().Last().position);

                                crossroads.Add(crossroad);
                            }
                        }
                        else if (isPosCrossroad)
                        {
                            UnityEngine.Debug.LogWarning("Join 4-crossroad (APPEND)");

                            foreach (var cros in GetRefCrossroads(check_spline))
                            {
                                cros.AddRoad(currentSpline);
                            }
                        }
                    }

                    // Check Appending Spline is Closed.
                    if (check_spline == null)
                    {
                        if (currentSpline.GetPoints().First().position ==
                            currentSpline.GetPoints().Last().position)
                        {
                            UnityEngine.Debug.LogWarning("LOOP");
                            currentSpline.Close();
                        }
                        else
                        {
                            var points = currentSpline.GetPoints();
                            var lastIndex = points.Length - 1;
                            
                            var overlappingPoint = new SplinePoint();
                            var overlappingPointIndex = -1;

                            for (var i = 0; i < points.Length; i++)
                            {
                                if (points[i].position == points[lastIndex].position && i != lastIndex)
                                {
                                    overlappingPoint = points[i];
                                    overlappingPointIndex = i;
                                    break;
                                }
                            }

                            if (overlappingPointIndex != -1)
                            {
                                var loopSpline = SplitSpline(overlappingPointIndex, currentSpline);
                                loopSpline.isLoop = true;
                                
                                crossroads.Add(new Crossroad(
                                    new List<SplineComputer>() { loopSpline, currentSpline }, 
                                    overlappingPoint.position));
                            }
                        }
                    }

                    // CHECK JOIN DURING BUILD
                    if (joinMode != JOINMODE.NONE)
                    {
                        if (joinMode == JOINMODE.TO3)
                        {
                            if (selectedSpline.isClosed)
                            {
                                var lane = selectedSpline.roadLane;
                                selectedSpline.Break();

                                var pointList = new List<SplinePoint>();
                                var points = new List<SplinePoint>(selectedSpline.GetPoints());

                                for (int i = selectedIndex; i >= 0; i--)
                                {
                                    pointList.Add(points[i]);
                                }

                                for (int i = points.Count - 2; i >= selectedIndex; i--)
                                {
                                    pointList.Add(points[i]);
                                }

                                Destroy(selectedSpline.gameObject);

                                var spline = InsPath(pointList[0].position, lane);
                                spline.SetPoints(pointList.ToArray());
                                spline.isLoop = true;

                                var crossroad = new Crossroad();
                                crossroad.AddRoad(spline);
                                crossroad.AddRoad(currentSpline);
                                crossroad.SetPosition(currentSpline.GetPoint(0).position);

                                crossroads.Add(crossroad);
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Join 3-crossroad (BUILD)");

                                crossNewSpline = SplitSpline(selectedIndex, selectedSpline);
                                crossOldSpline = selectedSpline;

                                var crossroad = new Crossroad();
                                crossroad.AddRoad(crossNewSpline);
                                crossroad.AddRoad(crossOldSpline);
                                crossroad.AddRoad(currentSpline);

                                crossroad.SetPosition(currentSpline.GetPoint(0).position); // CAUSE ERROR FREQUENTLY

                                crossroads.Add(crossroad);

                                joinMode = JOINMODE.NONE;
                                selectedSpline = null;
                                newIndex++;
                            }
                        }
                        else if (joinMode == JOINMODE.TO4)
                        {
                            UnityEngine.Debug.LogWarning("Join 4-crossroad (BUILD)");

                            selectedCrossroad.AddRoad(currentSpline);

                            newIndex++;
                            joinMode = JOINMODE.NONE;
                        }
                        else if (joinMode == JOINMODE.TO3_SPLIT)
                        {
                            var crossroad = new Crossroad();
                            crossroad.SetRoads(selectedSplines);
                            crossroad.AddRoad(currentSpline);
                            crossroad.SetPosition(currentSpline.GetPoint(0).position);

                            crossroads.Add(crossroad);

                            joinMode = JOINMODE.NONE;
                            selectedSplines = null;
                            newIndex++;
                        }
                        else if (joinMode == JOINMODE.TO3_NOSPLIT)
                        {
                            selectedCrossroad.AddRoad(currentSpline);

                            newIndex++;
                            joinMode = JOINMODE.NONE;
                        }
                    }
                    else
                    {
                        newIndex++;
                    }
                }
            }
            // -------------------------------------------------------------------

            if (currentSpline)
            {
                currentSpline.Rebuild(true);
            }

            if (crossNewSpline)
            {
                crossNewSpline.Rebuild(true);
            }

            if (crossOldSpline)
            {
                crossOldSpline.Rebuild(true);
            }

            if (selectedSpline)
            {
                selectedSpline.Rebuild(true);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (currentSpline)
            {
                if (currentSpline.GetPoints().Length <= 1)
                {
                    Destroy(currentSpline.gameObject);
                }
            }

            currentSpline = null;
            crossOldSpline = null;
            crossNewSpline = null;

            selectedSpline = null;
            selectedIndex = 0;
            selectedCrossroad = null;

            newIndex = 0;
            lastX = 0;
            lastZ = 0;

            joinMode = JOINMODE.NONE;
            currentMode = MODE.BUILD;
        }
    }
    
    List<SplineComputer> GetSplineComputers(Vector3 pos, bool heightFunc = true)
    {
        var spline_list = GameObject.FindObjectsOfType<SplineComputer>();
        var return_list = new List<SplineComputer>();

        foreach (var spline in spline_list)
        {
            var points = spline.GetPoints();

            for (var i = 0; i < points.Length; i++)
            {
                if (heightFunc)
                {
                    if (pos == points[i].position)
                    {
                        return_list.Add(spline);
                        break;
                    }
                }
                else
                {
                    if (pos.x == points[i].position.x && pos.z == points[i].position.z)
                    {
                        return_list.Add(spline);
                        break;
                    }
                }
            }
        }

        return return_list;
    }

    SplinePoint getSplinePoint(Vector3 pos, SplineComputer spline)
    {
        foreach (var point in spline.GetPoints())
        {
            if (point.position == pos)
            {
                return point;
            }
        }

        return new SplinePoint();
    }

    List<SplinePoint> GetSplinePoints(Vector3 pos, bool heightFunc = true)
    {
        var points = new List<SplinePoint>();
        
        foreach (var splineComputer in GetSplineComputers(pos, heightFunc))
        {
            if (splineComputer.GetPoints().Any(po => po.position.x == pos.x && po.position.z == pos.z))
            {
                points.Add(splineComputer.GetPoints().FirstOrDefault(po => po.position.x == pos.x && po.position.z == pos.z));
            }
        }

        return points;
    }

    int getSplinePointIndex(SplineComputer spline, SplinePoint point)
    {
        var points = spline.GetPoints();

        for (var i = 0; i < points.Length; i++)
        {
            if (points[i].position == point.position)
            {
                return i;
            }
        }

        return -1;
    }

    // Split Spline and return newly spawned SplineComputer.
    SplineComputer SplitSpline(int index, SplineComputer spline)
    {
        var oldPoints = new List<SplinePoint>();
        var newPoints = new List<SplinePoint>();
        var originPoints = spline.GetPoints();

        if (!spline.isClosed)
        {
            for (var i = 0; i <= index; i++)
            {
                oldPoints.Add(originPoints[i]);
            }

            for (var i = index; i < originPoints.Length; i++)
            {
                newPoints.Add(originPoints[i]);
            }
        }

        spline.SetPoints(oldPoints.ToArray());

        var newSpline = InsPath(newPoints[0].position, spline.roadLane);
        newSpline.SetPoints(newPoints.ToArray());

        // RE-ADD REF CROSSROAD
        var refCrossListNew = (from point in newSpline.GetPoints()
            from crossroad
                in crossroads
            where crossroad.getPosition() ==
                  point.position
            select crossroad).ToList();

        foreach (var refCross in refCrossListNew)
        {
            if (!spline.isLoop)
            {
                refCross.RemoveRoad(spline);
                refCross.AddRoad(newSpline);
            }
            else
            {
                refCross.AddRoad(newSpline);
            }
        }

        spline.isLoop = false;
        newSpline.isLoop = false;

        spline.Rebuild(true);
        newSpline.Rebuild(true);

        return newSpline;
    }

    // Check two vector create Clockwise or Counterclockwise.
    bool isVectorGoClockwise(Vector3 from, Vector3 to)
    {
        if (Vector3.SignedAngle(from, to, new Vector3(0, 1, 0)) <= 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Check two vector is Parallel.
    bool isVectorParallel(Vector3 v1, Vector3 v2)
    {
        if (Vector3.Angle(v1, v2) == 0 || Vector3.Angle(v1, v2) == 180)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool isVectorVertical(Vector3 v1, Vector3 v2)
    {
        if (Vector3.Dot(v1, v2) == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    SplineComputer MergeSplines(SplineComputer s1, SplineComputer s2)
    {
        var mergemode = MERGEMODE.NONE;

        if (s1.GetPoints().Last().position == s2.GetPoints().First().position &&
            s1.GetPoints().First().position == s2.GetPoints().Last().position) mergemode = MERGEMODE.LOOP;
        else if (s1.GetPoints().Last().position == s2.GetPoints().Last().position) mergemode = MERGEMODE.LL;
        else if (s1.GetPoints().First().position == s2.GetPoints().First().position) mergemode = MERGEMODE.FF;
        else if (s1.GetPoints().Last().position == s2.GetPoints().First().position) mergemode = MERGEMODE.LF;
        else if (s1.GetPoints().First().position == s2.GetPoints().Last().position) mergemode = MERGEMODE.FL;

        if (mergemode == MERGEMODE.LL)
        {
            UnityEngine.Debug.LogWarning("LL");
            var index = s1.GetPoints().Length;

            var points = s2.GetPoints();
            for (var i = points.Length - 2; i >= 0; i--)
            {
                s1.SetPoint(index, points[i]);
                index++;
            }

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);
            return s1;
        }
        else if (mergemode == MERGEMODE.FF)
        {
            UnityEngine.Debug.LogWarning("FF");
            var points = s1.GetPoints();
            var points2 = s2.GetPoints();

            var index = 0;

            for (var i = 0; i < points.Length; i++)
            {
                s1.SetPoint(i + points2.Length - 1, points[i]);
            }

            for (var i = points2.Length - 1; i >= 1; i--)
            {
                s1.SetPoint(index, points2[i]);
                index++;
            }

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);
            return s1;
        }
        else if (mergemode == MERGEMODE.LF)
        {
            UnityEngine.Debug.LogWarning("LF");
            var index = s1.GetPoints().Length;

            var points = s2.GetPoints();
            for (var i = 1; i < points.Length; i++)
            {
                s1.SetPoint(index, points[i]);
                index++;
            }

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s2));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s2);
                refCrossroad.AddRoad(s1);
            }

            Destroy(s2.gameObject);
            return s1;
        }
        else if (mergemode == MERGEMODE.FL)
        {
            UnityEngine.Debug.LogWarning("FL");
            var index = s2.GetPoints().Length;

            var points = s1.GetPoints();

            for (var i = 1; i <= points.Length - 1; i++)
            {
                s2.SetPoint(index, points[i]);
                index++;
            }

            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s1));
            if (refCrossroad != null)
            {
                refCrossroad.RemoveRoad(s1);
                refCrossroad.AddRoad(s2);
            }

            Destroy(s1.gameObject);
            return s2;
        }
        else if (mergemode == MERGEMODE.LOOP)
        {
            var refCrossroad = crossroads.FirstOrDefault(cros => cros.getRoads().Contains(s1));

            if (s1.GetPoints().Last().position == refCrossroad.getPosition())
            {
                // FL
                UnityEngine.Debug.LogWarning("LOOP - FL");
                var index = s2.GetPoints().Length;

                var points = s1.GetPoints();

                for (var i = 1; i <= points.Length - 1; i++)
                {
                    s2.SetPoint(index, points[i]);
                    index++;
                }

                if (refCrossroad != null)
                {
                    refCrossroad.RemoveRoad(s1);
                    refCrossroad.AddRoad(s2);
                }

                Destroy(s1.gameObject);
                return s2;
            }
            else if (s1.GetPoint(0).position == refCrossroad.getPosition())
            {
                // LF
                UnityEngine.Debug.LogWarning("LOOP - LF");
                var index = s1.GetPoints().Length;

                var points = s2.GetPoints();
                for (var i = 1; i < points.Length; i++)
                {
                    s1.SetPoint(index, points[i]);
                    index++;
                }

                if (refCrossroad != null)
                {
                    refCrossroad.RemoveRoad(s2);
                    refCrossroad.AddRoad(s1);
                }

                Destroy(s2.gameObject);
                return s1;
            }
        }
        else if (mergemode == MERGEMODE.NONE)
        {
            UnityEngine.Debug.LogWarning("NONE");
            return null;
        }

        return null;
    }

    Vector3 GetSplinePosition(SplineComputer spline)
    {
        return spline.GetPoint(spline.GetPoints().Length / 2).position;
        // return (spline.GetPoints().First().position + spline.GetPoints().Last().position) / 2;
    }

    List<Crossroad> GetRefCrossroads(SplineComputer spline)
    {
        return crossroads.Where(cros => cros.getRoads().Contains(spline)).ToList();
    }

    Crossroad GetCrossroad(Vector3 pos)
    {
        return crossroads.Find(cros => cros.getPosition() == pos);
    }

    SplineComputer GetOwnSpline(SplinePoint point)
    {
        var splineList = GameObject.FindObjectsOfType<SplineComputer>();
        return splineList.FirstOrDefault(spline => spline.GetPoints().Contains(point));
    }

    public void ResetMeshClip(SplineComputer spline)
    {
        for (int i = 0; i < 5; i++)
        {
            spline.GetComponent<SplineMesh>().GetChannel(i).clipFrom = 0.0f;
            spline.GetComponent<SplineMesh>().GetChannel(i).clipTo = 1.0f;
        }
    }

    bool CheckRoadConnectedValid(Crossroad crossroad, SplineComputer road, bool execute = false)
    {
        if (crossroad.getPosition() == road.GetPoints().Last().position ||
            crossroad.getPosition() == road.GetPoints().First().position)
        {
            return true;
        }
        else
        {
            if (execute)
            {
                crossroad.RemoveRoad(road);
            }
            return false;
        }
    }

    void Start()
    {
        cm = GetComponentInChildren<Camera>();
        itemManager = GetComponent<ItemManager>();
    }

    void Update()
    {
        RayTrace();

        snapPos = new Vector3(SnapGrid(pos.x, snapsize), height * 2, SnapGrid(pos.z, snapsize));
        lastPos = snapPos;
        debugobj.transform.position = snapPos;
        
        debugobj2.transform.position = new Vector3(snapPos.x, 0, snapPos.z);

        // Crossroad Clean Line Code
        for (var index = 0; index < crossroads.Count; index++)
        {
            var cros = crossroads[index];

            LogTextOnPos(index + "C = " + cros.getRoads().Count, cros.getPosition()); // DEBUG

            var roads = new List<SplineComputer>(cros.getRoads()); // COPY LIST
            var dirList = new List<Vector3>();
            var loopedRoad = roads.FirstOrDefault(road => road.isLoop);

            if (loopedRoad != null)
            {
                roads.Add(loopedRoad);
                roads = (from road in roads orderby road.isLoop ascending select road).ToList();

                var loopRoadStartIndex = 0;
                for (var i = 0; i < roads.Count; i++)
                {
                    if (!roads[i].isLoop) continue;
                    loopRoadStartIndex = i;
                    break;
                }

                // Make dirList
                for (var i = 0; i < roads.Count; i++)
                {
                    if (roads[i].isLoop) continue;
                    LogTextOnPos(index + "C - SP - " + i, GetSplinePosition(roads[i]));

                    if (roads[i].GetPoints().Last().position == cros.getPosition())
                    {
                        var last_index = roads[i].GetPoints().Length - 1;

                        var dir = roads[i].GetPoint(last_index - 1).position - cros.getPosition();
                        dirList.Add(dir);
                    }
                    else if (roads[i].GetPoints().First().position == cros.getPosition())
                    {
                        var dir = roads[i].GetPoint(1).position - cros.getPosition();
                        dirList.Add(dir);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("ERROR!");
                    }
                }

                LogTextOnPos(index + "C - LSP ", GetSplinePosition(loopedRoad));

                var joinIndex = 0;
                for (var i = 0; i < loopedRoad.GetPoints().Length; i++)
                {
                    if (loopedRoad.GetPoint(i).position != cros.getPosition()) continue;
                    joinIndex = i;
                    break;
                }

                var loopForwardDir = loopedRoad.GetPoint(joinIndex + 1).position -
                                     loopedRoad.GetPoint(joinIndex).position;

                var backIndex = joinIndex - 1;
                if (backIndex < 0)
                    backIndex = loopedRoad.GetPoints().Length - 1 + backIndex;

                var loopBackwardDir = loopedRoad.GetPoint(backIndex).position -
                                      loopedRoad.GetPoint(joinIndex).position;

                dirList.Add(loopForwardDir);
                dirList.Add(loopBackwardDir);

                // Apply To SplineMesh
                for (var i = 0; i < roads.Count; i++)
                {
                    var isRight = false;
                    var isLeft = false;

                    // Straigt Road
                    if (!roads[i].isLoop)
                    {
                        // Last Point
                        if (roads[i].GetPoints().Last().position == cros.getPosition())
                        {
                            foreach (var dir in dirList)
                            {
                                if (isVectorVertical(dirList[i], dir))
                                {
                                    if (isVectorGoClockwise(dirList[i], dir))
                                    {
                                        isLeft = true;
                                    }
                                    else
                                    {
                                        isRight = true;
                                    }
                                }
                            }
                            
                            var per = roads[i].Project(cros.getPosition() + dirList[i] / dividerList[(int)roads[i].roadLane]).percent;

                            roads[i].GetComponent<SplineMesh>().GetChannel(1).clipTo = per;

                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, true, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, true, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, true, per);
                            }
                        }
                        // First Point
                        else if (roads[i].GetPoint(0).position == cros.getPosition())
                        {
                            foreach (var dir in dirList)
                            {
                                if (isVectorVertical(dirList[i], dir))
                                {
                                    if (isVectorGoClockwise(dirList[i], dir))
                                    {
                                        isRight = true;
                                    }
                                    else
                                    {
                                        isLeft = true;
                                    }
                                }
                            }

                            var per = roads[i].Project(cros.getPosition() + dirList[i] / dividerList[(int)roads[i].roadLane]).percent;

                            roads[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, false, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, false, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, false, per);
                            }
                        }
                    }
                    // Looped Roads
                    else
                    {
                        foreach (var dir in dirList)
                        {
                            if (!isVectorParallel(dirList[i], dir))
                            {
                                if (isVectorGoClockwise(dirList[i], dir))
                                {
                                    if (i == loopRoadStartIndex)
                                        isRight = true;
                                    else if (i == loopRoadStartIndex + 1)
                                        isLeft = true;
                                }
                                else
                                {
                                    if (i == loopRoadStartIndex)
                                        isLeft = true;
                                    else if (i == loopRoadStartIndex + 1)
                                        isRight = true;
                                }
                            }
                        }

                        var per = roads[i].Project(cros.getPosition() + dirList[i] / dividerList[(int)roads[i].roadLane]).percent;

                        if (i == loopRoadStartIndex)
                        {
                            roads[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, false, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, false, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, false, per);
                            }
                        }
                        else if (i == loopRoadStartIndex + 1)
                        {
                            roads[i].GetComponent<SplineMesh>().GetChannel(1).clipTo = per;

                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, true, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, true, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, true, per);
                            }
                        }
                    }
                }
            }
            else
            {
                if (cros.getRoads().Count == 2)
                {
                    var spline = MergeSplines(cros.getRoads()[0], cros.getRoads()[1]);

                    crossroads.Remove(cros);
                }
                else
                {
                    for (var i = 0; i < roads.Count; i++)
                    {
                        LogTextOnPos(index + "C - SP - " + i, GetSplinePosition(roads[i]));

                        if (roads[i].GetPoints().Last().position == cros.getPosition())
                        {
                            var last_index = roads[i].GetPoints().Length - 1;

                            var dir = roads[i].GetPoint(last_index - 1).position - cros.getPosition();
                            dirList.Add(dir);
                        }
                        else if (roads[i].GetPoints().First().position == cros.getPosition())
                        {
                            var dir = roads[i].GetPoint(1).position - cros.getPosition();
                            dirList.Add(dir);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("ERROR!");
                        }
                    }

                    for (var i = 0; i < roads.Count; i++)
                    {
                        var isRight = false;
                        var isLeft = false;

                        if (roads[i].GetPoints().Last().position == cros.getPosition())
                        {
                            foreach (var dir in dirList)
                            {
                                if (isVectorVertical(dirList[i], dir))
                                {
                                    if (isVectorGoClockwise(dirList[i], dir))
                                    {
                                        isLeft = true;
                                    }
                                    else
                                    {
                                        isRight = true;
                                    }
                                }
                            }
                            
                            var per = roads[i].Project(cros.getPosition() + dirList[i] / dividerList[(int)roads[i].roadLane]).percent;

                            var mesh = roads[i].GetComponent<SplineMesh>();

                            mesh.GetChannel(1).clipTo = per;
                            
                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, true, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, true, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, true, per);
                            }
                        }
                        else if (roads[i].GetPoints().First().position == cros.getPosition())
                        {
                            foreach (var dir in dirList)
                            {
                                if (isVectorVertical(dirList[i], dir))
                                {
                                    if (isVectorGoClockwise(dirList[i], dir))
                                    {
                                        isRight = true;
                                    }
                                    else
                                    {
                                        isLeft = true;
                                    }
                                }
                            }

                            var per = roads[i].Project(cros.getPosition() + dirList[i] / dividerList[(int)roads[i].roadLane]).percent;
                            
                            var mesh = roads[i].GetComponent<SplineMesh>();
                            
                            roads[i].GetComponent<SplineMesh>().GetChannel(1).clipFrom = per;

                            if (isLeft && !isRight)
                            {
                                SetMeshClip(roads[i], 0, false, per);
                            }
                            else if (isRight && !isLeft)
                            {
                                SetMeshClip(roads[i], 1, false, per);
                            }
                            else if (isLeft && isRight)
                            {
                                SetMeshClip(roads[i], 2, false, per);
                            }
                        }
                    }
                }
            }
        }

        // Change MODE
        if (Input.GetKeyDown(KeyCode.B))
        {
            UnityEngine.Debug.LogWarning("Build Mode Enabled!");
            currentMode = MODE.BUILD;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.Debug.LogWarning("Remove Mode Enabled!");
            currentMode = MODE.REMOVE;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentRoadLane = ROADLANE.RL1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentRoadLane = ROADLANE.RL2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            currentRoadLane = ROADLANE.RL05;
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            if (height + 1 <= 5)
            {
                height++;
            }
            else
            {
                height = 5;
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            if (height - 1 >= 0)
            {
                height--;
            }
            else
            {
                height = 0;
            }
        }

        if (currentMode == MODE.BUILD)
        {
            // BUILD MODE
            if (Input.GetMouseButtonDown(0))
            {
                var crossroad = crossroads.FirstOrDefault(cros => snapPos == cros.getPosition());

                if (crossroad != null)
                {
                    if (crossroad.getRoads().Count == 3)
                    {
                        joinMode = JOINMODE.TO4;

                        selectedCrossroad = crossroad;

                        runBuildMode();
                    }
                    else if (crossroad.getRoads().Count == 2 && crossroad.getRoads().Any(road => road.isLoop))
                    {
                        joinMode = JOINMODE.TO3_NOSPLIT;
                        
                        selectedCrossroad = crossroad;
                        
                        runBuildMode();
                    }
                }
                else
                {
                    var splines = GetSplineComputers(snapPos);
                    SplineComputer spline = null;

                    if (splines.Count == 1)
                    {
                        spline = splines[0];

                        var point = getSplinePoint(snapPos, spline);
                        var point_index = getSplinePointIndex(spline, point);

                        if (point_index != -1)
                        {
                            if (point_index == spline.GetPoints().Count() - 1)
                            {
                                UnityEngine.Debug.LogWarning("Tail Append");

                                newIndex = point_index;
                                currentSpline = spline;

                                currentMode = MODE.APPEND;
                            }
                            else if (point_index == 0)
                            {
                                UnityEngine.Debug.LogWarning("Head Append");

                                selectedSpline = spline;

                                currentMode = MODE.APPEND;
                                joinMode = JOINMODE.HEAD;

                                lastX = snapPos.x;
                                lastZ = snapPos.z;
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("Split for Join");

                                selectedSpline = spline;
                                selectedIndex = point_index;

                                joinMode = JOINMODE.TO3;

                                runBuildMode();
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("ERROR - Can't find Point in Spline");
                        }
                    }
                    else if (splines.Count == 0)
                    {
                        runBuildMode();
                    }
                    else if (splines.Count == 2)
                    {
                        // Not Crossroad, But Splines are splitted.
                        UnityEngine.Debug.LogWarning("SPLIT TO3");

                        selectedSplines = splines;

                        currentMode = MODE.APPEND;
                        joinMode = JOINMODE.TO3_SPLIT;

                        runBuildMode();
                    }
                }
            }
        }
        else if (currentMode == MODE.APPEND)
        {
            // APPEND MODE
            runAppendModeGrid();
        }
        else if (currentMode == MODE.REMOVE)
        {
            // REMOVE MODE
            if (Input.GetMouseButton(0))
            {
                if (CheckSnap())
                {
                    var spline = new SplineComputer();
                    if (GetSplineComputers(snapPos).Count != 1)
                    {
                        if (removePointIndex == -1)
                        {
                            crossroadRef = GetCrossroad(snapPos);
                        }
                        else
                        {
                            foreach (var splineComputer in GetSplineComputers(snapPos)
                                .Where(splineComputer => splineComputer.GetPoints().Contains(removePoint)))
                            {
                                spline = splineComputer;
                            }
                        }
                    }
                    else
                    {
                        spline = GetSplineComputers(snapPos).First();
                    }

                    var point = getSplinePoint(snapPos, spline);
                    var pointIndex = getSplinePointIndex(spline, point);
                    var lastIndex = spline.GetPoints().Length - 1;

                    if (removeMode == REMOVEMODE.STANDBY)
                    {
                        if (crossroadRef == null)
                        {
                            removePointIndex = pointIndex;
                            removePoint = point;
                        }
                        removeMode = REMOVEMODE.EXECUTE;
                    }
                    else if (removeMode == REMOVEMODE.EXECUTE)
                    {
                        if (spline)
                        {
                            if (removePointIndex == lastIndex || removePointIndex == 0 || removePointIndex == -1)
                            {
                                UnityEngine.Debug.LogWarning("REMOVE");
                                if (crossroadRef == null)
                                {
                                    RemovePoint(spline, removePoint);
                                }
                                else
                                {
                                    removePoint = spline.GetPoints().FirstOrDefault(po => po.position == crossroadRef.getPosition());
                                    removePointIndex = getSplinePointIndex(spline, removePoint);
                                    RemovePoint(spline, removePoint);

                                    CheckRoadConnectedValid(crossroadRef, spline, true);
                                }

                                if (spline.GetPoints().Length <= 1)
                                {
                                    foreach (var cros in GetRefCrossroads(spline))
                                    {
                                        cros.RemoveRoad(spline);
                                    }
                                    
                                    Destroy(spline.gameObject);
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning("SPLIT");
                                var newSpline = SplitSpline(removePointIndex, spline);
                                
                                ResetMeshClip(spline);
                                ResetMeshClip(newSpline);

                                if (removePointIndex < pointIndex)
                                {
                                    RemovePoint(newSpline, removePoint);
                                    RebuildLate(spline);
                                    RebuildLate(newSpline);
                                }
                                else
                                {
                                    RemovePoint(spline, removePoint);
                                    RebuildLate(spline);
                                    RebuildLate(newSpline);
                                }
                                
                                CheckRoadConnectedValid(GetRefCrossroads(spline).First(), spline, true);
                                CheckRoadConnectedValid(GetRefCrossroads(newSpline).First(), newSpline, true);
                            }
                        }

                        RebuildLate(spline);

                        removePointIndex = pointIndex;
                        removePoint = point;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                removeMode = REMOVEMODE.STANDBY;
                removePoint = new SplinePoint();
                removePointIndex = -1;
                crossroadRef = null;

                lastX = 0;
                lastZ = 0;
            }
        }
    }

    public enum REMOVEMODE
    {
        STANDBY,
        EXECUTE
    }

    private void RebuildLate(SplineComputer spline)
    {
        _rebuildLateBool = true;
        _rebuildLateSpline = spline;

        StartCoroutine(Stop());

        IEnumerator Stop()
        {
            yield return new WaitForSeconds(0.01f);
            _rebuildLateBool = false;
            _rebuildLateSpline = null;
        }
    }

    private void LateUpdate()
    {
        if (_rebuildLateBool)
        {
            _rebuildLateSpline.Rebuild(true);
        }
    }
}