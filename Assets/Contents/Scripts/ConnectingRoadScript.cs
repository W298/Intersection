using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;

public class ConnectingRoadScript : MonoBehaviour
{
    private CreatePathManager pathManager;
    private GameObject dt;

    public SplineComputer connectingSpline;
    
    public SplineComputer enterRoad;
    public SplineComputer exitRoad;
    
    void Start()
    {
        connectingSpline = GetComponent<SplineComputer>();
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        dt = this.transform.parent.gameObject;
    }
    
    void Update()
    {
        var enterList = CreatePathManager.GetSplineComputers(connectingSpline.GetPoints().First().position);
        if (enterList.Count != 0)
        {
            enterRoad = enterList[0];
        }
        else
        {
            if (enterRoad)
            {
                enterRoad = null;
            }
        }

        var exitList = CreatePathManager.GetSplineComputers(connectingSpline.GetPoints().Last().position);
        if (exitList.Count != 0)
        {
            exitRoad = exitList[0];
        }
        else
        {
            if (exitRoad)
            {
                exitRoad = null;
            }
        }
    }
}
