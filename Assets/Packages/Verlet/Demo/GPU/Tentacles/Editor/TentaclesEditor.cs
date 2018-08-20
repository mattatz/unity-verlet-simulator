using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Verlet.Demo
{

    [CustomEditor (typeof(Tentacles))]
    public class TentaclesEditor : Editor {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("React"))
            {
                var t = target as Tentacles;
                t.React(Time.timeSinceLevelLoad);
            }
        }

    }

}


