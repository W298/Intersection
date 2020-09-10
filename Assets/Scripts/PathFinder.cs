using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dreamteck.Splines;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    private CreatePathManager pathManager;

    public List<List<SplineComputer>> pathList = new List<List<SplineComputer>>();
    public List<List<SplineComputer>> shortPathList = new List<List<SplineComputer>>();
    private void AppendSplineList(List<SplineComputer> origin, List<SplineComputer> list)
    {
        foreach (var item in list)
        {
            if (!origin.Contains(item))
            {
                origin.Add(item);
            }
        }
    }

    public void Loop(SplineComputer departure, SplineComputer arrival, List<SplineComputer> path)
    {
        foreach (var connectedSpline in departure.connectedSplineList)
        {
            if (!path.Contains(connectedSpline))
            {
                if (connectedSpline == arrival)
                {
                    path.Add(arrival);
                    pathList.Add(path);
                }
                else
                {
                    var clonedPath = new List<SplineComputer>(path);
                    clonedPath.Add(connectedSpline);
                    Loop(connectedSpline, arrival, clonedPath);
                }
            }
        }
    }
    
    public void Run(SplineComputer departure, SplineComputer arrival)
    {
        pathManager.LogTextOnPos("Departure", pathManager.GetSplinePosition(departure), true, false);
        pathManager.LogTextOnPos("Arrival", pathManager.GetSplinePosition(arrival), true, false);

        foreach (var crossroad in pathManager.crossroads)
        {
            foreach (var road in crossroad.getRoads())
            {
                var addList = new List<SplineComputer>(crossroad.getRoads());
                addList.Remove(road);
                
                AppendSplineList(road.connectedSplineList, addList);
            }
        }
        
        var path = new List<SplineComputer>();
        path.Add(departure);
        Loop(departure, arrival, path);
        
        var minCount = pathList.Select(p => p.Count).Min();
        shortPathList = pathList.Where(p => p.Count == minCount).ToList();

        for (var i = 0; i < shortPathList.Count; i++)
        {
            var p = shortPathList[i];
            for (var index = 0; index < p.Count; index++)
            {
                pathManager.LogTextOnPos(i + " / " + index, pathManager.GetSplinePosition(p[index]), true, false);
            }
        }
    }

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }
}
