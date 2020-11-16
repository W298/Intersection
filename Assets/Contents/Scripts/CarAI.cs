using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using SensorToolkit;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class CarAI : MonoBehaviour
{
    public enum CARSTAT {ON_ROAD, ON_BUILDING_NONE, ON_BUILDING_ORDER, ON_BUIDING_GETITEM}
    public CARSTAT carStat = CARSTAT.ON_ROAD;

    public DTBuilding dtBuilding;
    public PathFollower pathFollower;
    public TriggerSensor carSensor;
    public TextMesh indicator;

    public List<GameObject> excludeCars = new List<GameObject>();
    public List<GameObject> detectedCars => carSensor.DetectedObjects.Except(excludeCars).ToList();

    public GameObject detected_ClosestCar
    {
        get
        {
            GameObject closestCar;
            var distAry = new float[detectedCars.Count];
            var counter = 0;
            
            foreach (var car in detectedCars)
            {
                var dist = Vector3.Distance(this.gameObject.transform.position, car.transform.position);

                distAry[counter] = dist;
                counter++;
            }
            
            var minIndex = Array.IndexOf(distAry, distAry.Min());
            closestCar = detectedCars[minIndex];

            return closestCar;
        }
    }

    private bool isWaiting = false;

    public void AddExcludeCar(GameObject car)
    {
        if (!excludeCars.Contains(car))
        {
            excludeCars.Add(car);
            CheckCanGo();
        }
    }

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
        
        // carSensor.OnDetected.AddListener(OnDetected);
        carSensor.OnLostDetection.AddListener(OnLost);
    }

    /*
    void OnDetected(GameObject obj, Sensor sensor)
    {
        if (!IsCarSameWay(obj))
        {
            if (CheckWhoGoFirst(obj))
            {
                AddExcludeCar(obj);
            }
            else
            {
                obj.GetComponent<CarAI>().AddExcludeCar(this.gameObject);
            }
        }
        
        pathFollower.Stop();
    }
    */

    bool IsCarSameWay(GameObject obj)
    {
        if (pathFollower.splineFollower.spline == obj.GetComponent<PathFollower>().splineFollower.spline &&
            pathFollower.splineFollower.direction == obj.GetComponent<PathFollower>().splineFollower.direction)
            return true;

        var a = Vector3.Angle(gameObject.transform.forward, obj.transform.forward);
        indicator.text = a.ToString();
        return a <= 70;
    }

    bool CheckWhoGoFirst(GameObject obj)
    {
        // True - I go
        // False - You go

        var thisPer = pathFollower.splineFollower.spline.Project(this.gameObject.transform.position).percent;
        var oppPer = obj.GetComponent<PathFollower>().splineFollower.spline.Project(obj.transform.position).percent;

        return thisPer >= oppPer;
    }

    void OnLost(GameObject obj, Sensor sensor)
    {
        CheckCanGo();

        if (excludeCars.Contains(obj))
        {
            excludeCars.Remove(obj);
        }
    }

    bool CheckCanGo()
    {
        if (detectedCars.Count == 0 && !isWaiting)
        {
            pathFollower.Run();
            return true;
        }

        return false;
    }

    void Update()
    {
        if (detectedCars.Count != 0)
        {
            if (!excludeCars.Contains(detected_ClosestCar))
            {
                if (!IsCarSameWay(detected_ClosestCar))
                {
                    if (detected_ClosestCar.GetComponent<CarAI>().detected_ClosestCar == gameObject)
                    {
                        if (CheckWhoGoFirst(detected_ClosestCar))
                        {
                            AddExcludeCar(detected_ClosestCar);
                        }
                        else
                        {
                            detected_ClosestCar.GetComponent<CarAI>().AddExcludeCar(this.gameObject);
                        }
                    }
                    else
                    {
                        pathFollower.Stop();
                    }
                }
                else
                {
                    pathFollower.Stop();
                }
            }
            else
            {
                pathFollower.Stop();
            }
        }
    }
}
