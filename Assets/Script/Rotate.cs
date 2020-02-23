using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float RotateAngle;
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, RotateAngle * Time.deltaTime);
    }
}
