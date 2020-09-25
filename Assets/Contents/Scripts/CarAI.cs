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
    public TriggerSensor sensor;

    public void RunDTBehavior(DTBuilding dt)
    {
        UnityEngine.Debug.LogWarning(dt.name);
    }

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        sensor = GetComponentInChildren<TriggerSensor>();
        
        sensor.OnDetected.AddListener(OnDetected);
        sensor.OnLostDetection.AddListener(OnLost);
    }

    void OnDetected(GameObject obj, Sensor sensor)
    {
        if (this.sensor.DetectedObjects.Count != 0)
        {
            pathFollower.Stop();
        }
    }

    void OnLost(GameObject obj, Sensor sensor)
    {
        if (this.sensor.DetectedObjects.Count == 0)
        {
            pathFollower.Run();
        }
    }

    void Update()
    {
        
    }
}
