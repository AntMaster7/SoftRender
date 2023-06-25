﻿using SoftRender.SRMath;
using System.Diagnostics;

namespace SoftRender.Graphics
{
    // Implements a simplified scene with no hierarchy. Thats for later.
    public class Scene
    {
        public Camera? Camera { get; set; }

        public List<Model> Models { get; private set; } = new List<Model>();

        public List<Light> Lights { get; private set; } = new List<Light>();

        public void Render(Renderer renderer)
        {
            Debug.Assert(Camera != null);

            var projection = Camera.CreateProjectionMatrix();

            foreach (var model in Models)
            {
                var mv = model.Transform;
                var invMv = mv.GetUpperLeft().Inverse();
                renderer.VertexShader = new VertexShader(mv, projection, invMv, new Vector3D(3, 0, -1));

                renderer.Texture = model.Texture;

                renderer.Render(model.Vertices, model.Attributes);
            }
        }
    }
}