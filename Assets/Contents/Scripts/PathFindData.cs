using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;

using StartEndTuple = System.Tuple<Dreamteck.Splines.SplineComputer, Dreamteck.Splines.SplineComputer>;
using Path = System.Collections.Generic.List<Dreamteck.Splines.SplineComputer>;

public class PathFindData
{
    public StartEndTuple exToEnter;
    public StartEndTuple exitToEx;

    public GameObject targetBuilding;

    public List<Path> pathList;
    public Path currentPath;
    public int currentMode;

    public GameObject possesCar;
    
    public SplineComputer connectingRoad
    {
        get { return targetBuilding.GetComponent<DTBuilding>().connectingRoad; }
    }

    public PathFindData(PathFindData origin, GameObject possesCar)
    {
        exToEnter = origin.exToEnter;
        exitToEx = origin.exitToEx;
        targetBuilding = origin.targetBuilding;
        this.currentMode = 0;

        this.possesCar = possesCar;
    }

    // Constructor
    public PathFindData(StartEndTuple exToEnter, StartEndTuple exitToEx, GameObject targetBuilding)
    {
        this.exToEnter = exToEnter;
        this.exitToEx = exitToEx;
        this.targetBuilding = targetBuilding;
        this.currentMode = 0;
    }

    public bool IncreaseMode()
    {
        currentMode++;
        if (currentMode > 2)
        {
            return true;
        }
        
        if (currentMode == 1)
        {
            possesCar.GetComponent<CarAI>().RunDTBehavior(targetBuilding.GetComponent<DTBuilding>());
        }
        
        return false;
    }

    public void FindPathList()
    {
        switch (currentMode)
        {
            case 0:
                pathList = PathFinder.Run(exToEnter.Item1, exToEnter.Item2);
                break;
            case 1:
                pathList = new List<Path>() {new Path() {connectingRoad}};
                break;
            case 2:
                pathList = PathFinder.Run(exitToEx.Item1, exitToEx.Item2);
                break;
        }
    }

    public void SelectPath(bool shortestPath = true, int index = 0)
    {
        if (shortestPath)
        {
            var minCount = pathList.Select(p => p.Count).Min();
            var shortPathList = pathList.Where(p => p.Count == minCount).ToList();

            currentPath = shortPathList[index];
        }
        else
        {
            currentPath = pathList[index];
        }
    }

    public void PrintData()
    {
        UnityEngine.Debug.LogWarning("Ex-Enter : ");
        UnityEngine.Debug.LogWarning(exToEnter.Item1.name + ", " + exToEnter.Item2.name);
        
        UnityEngine.Debug.LogWarning("Exit-Ex : ");
        UnityEngine.Debug.LogWarning(exitToEx.Item1.name + ", " + exitToEx.Item2.name);
        
        UnityEngine.Debug.LogWarning("Current Path : ");
        UnityEngine.Debug.LogWarning(currentPath.Aggregate("", (current, road) => current + (road.name + ", ")));
    }
}