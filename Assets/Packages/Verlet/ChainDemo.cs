using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Verlet.Demo
{

    public class ChainDemo : DemoBase {

        [SerializeField] int count = 20;
        [SerializeField] int iterations = 12;

        [SerializeField] bool useGravity = true;
        [SerializeField] float gravity = 1f;

        [SerializeField] Transform control;
        List<GameObject> debuggers;

        VerletSimulator simulator;
        List<VParticle> particles;

        void Start () {
            debuggers = new List<GameObject>();
            particles = new List<VParticle>();
            for(int i = 0; i < count; i++)
            {
                var p = new VParticle(Vector3.right * i);
                particles.Add(p);

                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = Vector3.one * 0.25f;
                debuggers.Add(sphere);
            }; 

            for(int i = 0; i < count; i++)
            {
                if(i != count - 1)
                {
                    var a = particles[i];
                    var b = particles[i + 1];
                    var e = new VEdge(a, b);
                    a.Connect(e);
                    b.Connect(e);
                }
            }

            simulator = new VerletSimulator(particles);
        }
        
        void Update () {
            var dt = Time.deltaTime;

            if(useGravity)
            {
                var g = Vector3.down * gravity;
                particles.ForEach(p =>
                {
                    p.position += dt * g;
                });
            }

            simulator.Simulate(iterations, dt);

            var mouse = Input.mousePosition;
            var cam = Camera.main;
            var world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, cam.nearClipPlane + 20f));
            particles[0].position = control.position = world;

            for(int i = 0, n = particles.Count; i < n; i++)
            {
                debuggers[i].transform.position = particles[i].position;
            }
        }

        protected override void OnRenderObject()
        {
            base.OnRenderObject();

            RenderConnection(particles, Color.white);
        }

        void OnDrawGizmos()
        {
            if (simulator == null) return;
            simulator.DrawGizmos();
        }

    }

}


