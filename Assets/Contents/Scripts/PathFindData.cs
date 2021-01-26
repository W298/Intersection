using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dreamteck.Splines;
using JetBrains.Annotations;
using UnityEngine;
using MEC;

using StartEndTuple = System.Tuple<Dreamteck.Splines.SplineComputer, Dreamteck.Splines.SplineComputer>;
using Path = System.Collections.Generic.List<Dreamteck.Splines.SplineComputer>;

public class PathFindData
{
    public StartEndTuple exToEnter;
    public StartEndTuple exitToEx;

    public GameObject targetBuilding;

    public List<Path> pathData = new List<Path>();
    public List<Path> pathList = new List<Path>();
    
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
        
        PreCalcAllData();
    }

    // Constructor
    public PathFindData(StartEndTuple exToEnter, StartEndTuple exitToEx, GameObject targetBuilding)
    {
        this.exToEnter = exToEnter;
        this.exitToEx = exitToEx;
        this.targetBuilding = targetBuilding;
        this.currentMode = 0;
        /*    [MODE]
         *    0 : External -> Building
         *    1 : Inside Building
         *    2 : Building -> External
         */
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
            // DT Behavior
        }
        
        return false;
    }

    public void InitCurrentPath()
    {
        currentPath = pathData[currentMode];
    }
    
    public Path SelectPath(List<Path> pL, bool shortestPath, int index)
    {
        if (shortestPath)
        {
            var minCount = pL.Select(p => p.Count).Min();
            var shortPathList = pL.Where(p => p.Count == minCount).ToList();

            return shortPathList[index];
        }
        else
        {
            return pL[index];
        }
    }

    public void PreCalcAllData(bool shortestPath = true, int index = 0)
    {
        var p1 = PathFinder.Run(exToEnter.Item1, exToEnter.Item2);
        var p2 = new List<Path>() {new Path() {connectingRoad}};
        var p3 = PathFinder.Run(exitToEx.Item1, exitToEx.Item2);

        var ps1 = SelectPath(p1, shortestPath, index);
        var ps2 = SelectPath(p2, shortestPath, index);
        var ps3 = SelectPath(p3, shortestPath, index);
        
        pathData.Add(ps1);
        pathData.Add(ps2);
        pathData.Add(ps3);
    }

    public void PrintData()
    {
        UnityEngine.Debug.LogWarning("Ex-Enter : ");
        UnityEngine.Debug.LogWarning(exToEnter.Item1.name + ", " + exToEnter.Item2.name);
        
        UnityEngine.Debug.LogWarning("Exit-Ex : ");
        UnityEngine.Debug.LogWarning(exitToEx.Item1.name + ", " + exitToEx.Item2.name);

        if (currentPath != null)
        {
            UnityEngine.Debug.LogWarning("Current Path : ");
            UnityEngine.Debug.LogWarning(currentPath.Aggregate("", (current, road) => current + (road.name + ", ")));
        }
    }
    
    // Below Methods are for dynamic pathfinding ----------------------------------------------

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
}