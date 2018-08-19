using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Verlet.Demo
{

    public class ClothDemo : CPUDemoBase {

        [SerializeField] int count = 20;
        [SerializeField] int iterations = 12;

        [SerializeField] bool useGravity = true;
        [SerializeField] float gravity = 1f;

        [SerializeField] Transform control;
        List<GameObject> debuggers;

        VerletSimulator simulator;
        List<Node> particles;

        void Start () {
            debuggers = new List<GameObject>();
            particles = new List<Node>();

            var offset = -count * 0.5f;

            for(int y = 0; y < count; y++)
            {
                for(int x = 0; x < count; x++)
                {
                    var p = new Node(y * Vector3.forward + (Vector3.right * (x - offset)));
                    particles.Add(p);

                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.localScale = Vector3.one * 0.25f;
                    debuggers.Add(sphere);
                }
            }

            for(int y = 0; y < count; y++)
            {
                if(y != count - 1)
                {
                    for(int x = 0; x < count; x++) {
                        var index = y * count + x;
                        var c = particles[index];

                        if(x != count - 1)
                        {
                            var right = particles[index + 1];
                            var re = new Edge(c, right);
                            c.Connect(re); right.Connect(re);
                        }
                        var down = particles[index + count];
                        var de = new Edge(c, down);
                        c.Connect(de); down.Connect(de);
                    }
                } else
                {
                    for (int x = 0; x < count - 1; x++)
                    {
                        var index = y * count + x;
                        var c = particles[index];
                        var right = particles[index + 1];
                        var re = new Edge(c, right);
                        c.Connect(re); right.Connect(re);
                    }
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
            var world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, cam.nearClipPlane + 30f));
            particles[Mathf.FloorToInt(count * 0.5f)].position = control.position = world;

            for(int x = 0; x < count; x++)
            {
                // first row
                // particles[x].position = (Vector3.right * x);
            }

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


