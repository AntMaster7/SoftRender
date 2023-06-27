using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public partial class MainForm : Form
    {
        private const int TargetFrameRate = 60;
        private const int MaxFrameTime = 200;

        private Bitmap bitmap;
        private Stopwatch tickStopWatch = new Stopwatch();
        private float frameTimeAccumulator = 0;
        private MovingAverage averageElapsedMilliseconds = new MovingAverage(10);

        private Scene scene = new Scene();

        public MainForm()
        {
            InitializeComponent();

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

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;

            Application.Idle += Application_Idle;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var suzanne = MeshLoader.Load("Suzanne.obj");
            suzanne.Texture = LoadTexture("brickwall-512x512.jpg");
            suzanne.Transform = Matrix4D.CreateTranslate(0, 0, -3);
            scene.Models.Add(suzanne);

            var plane = MeshLoader.Load("Plane.obj");
            plane.Texture = LoadTexture("white-1x1.jpg");
            plane.Transform = Matrix4D.CreateTranslate(0, -1, -3f) * Matrix4D.CreateScale(1.4f, 1, 2);
            scene.Models.Add(plane);

            var spotLight1 = new Light();
            spotLight1.Transform = Matrix4D.CreateTranslate(0, 2, -2);
            scene.Lights.Add(spotLight1);

            scene.Camera = new Camera((float)bitmap.Width / bitmap.Height);
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
                DrawScene();
            }
        }

        private void Update(TimeSpan delta)
        {
            const float AngularVelocity = 0.4f;

            var step = AngularVelocity * (float)delta.TotalMilliseconds / 1000;

            // scene.Models[0].Transform = scene.Models[0].Transform * Matrix4D.CreateYaw(step); // * Matrix4D.CreateYaw(step);

            if ((WinNative.GetKeyState(Keys.Down) & WinNative.KEY_PRESSED) == WinNative.KEY_PRESSED)
            {
                scene.Lights[0].Transform *= Matrix4D.CreateTranslate(0, -0.01f, 0);
            }
            else if ((WinNative.GetKeyState(Keys.Up) & WinNative.KEY_PRESSED) == WinNative.KEY_PRESSED)
            {
                scene.Lights[0].Transform *= Matrix4D.CreateTranslate(0, 0.01f, 0);
            }

        }

        private unsafe void DrawScene()
        {
            int w = bitmap.Width;
            int h = bitmap.Height;
            var vpt = new ViewportTransform(w, h);

            var frameTimer = new Stopwatch();

            Renderer renderer;

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var rasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride, new Size(w, h), vpt);
                rasterizer.Mode = RasterizerMode.Fill; // | RasterizerMode.Wireframe;

                renderer = new Renderer(rasterizer);

                frameTimer.Start();

                scene.Render(renderer);

                frameTimer.Stop();



                // frameTime = renderer.Render(model.Vertices, model.Attributes, sampler);

                // var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride, vpt);

                // Quick hack to render normals
                //for (int i = 0; i < ns.Length; i++)
                //{
                //    var a = cs[i].PerspectiveDivide();
                //    var p = new Vector4D(ns[i].X, ns[i].Y, ns[i].Z, 0);
                //    var q = mv * model.Vertices[i] + p;
                //    var b = (projection * q).PerspectiveDivide();
                //    var p1 = vpt * a;
                //    var p2 = vpt * b;
                //    ctx.DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, new ColorRGB(255, 0, 0));
                //}
            }

            averageElapsedMilliseconds.Push((int)frameTimer.ElapsedMilliseconds);

            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                var fps = (int)(renderer.Iterations * 1000 / System.Math.Max(1, frameTimer.ElapsedMilliseconds));
                var info = $"{averageElapsedMilliseconds.GetAverage()} ms / {renderer.Iterations} iterations = {fps} fps";
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