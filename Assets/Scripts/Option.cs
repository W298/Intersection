using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Option : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SROptions.Current.pathManager = GetComponent<CreatePathManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public partial class SROptions
{
    public CreatePathManager pathManager;

    [Category("PathManager")]
    public void ResetMeshClip()
    {
        foreach (var spline in GameObject.FindObjectsOfType<SplineComputer>())
        {
            pathManager.ResetMeshClip(spline);
        }
    }
}
