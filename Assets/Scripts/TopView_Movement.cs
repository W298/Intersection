using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopView_Movement : MonoBehaviour
{
    public float zoom_speed = 5000;
    public float move_speed = 50;

    private float zoom;

    private Transform tr;
    private Camera cm;

    void Start()
    {
        tr = GetComponent<Transform>();
        cm = GetComponentInChildren<Camera>();

        cm.transform.rotation = Quaternion.Euler(90, 0, 0);
        cm.orthographic = false;
    }

    void Update()
    {
        zoom = Mathf.Lerp(zoom, Input.GetAxis("Zoom"), Time.deltaTime * 10);

        Vector3 dir = Vector3.forward * Input.GetAxis("Vertical") + 
                      Vector3.right * Input.GetAxis("Horizontal");
        tr.Translate(dir * move_speed * Time.deltaTime, Space.Self);

        tr.Translate(Vector3.up * zoom * zoom_speed * Time.deltaTime, Space.Self);
    }
}
