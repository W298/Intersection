using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private CreatePathManager pathManager;

    public int remainRoad = 10;

    void Start()
    {
        pathManager = GetComponent<CreatePathManager>();
    }
    
    void Update()
    {
        
    }
}
