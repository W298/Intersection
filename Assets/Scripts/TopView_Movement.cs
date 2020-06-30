using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopView_Movement : MonoBehaviour
{
    private float zoom;

    private Transform tr;
    private Camera cm;

    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        cm = GetComponentInChildren<Camera>();

        cm.transform.rotation = Quaternion.Euler(90, 0, 0);
        cm.orthographic = false;
    }

    // Update is called once per frame
    void Update()
    {
        zoom = Mathf.Lerp(zoom, Input.GetAxis("Zoom"), Time.deltaTime * 10);

        Vector3 dir = Vector3.forward * Input.GetAxis("Vertical") + 
                      Vector3.right * Input.GetAxis("Horizontal");
        tr.Translate(dir * 20 * Time.deltaTime, Space.Self);
    }
}
