using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using Random = UnityEngine.Random;

public class CarManager : MonoBehaviour
{
    private PathFinder pathFinder;
    public GameObject carPrefab;

    public List<GameObject> cars;
    
    public GameObject Spawn()
    {
        var car = Instantiate(carPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        cars.Add(car);

        return car;
    }

    public void SetPathList(GameObject car, List<List<SplineComputer>> pathList)
    {
        car.GetComponent<PathFollower>().pathList = pathList;
    }

    public void FindAndSetPath()
    {
        var roads = GameObject.FindObjectsOfType<SplineComputer>().ToList();
        var sel = roads.Where(road => road.roadMode == SplineComputer.MODE.LAST_OPEN || road.roadMode == SplineComputer.MODE.FIRST_OPEN).ToList();

        foreach (var car in cars)
        {
            var n1 = Random.Range(0, sel.Count);
            var n2 = Random.Range(0, sel.Count);

            while (n1 == n2)
            {
                n2 = Random.Range(0, sel.Count);
            }
            
            var pathList = pathFinder.Run(sel[n1], sel[n2]);
            SetPathList(car, pathList);
        }
    }

    private void Start()
    {
        pathFinder = GetComponent<PathFinder>();
    }
}
