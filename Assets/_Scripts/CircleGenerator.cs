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

    [SerializeField]
    GameObject circlePrefab;

    //LightMesh lMesh = new LightMesh();
    List<LightMesh> lMeshes = new List<LightMesh>();
    public int testIndex;

    Stack<RaycastHit> right = new Stack<RaycastHit>();
    Stack<RaycastHit> left = new Stack<RaycastHit>();
    Stack<RaycastHit> up = new Stack<RaycastHit>();
    Stack<RaycastHit> down = new Stack<RaycastHit>();
    Stack<RaycastHit> front = new Stack<RaycastHit>();
    Stack<RaycastHit> back = new Stack<RaycastHit>();

    Dictionary<int, Stack<RaycastHit>> rayDict = new Dictionary<int, Stack<RaycastHit>>();

    List<GameObject> circleObjects = new List<GameObject>();

    void Start()
    {
        //Prepare Circle
        //circle = transform.GetChild(0);
        //filter = circle.GetComponent<MeshFilter>();
        //mesh = filter.mesh = new Mesh();
        //circle.parent = null;
        //circle.rotation = Quaternion.identity;

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

        //Fill Dict
        rayDict.Add(0, right);
        rayDict.Add(1, left);
        rayDict.Add(2, up);
        rayDict.Add(3, down);
        rayDict.Add(4, front);
        rayDict.Add(5, back);
    }

    void Update()
    {
        CheckRays();

        if (currentHits > 0)
        {
            //CreateMesh();
            SeperateMeshes();
        }
        else
        {
            //filter.mesh = null;
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

    void SeperateMeshes()
    {
        foreach(GameObject c in circleObjects)
        {
            Destroy(c);
        }

        //Note: Deleting the references and creating new instances isn't optimal.
        lMeshes.Clear();

        for (int i = 0; i < 6; i++)
        {
                rayDict[i].Clear();
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
                    right.Push(h);
                }
                else if(d.x < -m)
                {
                    left.Push(h);
                }
                else if(d.y > m)
                {
                    up.Push(h);
                }
                else if(d.y < -m)
                {
                    down.Push(h);
                }
                else if(d.z > m)
                {
                    front.Push(h);
                }
                else if(d.z < -m)
                {
                    back.Push(h);
                }
                else
                {
                    print("nothing correct");
                }
            }
        }

        lMeshes.Clear();

        for (int i = 0; i < 6; i++)
        {
            if (rayDict[i].Count > 0)
            {
                print(i + ": " + rayDict[i].Count);
                CreateMesh(rayDict[i]);
            }
        }
    }

    private void CreateMesh(Stack<RaycastHit> hitStack)
    {
        //lMesh.vertices = new Vector3[currentHits + 1];        

        Transform circle = Instantiate(circlePrefab).transform;
        circleObjects.Add(circle.gameObject);

        LightMesh lMesh = new LightMesh();
        lMeshes.Add(lMesh);

        lMesh.vertices = new Vector3[hitStack.Count + 1];
        lMesh.hits = new RaycastHit[hitStack.Count + 1];

        int vIndex = 1;

        for (int i = lMesh.vertices.Length - 1; i > 0; i--)
        {
            //print(i + " " + lMesh.vertices.Length);
            lMesh.hits[i] = hitStack.Pop();

            lMesh.vertices[i] = lMesh.hits[i].point;
            //lMesh.vertices[i] = SnapToEdge(hits[i], lMesh, snapRadius) + (hits[i].normal * 0.04f);

        }

        ////Get all vertices except [0] which is for the middle vertex
        //for (int i = 0; i < rayDensity; i++)
        //{
        //    if (hasHit[i])
        //    {
        //        //lMesh.vertices[vIndex] = hits[i].point + hits[i].normal * 0.01f;

        //        //Snaps the vertices if they are very close to an edge. Then adds the hit.normal to avoid Z-Fighting
        //        lMesh.vertices[vIndex] = SnapToEdge(hits[i], lMesh, snapRadius) + (hits[i].normal * 0.04f);

        //        vIndex++;
        //    }
        //}

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
        //if (!displayGizmos) { return; }

        //foreach(LightMesh lMesh in lMeshes)
        //{
        //    Gizmos.color = Color.white;

        //    //Test all vertices
        //    for (int i = 0; i < lMesh.vertices.Length; i++)
        //    {
        //        if(lMesh.vertices[i] != null)
        //        Gizmos.DrawSphere(lMesh.vertices[i] + circle.position, 0.2f);
        //    }


        //    // Test Selected vertex
        //    Gizmos.color = Color.magenta;
        //    if (lMesh.vertices != null && testIndex < lMesh.vertices.Length)
        //    {
        //        Gizmos.DrawSphere(lMesh.vertices[testIndex] + circle.position, 0.3f);
        //    }
        //}
    }
}
