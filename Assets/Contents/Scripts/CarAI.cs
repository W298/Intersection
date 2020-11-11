using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;
using UnityEngine.Events;

public class CarAI : MonoBehaviour
{
    public PathFollower pathFollower;
    public TriggerSensor carSensor;

    public void RunDTBehavior(DTBuilding dt)
    {
        Debug.LogWarning(gameObject.name + " is now in " + dt.gameObject.name);
    }

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        carSensor = GetComponentInChildren<TriggerSensor>();
        
        carSensor.OnDetected.AddListener(OnDetected);
        carSensor.OnLostDetection.AddListener(OnLost);
    }

    void OnDetected(GameObject obj, Sensor sensor)
    {
        if (this.carSensor.DetectedObjects.Count != 0)
        {
            pathFollower.Stop();
        }
    }

    void OnLost(GameObject obj, Sensor sensor)
    {
        if (this.carSensor.DetectedObjects.Count == 0)
        {
            pathFollower.Run();
        }
    }

    void Update()
    {
        
    }
}
