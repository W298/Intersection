using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.EventSystems;

public class Junction : MonoBehaviour
{
    private SplineFollower sf;

    public SplineComputer cm2;

    public enum ROAD { plus_road, minus_road }

    public ROAD follow_road;
    void Start()
    {
        sf = GetComponent<SplineFollower>();

        sf.motion.velocityHandleMode = TransformModule.VelocityHandleMode.Preserve;
        applyFollowRoad();

        sf.onEndReached += EndReach;
    }

    private void EndReach(double last_percent)
    {
        sf.spline = cm2;
        setFollowRoad(ROAD.minus_road);

        sf.clipTo = cm2.GetPointPercent(3);
    }

    void Update()
    {
        
    }

    public void setFollowRoad(ROAD _roadtype)
    {
        follow_road = _roadtype;
        applyFollowRoad();
    }

    private void applyFollowRoadOffset()
    {
        if (follow_road == ROAD.plus_road)
            sf.motion.offset = new Vector2(1, 0);
        else if (follow_road == ROAD.minus_road)
            sf.motion.offset = new Vector2(-1, 0);
    }

    private void applyFollowRoadDirection()
    {
        if (follow_road == ROAD.plus_road)
        {
            sf.direction = Spline.Direction.Forward;
            sf.startPosition = 0;
        }
        else if (follow_road == ROAD.minus_road)
        {
            sf.direction = Spline.Direction.Backward;
            sf.startPosition = 1;
        }
    }

    public void applyFollowRoad()
    {
        applyFollowRoadOffset();
        applyFollowRoadDirection();
    }
}
