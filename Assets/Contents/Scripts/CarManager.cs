using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using Random = UnityEngine.Random;

using StartEndTuple = System.Tuple<Dreamteck.Splines.SplineComputer, Dreamteck.Splines.SplineComputer>;

public class CarManager : MonoBehaviour
{
    private PathFinder pathFinder;
    private CreatePathManager pathManager;
    
    public GameObject carPrefab;

    public List<GameObject> cars;
    
    public List<StartEndTuple> exToEnterTupleList = new List<StartEndTuple>();
    public List<StartEndTuple> exitToExTupleList = new List<StartEndTuple>();
    
    public List<PathFindData> pathFindDataList = new List<PathFindData>();
    
    public List<float> weightList;

    public List<SplineComputer> externalRoadList
    {
        get
        {
            var roads = CreatePathManager.FindAllSplineComputers(true);
            return roads.Where(road => 
                (!(road.isExitRoad || road.isEnterRoad) && 
                 (road.roadMode == SplineComputer.MODE.LAST_OPEN || road.roadMode == SplineComputer.MODE.FIRST_OPEN)))
                .ToList();
        }
    }

    public List<SplineComputer> enterRoadList
    {
        get
        {
            var roads = CreatePathManager.FindAllSplineComputers(true);
            return roads.Where(road => road.isEnterRoad).ToList();
        }
    }

    public List<SplineComputer> exitRoadList
    {
        get
        {
            var roads = CreatePathManager.FindAllSplineComputers(true);
            return roads.Where(road => road.isExitRoad).ToList();
        }
    }
    
    public GameObject Spawn()
    {
        var car = Instantiate(carPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        cars.Add(car);

        return car;
    }

    public static PathFindData WeightedRandom(List<PathFindData> inputPathFindData, List<float> inputWeightList)
    {
        var n = Random.Range(0, inputWeightList.Sum());

        var preValue = 0.0f;
        for (var index = 0; index < inputWeightList.Count; index++)
        {
            if (preValue <= n && n <= preValue + inputWeightList[index])
            {
                return inputPathFindData[index];
            }

            preValue += inputWeightList[index];
        }

        return null;
    }

    public void SetPathFindDataList()
    {
        exToEnterTupleList.Clear();
        exitToExTupleList.Clear();
        pathFindDataList.Clear();
        weightList.Clear();
        
        foreach (var externalRoad in externalRoadList)
        {
            foreach (var enterRoad in enterRoadList)
            {
                if (externalRoad != enterRoad)
                {
                    var tuple = new StartEndTuple(externalRoad, enterRoad);
                    exToEnterTupleList.Add(tuple);
                }
            }
        }

        foreach (var exitRoad in exitRoadList)
        {
            foreach (var externalRoad in externalRoadList)
            {
                if (exitRoad != externalRoad)
                {
                    var tuple = new StartEndTuple(exitRoad, externalRoad);
                    exitToExTupleList.Add(tuple);
                }
            }
        }
        
        foreach (var exToEnterTuple in exToEnterTupleList)
        {
            foreach (var exitToExTuple in exitToExTupleList)
            {
                if (exToEnterTuple.Item2.connectedBuilding.GetComponent<DTBuilding>().exitRoad == exitToExTuple.Item1)
                {
                    pathFindDataList.Add(new PathFindData(
                        exToEnterTuple, 
                        exitToExTuple, 
                        exToEnterTuple.Item2.connectedBuilding));
                }
            }
        }

        weightList = new List<float>(Enumerable.Repeat(1.0f, pathFindDataList.Count));
    }

    public static void SelectPathFindDataToCar(GameObject car, List<PathFindData> pathFindDataList, List<float> weightList)
    {
        car.GetComponent<PathFollower>().pathFindData = new PathFindData(WeightedRandom(pathFindDataList, weightList), car);
    }

    public void MoveAll()
    {
        IEnumerator MoveAllCar()
        {
            foreach (var car in cars)
            {
                car.GetComponent<PathFollower>().Run();
                yield return new WaitForSeconds(1f);
            }
        }

        StartCoroutine(MoveAllCar());
    }

    public void StopAll()
    {
        foreach (var car in cars)
        {
            car.GetComponent<PathFollower>().Stop();
        }
    }
    
    private void Start()
    {
        pathFinder = GetComponent<PathFinder>();
        pathManager = GetComponent<CreatePathManager>();
    }
}
