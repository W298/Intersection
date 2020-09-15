using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;

public class Building : MonoBehaviour
{
    private CreatePathManager pathManager;
    public Vector3 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public SplineComputer enterRoad
    {
        get
        {
            return FindObjectsOfType<SplineComputer>().FirstOrDefault(road => road.isEnterRoad);
        }
    }

    public SplineComputer exitRoad
    {
        get
        {
            return FindObjectsOfType<SplineComputer>().FirstOrDefault(road => road.isExitRoad);
        }
    }

    public int capacity = 10;
    public int upgrade = 0;

    void Start()
    {
        pathManager = GameObject.FindGameObjectWithTag("Player").GetComponent<CreatePathManager>();
    }

    void Update()
    {
        UnityEngine.Debug.LogWarning(enterRoad == null);
    }
}