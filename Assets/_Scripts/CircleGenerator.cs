using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGenerator : MonoBehaviour
{
    [SerializeField]
    LayerMask mask;

    int rayDensity = 36;
    float size = 0.2f;
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

    Vector3 testOffset;

    Vector3[] vertices;

    public int testIndex;

    Vector3 middlePos;

    void Start()
    {
        circle = transform.GetChild(0);
        filter = circle.GetComponent<MeshFilter>();
        mesh = filter.mesh = new Mesh();

        increment = 360 / rayDensity;
        radian = increment * Mathf.Deg2Rad;

        hits = new RaycastHit[rayDensity];
        dir = new Vector3[rayDensity];
        hasHit = new bool[rayDensity];

        //Get ray directions
        for (int i = 0; i < rayDensity; i++)
        {
            dir[i] = new Vector3(Mathf.Sin(radian * i) * size, Mathf.Cos(radian * i) * size, 1f);
        }
    }

    void Update()
    {
        CheckRays();
        CreateMesh();
    }



    private void CheckRays()
    {
        currentHits = 0;

        for (int i = 0; i < rayDensity; i++)
        {
            Vector3 localDir = transform.TransformDirection(dir[i]);
            Debug.DrawRay(transform.position, localDir * range);
            if (Physics.Raycast(transform.position, localDir * range, out hits[i], mask))
            {
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
        //testOffset = -transform.forward * 0.1f;
        testOffset = Vector3.zero;

        vertices = new Vector3[currentHits + 1];
        int vIndex = 1;

        //Get all vertices except [0] which is for the middle vertex
        for (int i = 0; i < vertices.Length - 1; i++)
        {
            if (hasHit[i])
            {
                vertices[vIndex] = hits[i].point + testOffset + hits[i].normal * 0.01f ;
                vIndex++;
            }
        }

        //Get the middlePosition
        for (int i = 1; i < vertices.Length; i++)
        {
            middlePos += vertices[i];
        }

        //middlePos /= vertices.Length;

        middlePos = GetNewMiddle(middlePos);

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
        tris[triIndex] = 1;
        tris[triIndex + 1] = 0;
        tris[triIndex + 2] = vertices.Length - 1;

        //create triangle out of first, middle and last(which is one before middle)
        mesh.vertices = vertices;
        mesh.triangles = tris;
        filter.mesh = mesh;
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

        //for (int i = 0; i < hits.Length; i++)
        //{
        //    if(hasHit[i])
        //    {
        //    Gizmos.DrawSphere(hits[i].point, 0.1f);
        //    }
        //}

        //for (int i = 0; i < vertices.Length; i++)
        //{
        if (vertices != null && testIndex < vertices.Length)
            Gizmos.DrawSphere(vertices[testIndex] + middlePos, 0.1f);

        //}
    }
}
