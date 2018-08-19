using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class Node {
        public List<Edge> Connection { get { return connection; } }

        public Vector3 position;
        protected Vector3 prev;

        List<Edge> connection;
        
        public Node(Vector3 p)
        {
            position = prev = p;
            connection = new List<Edge>();
        }

        public void Step()
        {
            var v = position - prev;
            var next = position + v;
            prev = position;
            position = next;
        }

        public void Connect(Edge e)
        {
            connection.Add(e);
        }

    }

}


