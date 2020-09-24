using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using SensorToolkit;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class PathFollower : MonoBehaviour
{
    private SplineFollower splineFollower;

    public PathFindData pathFindData;
    public int currentPathIndex;

    public bool isStraight = true;
    public float defY = 0.35f;

    // Initiate Running
    public void Initiate(int startIndex = 0)
    {
        if (pathFindData == null) return;
        
        pathFindData.FindPathList();
        pathFindData.SelectPath();
        
        // Set First Road
        SetRunningRoad(pathFindData.currentPath[startIndex]);
        currentPathIndex = 0;
    }

    // Set Spline to splineFollower.spline
    public void SetRunningRoad(SplineComputer _spline)
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
        }
    }

    // Set Move Direction & Position by isSt parameter
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
        if (pathFindData.currentPath.Count <= currentPathIndex + 1)
        {
            if (pathFindData.IncreaseMode())
            {
                Destroy(this.gameObject);
                return;
            }

            var connectingPos = pathFindData.currentPath[currentPathIndex].GetPoints().Last().position;

            Initiate();

            if (pathFindData.currentPath[currentPathIndex].GetPoints().Last().position == connectingPos)
            {
                SetMoveDir(false);
            }
            else if (pathFindData.currentPath[currentPathIndex].GetPoints().First().position == connectingPos)
            {
                SetMoveDir(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ERROR");
            }
            
            Reset();
            return;
        }
        
        if (isStraight)
        {
            var connectingPos = pathFindData.currentPath[currentPathIndex].GetPoints().Last().position;

            currentPathIndex++;
            var nextSpline = pathFindData.currentPath[currentPathIndex];
            
            SetRunningRoad(nextSpline);

            if (nextSpline.GetPoints().Last().position == connectingPos)
            {
                SetMoveDir(false);
            }
            else if (nextSpline.GetPoints().First().position == connectingPos)
            {
                SetMoveDir(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ERROR");
            }
            
            Reset();
        }
    }

    // Only If Current Road is Reverse
    private void BeginReach(double percent)
    {
        if (pathFindData.currentPath.Count <= currentPathIndex + 1)
        {
            if (pathFindData.IncreaseMode())
            {
                Destroy(this.gameObject);
                return;
            }
            
            var connectingPos = pathFindData.currentPath[currentPathIndex].GetPoints().First().position;
            
            Initiate();

            if (pathFindData.currentPath[currentPathIndex].GetPoints().Last().position == connectingPos)
            {
                SetMoveDir(false);
            }
            else if (pathFindData.currentPath[currentPathIndex].GetPoints().First().position == connectingPos)
            {
                SetMoveDir(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ERROR");
            }
            
            Reset();
            return;
        }
        
        if (!isStraight)
        {
            var connectingPos = pathFindData.currentPath[currentPathIndex].GetPoints().First().position;

            currentPathIndex++;
            var nextSpline = pathFindData.currentPath[currentPathIndex];

            SetRunningRoad(nextSpline);
            
            if (nextSpline.GetPoints().Last().position == connectingPos)
            {
                SetMoveDir(false);
            }
            else if (nextSpline.GetPoints().First().position == connectingPos)
            {
                SetMoveDir(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ERROR");
            }
            
            Reset();
        }
    }

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();

        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;

        splineFollower.onBeginningReached += BeginReach;
        splineFollower.onEndReached += EndReach;
        
        currentPathIndex = 0;
    }

    void Update()
    {
        
    }
}