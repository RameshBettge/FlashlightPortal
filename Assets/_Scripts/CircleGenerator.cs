using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ideas:
// - seperate mesh into multiple meshes based on matching normals. Create midle vertex for each.
// - snap every vertex to very close edges

public class CircleGenerator : MonoBehaviour
{
    [SerializeField]
    LayerMask mask;

    [Header("Debugging")]
    [SerializeField]
    bool displayGizmos = true;
    [SerializeField]
    bool displayRays = true;

    int rayDensity = 150;
    float size = 0.3f;
    float range = 15f;
    float snapRadius = 0.5f; //Creates an interesting effect if set to 1 or higher

    float increment;
    float radian;

    Vector3[] dir;

    bool[] hasHit;
    RaycastHit[] hits;

    int currentHits;

    Transform circle;
    MeshFilter filter;
    Mesh mesh;

    LightMesh lMesh = new LightMesh();

    public int testIndex;

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
               /* && Vector3.Dot(transform.forward, -hits[i].normal) > 0.4f*/)
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
        lMesh.vertices = new Vector3[currentHits + 1];
        lMesh.vertices = new Vector3[currentHits + 1];
        int vIndex = 1;

        //Get all vertices except [0] which is for the middle vertex
        for (int i = 0; i < rayDensity; i++)
        {
            if (hasHit[i])
            {
                //lMesh.vertices[vIndex] = hits[i].point + hits[i].normal * 0.01f;

                //Snaps the vertices if they are very close to an edge. Then adds the hit.normal to avoid Z-Fighting
                lMesh.vertices[vIndex] = SnapToEdge(hits[i], lMesh, snapRadius) + (hits[i].normal * 0.04f);

                vIndex++;
            }
        }

        //Get middle Position
        lMesh.middlePos = GetMiddlePosition(lMesh);
        lMesh.vertices[0] = lMesh.middlePos;

        //normalize the positions of the vertices. Middle = Vector3.zero. Then Move the mesh to the object hit
        for (int i = 0; i < lMesh.vertices.Length; i++)
        {
            lMesh.vertices[i] -= lMesh.middlePos;
        }

        circle.position = lMesh.middlePos;


        filter.mesh = lMesh.GenerateMesh();
    }

    Vector3 GetMiddlePosition(LightMesh lM)
    {
        Vector3 pos = Vector3.zero;
        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(transform.position, transform.forward * range, out hit, mask))
        {
            //pos = hit.point;
            pos = GetBoundingPosition(hit, lM);

            if (displayRays)
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.cyan);
            }
        }
        else
        {
            for (int i = 1; i < lM.vertices.Length; i++)
            {
                pos += lM.vertices[i];
            }
            pos /= lM.vertices.Length - 1;

            if (Physics.Raycast(transform.position, pos - transform.position, out hit, mask))
            {
                pos = GetBoundingPosition(hit, lM, true);
            }
        }

        return pos;
    }

    private Vector3 GetBoundingPosition(RaycastHit hit, LightMesh lM, bool setRadius = false)
    {
        float avgRadius = 0f;

        if (setRadius)
        {
            avgRadius = 5f;
        }
        else
        {
            for (int i = 1; i < lM.vertices.Length; i++)
            {
                avgRadius += (lM.vertices[i] - hit.point).sqrMagnitude;
            }
            avgRadius /= lM.vertices.Length - 1;
            avgRadius = Mathf.Sqrt(avgRadius);
        }

        //Increases the radius. Can lead to unintended snapping but prevents the circle from missing pieces.
        avgRadius *= 1.05f;

        Vector3 o = SnapToEdge(hit, lM, avgRadius);

        //Clamp the middle pos on each axis.
        lM.SetMinMax();
        if (o.x > lM.maxPos.x)
        {
            o.x = lM.maxPos.x;
        }
        else if (o.x < lM.minPos.x)
        {
            o.x = lM.minPos.x;
        }

        if (o.y > lM.maxPos.y)
        {
            o.y = lM.maxPos.y;
        }
        else if (o.y < lM.minPos.y)
        {
            o.y = lM.minPos.y;
        }

        if (o.z > lM.maxPos.z)
        {
            o.z = lM.maxPos.z;
        }
        else if (o.z < lM.minPos.z)
        {
            o.z = lM.minPos.z;
        }

        // Cast a ray to hit edge and find normal
        //Vector3 dir = (o - transform.position) * 1.01f;
        //if (Physics.Raycast(transform.position, dir, out hit, mask))
        //{
        //o += hit.normal * 0.01f;
        //}

        return o;
    }

    private Vector3 SnapToEdge(RaycastHit hit, LightMesh lM, float radius)
    {
        Vector3 o = hit.point;
        Vector3 otherPos = hit.collider.transform.position;

        Vector3 halfExtends = hit.collider.bounds.extents;

        //Scaling the avgRadius to avoid snapping to points outside the nearer vertices
        //avgRadius *= 0.9f;

        //// END WARNING
        Vector3 posDistance = new Vector3(
          otherPos.x + halfExtends.x - o.x,
          otherPos.y + halfExtends.y - o.y,
          otherPos.z + halfExtends.z - o.z
          );

        Vector3 negDistance = new Vector3(
          Mathf.Abs(otherPos.x - halfExtends.x - o.x),
          Mathf.Abs(otherPos.y - halfExtends.y - o.y),
          Mathf.Abs(otherPos.z - halfExtends.z - o.z)
          );


        if (posDistance.x < radius && posDistance.x < negDistance.x)
        {
            o.x = otherPos.x + halfExtends.x;
        }
        else if (negDistance.x < radius)
        {
            o.x = otherPos.x - halfExtends.x;
        }

        if (posDistance.y < radius && posDistance.y < negDistance.y)
        {
            o.y = otherPos.y + halfExtends.y;
        }
        else if (negDistance.y < radius)
        {
            o.y = otherPos.y - halfExtends.y;
        }

        if (posDistance.z < radius && posDistance.z < negDistance.z)
        {
            o.z = otherPos.z + halfExtends.z;

        }
        else if (negDistance.z < radius)
        {
            o.z = otherPos.z - halfExtends.z;
        }

        // Cast a ray to hit edge and find normal
        //Vector3 dir = (o - transform.position) * 1.01f;
        //if (Physics.Raycast(transform.position, dir, out hit, mask))
        //{
        //o += hit.normal * 0.01f;
        //}

        return o;
    }

    private void OnDrawGizmos()
    {
        if (!displayGizmos) { return; }

        Gizmos.color = Color.white;

        //Test all vertices
        for (int i = 0; i < lMesh.vertices.Length; i++)
        {
            Gizmos.DrawSphere(lMesh.vertices[i] + circle.position, 0.2f);
        }


        // Test Selected vertex
        Gizmos.color = Color.magenta;
        if (lMesh.vertices != null && testIndex < lMesh.vertices.Length)
        {
            Gizmos.DrawSphere(lMesh.vertices[testIndex] + circle.position, 0.3f);
        }
    }
}
