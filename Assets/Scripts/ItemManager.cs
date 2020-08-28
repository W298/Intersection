using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private CreatePathManager pathManager;

    public Dictionary<CreatePathManager.ROADLANE, int>
        remainRoadList = new Dictionary<CreatePathManager.ROADLANE, int>()
        {
            {CreatePathManager.ROADLANE.RL1, 100},
            {CreatePathManager.ROADLANE.RL2, 50}
        };

    public int RL1;
    public int RL2;

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }

    void Update()
    {
        RL1 = remainRoadList[CreatePathManager.ROADLANE.RL1];
        RL2 = remainRoadList[CreatePathManager.ROADLANE.RL2];
    }
}