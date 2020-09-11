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
    private CreatePathManager pathManager;
    
    public GameObject carPrefab;

    public List<GameObject> cars;
    public List<Tuple<SplineComputer, SplineComputer>> roadTuple = new List<Tuple<SplineComputer, SplineComputer>>();
    public List<float> weightList;
    
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

    public Tuple<SplineComputer, SplineComputer> WeightedRandom(List<Tuple<SplineComputer, SplineComputer>> _roadTuple, List<float> weightList)
    {
        var n = Random.Range(0, weightList.Sum());

        var preValue = 0.0f;
        for (var index = 0; index < weightList.Count; index++)
        {
            if (preValue <= n && n <= preValue + weightList[index])
            {
                return _roadTuple[index];
            }

            preValue += weightList[index];
        }

        return null;
    }

    public void Prepare()
    {
        var roads = GameObject.FindObjectsOfType<SplineComputer>().ToList();
        var sel = roads.Where(road => road.roadMode == SplineComputer.MODE.LAST_OPEN || road.roadMode == SplineComputer.MODE.FIRST_OPEN).ToList();

        foreach (var d in sel)
        {
            foreach (var a in sel)
            {
                if (a != d)
                {
                    var t = new Tuple<SplineComputer, SplineComputer>(d, a);
                    roadTuple.Add(t);
                }
            }
        }
        
        weightList = new List<float>(Enumerable.Repeat(1.0f, roadTuple.Count));
    }

    public void FindAndSetPath()
    {
        foreach (var car in cars)
        {
            var selectedTuple = WeightedRandom(roadTuple, weightList);

            var pathList = pathFinder.Run(selectedTuple.Item1, selectedTuple.Item2);
            SetPathList(car, pathList);
        }

        for (var i = 0; i < roadTuple.Count; i++)
        {
            var str = i + " weight : " + weightList[i];
            pathManager.LogTextOnPos(str, pathManager.GetSplinePosition(roadTuple[i].Item1), true, false);
        }
    }

    public void MoveAll()
    {
        IEnumerator MoveAllCar()
        {
            foreach (var car in cars)
            {
                car.GetComponent<PathFollower>().Run();
                yield return new WaitForSeconds(0.5f);
            }
        }

        StartCoroutine(MoveAllCar());
    }
    
    private void Start()
    {
        pathFinder = GetComponent<PathFinder>();
        pathManager = GetComponent<CreatePathManager>();
    }
}
