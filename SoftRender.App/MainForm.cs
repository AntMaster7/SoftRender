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
        private float frameTimeAccumulator = 0;

        private Model model;

        public MainForm()
        {
            InitializeComponent();

            model = MeshLoader.Load("Cube.obj");

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
            model.Transform = Matrix4D.CreateTranslate(0, 0, -4);

            DrawArrays(model);
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
            var updated = false;

            if (!tickStopWatch.IsRunning)
            {
                tickStopWatch.Start();
                updated = true; // initial draw
            }

            var targetFrameTime = 1000 / TargetFrameRate;

            frameTimeAccumulator += (float)tickStopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond;
            if (frameTimeAccumulator > MaxFrameTime)
            {
                frameTimeAccumulator = MaxFrameTime;
            }

            tickStopWatch.Restart();

            while (frameTimeAccumulator >= targetFrameTime)
            {
                Update(TimeSpan.FromMilliseconds(targetFrameTime));
                frameTimeAccumulator -= targetFrameTime;

                updated = true;
            }

            if (updated)
            {
                DrawArrays(model);
            }
        }

        private void Update(TimeSpan delta)
        {
            const float AngularVelocity = 0.6f;

            var step = AngularVelocity * (float)delta.TotalMilliseconds / 1000;

            model.Transform = model.Transform * Matrix4D.CreateRoll(step); // * Matrix4D.CreateYaw(step);
        }

        private unsafe void DrawArrays(Model model)
        {
            if (model.Vertices.Length % 3 != 0)
            {
                throw new ArgumentException("Number of vertices must be a multiple of 3.");
            }

            int w = bitmap.Width;
            int h = bitmap.Height;

            var camera = new Camera((float)w / h);
            var projection = camera.CreateProjectionMatrix();

            var vpt = new ViewportTransform(w, h);

            int iterations = 100;
            var frameTimer = new Stopwatch();

            var mv = model.Transform;
            var mvp = projection * mv;
            var invMv = mv.GetUpperLeft().Inverse();
            var vertexShader = new VertexShader(mvp, invMv, new Vector3D(3, 0, -1));

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride, new Size(w, h), vpt);
                fastRasterizer.Mode = RasterizerMode.Fill | RasterizerMode.Wireframe;

                var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride, vpt);

                var opts = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4,
                };

                Span<Vector4D> cs = new Vector4D[model.Vertices.Length];
                Span<VertexAttributes> at = new VertexAttributes[model.Vertices.Length];
                Span<Vector3D> ns = new Vector3D[model.Vertices.Length];

                // Parallel.For(0, 100, opts, (iter) =>
                // Task.Factory.StartNew(() =>
                for (int i = 0; i < model.Vertices.Length; i += 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var vertexShaderOutput = vertexShader.Run(model.Vertices[i + j], model.Attributes[i + j].Normal);

                        cs[i + j] = vertexShaderOutput.OutputVertex;
                        ns[i + j] = vertexShaderOutput.OutputNormal;
                        at[i + j] = model.Attributes[i + j];
                        at[i + j].LightDirection = vertexShaderOutput.LightDirection;
                    }
                }

                frameTimer.Start();

                for (int iter = 0; iter < iterations; iter++)
                {
                    for (int i = 0; i < cs.Length; i += 3)
                    {
                        fastRasterizer.Normals[0] = ns[i];
                        fastRasterizer.Face = i / 3;
                        fastRasterizer.Rasterize(cs.Slice(i, 3), at.Slice(i, 3), sampler);
                    }
                }

                frameTimer.Stop();

                // Quick hack to render normals
                for (int i = 0; i < ns.Length; i++)
                {
                    var a = cs[i].PerspectiveDivide();
                    var p = new Vector4D(ns[i].X, ns[i].Y, ns[i].Z, 0);
                    var q = mv * model.Vertices[i] + p;
                    var b = (projection * q).PerspectiveDivide();
                    var p1 = vpt * a;
                    var p2 = vpt * b;
                    ctx.DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, new ColorRGB(255, 0, 0));
                }
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
            if (toolTipVisible)
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