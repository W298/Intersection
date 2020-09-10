using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dreamteck.Splines;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    private CreatePathManager pathManager;
    
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

    public void Loop(SplineComputer departure, SplineComputer arrival, List<SplineComputer> path,
        List<List<SplineComputer>> pathList)
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
                    Loop(connectedSpline, arrival, clonedPath, pathList);
                }
            }
        }
    }
    
    public List<List<SplineComputer>> Run(SplineComputer departure, SplineComputer arrival)
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
        
        var pathList = new List<List<SplineComputer>>();

        var path = new List<SplineComputer>();
        path.Add(departure);
        Loop(departure, arrival, path, pathList);

        return pathList;
    }

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }
}
