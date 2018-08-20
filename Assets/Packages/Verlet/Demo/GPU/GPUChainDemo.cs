using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet.Demo
{

    public class GPUChainDemo : GPUDemoBase {

        protected override void Start()
        {
            edgesCount = nodesCount - 1;

            var nodes = new GPUNode[nodesCount];
            for(int i = 0; i < nodesCount; i++)
            {
                var n = nodes[i];
                var p = new Vector3(Random.value - 0.5f, i * edgeLength, Random.value - 0.5f);
                n.position = n.prev = p;
                nodes[i] = n;
            }

            var edges = new GPUEdge[edgesCount];
            for(int i = 0; i < edgesCount; i++)
            {
                var e = edges[i];
                e.a = i;
                e.b = i + 1;
                e.length = edgeLength;
                edges[i] = e;
            }

            simulator = new GPUVerletSimulator(nodes, edges);

            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            // if(Input.GetMouseButton(0))
            {
                var m = Input.mousePosition;
                m = Camera.main.ScreenToWorldPoint(new Vector3(m.x, m.y, Camera.main.nearClipPlane + 10f));
                simulator.FixOne(compute, 0, m);
            }

        }

    }

}


