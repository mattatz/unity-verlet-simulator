using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet.Demo
{
    public class Tentacles : MonoBehaviour {

        [SerializeField] protected int tentaclesCount = 64, divisionsCount = 32;
        [SerializeField] protected int radialSegments = 8;

        [SerializeField] protected float edgeLength = 0.5f;

        [SerializeField] protected ComputeShader verletCompute, tentacleCompute;
        [SerializeField, Range(2, 16)] protected int iterations = 4;
        [SerializeField, Range(0.5f, 1f)] protected float decay = 1f;
        [SerializeField] protected Vector3 gravity;
        [SerializeField] protected Material tipMaterial, tentacleMaterial;
        [SerializeField] protected Bounds bounds;

        protected GPUVerletSimulator simulator;

        protected Mesh tipMesh, tentacleMesh;
        protected ComputeBuffer tipArgsBuffer, tentacleArgsBuffer;

        [SerializeField] protected float flowScale = 0.5f, flowIntensity = 0.25f;

        protected void Start () {
            var nodes = new GPUNode[tentaclesCount * divisionsCount];
            var edges = new List<GPUEdge>();

            for(int y = 0; y < tentaclesCount; y++)
            {
                var yoff = y * divisionsCount;
                for(int x = 0; x < divisionsCount - 1; x++)
                {
                    var idx = yoff + x;
                    var e = new GPUEdge(idx, idx + 1, edgeLength);
                    edges.Add(e);
                }
            }

            simulator = new GPUVerletSimulator(nodes, edges.ToArray());

            tipMesh = BuildTipMesh(1f);
            tentacleMesh = BuildTentacleMesh(divisionsCount, radialSegments);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            args[0] = tipMesh.GetIndexCount(0);
            args[1] = (uint)tentaclesCount;
            tipArgsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
            tipArgsBuffer.SetData(args);

            args[0] = tentacleMesh.GetIndexCount(0);
            args[1] = (uint)tentaclesCount;
            tentacleArgsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
            tentacleArgsBuffer.SetData(args);

            Init();
        }
        
        protected void Update () {
            var dt = Time.deltaTime;

            simulator.Step(verletCompute, decay);
            for(int i = 0; i < iterations; i++)
            {
                // simulator.Solve(verletCompute);
                Solve(); // optimized solver
            }
            simulator.Gravity(verletCompute, gravity, dt);
            Flow(dt);
            Relax(dt);
            Decay(dt);

            // if(Input.GetMouseButton(0))
            {
                Touch(Input.mousePosition);
            }

            Render();
        }

        protected void Render()
        {
            RenderTentacles();
            RenderTips();
        }

        protected void RenderTentacles()
        {
            tentacleMaterial.SetMatrix("_World2Local", transform.worldToLocalMatrix);
            tentacleMaterial.SetMatrix("_Local2World", transform.localToWorldMatrix);

            tentacleMaterial.SetBuffer("_Nodes", simulator.NodeBuffer);
            tentacleMaterial.SetInt("_TentaclesCount", tentaclesCount);
            tentacleMaterial.SetInt("_DivisionsCount", divisionsCount);
            tentacleMaterial.SetFloat("_InvTubularCount", 1f / divisionsCount);
            tentacleMaterial.SetFloat("_InvRadialsCount", 1f / radialSegments);

            var cameraDir = Camera.main.transform.forward;
            var localCameraDir = transform.InverseTransformDirection(cameraDir);
            tentacleMaterial.SetVector("_LocalCameraDirection", localCameraDir);

            Graphics.DrawMeshInstancedIndirect(tentacleMesh, 0, tentacleMaterial, new Bounds(Vector3.zero, Vector3.one * 100f), tentacleArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true);
        }

        protected void RenderTips()
        {
            tipMaterial.SetMatrix("_World2Local", transform.worldToLocalMatrix);
            tipMaterial.SetMatrix("_Local2World", transform.localToWorldMatrix);

            tipMaterial.SetBuffer("_Nodes", simulator.NodeBuffer);
            tipMaterial.SetInt("_TentaclesCount", tentaclesCount);
            tipMaterial.SetInt("_DivisionsCount", divisionsCount);

            Graphics.DrawMeshInstancedIndirect(tipMesh, 0, tipMaterial, new Bounds(Vector3.zero, Vector3.one * 100f), tipArgsBuffer);
        }

        #region Kernels

        protected void SetupKernel(int kernel)
        {
            tentacleCompute.SetBuffer(kernel, "_Nodes", simulator.NodeBuffer);
            tentacleCompute.SetInt("_NodesCount", simulator.NodeBuffer.count);
            tentacleCompute.SetBuffer(kernel, "_Edges", simulator.EdgeBuffer);
            tentacleCompute.SetInt("_EdgesCount", simulator.EdgeBuffer.count);
            tentacleCompute.SetInt("_TentaclesCount", tentaclesCount);
            tentacleCompute.SetFloat("_InvTentaclesCount", 1f / tentaclesCount);
            tentacleCompute.SetInt("_DivisionsCount", divisionsCount);
            tentacleCompute.SetFloat("_InvDivisionsCount", 1f / divisionsCount);
        }

        protected void Init()
        {
            var kernel = tentacleCompute.FindKernel("Init");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.SetFloat("_EdgeLength", edgeLength);
            tentacleCompute.SetVector("_BoundsMin", bounds.min);
            tentacleCompute.SetVector("_BoundsMax", bounds.max);

            var nodesCount = simulator.NodeBuffer.count;
            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void Flow(float dt)
        {
            var kernel = tentacleCompute.FindKernel("Flow");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.SetFloat("_FlowScale", flowScale);
            tentacleCompute.SetFloat("_FlowIntensity", flowIntensity);
            tentacleCompute.SetFloat("_DT", dt);

            var nodesCount = simulator.NodeBuffer.count;
            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void Relax(float dt)
        {
            var kernel = tentacleCompute.FindKernel("Relax");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.SetFloat("_EdgeLength", Mathf.Max(0.1f, edgeLength));
            tentacleCompute.SetFloat("_DT", dt);

            var edgesCount = simulator.EdgeBuffer.count;
            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(edgesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void Decay(float dt)
        {
            var kernel = tentacleCompute.FindKernel("Decay");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.SetFloat("_DT", dt);
            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(simulator.NodeBuffer.count / (int)tx) + 1, (int)ty, (int)tz);
        }

        public void React(float t)
        {
            var kernel = tentacleCompute.FindKernel("React");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);
            SetupKernel(kernel);

            tentacleCompute.SetFloat("_Time", t);

            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(tentaclesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        public void Touch(Vector2 screen, float depth = 10f)
        {
            var kernel = tentacleCompute.FindKernel("Touch");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);
            SetupKernel(kernel);

            var cam = Camera.main;

            var p = cam.projectionMatrix;
            var v = cam.worldToCameraMatrix;
            var m = transform.localToWorldMatrix;
            var mvp = p * v * m;

            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, cam.nearClipPlane + depth));
            var localScr = transform.InverseTransformPoint(world);
            var projected = mvp.MultiplyPoint(localScr);
            
            tentacleCompute.SetMatrix("_MVP", mvp);
            tentacleCompute.SetVector("_Point", new Vector2(projected.x / projected.z, projected.y / projected.z));
            tentacleCompute.SetFloat("_Distance", 0.1f);

            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(tentaclesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void Solve()
        {
            var kernel = tentacleCompute.FindKernel("Solve");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(tentaclesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        #endregion

        protected Mesh BuildTentacleMesh(int tubularSegments, int radialSegments)
        {
            var mesh = new Mesh();

            var count = tubularSegments * radialSegments;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];

            var invTubularSegments = 1f / tubularSegments;
            var invRadialSegments = 1f / (radialSegments - 1);

            const float pi2 = Mathf.PI * 2f;

            for(int y = 0; y < tubularSegments; y++)
            {
                var v = y * invTubularSegments;
                var yoff = y * radialSegments;
                for(int x = 0; x < radialSegments; x++)
                {
                    var idx = yoff + x;
                    var u = x * invRadialSegments;

                    var theta = u * pi2;
                    vertices[idx].Set(Mathf.Sin(theta), y, Mathf.Cos(theta));
                    normals[idx].Set(Mathf.Sin(theta), 0f, Mathf.Cos(theta));
                    uv[idx].Set(u, v);
                }
            }

            var indices = new List<int>();
            for (int j = 1; j < tubularSegments; j++)
            {
                for (int i = 1; i < radialSegments; i++)
                {
                    int a = radialSegments * (j - 1) + (i - 1);
                    int b = radialSegments * j + (i - 1);
                    int c = radialSegments * j + i;
                    int d = radialSegments * (j - 1) + i;
                    indices.Add(a); indices.Add(d); indices.Add(b);
                    indices.Add(b); indices.Add(d); indices.Add(c);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

            mesh.hideFlags = HideFlags.DontSave;
            return mesh;
        }

        protected Mesh BuildTipMesh(float radius = 1f, int widthSegments = 8, int heightSegments = 6, float phiStart = 0f, float phiLength = Mathf.PI * 2f, float thetaStart = 0f, float thetaLength = Mathf.PI)
        {
            var mesh = new Mesh();

            widthSegments = Mathf.Max(3, widthSegments);
            heightSegments = Mathf.Max(2, heightSegments);

            var thetaEnd = thetaStart + thetaLength;

            var index = 0;
            List<int[]> grid = new List<int[]>();

            var indices = new List<int>();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            int ix, iy;
            for (iy = 0; iy <= heightSegments; iy++)
            {
                int[] verticesRow = new int[widthSegments + 1];

                var v = 1f * iy / heightSegments;

                for (ix = 0; ix <= widthSegments; ix++)
                {
                    var u = 1f * ix / widthSegments;

                    // vertex
                    var vertex = Vector3.zero;
                    vertex.x = -radius * Mathf.Cos(phiStart + u * phiLength) * Mathf.Sin(thetaStart + v * thetaLength);
                    vertex.y = radius * Mathf.Cos(thetaStart + v * thetaLength);
                    vertex.z = radius * Mathf.Sin(phiStart + u * phiLength) * Mathf.Sin(thetaStart + v * thetaLength);
                    vertices.Add(vertex);

                    // uv
                    uvs.Add(new Vector2(u, 1f - v));

                    verticesRow[ix] = index++;
                }

                grid.Add(verticesRow);
            }

            // indices
            for (iy = 0; iy < heightSegments; iy++)
            {
                for (ix = 0; ix < widthSegments; ix++)
                {
                    var a = grid[iy][ix + 1];
                    var b = grid[iy][ix];
                    var c = grid[iy + 1][ix];
                    var d = grid[iy + 1][ix + 1];

                    if (iy != 0 || thetaStart > 0)
                    {
                        indices.Add(a);
                        indices.Add(b);
                        indices.Add(d);
                    }

                    if (iy != heightSegments - 1 || thetaEnd < Mathf.PI)
                    {
                        indices.Add(b);
                        indices.Add(c);
                        indices.Add(d);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            mesh.hideFlags = HideFlags.DontSave;

            return mesh;
        }

        protected void OnDestroy()
        {
            simulator.Dispose();
            tipArgsBuffer.Dispose();
            tentacleArgsBuffer.Dispose();
        }

        protected void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        /*
        protected void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;

            var nodesCount = simulator.NodeBuffer.count;
            GPUNode[] nodes = new GPUNode[nodesCount];
            simulator.NodeBuffer.GetData(nodes);
            for(int i = 0; i < nodesCount; i++)
            {
                var n = nodes[i];
                Gizmos.DrawSphere(n.position, 0.25f);
            }

            var edgesCount = simulator.EdgeBuffer.count;
            GPUEdge[] edges = new GPUEdge[edgesCount];
            simulator.EdgeBuffer.GetData(edges);
            for(int i = 0; i < edgesCount; i++)
            {
                var e = edges[i];
                var a = nodes[e.a];
                var b = nodes[e.b];
                Gizmos.DrawLine(a.position, b.position);
            }
        }
         */
        
    }

}


