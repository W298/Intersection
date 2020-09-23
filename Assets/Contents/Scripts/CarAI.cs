using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    private CarManager carManager;
    private PathFinder pathFinder;
    private PathFollower pathFollower;
    
    private DTBuilding dtBuilding;
    public void RunDTBehavior(DTBuilding dt)
    {
        /*
        dtBuilding = dt;

        var connetingSpline = dtBuilding.connectingRoadScript.connectingSpline;
        
        StartCoroutine(AfterWait());

        IEnumerator AfterWait()
        {
            yield return new WaitForSeconds(2f);
            var exitTupleList = carManager.roadTuple.Where(tuple => tuple.Item1 == dtBuilding.exitRoad).ToList();
            var selectedTuple = carManager.WeightedRandom
                (exitTupleList, new List<float>(Enumerable.Repeat<float>(1f, exitTupleList.Count)));

            var pathList = pathFinder.Run(selectedTuple.Item1, selectedTuple.Item2);

            carManager.SetPathList(this.gameObject, pathList);
            pathFollower.selectPath();
            pathFollower.Run();
            pathFollower.Reset();
        }
        */
        
    }
    
    void Start()
    {
        carManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CarManager>();
        pathFinder = GameObject.FindGameObjectWithTag("Player").GetComponent<PathFinder>();
        pathFollower = GetComponent<PathFollower>();
    }
    
    void Update()
    {
        
    }
}
