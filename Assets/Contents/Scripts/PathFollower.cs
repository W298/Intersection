using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathFollower : MonoBehaviour
{
    private SplineFollower splineFollower;
    private CreatePathManager pathManager;
    
    public List<List<SplineComputer>> pathList;

    public List<List<SplineComputer>> shortPathList
    {
        get
        {
            var minCount = pathList.Select(p => p.Count).Min();
            return pathList.Where(p => p.Count == minCount).ToList();
        }
    }
    
    public List<SplineComputer> path;
    public int pathIndex = 0;

    public bool isStraight = true;
    public float defY = 0.35f;

    public void selectPath(int index = 0, bool shortPathOnly = true)
    {
        path = shortPathOnly ? shortPathList[index] : pathList[index];
        pathIndex = 0;
        
        setSpline(path[pathIndex]);
    }
    
    public void setSpline(SplineComputer _spline)
    {
        splineFollower.spline = _spline;

        switch (_spline.roadLane)
        {
            case CreatePathManager.ROADLANE.RL1:
                switch (_spline.roadMode)
                {
                    case SplineComputer.MODE.FIRST_OPEN:
                        setMoveDir(true);
                        break;
                    case SplineComputer.MODE.LAST_OPEN:
                        setMoveDir(false);
                        break;
                }
                break;
            default:
                break;
        }
    }

    public void setMoveDir(bool _isStraight)
    {
        if (splineFollower.spline)
        {
            switch (splineFollower.spline.roadLane)
            {
                case CreatePathManager.ROADLANE.RL1:
                    if (_isStraight)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0.65f, defY);

                        isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(-0.65f, defY);
                        
                        isStraight = false;
                    }
                    break;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Spline is not defined.");
        }
    }

    public void Run()
    {
        splineFollower.follow = true;
    }

    public void Stop()
    {
        splineFollower.follow = false;
    }

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

    private void EndReach(double percent)
    {
        if (isStraight)
        {
            pathIndex += 1;
            var nextSpline = path[pathIndex];
            Crossroad connectedCrossroad;
        
            connectedCrossroad = pathManager.GetCrossroad(splineFollower.spline.GetPoints().Last().position);
        
            setSpline(nextSpline);

            if (nextSpline.GetPoints().Last().position == connectedCrossroad.getPosition())
            {
                setMoveDir(false);
            }
            else
            {
                setMoveDir(true);
            }
        
            Reset();
        }
    }

    private void BeginReach(double percent)
    {
        if (!isStraight)
        {
            pathIndex += 1;
            var nextSpline = path[pathIndex];
            Crossroad connectedCrossroad;
            
            connectedCrossroad = pathManager.GetCrossroad(splineFollower.spline.GetPoints().First().position);
            
            setSpline(nextSpline);
            
            if (nextSpline.GetPoints().Last().position == connectedCrossroad.getPosition())
            {
                setMoveDir(false);
            }
            else
            {
                setMoveDir(true);
            }
        
            Reset();
        }
        
    }

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        
        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;

        splineFollower.onBeginningReached += BeginReach;
        splineFollower.onEndReached += EndReach;
    }

    void Update()
    {
    }
}