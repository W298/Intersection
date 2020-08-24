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
            {CreatePathManager.ROADLANE.RL1, 20},
            {CreatePathManager.ROADLANE.RL2, 5}
        };

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }

    void Update()
    {
        UnityEngine.Debug.LogWarning(remainRoadList[CreatePathManager.ROADLANE.RL2]);
    }
}