using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGenerator : MonoBehaviour
{
    [SerializeField]
    LayerMask mask;

    [Header("Debugging")]
    [SerializeField]
    bool displayGizmos = true;
    [SerializeField]
    bool displayRays = true;

    int rayDensity = 500;
    float size = 0.3f;
    float range = 15f;

    float increment;
    float radian;

    Vector3[] dir;

    bool[] hasHit;
    RaycastHit[] hits;

    int currentHits;

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

        if (currentHits > 0)
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
            if (Physics.Raycast(transform.position, localDir * range, out hits[i], mask) 
                && Vector3.Dot(transform.forward, -hits[i].normal) > 0.4f) 
                //second condition avoids creating a mesh on walls which are nearly perpendicular to light direction
            {
                if (displayRays)
                {
                    Debug.DrawRay(transform.position, localDir * hits[i].distance);
                }

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

        if (Physics.Raycast(transform.position, transform.forward * range, out hit, mask))
        {
            //pos = hit.point;
            pos = GetBoundingPosition(hit);

            if (displayRays)
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.cyan);
            }
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
    }

    private Vector3 GetBoundingPosition(RaycastHit hit)
    {
        //InverseTransformPoint() should be used here instead of InverseTransformDirection.
        //-> the latter interprets the Point as a direction and only rotates it.
        //-> the latter works on the X Axis as long as the cube hit is not rotated or rotated exaxctly 180 degrees.
        //-> The first however behaves ver unexpectedly. might have to do with it taking the target's scale in consideration.

        //If I make the snapping work for one dimension at a time, I can use the hit.normal to determine which one should be used.


        Vector3 o = hit.point;

        Vector3 halfExtends = hit.collider.bounds.extents;
        float avgRadius = 0f;

        Vector3 localPoint = hit.collider.transform.InverseTransformPoint(hit.point);
        Vector3 scaledPoint = Vector3.Scale(localPoint, halfExtends);

        for (int i = 1; i < vertices.Length; i++)
        {
            avgRadius += (vertices[i] - hit.point).sqrMagnitude;
        }
        avgRadius /= vertices.Length - 1;
        avgRadius = Mathf.Sqrt(avgRadius);

        //// WARNING: ONLY FOR DEBUGGING:
        //avgRadius = 100f;
        //avgRadius *= 6;
        //// END WARNING

        //float rightDist = halfExtends.x - localPoint.x;
        //float leftDist = Mathf.Abs(-halfExtends.x - localPoint.x);

        //float rightDist2 = halfExtends.x - scaledPoint.x;
        //float leftDist2 = Mathf.Abs(-halfExtends.x - scaledPoint.x);

        float rightDist = 0.5f - localPoint.x;
        float leftDist = Mathf.Abs(-0.5f - localPoint.x);
        float xRadius = avgRadius / halfExtends.x / 2f;

        if (rightDist < xRadius && rightDist < leftDist)
        {
            localPoint.x = 0.5f;
            o = hit.collider.transform.TransformPoint(localPoint);
        }
        else if (leftDist < xRadius)
        {
            localPoint.x = -0.5f;
            o = hit.collider.transform.TransformPoint(localPoint);
        }

        float frontDist = halfExtends.z - localPoint.z;
        float backDist = Mathf.Abs(-halfExtends.z - localPoint.z);



        // Code for other two dimensions.

        //if (frontDist < avgRadius && frontDist < backDist)
        //{
        //    localPoint.z = halfExtends.z;
        //    print(halfExtends);
        //    o = hit.collider.transform.TransformPoint(localPoint);
        //}
        //else if (backDist < avgRadius)
        //{
        //    localPoint.z = -halfExtends.z;
        //    o = hit.collider.transform.TransformPoint(localPoint);
        //}


        //float topDist = halfExtends.y - localPoint.y;
        //float bottomDist = Mathf.Abs(-halfExtends.y - localPoint.y);

        //if (Input.GetKeyDown(KeyCode.D)) { Debug.Log("positiveDist = " + topDist + "; negativeDist = " + bottomDist); }

        //if (topDist < avgRadius)
        //{
        //    localPoint.y = halfExtends.y;
        //    o = hit.collider.transform.TransformDirection(localPoint);
        //}
        //else if (bottomDist < avgRadius)
        //{
        //    localPoint.y = -halfExtends.y;
        //    o = hit.collider.transform.TransformDirection(localPoint);
        //}

        return o;
    }

    private void OnDrawGizmos()
    {
        if (!displayGizmos) { return; }

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
