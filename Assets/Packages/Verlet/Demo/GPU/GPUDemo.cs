using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet.Demo
{

    public class GPUDemo : MonoBehaviour {

        [SerializeField] protected ComputeShader compute;

        protected ComputeBuffer nodes, edges;

        void Start () {
        }
        
        void Update () {
        }

        protected void OnDestroy()
        {
            ReleaseBuffer(ref nodes);
            ReleaseBuffer(ref edges);
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


