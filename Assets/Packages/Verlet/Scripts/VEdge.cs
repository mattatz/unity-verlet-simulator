using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class VEdge {

        public float Length { get { return length; } }

        VParticle a, b;
        float length;

        public VEdge(VParticle a, VParticle b)
        {
            this.a = a;
            this.b = b;
            this.length = (a.position - b.position).magnitude;
        }

        public VEdge(VParticle a, VParticle b, float len)
        {
            this.a = a;
            this.b = b;
            this.length = len;
        }

        public VParticle Other(VParticle p)
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


