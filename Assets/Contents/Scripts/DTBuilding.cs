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
    private TriggerSensor globalCollision;
    
    private TextMesh indicatorText;

    public ConnectingRoadScript connectingRoadScript;
    private CreatePathManager pathManager;

    public List<GameObject> cars = new List<GameObject>();
    
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
        InitTriggers();
        
        indicatorText = GetComponentInChildren<TextMesh>();
        connectingRoadScript = GetComponentInChildren<ConnectingRoadScript>();
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        pathManager.buildingPosList.Add(position);
    }

    void InitTriggers()
    {
        var collisions = GetComponentsInChildren<TriggerSensor>();
        foreach (var collision in collisions)
        {
            switch (collision.name)
            {
                case "OrderCollision":
                    orderCollision = collision;
                    break;
                case "GetItemCollision":
                    getItemCollision = collision;
                    break;
                case "GlobalCollision":
                    globalCollision = collision;
                    break;
            }
        }
        
        orderCollision.OnDetected.AddListener(OnOrderCollision);
        getItemCollision.OnDetected.AddListener(OnGetItemCollision);
        globalCollision.OnDetected.AddListener(OnGlobalCollision);
        globalCollision.OnLostDetection.AddListener(OutGlobalCollision);
    }

    void OnOrderCollision(GameObject obj, Sensor sensor)
    {
        obj.gameObject.GetComponent<CarAI>().OrderBe();
    }

    void OnGetItemCollision(GameObject obj, Sensor sensor)
    {
        obj.gameObject.GetComponent<CarAI>().GetItemBe();
    }

    void OnGlobalCollision(GameObject obj, Sensor sensor)
    {
        cars.Add(obj.gameObject);
        obj.gameObject.GetComponent<CarAI>().EnterDT(this);
    }

    void OutGlobalCollision(GameObject obj, Sensor sensor)
    {
        cars.Remove(obj.gameObject);
        obj.gameObject.GetComponent<CarAI>().OutDT();
    }

    void UpdateIndicator()
    {
        indicatorText.text = "[Upgrade] : " + upgrade + "\n" + 
                             "[Car Count] : " + cars.Count + "\n" + 
                             "[Ordering Car] : " + cars.Count(car => car.GetComponent<CarAI>().carStat == CarAI.CARSTAT.ON_BUILDING_ORDER) + "\n" + 
                             "[Getting item Car] : " + cars.Count(car => car.GetComponent<CarAI>().carStat == CarAI.CARSTAT.ON_BUIDING_GETITEM);
    }

    void Update()
    {
        UpdateIndicator();
    }
}