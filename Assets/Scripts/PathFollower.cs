using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathFollower : MonoBehaviour
{
    private SplineFollower splineFollower;

    public bool isStraight = true; // Reset on the end
    public float defY = 0.35f;

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

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;
    }

    void Update()
    {
    }
}