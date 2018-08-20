using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet.Demo
{

    public class GPUClothDemo : GPUDemoBase {

        [SerializeField, Range(8, 256)] protected int rows = 128, columns = 64;

        protected override void Start()
        {
            nodesCount = rows * columns;

            var nodes = new GPUNode[nodesCount];

            var hCols = columns * 0.5f;

            for(int y = 0; y < rows; y++)
            {
                bool stable = (y == 0);
                var yoff = y * columns;

                for(int x = 0; x < columns; x++)
                {
                    var idx = yoff + x;
                    var n = nodes[idx];
                    n.position = n.prev = 
                        (Vector3.forward * y * edgeLength) + 
                        (Vector3.right * (x - hCols) * edgeLength);
                    n.stable = (uint)(stable ? 1 : 0);
                    nodes[idx] = n;
                }
            }

            var edges = new List<GPUEdge>();
            for(int y = 0; y < rows; y++)
            {
                var yoff = y * columns;
                if(y != rows - 1)
                {
                    for(int x = 0; x < columns; x++) {
                        var idx = yoff + x;
                        if(x != columns - 1)
                        {
                            var right = idx + 1;
                            edges.Add(new GPUEdge(idx, right, edgeLength));
                        }
                        var down = idx + columns;
                        edges.Add(new GPUEdge(idx, down, edgeLength));
                    }
                } else
                {
                    for (int x = 0; x < columns - 1; x++)
                    {
                        var idx = yoff + x;
                        var right = idx + 1;
                        edges.Add(new GPUEdge(idx, right, edgeLength));
                    }
                }
            }

            edgesCount = edges.Count;

            simulator = new GPUVerletSimulator(nodes, edges.ToArray());

            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            // if(Input.GetMouseButton(0))
            /*
            {
                var m = Input.mousePosition;
                m = Camera.main.ScreenToWorldPoint(new Vector3(m.x, m.y, Camera.main.nearClipPlane + 10f));
                simulator.FixOne(compute, 0, m);
            }
            */

        }

    }

}


