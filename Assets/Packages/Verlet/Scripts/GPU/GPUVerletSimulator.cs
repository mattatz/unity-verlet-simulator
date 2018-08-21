using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Verlet
{

    public class GPUVerletSimulator : IDisposable {

        public ComputeBuffer NodeBuffer { get { return nodeBufferRead; } }
        public ComputeBuffer EdgeBuffer { get { return edgeBuffer; } }

        protected ComputeBuffer nodeBufferRead, nodeBufferWrite, edgeBuffer;
        protected int nodesCount, edgesCount;

        public GPUVerletSimulator(GPUNode[] nodes, GPUEdge[] edges)
        {
            nodesCount = nodes.Length;
            edgesCount = edges.Length;

            nodeBufferRead = new ComputeBuffer(nodesCount, Marshal.SizeOf(typeof(GPUNode)));
            nodeBufferWrite = new ComputeBuffer(nodesCount, Marshal.SizeOf(typeof(GPUNode)));
            edgeBuffer = new ComputeBuffer(edgesCount, Marshal.SizeOf(typeof(GPUEdge)));

            nodeBufferRead.SetData(nodes);
            nodeBufferWrite.SetData(nodes);
            edgeBuffer.SetData(edges);
        }

        public void Step(ComputeShader compute, float decay = 1f)
        {
            var kernel = compute.FindKernel("Step");
            uint tx, ty, tz;
            compute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            compute.SetBuffer(kernel, "_Nodes", nodeBufferRead);
            compute.SetInt("_NodesCount", nodesCount);

            compute.SetFloat("_Decay", decay);

            compute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        public void Solve(ComputeShader compute)
        {
            var kernel = compute.FindKernel("Solve");
            uint tx, ty, tz;
            compute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            compute.SetBuffer(kernel, "_NodesRead", nodeBufferRead);
            compute.SetBuffer(kernel, "_Nodes", nodeBufferWrite);
            compute.SetInt("_NodesCount", nodesCount);

            compute.SetBuffer(kernel, "_Edges", edgeBuffer);
            compute.SetInt("_EdgesCount", edgesCount);

            compute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);

            SwapBuffer(ref nodeBufferRead, ref nodeBufferWrite);
        }

        public void FixOne(ComputeShader compute, int id, Vector3 point)
        {
            var kernel = compute.FindKernel("FixOne");
            uint tx, ty, tz;
            compute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            compute.SetBuffer(kernel, "_Nodes", nodeBufferRead);
            compute.SetInt("_FixedID", id);
            compute.SetVector("_FixedPoint", point);

            compute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        public void Gravity(ComputeShader compute, Vector3 gravity, float dt)
        {
            var kernel = compute.FindKernel("Gravity");
            uint tx, ty, tz;
            compute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

            compute.SetBuffer(kernel, "_Nodes", nodeBufferRead);
            compute.SetInt("_NodesCount", nodesCount);

            compute.SetVector("_Gravity", gravity);
            compute.SetFloat("_DT", dt);

            compute.Dispatch(kernel, Mathf.FloorToInt(nodesCount / (int)tx) + 1, (int)ty, (int)tz);
        }

        protected void SwapBuffer(ref ComputeBuffer a, ref ComputeBuffer b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        protected void ReleaseBuffer(ref ComputeBuffer buf)
        {
            if(buf != null)
            {
                buf.Release();
            }
            buf = null;
        }

        public void Dispose()
        {
            ReleaseBuffer(ref nodeBufferRead);
            ReleaseBuffer(ref nodeBufferWrite);
            ReleaseBuffer(ref edgeBuffer);
        }

    }

}

