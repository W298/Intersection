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
    public const float DefY = 0.35f;

    public Vector3 checkingPos;

    public void SetSpeed(float speed = 5.0f)
    {
        splineFollower.followSpeed = speed;
    }

    // Initiate Running
    public void Initiate(int startIndex = 0)
    {
        if (pathFindData == null) return;

        pathFindData.FindPathList();
        pathFindData.SelectPath();
        
        // Set First Road
        currentPathIndex = 0;
        SetNextRoad(pathFindData.currentPath[startIndex], false);

        if (pathFindData.currentMode == 0)
        {
            pathFindData.PreCalcAllData();
        }
    }

    // Set Spline to splineFollower.spline
    public void SetNextRoad(SplineComputer _spline, bool toConnectingRoad)
    {
        if (toConnectingRoad)
        {
            var roadConnection = splineFollower.spline.roadConnectionList.FirstOrDefault(rc => rc.GetconnectedRoad() == _spline);
            var cs = roadConnection.GetConnectingSpline(0);
            cs.Rebuild(true);
            
            splineFollower.spline = cs;
            SetMoveDir(true);

            checkingPos = cs.GetPoints().Last().position;
            GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>().debugPointPer(checkingPos);
        }
        else
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
            
            EndBeginEventChecker_Prepare();
        }
    }

    // Set Move Direction & Position by isSt parameter
    public void SetMoveDir(bool isSt)
    {
        if (splineFollower.spline)
        {
            if (splineFollower.spline.isConnectingRoad)
            {
                splineFollower.direction = Spline.Direction.Forward;
                splineFollower.startPosition = 0;
                splineFollower.motion.offset = new Vector2(0, DefY);

                this.isStraight = true;
                
                return;
            }
            
            switch (splineFollower.spline.roadLane)
            {
                case CreatePathManager.ROADLANE.RL1:
                    if (isSt)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0.65f, DefY);

                        this.isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(-0.65f, DefY);
                        
                        this.isStraight = false;
                    }
                    break;
                case CreatePathManager.ROADLANE.RL05:
                    if (isSt)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0, DefY);

                        this.isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(0, DefY);
                        
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

    private void OnEnd()
    {
        if (isStraight)
        {
            PrepareNextRoad();
        }
    }

    private void OnBegin()
    {
        if (!isStraight)
        {
            PrepareNextRoad();
        }
    }

    private void PrepareNextRoad()
    {
        SplineComputer nextSpline;
            
        if (splineFollower.spline.isConnectingRoad)
        {
            var connectingPos = GetConnectingPos();
                
            nextSpline = pathFindData.currentPath[currentPathIndex];
            SetNextRoad(nextSpline, false);
                
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
        }
        else
        {
            currentPathIndex++;
                
            if (pathFindData.currentPath.Count - 1 < currentPathIndex)
            {
                NextMode();
                return;
            }
                
            nextSpline = pathFindData.currentPath[currentPathIndex];
            SetNextRoad(nextSpline, true);
        }

        Reset();
    }
    
    private void NextMode()
    {
        if (pathFindData.IncreaseMode())
        {
            Destroy(this.gameObject);
            return;
        }
            
        var lastPoints = pathFindData.currentPath[currentPathIndex - 1].GetPoints();
            
        Initiate();  // Warning : currentPath is changed
            
        var curPoints = pathFindData.currentPath[currentPathIndex].GetPoints();

        var connectingPoint = curPoints.FirstOrDefault(cur_point =>
            lastPoints.Any(last_point => cur_point.position == last_point.position));
        var connectingPos = connectingPoint.position;

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
    }

    private Vector3 GetConnectingPos()
    {
        var connectingPos = new Vector3();
        if (currentPathIndex - 1 >= 0)
        {
            var curPoints = pathFindData.currentPath[currentPathIndex].GetPoints();
            var lastPoints = pathFindData.currentPath[currentPathIndex - 1].GetPoints();

            var connectingPoint = curPoints.FirstOrDefault(cur_point =>
                lastPoints.Any(last_point => cur_point.position == last_point.position));

            connectingPos = connectingPoint.position;
            return connectingPos;
        }

        return Vector3.zero;
    }

    private void EndBeginEventChecker_Prepare()
    {
        // Only If pathFindData is valid
        if (pathFindData == null)
        {
            Debug.LogError("PathFind Data is not set, but tried to access.");
            return;
        }

        SplineComputer curSpline, nextSpline;

        try
        {
            if (splineFollower.spline.isFixed)
            {
                curSpline = splineFollower.spline;
            }
            else
            {
                curSpline = pathFindData.currentPath[currentPathIndex];
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError("From curSpline, index is out of range");
            return;
        }
        
        try
        {
            if (curSpline.isFixed)
            {
                nextSpline = pathFindData.preCalculatedData[pathFindData.currentMode + 1][0];
            }
            else if (currentPathIndex == pathFindData.currentPath.Count - 1)
            {
                if (pathFindData.currentMode >= 2)
                {
                    checkingPos = pathFindData.currentPath[currentPathIndex].GetPoints().Last().position;
                    GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>().debugPointPer(checkingPos);
                    return;
                }
                else
                {
                    nextSpline = pathFindData.preCalculatedData[pathFindData.currentMode + 1][0];
                }
            }
            else
            {
                nextSpline = pathFindData.currentPath[currentPathIndex + 1];
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError("From nextSpline, index is out of range");
            return;
        }

        if (curSpline.isFixed)
        {
            checkingPos = curSpline.GetPoints().Last().position;
        }
        else if (currentPathIndex == pathFindData.currentPath.Count - 1)
        {
            int last = curSpline.GetPoints().Length - 1;

            var po = curSpline.GetPoint(last).position;

            var dir = curSpline.GetPoint(last).position - curSpline.GetPoint(last - 1).position;
            var rightDir = Quaternion.AngleAxis(90, Vector3.up) * dir;
            rightDir.Normalize();

            po += rightDir * 0.65f;

            checkingPos = po;
        }
        else
        {
            var crc = curSpline.roadConnectionList.FirstOrDefault(rc => rc.GetconnectedRoad() == nextSpline);

            if (crc == null)
            {
                Debug.LogError("Road is not connected!");
                return;
            }

            checkingPos = crc.GetConnectingSpline().GetPoint(0).position;
        }

        GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>().debugPointPer(checkingPos);
    }

    private void EndBeginEventChecker()
    {
        if (checkingPos == Vector3.zero) return;
        
        Vector2 poV2 = new Vector2(checkingPos.x, checkingPos.z);
        Vector2 cpV2 = new Vector2(transform.position.x, transform.position.z);

        var dist = Vector2.Distance(poV2, cpV2);
            
        if (dist <= 0.1f && Mathf.Abs(checkingPos.y - transform.position.y) <= 0.5f)
        {
            if (isStraight)
            {
                UnityEngine.Debug.LogWarning("END");
                OnEnd();
            }
            else
            {
                UnityEngine.Debug.LogWarning("BEGIN");
                OnBegin();
            }
        }
    }

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();

        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;

        currentPathIndex = 0;
        splineFollower.followSpeed = 0;
    }

    void Update()
    {
        if (splineFollower.follow)
        {
            splineFollower.followSpeed = Mathf.Lerp(splineFollower.followSpeed, 5, Time.deltaTime);
        }
        else
        {
            splineFollower.followSpeed = Mathf.Lerp(splineFollower.followSpeed, 0, Time.deltaTime);
        }
        
        EndBeginEventChecker();
    }
}