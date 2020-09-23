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

    public int index = 0;

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
    public void MoveAll()
    {
        carManager.MoveAll();
    }
    
    [Category("Path Follower")]
    public void StopAll()
    {
        foreach (var car in carManager.cars)
        {
            car.GetComponent<PathFollower>().Stop();
        }
    }

    [Category("Path Follower")]
    public void SelectSplineFromPath()
    {
        foreach (var car in carManager.cars)
        {
            car.GetComponent<PathFollower>().selectPath(index, true);
        }
    }

    [Category("Path Finder")]
    public void SetPathFindDataList()
    {
        carManager.SetPathFindDataList();
    }
    
    [Category("Path Finder")]
    public void SelectPathFindDataEach()
    {
        foreach (var car in carManager.cars)
        {
            CarManager.SelectPathFindDataToCar(car, carManager.pathFindDataList, carManager.weightList);
        }
    }

    [Category("Car Manager")]
    public void Spawn()
    {
        carManager.Spawn();
    }

    [Category("Car Manager")]
    public void Spawn_10()
    {
        for (int i = 0; i < 10; i++)
        {
             carManager.Spawn();
        }
    }

    [Category("Car Manager")]
    public int carCount
    {
        get { return carManager.cars.Count; }
    }
    
    [Category("Car Manager")]
    public void IncreaseChosenSplineWeight()
    {
        for (var i = 0; i < carManager.exToEnterTupleList.Count; i++)
        {
            var tuple = carManager.exToEnterTupleList[i];
            if (tuple.Item1 == pathManager.chosenSpline)
            {
                carManager.weightList[i] += 1.0f;
            }
        }
    }
    
    [Category("Car Manager")]
    public void DecreaseChosenSplineWeight()
    {
        for (var i = 0; i < carManager.exToEnterTupleList.Count; i++)
        {
            var tuple = carManager.exToEnterTupleList[i];
            if (tuple.Item1 == pathManager.chosenSpline)
            {
                carManager.weightList[i] -= 1.0f;
                if (carManager.weightList[i] < 0)
                {
                    carManager.weightList[i] = 0.0f;
                }
            }
        }
    }
}
