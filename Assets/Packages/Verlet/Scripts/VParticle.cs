using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class VParticle {
        public List<VEdge> Connection { get { return connection; } }

        public Vector3 position;
        protected Vector3 prev;

        List<VEdge> connection;
        
        public VParticle(Vector3 p)
        {
            position = prev = p;
            connection = new List<VEdge>();
        }

        public void Step()
        {
            var v = position - prev;
            var next = position + v;
            prev = position;
            position = next;
        }

        public void Connect(VEdge e)
        {
            connection.Add(e);
        }

    }

}


