using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Verlet.Demo
{

    public class GPUDemo : MonoBehaviour {

        [SerializeField] protected ComputeShader compute;
        [SerializeField] protected int count = 128;

        protected ComputeBuffer nodeBufferRead, nodeBufferWrite, edgeBuffer;

        void Start () {
            nodeBufferRead = new ComputeBuffer(count, Marshal.SizeOf(typeof(GPUNode)));
            nodeBufferWrite = new ComputeBuffer(count, Marshal.SizeOf(typeof(GPUNode)));
            edgeBuffer = new ComputeBuffer((count - 1), Marshal.SizeOf(typeof(GPUEdge)));

            var nodes = new GPUNode[count];
            for(int i = 0; i < count; i++)
            {
                var n = nodes[i];
                n.position = new Vector3(0f, i, 0f);
                nodes[i] = n;
            }
            nodeBufferRead.SetData(nodes);
            nodeBufferWrite.SetData(nodes);

            var edges = new GPUEdge[count - 1];
            for(int i = 0, n = count - 1; i < n; i++)
            {
                var e = edges[i];
                e.a = i;
                e.b = i + 1;
                e.length = 0.5f;
                edges[i] = e;
            }
            edgeBuffer.SetData(edges);
        }
        
        void Update () {
        }

        protected void Step()
        {
            var kernel = compute.FindKernel("Step");
            uint tx, ty, tz;
            compute.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);
        }

        protected void Solve()
        {
        }

        protected void OnDestroy()
        {
            ReleaseBuffer(ref nodeBufferRead);
            ReleaseBuffer(ref nodeBufferWrite);
            ReleaseBuffer(ref edgeBuffer);
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

    }

}


