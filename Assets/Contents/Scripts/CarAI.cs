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
            yield return new WaitForSeconds(5f);
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
            yield return new WaitForSeconds(5f);
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
        if (carSensor.DetectedObjects.Count != 0)
        {
            pathFollower.Stop();
        }
    }

    void OnLost(GameObject obj, Sensor sensor)
    {
        if (carSensor.DetectedObjects.Count == 0)
        {
            pathFollower.Run();
        }
    }

    void Update()
    {
        indicator.text = carStat.ToString();
    }
}
