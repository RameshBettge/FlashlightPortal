using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynchCams : MonoBehaviour
{


    [SerializeField]
    Transform secundaryCam;

    [SerializeField]
    Vector3 offset = Vector3.right * 500f;

    void Update()
    {
        secundaryCam.position = transform.position + offset;
        secundaryCam.rotation = transform.rotation;
    }
}
