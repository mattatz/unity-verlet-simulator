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
        [SerializeField] protected Material render;
        [SerializeField] protected Bounds bounds;

        protected GPUVerletSimulator simulator;

        [SerializeField] protected Mesh mesh;
        protected ComputeBuffer argsBuffer;

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

            mesh = Build(divisionsCount, radialSegments);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)tentaclesCount;
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            Init();
        }
        
        protected void Update () {
            var dt = Time.deltaTime;

            simulator.Step(verletCompute);
            for(int i = 0; i < 16; i++)
            {
                simulator.Solve(verletCompute);
            }
            Flow(dt);
            Relax(dt);

            Render();
        }

        protected void Render()
        {
            render.SetMatrix("_World2Local", transform.worldToLocalMatrix);
            render.SetMatrix("_Local2World", transform.localToWorldMatrix);

            render.SetBuffer("_Nodes", simulator.NodeBuffer);
            render.SetInt("_TentaclesCount", tentaclesCount);
            render.SetInt("_DivisionsCount", divisionsCount);
            render.SetFloat("_InvTubularCount", 1f / divisionsCount);
            render.SetFloat("_InvRadialsCount", 1f / radialSegments);

            var cameraDir = Camera.main.transform.forward;
            var localCameraDir = transform.InverseTransformDirection(cameraDir);
            render.SetVector("_LocalCameraDirection", localCameraDir);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, render, new Bounds(Vector3.zero, Vector3.one * 100f), argsBuffer);
        }

        #region Kernels

        protected void SetupKernel(int kernel)
        {
            var nodesCount = simulator.NodeBuffer.count;
            tentacleCompute.SetBuffer(kernel, "_Nodes", simulator.NodeBuffer);
            tentacleCompute.SetBuffer(kernel, "_Edges", simulator.EdgeBuffer);
            tentacleCompute.SetInt("_NodesCount", nodesCount);
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

        public void Relax(float dt)
        {
            var kernel = tentacleCompute.FindKernel("Relax");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            SetupKernel(kernel);

            tentacleCompute.SetFloat("_DT", dt);

            var edgesCount = simulator.EdgeBuffer.count;
            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(edgesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void React(float t)
        {
            var kernel = tentacleCompute.FindKernel("React");
            uint tx, ty, tz;
            tentacleCompute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);
            SetupKernel(kernel);

            tentacleCompute.SetFloat("_Time", t);

            tentacleCompute.Dispatch(kernel, Mathf.FloorToInt(tentaclesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        #endregion

        protected Mesh Build(int tubularSegments, int radialSegments)
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

        protected void OnDestroy()
        {
            simulator.Dispose();
            argsBuffer.Dispose();
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


