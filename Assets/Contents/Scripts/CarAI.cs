using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;
using UnityEngine.Events;

public class CarAI : MonoBehaviour
{
    public enum CARSTAT {ON_ROAD, ON_BUILDING_NONE, ON_BUILDING_ORDER, ON_BUIDING_GETITEM}
    public CARSTAT carStat = CARSTAT.ON_ROAD;

    public DTBuilding dtBuilding;
    public PathFollower pathFollower;
    public TriggerSensor carSensor;
    public TextMesh indicator;

    private bool isWaiting = false;

    public void EnterDT(DTBuilding dt)
    {
        dtBuilding = dt;
        carStat = CARSTAT.ON_BUILDING_NONE;
    }

    public void OutDT()
    {
        dtBuilding = null;
        carStat = CARSTAT.ON_ROAD;
    }

    public void OrderBe()
    {
        carStat = CARSTAT.ON_BUILDING_ORDER;
        pathFollower.Stop();

        StartCoroutine(_End());
        IEnumerator _End()
        {
            isWaiting = true;
            yield return new WaitForSeconds(5f);
            isWaiting = false;
            
            pathFollower.SetSpeed(2.5f);
            pathFollower.Run();
        }
    }

    public void GetItemBe()
    {
        carStat = CARSTAT.ON_BUIDING_GETITEM;
        pathFollower.Stop();
        
        StartCoroutine(_End());
        IEnumerator _End()
        {
            isWaiting = true;
            yield return new WaitForSeconds(5f);
            isWaiting = false;
            
            pathFollower.SetSpeed();
            pathFollower.Run();
        }
    }

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        carSensor = GetComponentInChildren<TriggerSensor>();
        indicator = GetComponentInChildren<TextMesh>();
        
        carSensor.OnDetected.AddListener(OnDetected);
        carSensor.OnLostDetection.AddListener(OnLost);
    }

    void OnDetected(GameObject obj, Sensor sensor)
    {
        if (!IsCarSameWay(obj))
        {
            if (CheckWhoGoFirst(obj))
            {
                pathFollower.Run();
            }
            else
            {
                obj.GetComponent<PathFollower>().Run();
            }
        }
        
        pathFollower.Stop();
    }

    bool IsCarSameWay(GameObject obj)
    {
        if (pathFollower.splineFollower.spline == obj.GetComponent<PathFollower>().splineFollower.spline)
            return true;
        
        var a = Vector3.Angle(gameObject.transform.forward, obj.transform.forward) <= 30;
        return a;
    }

    bool CheckWhoGoFirst(GameObject obj)
    {
        // True - I go
        // False - You go

        var r = Random.Range(0, 2);
        return r != 0;
    }

    void OnLost(GameObject obj, Sensor sensor)
    {
        if (carSensor.DetectedObjects.Count == 0 && !isWaiting)
        {
            pathFollower.Run();
        }
    }

    void Update()
    {
        // indicator.text = carStat.ToString();

        if (carSensor.DetectedObjects.Count != 0)
        {
            if (carSensor.DetectedObjects[0] == gameObject && CheckWhoGoFirst(carSensor.DetectedObjects[0]))
            {
                pathFollower.Run();
                indicator.text = "I GO";
            }
            else
            {
                carSensor.DetectedObjects[0].GetComponent<PathFollower>().Run();
                indicator.text = "You GO";
            }
        }
    }
}
