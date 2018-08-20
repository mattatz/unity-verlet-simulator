using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Verlet.Demo
{

    public abstract class GPUDemoBase : MonoBehaviour {

        [SerializeField] protected ComputeShader compute;

        [SerializeField] protected Material nodeMaterial, edgeMaterial;
        [SerializeField] protected Mesh nodeMesh;
        [SerializeField] protected bool drawNode, drawEdge = true;
        protected Mesh edgeMesh;

        [SerializeField] protected int nodesCount = 128;
        [SerializeField, Range(0.01f, 1f)] protected float edgeLength = 0.1f;
        protected float _edgeLength;

        protected int edgesCount;

        [SerializeField, Range(1, 32)] protected int iterations = 8;
        protected GPUVerletSimulator simulator;

        protected ComputeBuffer drawNodeArgsBuffer;
        protected uint[] drawNodeArgs = new uint[5] { 0, 0, 0, 0, 0 };

        protected ComputeBuffer drawEdgeArgsBuffer;
        protected uint[] drawEdgeArgs = new uint[5] { 0, 0, 0, 0, 0 };

        protected virtual void Start () {
            _edgeLength = edgeLength;

            edgeMesh = BuildLine();
            SetupArguments();
        }

        protected void SetupArguments()
        {
            drawNodeArgs[0] = nodeMesh.GetIndexCount(0);
            drawNodeArgs[1] = (uint)nodesCount;
            drawNodeArgsBuffer = new ComputeBuffer(1, sizeof(uint) * drawNodeArgs.Length, ComputeBufferType.IndirectArguments);
            drawNodeArgsBuffer.SetData(drawNodeArgs);

            drawEdgeArgs[0] = edgeMesh.GetIndexCount(0);
            drawEdgeArgs[1] = (uint)edgesCount;
            drawEdgeArgsBuffer = new ComputeBuffer(1, sizeof(uint) * drawEdgeArgs.Length, ComputeBufferType.IndirectArguments);
            drawEdgeArgsBuffer.SetData(drawEdgeArgs);
        }
        
        protected virtual void Update () {
            if(_edgeLength != edgeLength)
            {
                simulator.UpdateLength(compute, edgeLength);
                _edgeLength = edgeLength;
            }
            simulator.Gravity(compute, new Vector3(0f, -1f, 0f), Time.deltaTime);
            simulator.Step(compute);
            for(int i = 0; i < iterations; i++)
            {
                simulator.Solve(compute);
            }

            if(drawNode)
            {
                RenderNodes();
            }
            if(drawEdge)
            {
                RenderEdges();
            }
        }

        protected void RenderNodes()
        {
            nodeMaterial.SetBuffer("_Nodes", simulator.NodeBuffer);
            nodeMaterial.SetMatrix("_World2Local", transform.worldToLocalMatrix);
            nodeMaterial.SetMatrix("_Local2World", transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(nodeMesh, 0, nodeMaterial, new Bounds(Vector3.zero, Vector3.one * 100f), drawNodeArgsBuffer);
        }

        protected void RenderEdges()
        {
            edgeMaterial.SetBuffer("_Nodes", simulator.NodeBuffer);
            edgeMaterial.SetBuffer("_Edges", simulator.EdgeBuffer);
            edgeMaterial.SetMatrix("_World2Local", transform.worldToLocalMatrix);
            edgeMaterial.SetMatrix("_Local2World", transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(edgeMesh, 0, edgeMaterial, new Bounds(Vector3.zero, Vector3.one * 100f), drawEdgeArgsBuffer);
        }

        protected void OnDestroy()
        {
            simulator.Dispose();

            drawNodeArgsBuffer.Release();
            drawEdgeArgsBuffer.Release();
        }

        protected Mesh BuildLine()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[2] { Vector3.zero, Vector3.up };
            mesh.uv = new Vector2[2] { new Vector2(0f, 0f), new Vector2(0f, 1f) };
            mesh.SetIndices(new int[2] { 0, 1 }, MeshTopology.Lines, 0);
            return mesh;
        }

        /*
        protected void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;

            GPUNode[] nodes = new GPUNode[nodesCount];
            simulator.NodeBuffer.GetData(nodes);
            for(int i = 0; i < nodesCount; i++)
            {
                var n = nodes[i];
                Gizmos.DrawSphere(n.position, 0.1f);
            }

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


