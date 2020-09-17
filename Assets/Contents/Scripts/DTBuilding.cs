using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;

public class DTBuilding : MonoBehaviour
{
    public ConnectingRoadScript connectingRoadScript;
    private CreatePathManager pathManager;
    public Vector3 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public SplineComputer enterRoad
    {
        get { return connectingRoadScript.enterRoad; }
    }

    public SplineComputer exitRoad
    {
        get { return connectingRoadScript.exitRoad; }
    }
    
    public int upgrade = 1;
    public int speed = 10;
    public int lane = 1;
    public int capacity = 10;

    public List<GameObject> detectedCars
    {
        get { return GetComponent<TriggerSensor>().
            DetectedObjects.Select(sen => sen.gameObject).ToList(); }
    }

    public List<GameObject> insideCarList;

    void Start()
    {
        connectingRoadScript = GetComponentInChildren<ConnectingRoadScript>();
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        pathManager.buildingPosList.Add(position);

        GetComponent<TriggerSensor>().OnDetected.AddListener(OnDetected);
    }

    void OnDetected(GameObject obj, Sensor sensor)
    {
        // Add Unique Car
        if (!insideCarList.Contains(obj))
        {
            insideCarList.Add(obj);
            // obj.GetComponent<CarBehavior>().RunDTBehavior(this);
        }
    }

    void Update()
    {
        
    }
}