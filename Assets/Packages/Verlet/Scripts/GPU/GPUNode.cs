using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Verlet
{

    [StructLayout (LayoutKind.Sequential)]
    public struct GPUNode {
        public Vector3 position;
        public Vector3 prev;
        public uint stable;
    }

}


