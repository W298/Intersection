﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Option : MonoBehaviour
{
    void Start()
    {
        SROptions.Current.pathManager = GetComponent<CreatePathManager>();
        SROptions.Current.pathFinder = GetComponent<PathFinder>();
    }
}

public partial class SROptions
{
    public CreatePathManager pathManager;
    public PathFinder pathFinder;

    [Category("Path Manager")]
    public void ResetAllMeshClip()
    {
        foreach (var spline in GameObject.FindObjectsOfType<SplineComputer>())
        {
            pathManager.ResetMeshClip(spline);
        }
    }

    [Category("Path Manager")]
    public float divider_RL1
    {
        get { return pathManager.dividerList[0]; }
        set { pathManager.dividerList[0] = value; }
    }

    [Category("Path Manager")]
    public float divider_RL2
    {
        get { return pathManager.dividerList[1]; }
        set { pathManager.dividerList[1] = value; }
    }

    [Category("Path Manager")]
    public float divider_RL05
    {
        get { return pathManager.dividerList[4]; }
        set { pathManager.dividerList[4] = value; }
    }

    [Category("Path Manager")]
    public float changer
    {
        get { return pathManager.changer; }
        set { pathManager.changer = value; }
    }

    [Category("Path Follower")]
    public void Move()
    {
        pathManager.car.GetComponent<PathFollower>().Run();
    }
    
    [Category("Path Follower")]
    public void Stop()
    {
        pathManager.car.GetComponent<PathFollower>().Stop();
    }
    
    [Category("Path Follower")]
    public void Reset()
    {
        pathManager.car.GetComponent<PathFollower>().Reset();
    }

    [Category("Path Follower")]
    public void SetMoveDirStraight()
    {
        pathManager.car.GetComponent<PathFollower>().setMoveDir(true);
    }
    
    [Category("Path Follower")]
    public void SetMoveDirReverse()
    {
        pathManager.car.GetComponent<PathFollower>().setMoveDir(false);
    }
    
    [Category("Path Follower")]
    public void SetSpline()
    {
        pathManager.car.GetComponent<PathFollower>().setPath(pathFinder.shortPathList[1]);
    }

    [Category("Path Finder")]
    public void Find()
    {
        var roads = GameObject.FindObjectsOfType<SplineComputer>().ToList();
        var sel = roads.Where(road =>
            road.roadMode == SplineComputer.MODE.LAST_OPEN || road.roadMode == SplineComputer.MODE.FIRST_OPEN).ToList();
        
        pathFinder.Run(sel[0], sel[1]);
    }
}
