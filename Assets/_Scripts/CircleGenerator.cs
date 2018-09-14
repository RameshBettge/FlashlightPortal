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

    int rayDensity = 500;
    float size = 0.3f;
    float range = 15f;
    float snapRadius = 0.1f; //Creates an interesting effect if set to 1 or higher
    float minDot = 0.01f; //How much the light direction and the -hit.normal have to allign.

    float increment;
    float radian;

    Vector3[] dir;

    bool[] hasHit;
    RaycastHit[] hits;

    int currentHits;

    [SerializeField]
    GameObject circlePrefab;

    List<LightMesh> lMeshes = new List<LightMesh>();
    public int testIndex;

    // Dictionary array is used to seperate the Raycasthits. The Dictionary seperates by GameObject hit
    // and the array seperates by hit.normal
    Dictionary<GameObject, Stack<RaycastHit>>[] rayDicts = new Dictionary<GameObject, Stack<RaycastHit>>[6];

    //Could be used to avoid garbage collection by reusing objects. 
    //Creating a stack creating two stacks for inactive and active circles may be the way to go.
    List<GameObject> circleObjects = new List<GameObject>();



    void Start()
    {
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

        //Create Dicts
        for (int i = 0; i < 6; i++)
        {
            rayDicts[i] = new Dictionary<GameObject, Stack<RaycastHit>>();
        }
    }

    void Update()
    {
        CheckRays();

        if (currentHits > 0)
        {
            SeperateMeshes();
        }
    }

    private void CheckRays()
    {
        currentHits = 0;

        for (int i = 0; i < rayDensity; i++)
        {
            Vector3 localDir = transform.TransformDirection(dir[i]);
            if (Physics.Raycast(transform.position, localDir * range, out hits[i], mask)
                && Vector3.Dot(hits[i].point - transform.position, -hits[i].normal) > minDot)
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

    void SeperateMeshes()
    {
        foreach(GameObject c in circleObjects)
        {
            Destroy(c);
        }

        lMeshes.Clear();

        for (int i = 0; i < 6; i++)
        {
                rayDicts[i].Clear();
        }

        float m = 0.9f;
        for (int i = 0; i < rayDensity; i++)
        {
            if (hasHit[i])
            {
                Vector3 d = hits[i].normal;
                RaycastHit h = hits[i];
                if(d.x > m)
                {
                    SortToStack(i, 0);
                }
                else if(d.x < -m)
                {
                    SortToStack(i, 0);
                }
                else if(d.y > m)
                {
                    SortToStack(i, 0);
                }
                else if(d.y < -m)
                {
                    SortToStack(i, 0);
                }
                else if(d.z > m)
                {
                    SortToStack(i, 0);
                }
                else if(d.z < -m)
                {
                    SortToStack(i, 0);
                }
                else
                {
                    Debug.LogWarning("Unable to sort rayCastHit. " +
                        "Hit.normal is not alligned to any world axis");
                }
            }
        }

        circleObjects.Clear();
        lMeshes.Clear();

        for (int i = 0; i < 6; i++)
        {
            foreach(KeyValuePair<GameObject, Stack<RaycastHit>> pair in rayDicts[i])
            {
                CreateMesh(pair.Value);
            }
        }
    }

    void SortToStack(int hitID,int dictID)
    {
        GameObject gO = hits[hitID].collider.gameObject;
        if (!rayDicts[dictID].ContainsKey(gO))
        {
            rayDicts[dictID].Add(gO, new Stack<RaycastHit>());
        }
        
            rayDicts[dictID][gO].Push(hits[hitID]);
    }

    private void CreateMesh(Stack<RaycastHit> hitStack)
    {
        Transform circle = Instantiate(circlePrefab).transform;
        circleObjects.Add(circle.gameObject);

        LightMesh lMesh = new LightMesh();
        lMeshes.Add(lMesh);

        lMesh.vertices = new Vector3[hitStack.Count + 1];
        lMesh.hits = new RaycastHit[lMesh.vertices.Length - 1];

        int vIndex = 1;

        for (int i = lMesh.vertices.Length - 1; i > 0; i--)
        {
            int j = i - 1;
            lMesh.hits[j] = hitStack.Pop();

            lMesh.vertices[i] = SnapToEdge(lMesh.hits[j], lMesh, snapRadius)/* + (lMesh.hits[j].normal * 0.04f)*/;
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

        circle.GetComponent<MeshFilter>().mesh = lMesh.GenerateMesh();
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
        return o;
    }

    private void OnDrawGizmos()
    {
        if (!displayGizmos) { return; }

        foreach (LightMesh lM in lMeshes)
        {
            Gizmos.color = Color.white;

            //Test all vertices
            //for (int i = 0; i < lM.vertices.Length; i++)
            //{
            //    if (lM.vertices[i] != null)
            //        Gizmos.DrawSphere(lM.vertices[i] /*+ lM.middlePos*/, 0.2f);
            //}


            // Test Selected vertex
            //Gizmos.color = Color.magenta;
            //if (lM.vertices != null && testIndex < lM.vertices.Length)
            //{
            //    Gizmos.DrawSphere(lM.vertices[testIndex] + lM.middlePos, 5f);
            //}
        }
    }
}
