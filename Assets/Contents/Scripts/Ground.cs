using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ground : MonoBehaviour
{
    private CreatePathManager pathManager;
    
    public Tuple<Vector3, Vector3> boundaryPoint;
    public List<Vector3> externalPoint;
    void Start()
    {
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
        
        var plus = new Vector3(transform.position.x + (transform.localScale.x) / 2, 0,
            transform.position.z + (transform.localScale.z) / 2);
        var minus = new Vector3(transform.position.x - (transform.localScale.x) / 2, 0,
            transform.position.z - (transform.localScale.z) / 2);
        
        boundaryPoint = new Tuple<Vector3, Vector3>(plus, minus);
        
        CreateExternalPoint(5);
    }

    Vector3 GetRandomPoint()
    {
        var x = Random.Range(boundaryPoint.Item2.x, boundaryPoint.Item1.x);
        var z = Random.Range(boundaryPoint.Item2.z, boundaryPoint.Item1.z);
        
        return new Vector3(x, 0, z);
    }

    Vector3 GetRandomBoundaryPoint()
    {
        var d = Random.Range(0, 3);
        switch (d)
        {
            case 0:
                return new Vector3(Random.Range(boundaryPoint.Item2.x, boundaryPoint.Item1.x), 0, boundaryPoint.Item1.z);
            case 1:
                return new Vector3(Random.Range(boundaryPoint.Item2.x, boundaryPoint.Item1.x), 0, boundaryPoint.Item2.z);
            case 2:
                return new Vector3(boundaryPoint.Item1.x, 0, Random.Range(boundaryPoint.Item2.z, boundaryPoint.Item1.z));
            case 3:
                return new Vector3(boundaryPoint.Item2.x, 0, Random.Range(boundaryPoint.Item2.z, boundaryPoint.Item1.z));
            default:
                return new Vector3();
        }
    }

    void CreateExternalPoint(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var point = GetRandomBoundaryPoint();
            point = new Vector3(CreatePathManager.SnapGrid(point.x, 10), 0, CreatePathManager.SnapGrid(point.z, 10));
            externalPoint.Add(point);
            Instantiate(pathManager.debugobj, point, Quaternion.identity);
        }
    }
    
    void Update()
    {
        
    }
}
