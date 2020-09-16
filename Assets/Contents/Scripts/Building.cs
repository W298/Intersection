using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;

public class Building : MonoBehaviour
{
    private CreatePathManager pathManager;
    public Vector3 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public SplineComputer enterRoad
    {
        get
        {
            return FindObjectsOfType<SplineComputer>().FirstOrDefault(road => road.isEnterRoad);
        }
    }

    public SplineComputer exitRoad
    {
        get
        {
            return FindObjectsOfType<SplineComputer>().FirstOrDefault(road => road.isExitRoad);
        }
    }
    
    public int upgrade = 1;
    public int speed;
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
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        pathManager.buildingPosList.Add(position);

        GetComponent<TriggerSensor>().OnDetected.AddListener(OnDetected);
    }

    void OnDetected(GameObject obj, Sensor sensor)
    {
        insideCarList.Add(obj);
    }

    void Update()
    {
        
    }
}