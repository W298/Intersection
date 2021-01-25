using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using Random = UnityEngine.Random;

class connectorType
{
    public int startOffset;
    public int endOffset;
    public SplineComputer spline;

    public connectorType(int so, int eo, SplineComputer spline)
    {
        startOffset = so;
        endOffset = eo;
        this.spline = spline;
    }
}

public class RoadConnection
{
    private SplineComputer connectedRoad;
    private List<connectorType> connectorList = new List<connectorType>();

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

    public bool AddConnector(SplineComputer spline, int start_offset, int end_offset)
    {
        var index = connectorList.FindIndex(con => con.spline.name == spline.name);
        if (index != -1)
        {
            CreatePathManager.DestroySpline(connectorList[index].spline);

            connectorList[index].spline = spline;
            spline.is_connector = true;

            return true;
        }
        else
        {
            connectorList.Add(new connectorType(start_offset, end_offset, spline));
            spline.is_connector = true;

            return false;
        }
    }
    
    public SplineComputer GetConnector(bool randomSelection, out int endO, int startOffset = 1, int endOffset = 1)
    {
        endO = 1;
        
        if (randomSelection)
        {
            var connectorCand = connectorList.Where(con => con.startOffset == startOffset).ToList();

            if (connectorCand.Count == 0)
            {
                Debug.LogError("Connector is not found!");
                return null;
            }

            var index = Random.Range(0, connectorCand.Count);

            endO = index + 1;
            return connectorCand[index].spline;
        }
        else
        {
            var connector = connectorList.FirstOrDefault(con => con.startOffset == startOffset &&
                                                                con.endOffset == endOffset);
            if (connector != null)
            {
                endO = endOffset;
                return connector.spline;
            }
            else
            {
                Debug.LogError("Connector is not found!");
            }
        }

        return null;
    }
}
