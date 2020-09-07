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

    [Category("Path Manager")]
    public void ResetAllMeshClip()
    {
        foreach (var spline in GameObject.FindObjectsOfType<SplineComputer>())
        {
            pathManager.ResetMeshClip(spline);
        }
    }

    [Category("Path Manager")]
    public float divider_RL1
    {
        get { return pathManager.dividerList[0]; }
        set { pathManager.dividerList[0] = value; }
    }

    [Category("Path Manager")]
    public float divider_RL2
    {
        get { return pathManager.dividerList[1]; }
        set { pathManager.dividerList[1] = value; }
    }

    [Category("Path Manager")]
    public float divider_RL05
    {
        get { return pathManager.dividerList[4]; }
        set { pathManager.dividerList[4] = value; }
    }

    [Category("Path Manager")]
    public float changer
    {
        get { return pathManager.changer; }
        set { pathManager.changer = value; }
    }

    [Category("Path Follower")]
    public void Move()
    {
        pathManager.car.GetComponent<PathFollower>().Run();
    }
    
    [Category("Path Follower")]
    public void Stop()
    {
        pathManager.car.GetComponent<PathFollower>().Stop();
    }
    
    [Category("Path Follower")]
    public void Reset()
    {
        pathManager.car.GetComponent<PathFollower>().Reset();
    }

    [Category("Path Follower")]
    public void SetMoveDirStraight()
    {
        pathManager.car.GetComponent<PathFollower>().setMoveDir(true);
    }
    
    [Category("Path Follower")]
    public void SetMoveDirReverse()
    {
        pathManager.car.GetComponent<PathFollower>().setMoveDir(false);
    }
    
    [Category("Path Follower")]
    public void SetSpline()
    {
        var roads = GameObject.FindObjectsOfType<SplineComputer>().ToList();
        pathManager.car.GetComponent<PathFollower>().setPath(roads, 1);
    }
}
