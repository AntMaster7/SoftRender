using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public partial class MainForm : Form
    {
        private const int TargetFrameRate = 60;
        private const int MaxFrameTime = 200;

        private Bitmap bitmap;
        private ISampler sampler;
        private Stopwatch tickStopWatch = new Stopwatch();
        private int frameTimeAccumulator = 0;

        private Model model;

        public MainForm()
        {
            InitializeComponent();

            model = MeshLoader.Load("Suzanne.obj");

            //model = new Model();
            //model.Vertices = new Vector3D[3]
            //{
            //    new Vector3D(0f, 0.5f, 0f),
            //    new Vector3D(-0.5f, -0.5f, 0f),
            //    new Vector3D(0.5f, -0.5f, 0f),
            //};
            //model.Attributes = new VertexAttributes[3]
            //{
            //    new VertexAttributes(0.5f, 1, 0,0, 1),
            //    new VertexAttributes(0f, 0f, 0,0, 1),
            //    new VertexAttributes(1f, 0, 0,0, 1)
            //};

            sampler = LoadTexture("brickwall-512x512.jpg");

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;
            
            Application.Idle += Application_Idle;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var rot = Matrix4D.CreateTranslate(0, 0, -2) * Matrix4D.CreateFromYaw(0.0f);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                model.Vertices[i] = (rot * model.Vertices[i]).PerspectiveDivide();
            }

            DrawArrays(model.Vertices, model.Attributes);
        }

        private void Application_Idle(object? sender, EventArgs e)
        {
            while (!WinNative.PeekMessage(out WinNative.NativeMessage _, new HandleRef(this, Handle), 0, 0, 0))
            {
                Tick();
            }
        }

        private void Tick()
        {
            if (!tickStopWatch.IsRunning)
            {
                tickStopWatch.Start();
            }

            var targetFrameTime = 1000 / TargetFrameRate;

            var updated = false;

            frameTimeAccumulator += tickStopWatch.Elapsed.Milliseconds;
            if (frameTimeAccumulator > MaxFrameTime)
            {
                frameTimeAccumulator = MaxFrameTime;
            }

            while (frameTimeAccumulator >= targetFrameTime)
            {
                tickStopWatch.Restart();

                Update(TimeSpan.FromMilliseconds(targetFrameTime));
                frameTimeAccumulator -= targetFrameTime;

                updated = true;
            }

            if (updated)
            {
                DrawArrays(model.Vertices, model.Attributes);
            }
        }

        private void Update(TimeSpan delta)
        {
            const float AngularVelocity = 0.2f;

            var step = AngularVelocity * (float)delta.TotalMilliseconds / 1000;

            var rot = Matrix4D.CreateTranslate(0, 0, -2) * Matrix4D.CreateFromYaw(step) * Matrix4D.CreateTranslate(0, 0, 2);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                // model.Vertices[i] = (rot * model.Vertices[i]).PerspectiveDivide();
            }
        }

        private unsafe void DrawArrays(Vector3D[] vertices, VertexAttributes[] attributes)
        {
            if (vertices.Length % 3 != 0)
            {
                throw new ArgumentException("Number of vertices must be a multiple of 3.");
            }

            int w = bitmap.Width;
            int h = bitmap.Height;

            var camera = new Camera((float)w / h);
            var frustum = camera.CreateProjectionMatrix();

            var vpt = new ViewportTransform(w, h);

            int iterations = 100;
            var frameTimer = new Stopwatch();

            var vertexShader = new VertexShader();
            vertexShader.ProjectionMatrix = frustum;
            vertexShader.LightSource = new Vector3D(3, 0, -1);

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride, new Size(w, h), vpt);
                fastRasterizer.Mode = RasterizerMode.Fill; // | RasterizerMode.Wireframe;

                var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride, vpt);

                //var clipSpace = new Vector4D[3];
                //var attribs = new VertexAttributes[3];

                var opts = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4,
                };

                Span<Vector4D> cs = new Vector4D[vertices.Length];
                Span<VertexAttributes> at = new VertexAttributes[vertices.Length];
                Span<Vector3D> ns = new Vector3D[vertices.Length];
                bool[] mask = new bool[vertices.Length];

                // Parallel.For(0, 100, opts, (iter) =>
                // Task.Factory.StartNew(() =>
                for (int i = 0; i < vertices.Length; i += 3)
                {
                    var normal = Vector3D.CrossProduct(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]);

                    if (Vector3D.DotProduct(normal, vertices[i]) < 0)
                    {
                        mask[i] = true;

                        for (int j = 0; j < 3; j++)
                        {
                            var vertexShaderOutput = vertexShader.Run(vertices[i + j]);

                            // attribs[j] = attributes[i + j];
                            // attribs[j].LightDirection = vertexShaderOutput.LightDirection;

                            cs[i + j] = vertexShaderOutput.OutputVertex;
                            at[i + j] = attributes[i + j];
                            at[i + j].LightDirection = vertexShaderOutput.LightDirection;
                        }

                        ns[i] = normal.Normalize();
                    }
                }

                frameTimer.Start();

                for (int iter = 0; iter < iterations; iter++)
                {
                    for (int i = 0; i < cs.Length; i += 3)
                    {
                        if (mask[i])
                        {
                            fastRasterizer.Normals[0] = ns[i];
                            fastRasterizer.Face = i / 3;
                            fastRasterizer.Rasterize(cs.Slice(i, 3), at.Slice(i, 3), sampler);
                        }
                    }
                }

                frameTimer.Stop();
            }

            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                var fps = (int)(iterations * 1000 / System.Math.Max(1, frameTimer.ElapsedMilliseconds));
                var info = $"{frameTimer.ElapsedMilliseconds} ms / {iterations} iterations = {fps} fps";
                g.DrawString(info, SystemFonts.DefaultFont, Brushes.White, 10, 17);
            }

            renderPictureBox.Invalidate();
        }

        private ISampler LoadTexture(string filename)
        {
            using (var image = Image.FromFile(filename))
            using (var bitmap = new Bitmap(image))
            {
                var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                var texture = new byte[image.Width * image.Height * 4];
                Marshal.Copy(bitmapData.Scan0, texture, 0, texture.Length);

                bitmap.UnlockBits(bitmapData);

                return new NearestSampler(texture, bitmap.Size);
            }
        }

        private bool toolTipVisible;
        private ToolTip toolTip = new ToolTip();

        private void RenderPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if(toolTipVisible)
            {
                toolTip.Hide(this);
            }
            else
            {
                var pos = e.Location;
                toolTip.Show($"X={pos.X} Y={pos.Y}", this, e.Location);
            }

            toolTipVisible = !toolTipVisible;
        }
    }
}