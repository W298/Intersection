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
                        splineFollower.direction = Spline.Direction.Forward;
                        splineFollower.startPosition = 0;
                        splineFollower.motion.offset = new Vector2(0.65f, defY);
                        break;
                    case SplineComputer.MODE.LAST_OPEN:
                        splineFollower.direction = Spline.Direction.Backward;
                        splineFollower.startPosition = 1;
                        splineFollower.motion.offset = new Vector2(-0.65f, defY);
                        break;
                }
                break;
            default:
                break;
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

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        splineFollower.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;
    }

    void Update()
    {
    }
}