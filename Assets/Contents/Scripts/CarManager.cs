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
        
        var externalRoadList = roads.Where(road => 
            (!(road.isExitRoad || road.isEnterRoad) && 
             (road.roadMode == SplineComputer.MODE.LAST_OPEN || road.roadMode == SplineComputer.MODE.FIRST_OPEN))).ToList();
        var enterRoadList = roads.Where(road => road.isEnterRoad).ToList();
        var exitRoadList = roads.Where(road => road.isExitRoad).ToList();

        foreach (var externalRoad in externalRoadList)
        {
            foreach (var enterRoad in enterRoadList)
            {
                var tuple = new Tuple<SplineComputer, SplineComputer>(externalRoad, enterRoad);
                roadTuple.Add(tuple);
            }

            foreach (var exitRoad in exitRoadList)
            {
                var tuple = new Tuple<SplineComputer, SplineComputer>(exitRoad, externalRoad);
                roadTuple.Add(tuple);
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
    }

    public void MoveAll()
    {
        var groupList = cars.GroupBy(car => car.GetComponent<PathFollower>().path).ToList();
        
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
