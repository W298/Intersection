using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using SensorToolkit;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathFollower : MonoBehaviour
{
    private SplineFollower splineFollower;
    private CreatePathManager pathManager;
    private GameObject player;
    private CarManager carManager;

    public PathFindData pathFindData;

    public List<List<SplineComputer>> pathList;
    public List<SplineComputer> selectedPath;
    public int currentPathIndex;

    public bool isStraight = true;
    public float defY = 0.35f;

    // Select Path from pathList variable : Need To delete
    public void selectPath(int index = 0, bool shortPathOnly = true)
    {
        selectedPath = shortPathOnly ? GetShortPathList(pathList)[index] : pathList[index];
        currentPathIndex = 0;
        
        SetCurrentRoad(selectedPath[currentPathIndex]);
    }
    
    // Select Short Path List from PathList
    public static List<List<SplineComputer>> GetShortPathList(List<List<SplineComputer>> pathList)
    {
        var minCount = pathList.Select(p => p.Count).Min();
        var shortPathList = pathList.Where(p => p.Count == minCount).ToList();

        return shortPathList;
    }

    // Select Path from pathList parameter
    public static List<SplineComputer> SelectPath(List<List<SplineComputer>> pathList, int index = 0, bool shortPathOnly = true)
    {
        return shortPathOnly ? GetShortPathList(pathList)[index] : pathList[index];
    }

    // Set Spline to splineFollower.spline
    public void SetCurrentRoad(SplineComputer _spline)
    {
        splineFollower.spline = _spline;

        switch (_spline.roadLane)
        {
            case CreatePathManager.ROADLANE.RL1:
                switch (_spline.roadMode)
                {
                    case SplineComputer.MODE.FIRST_OPEN:
                        SetMoveDir(true);
                        break;
                    case SplineComputer.MODE.LAST_OPEN:
                        SetMoveDir(false);
                        break;
                }
                break;
            default:
                break;
        }
    }

    // Set Move Direction & Position by is_straight parameter
    public void SetMoveDir(bool isSt)
    {
        if (splineFollower.spline)
        {
            switch (splineFollower.spline.roadLane)
            {
                case CreatePathManager.ROADLANE.RL1:
                    if (isSt)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0.65f, defY);

                        this.isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(-0.65f, defY);
                        
                        this.isStraight = false;
                    }
                    break;
                case CreatePathManager.ROADLANE.RL05:
                    if (isSt)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0, defY);

                        this.isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(0, defY);
                        
                        this.isStraight = false;
                    }
                    break;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Spline is not defined.");
        }
    }

    // Enable following
    public void Run()
    {
        splineFollower.follow = true;
    }

    // Disable following
    public void Stop()
    {
        splineFollower.follow = false;
    }

    // Re-set Position for start following
    public void Reset()
    {
        if (isStraight)
        {
            splineFollower.SetPercent(0.0f);
        }
        else
        {
            splineFollower.SetPercent(1.0f);
        }
    }

    // Only If Current Road is Straight
    private void EndReach(double percent)
    {
        if (isStraight)
        {
            if (splineFollower.spline.isFixed)
            {
                /*
                var exitTupleList = carManager.exToEnterTupleList.Where(tuple => tuple.Item1 == splineFollower.spline.connectedBuilding.GetComponent<DTBuilding>().exitRoad).ToList();
                var selectedTuple = carManager.WeightedRandom(exitTupleList, new List<float>(Enumerable.Repeat<float>(1f, exitTupleList.Count)));

                var pathList = PathFinder.Run(selectedTuple.Item1, selectedTuple.Item2);
                var pathToAdd = SelectPath(pathList);
                
                selectedPath.AddRange(pathToAdd);
                */
            }
            
            currentPathIndex += 1;
            var nextSpline = selectedPath[currentPathIndex];

            if (nextSpline.isFixed)
            {
                SetCurrentRoad(nextSpline);
                SetMoveDir(true);
                Reset();
            }
            else if (nextSpline != null)
            {
                if (nextSpline.isEnterRoad)
                {
                    selectedPath.Add(nextSpline.connectedBuilding.GetComponentInChildren<SplineComputer>());
                }
                
                Crossroad connectedCrossroad;
                connectedCrossroad = pathManager.GetCrossroad(splineFollower.spline.GetPoints().Last().position);
                
                SetCurrentRoad(nextSpline);

                if (nextSpline.GetPoints().Last().position == connectedCrossroad.getPosition())
                {
                    SetMoveDir(false);
                }
                else
                {
                    SetMoveDir(true);
                }
        
                Reset();
            }
        }
    }

    // Only If Current Road is Reverse
    private void BeginReach(double percent)
    {
        if (!isStraight)
        {
            currentPathIndex += 1;
            var nextSpline = selectedPath[currentPathIndex];

            if (nextSpline.isFixed)
            {
                SetCurrentRoad(nextSpline);
                SetMoveDir(true);
                Reset();
            }
            else if (nextSpline != null)
            {
                if (nextSpline.isEnterRoad)
                {
                    selectedPath.Add(nextSpline.connectedBuilding.GetComponentInChildren<SplineComputer>());
                }

                Crossroad connectedCrossroad;
                connectedCrossroad = pathManager.GetCrossroad(splineFollower.spline.GetPoints().First().position);
                
                SetCurrentRoad(nextSpline);

                if (nextSpline.GetPoints().Last().position == connectedCrossroad.getPosition())
                {
                    SetMoveDir(false);
                }
                else
                {
                    SetMoveDir(true);
                }
        
                Reset();
            }
        }
        
    }

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        player = GameObject.FindGameObjectWithTag("Player");
        pathManager = player.GetComponent<CreatePathManager>();
        carManager = player.GetComponent<CarManager>();

        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;

        splineFollower.onBeginningReached += BeginReach;
        splineFollower.onEndReached += EndReach;
        
        currentPathIndex = 0;
    }

    void Update()
    {
        if (pathFindData != null)
        {
            pathFindData.FindPathList();
            pathFindData.SelectPath(true);
            pathFindData.PrintData();
        }
    }
}