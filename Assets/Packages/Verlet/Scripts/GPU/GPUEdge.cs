using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Verlet
{

    [StructLayout (LayoutKind.Sequential)]
    public struct GPUEdge {

        public int a;
        public int b;
        public float length;

    }

}


