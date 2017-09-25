using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verlet
{

    public class VerletSimulator {

        List<VParticle> particles;

        public VerletSimulator(List<VParticle> particles)
        {
            this.particles = particles;
        }

       
        public void Simulate (int iterations, float dt) {
            Step();
            Solve(iterations, dt);
        }

        void Step()
        {
            particles.ForEach(p => {
                p.Step();
            });
        }

        void Solve(int iterations, float dt)
        {
            for(int iter = 0; iter < iterations; iter++)
            {
                particles.ForEach(p => Solve(p));
            }
        }

        void Solve(VParticle particle)
        {
            particle.Connection.ForEach(e =>
            {
                var other = e.Other(particle);
                Solve(particle, other, e.Length);
            });
        }

        void Solve(VParticle a, VParticle b, float rest)
        {
            var delta = a.position - b.position;
            var current = delta.magnitude;
            var f = (current - rest) / current;
            a.position -= f * 0.5f * delta;
            b.position += f * 0.5f * delta;
        }

        public void DrawGizmos()
        {
            for(int i = 0, n = particles.Count; i < n; i++)
            {
                var p = particles[i];
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(p.position, 0.2f);

                Gizmos.color = Color.white;
                p.Connection.ForEach(e => {
                    var other = e.Other(p);
                    Gizmos.DrawLine(p.position, other.position);
                });
            }
        }

    }

}


