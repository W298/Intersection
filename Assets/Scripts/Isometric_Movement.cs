using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using UnityEngine;

public class Isometric_Movement : MonoBehaviour
{
    private Vector3 front_vector = Vector3.forward + Vector3.right;
    private Vector3 right_vector = Vector3.right - Vector3.forward;

    private Vector3 movedir;
    private float zoom;

    public float camera_move_speed = 5.0f;
    public float camera_zoom_speed = 1.5f;
    public float camera_zoom_breaker = 10.0f;

    private Transform tr;
    private Camera cm;
    
    void Start()
    {
        UnityEngine.Debug.Log("Movement Script Init Complete");
        tr = GetComponent<Transform>();
        cm = GetComponentInChildren<Camera>();

        cm.transform.rotation = Quaternion.Euler(45, 45, 0);
        cm.orthographic = true;
    }

    void Update()
    {
        zoom = Mathf.Lerp(zoom, Input.GetAxis("Zoom"), 
            Time.deltaTime * camera_zoom_breaker);

        movedir = 
            front_vector * Input.GetAxis("Vertical") + 
            right_vector * Input.GetAxis("Horizontal");

        tr.Translate(movedir * camera_move_speed * Time.deltaTime, Space.Self);

        cm.orthographicSize += zoom * camera_zoom_speed;
        if (cm.orthographicSize < 2)
        {
            cm.orthographicSize = 2;
        }
        else if (cm.orthographicSize > 10)
        {
            cm.orthographicSize = 10;
        }
    }
}
