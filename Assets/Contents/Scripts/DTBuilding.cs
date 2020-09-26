using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;

public class DTBuilding : MonoBehaviour
{
    private TriggerSensor orderCollision;
    private TriggerSensor getItemCollision;
    
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

    public SplineComputer connectingRoad
    {
        get { return GetComponentInChildren<SplineComputer>(); }
    }
    
    public int upgrade = 1;
    public int speed = 10;
    public int lane = 1;
    public int capacity = 10;

    void Start()
    {
        var collisions = GetComponentsInChildren<TriggerSensor>();
        orderCollision = collisions[0];
        getItemCollision = collisions[1];
        
        connectingRoadScript = GetComponentInChildren<ConnectingRoadScript>();
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        pathManager.buildingPosList.Add(position);
        
        orderCollision.OnDetected.AddListener(OnOrderCollision);
        getItemCollision.OnDetected.AddListener(OnGetItemCollision);
    }

    void OnOrderCollision(GameObject obj, Sensor sensor)
    {
        obj.GetComponent<PathFollower>().Stop();
        StartCoroutine(OrderEnd());

        IEnumerator OrderEnd()
        {
            yield return new WaitForSeconds(5f);
            obj.GetComponent<PathFollower>().SetSpeed(2.5f);
            obj.GetComponent<PathFollower>().Run();
        }
    }

    void OnGetItemCollision(GameObject obj, Sensor sensor)
    {
        obj.GetComponent<PathFollower>().Stop();
        StartCoroutine(OrderEnd());

        IEnumerator OrderEnd()
        {
            yield return new WaitForSeconds(5f);
            obj.GetComponent<PathFollower>().SetSpeed();
            obj.GetComponent<PathFollower>().Run();
        }
    }

    void Update()
    {
        
    }
}