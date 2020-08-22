using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private CreatePathManager pathManager;

    public Dictionary<CreatePathManager.ROADLANE, int> 
        remainRoadList = new Dictionary<CreatePathManager.ROADLANE, int>()
    {
        {CreatePathManager.ROADLANE.RL1, 20},
        {CreatePathManager.ROADLANE.RL2, 5}
    };

    public int remainRL1 = 0;
    public int remainRL2 = 0;

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }
    
    void Update()
    {
        remainRL1 = remainRoadList[CreatePathManager.ROADLANE.RL1];
        remainRL2 = remainRoadList[CreatePathManager.ROADLANE.RL2];
    }
}