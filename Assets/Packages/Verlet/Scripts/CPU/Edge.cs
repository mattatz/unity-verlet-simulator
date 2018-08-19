using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class Edge {

        public float Length { get { return length; } }

        Node a, b;
        float length;

        public Edge(Node a, Node b)
        {
            this.a = a;
            this.b = b;
            this.length = (a.position - b.position).magnitude;
        }

        public Edge(Node a, Node b, float len)
        {
            this.a = a;
            this.b = b;
            this.length = len;
        }

        public Node Other(Node p)
        {
            if(a == p)
            {
                return b;
            } else
            {
                return a;
            }
        }

    }

}


