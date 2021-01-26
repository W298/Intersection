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
    public SplineFollower splineFollower;
    
    public PathFindData pathFindData;
    public int currentPathIndex;

    public bool isStraight = true;
    public const float DefY = 0.35f;
    public float minSpeed = 0.0f;
    public float maxSpeed = 4.0f;
    public float acc = 8.0f;
    public enum MOVESTAT {DECEASE, INCREASE, ZERO}
    public MOVESTAT moveStat = MOVESTAT.ZERO;

    public Vector3 checkingPos;
    public int currentOffset = 1;

    public double percent
    {
        get
        {
            var per = splineFollower.spline.Project(gameObject.transform.position).percent;
            if (!isStraight)
            {
                per = 1 - per;
            }

            return per;
        }
    }

    public void SetSpeed(float speed = 5.0f)
    {
        maxSpeed = speed;
    }

    // Initiate Running
    public void Initiate(int startIndex = 0)
    {
        pathFindData.InitCurrentPath();

        // Set First Road
        currentPathIndex = 0;
        SetNextRoad(pathFindData.currentPath[startIndex], false);
    }

    // Set Spline to splineFollower.spline
    public void SetNextRoad(SplineComputer _spline, bool toConnector)
    {
        if (toConnector)
        {
            moveStat = MOVESTAT.INCREASE;
            maxSpeed = 2.0f;
            var roadConnection = splineFollower.spline.roadConnectionList.FirstOrDefault(rc => rc.GetconnectedRoad() == _spline);

            var cs = roadConnection.GetConnector(true, out var _endO, currentOffset);
            
            if (cs == null)
                Debug.LogError("Connection is not found!");
            
            currentOffset = _endO;    // Change Current Offset to last End Offset

            cs.Rebuild(true);
            
            splineFollower.spline = cs;
            SetMoveDir(true);

            checkingPos = cs.GetPoints().Last().position;
            GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>().debugPointPer(checkingPos);
        }
        else
        {
            moveStat = MOVESTAT.INCREASE;
            maxSpeed = 4.0f;
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
                case CreatePathManager.ROADLANE.RL2:
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
            if (splineFollower.spline.is_connector)
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
                case CreatePathManager.ROADLANE.RL2:
                    if (isSt)
                    {
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;

                        switch (currentOffset)
                        {
                            case 1:
                                splineFollower.motion.offset = new Vector2(0.65f, DefY);
                                break;
                            case 2:
                                splineFollower.motion.offset = new Vector2(0.65f * 3, DefY);
                                break;
                        }
                        
                        this.isStraight = true;
                    }
                    else
                    {
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        
                        switch (currentOffset)
                        {
                            case 1:
                                splineFollower.motion.offset = new Vector2(-0.65f, DefY);
                                break;
                            case 2:
                                splineFollower.motion.offset = new Vector2(-0.65f * 3, DefY);
                                break;
                        }
                        
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
        moveStat = MOVESTAT.INCREASE;
    }

    // Disable following
    public void Stop()
    {
        moveStat = MOVESTAT.DECEASE;
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
            
        if (splineFollower.spline.is_connector)
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
        
        ProjectToSpline(transform.position, splineFollower.spline);
    }


    private void ProjectToSpline(Vector3 pos, SplineComputer spline)
    {
        var per = spline.Project(pos).percent;
        splineFollower.SetPercent(per);
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
           
        ProjectToSpline(transform.position, splineFollower.spline);
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
                nextSpline = pathFindData.pathData[pathFindData.currentMode + 1][0];
            }
            else if (currentPathIndex == pathFindData.currentPath.Count - 1)
            {
                if (pathFindData.currentMode >= 2)
                {
                    checkingPos = pathFindData.currentPath[currentPathIndex].GetPoints().Last().position;
                    return;
                }
                else
                {
                    nextSpline = pathFindData.pathData[pathFindData.currentMode + 1][0];
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
            Vector3 po;
            Vector3 rightDir;

            var isStCon = nextSpline.GetPoint(0).position == curSpline.GetPoints().Last().position;
            
            if (isStCon)
            {
                var last = curSpline.GetPoints().Length - 1;
                po = curSpline.GetPoint(last).position;

                var dir = po - curSpline.GetPoint(last - 1).position;
                rightDir = Quaternion.AngleAxis(90, Vector3.up) * dir;
                rightDir.Normalize();
            }
            else
            {
                po = curSpline.GetPoint(0).position;

                var dir = po - curSpline.GetPoint(1).position;
                rightDir = Quaternion.AngleAxis(90, Vector3.up) * dir;
                rightDir.Normalize();
            }

            po += rightDir * Crossroad.road_offset[currentOffset];
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

            var connector = crc.GetConnector(true, out var _endO, currentOffset);

            if (connector == null)
            {
                Debug.LogError("Connector is not found!");
                return;
            }

            checkingPos = connector.GetPoint(0).position;
            // currentOffset = _endO;
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
                OnEnd();
            }
            else
            {
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
        if (pathFindData == null) return;
        
        if (pathFindData.currentMode == 2 && 
            currentPathIndex == pathFindData.currentPath.Count - 1 && 
            !splineFollower.spline.is_connector &&
            percent >= 0.9)
        {
            Destroy(this.gameObject);
            return;
        }
        
        switch (moveStat)
        {
            case MOVESTAT.ZERO:
                break;
            case MOVESTAT.INCREASE:
                splineFollower.followSpeed = Mathf.Lerp(splineFollower.followSpeed, maxSpeed, Time.deltaTime * acc);
                if (Mathf.Abs(splineFollower.followSpeed - maxSpeed) <= 0.1)
                {
                    moveStat = MOVESTAT.ZERO;
                }
                
                break;
            case MOVESTAT.DECEASE:
                splineFollower.followSpeed = Mathf.Lerp(splineFollower.followSpeed, minSpeed, Time.deltaTime * acc);
                if (Mathf.Abs(splineFollower.followSpeed - minSpeed) <= 0.1)
                {
                    splineFollower.follow = false;
                    moveStat = MOVESTAT.ZERO;
                }
                
                break;
        }

        EndBeginEventChecker();
    }
}