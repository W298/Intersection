using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;

public class RoadConnection
{
    private SplineComputer connectedRoad;
    private List<SplineComputer> connectingSpline = new List<SplineComputer>();

    public RoadConnection(SplineComputer connectedRoad)
    {
        this.connectedRoad = connectedRoad;
    }

    public void SetConnectedRoad(SplineComputer connectedRoad)
    {
        this.connectedRoad = connectedRoad;
    }
    
    public SplineComputer GetconnectedRoad()
    {
        return connectedRoad;
    }

    public bool AddConnectingSpline(SplineComputer spline)
    {
        var index = connectingSpline.FindIndex(cs => cs.name == spline.name);
        if (index != -1)
        {
            CreatePathManager.DestroySpline(connectingSpline[index]);
            
            connectingSpline[index] = spline;
            spline.isConnectingRoad = true;

            return true;
        }
        else
        {
            connectingSpline.Add(spline);
            spline.isConnectingRoad = true;

            return false;
        }
    }

    public SplineComputer GetConnectingSpline(int index = 0)
    {
        return connectingSpline[index];
    }
}
