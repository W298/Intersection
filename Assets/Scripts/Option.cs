using System.Collections;
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
        SROptions.Current.carManager = GetComponent<CarManager>();
    }
}

public partial class SROptions
{
    public CreatePathManager pathManager;
    public PathFinder pathFinder;
    public CarManager carManager;

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
    public void MoveAll()
    {
        foreach (var car in carManager.cars)
        {
            car.GetComponent<PathFollower>().Run();
        }
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
    public void SelectSpline()
    {
        foreach (var car in carManager.cars)
        {
            car.GetComponent<PathFollower>().selectPath(0, true);
        }
    }

    [Category("Path Finder")]
    public void Prepare()
    {
        carManager.Prepare();
    }
    
    [Category("Path Finder")]
    public void Find()
    {
        carManager.FindAndSetPath();
    }

    [Category("Car Manager")]
    public void Spawn()
    {
        carManager.Spawn();
    }

    [Category("Car Manager")]
    public int carCount
    {
        get { return carManager.cars.Count; }
    }
}
