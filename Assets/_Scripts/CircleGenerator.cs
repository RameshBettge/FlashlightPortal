using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGenerator : MonoBehaviour
{
    [SerializeField]
    LayerMask mask;

    int rayDensity = 20;
    float size = 0.3f;
    float range = 15f;

    float increment;
    float radian;

    Vector3[] dir;

    RaycastHit[] hits;
    public bool[] hasHit;

    int currentHits;

    //List<Vector3> hitPoints = new List<Vector3>();

    Transform circle;
    MeshFilter filter;
    Mesh mesh;

    public Vector3[] vertices;

    public int testIndex;

    Vector3 middlePos;


    void Start()
    {
        //Prepare Circle
        circle = transform.GetChild(0);
        filter = circle.GetComponent<MeshFilter>();
        mesh = filter.mesh = new Mesh();
        circle.parent = null;
        circle.rotation = Quaternion.identity;

        //Set arrays
        hits = new RaycastHit[rayDensity];
        dir = new Vector3[rayDensity];
        hasHit = new bool[rayDensity];


        //Get ray directions
        increment = 360f / rayDensity;
        radian = increment * Mathf.Deg2Rad;
        for (int i = 0; i < rayDensity; i++)
        {
            dir[i] = new Vector3(Mathf.Sin(radian * i) * size, Mathf.Cos(radian * i) * size, 1f);
        }
    }

    void Update()
    {
        CheckRays();

        if(currentHits > 0)
        {
        CreateMesh();
        }
        else
        {
            filter.mesh = null;
        }
    }



    private void CheckRays()
    {
        currentHits = 0;

        for (int i = 0; i < rayDensity; i++)
        {
            Vector3 localDir = transform.TransformDirection(dir[i]);
            if (Physics.Raycast(transform.position, localDir * range, out hits[i], mask))
            {
                Debug.DrawRay(transform.position, localDir * hits[i].distance);
                hasHit[i] = true;
                currentHits++;
            }
            else
            {
                hasHit[i] = false;
            }
        }
    }

    private void CreateMesh()
    {
        vertices = new Vector3[currentHits + 1];
        int vIndex = 1;

        //Get all vertices except [0] which is for the middle vertex
        for (int i = 0; i < rayDensity; i++)
        {
            if (hasHit[i])
            {
                vertices[vIndex] = hits[i].point + hits[i].normal * 0.01f;
                vIndex++;
            }
        }

        //Get middle Position
        middlePos = GetMiddlePosition();


        vertices[0] = middlePos;

        //normalize the positions of the vertices. Middle = Vector3.zero. Then Move the mesh to the object hit
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= middlePos;
        }
        circle.position = middlePos;

        //Create triangles
        int triIndex = 0;
        int[] tris = new int[(vertices.Length - 1) * 3];
        for (int i = 0; i < vertices.Length - 2; i++, triIndex += 3)
        {
            tris[triIndex] = i + 2;
            tris[triIndex + 1] = 0;
            tris[triIndex + 2] = i + 1;
        }
        //create the last triangle
        tris[triIndex] = 1;
        tris[triIndex + 1] = 0;
        tris[triIndex + 2] = vertices.Length - 1;

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;

        filter.mesh = mesh;
    }

    Vector3 GetMiddlePosition()
    {
        Vector3 pos = Vector3.zero;
        RaycastHit hit = new RaycastHit();

        //Old Approach
        if (Physics.Raycast(transform.position, transform.forward * range, out hit, mask))
        {
            pos = hit.point;

            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.cyan);
        }
        else
        {
            for (int i = 1; i < vertices.Length; i++)
            {
               pos += vertices[i];
            }
            pos /= vertices.Length - 1;
        }
        return pos;


        ////Experimental Approach
        //int maxTries = 50;
        //float increment = 0.1f;

        //int dir = -1; //it always goes to the left now


        //for (int i = 0; i < maxTries; i++)
        //{
        //    Vector3 rayDir = (transform.forward * range) + new Vector3(increment * dir * i, 0f, 0f);
        //    if (!Physics.Raycast(transform.position, rayDir, out hit, mask))
        //    {
        //        return hit.point;
        //    }
        //}

        //if(hit.point == null)
        //{
        //    Debug.LogWarning("No Ray hit.");

        //    Vector3 midPos = Vector3.zero;
        //    for (int i = 1; i < vertices.Length; i++)
        //    {
        //        midPos += vertices[i];
        //    }
        //    midPos /= vertices.Length - 1;
        //    return midPos;
        //}

        
        //return hit.point;
    }

    private Vector3 GetNewMiddle(Vector3 p)
    {
        RaycastHit hit = new RaycastHit();

        Vector3 dir = (p - transform.position).normalized;

        if (Physics.Raycast(transform.position, p * range, out hit, mask))
        {
            p = hit.point;
        }

        return p;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        //Test all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i] + middlePos, 0.2f);
        }


        // Test Selected vertex
        Gizmos.color = Color.magenta;
        if (vertices != null && testIndex < vertices.Length)
        {
            Gizmos.DrawSphere(vertices[testIndex] + middlePos, 0.3f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(middlePos, 0.3f);
    }
}
