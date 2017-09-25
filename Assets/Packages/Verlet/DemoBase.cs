using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Verlet.Demo
{

    public class DemoBase : MonoBehaviour {

        const string SHADER_PATH = "Hidden/Internal-Colored";

        protected Material lineMaterial;

        protected virtual void OnRenderObject()
        {
            CheckInit();

            lineMaterial.SetPass(0);
            lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        protected void RenderConnection(List<VParticle> particles, Color color)
        {
            particles.ForEach(p => RenderConnection(p, color));
        }

        protected void RenderConnection(VParticle p, Color color)
        {
            p.Connection.ForEach(e => {
                var other = e.Other(p);

                GL.PushMatrix();
                GL.Begin(GL.LINES);

                GL.Vertex(p.position);
                GL.Vertex(other.position);

                GL.Color(color);
                GL.End();
                GL.PopMatrix();
            });
        }

        void CheckInit()
        {
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find(SHADER_PATH);
                if (shader == null) return;
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

    }

}


