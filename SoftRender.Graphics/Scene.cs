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
            var viewMatrix = Camera.Transform.GetInverse();
            var lights = Lights.ToArray();

            var lightPackets = new LightPacket[Lights.Count()];
            for (int i = 0; i < Lights.Count(); i++)
            {
                var lightPos = Lights[i].GetWorldPosition();
                lightPackets[i] = new LightPacket(lightPos.X, lightPos.Y, lightPos.Z);
            }

            foreach (var model in Models)
            {
                renderer.VertexShader = new VertexShader(model.Transform, viewMatrix, projection);
                renderer.PixelShader = new PixelShader((NearestSampler)model.Texture, lightPackets);

                renderer.Render(model.Vertices, model.Attributes, lights);
            }
        }
    }
}
