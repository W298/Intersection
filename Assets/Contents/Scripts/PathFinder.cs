using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dreamteck.Splines;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    private static void AddUnique(List<RoadConnection> origin, List<SplineComputer> list)
    {
        foreach (var item in list)
        {
            if (origin.All(rc => rc.GetconnectedRoad() != item))
            {
                var roadConnection = new RoadConnection(item);
                origin.Add(roadConnection);
            }
        }
    }

    private static void Loop(SplineComputer departure, SplineComputer arrival, List<SplineComputer> path,
        List<List<SplineComputer>> pathList)
    {
        foreach (var roadConnection in departure.roadConnectionList)
        {
            var connectedRoad = roadConnection.GetconnectedRoad();
            if (!path.Contains(connectedRoad))
            {
                if (connectedRoad == arrival)
                {
                    path.Add(arrival);
                    pathList.Add(path);
                }
                else
                {
                    var clonedPath = new List<SplineComputer>(path);
                    clonedPath.Add(connectedRoad);
                    Loop(connectedRoad, arrival, clonedPath, pathList);
                }
            }
        }
    }
    
    public static List<List<SplineComputer>> Run(SplineComputer departure, SplineComputer arrival)
    {
        var pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        
        /*
        foreach (var crossroad in pathManager.crossroads)
        {
            foreach (var road in crossroad.GetRoads())
            {
                var addList = new List<SplineComputer>(crossroad.GetRoads());
                addList.Remove(road);
                
                AddUnique(road.roadConnectionList, addList);
            }
        }
        */
        
        var pathList = new List<List<SplineComputer>>();

        var path = new List<SplineComputer>();
        path.Add(departure);
        Loop(departure, arrival, path, pathList);

        return pathList;
    }
}
