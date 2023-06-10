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
        
        //private Vector3D[] model = new Vector3D[]
        //{
        //    new Vector3D(0.0f, 0.3f, -1f),
        //    new Vector3D(-0.3f, -0.7f, -1f),
        //    new Vector3D(0.7f, -0.7f, -1f)

        //    //new Vector3D(1.0f, 1.0f, 1f),
        //    //new Vector3D(-1.0f, 1.0f,1f),
        //    //new Vector3D(-1.0f, -1.0f, 1f)
        //};

        private Model model;

        public MainForm()
        {
            InitializeComponent();

            model = ObjLoader.Load("Cube.obj");

            sampler = LoadTexture("brickwall-512x512.jpg");
            //sampler = LoadTexture("test.png");

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;

            Application.Idle += Application_Idle;
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
            if(frameTimeAccumulator > MaxFrameTime)
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
            const float AngularVelocity = 1f;

            var step = AngularVelocity * (float)delta.TotalMilliseconds / 1000; 

            var rot = Matrix4D.CreateTranslate(0, 0, -4) * Matrix4D.CreateFromYaw(step) * Matrix4D.CreateTranslate(0, 0, 4);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                model.Vertices[i] = (rot * model.Vertices[i]).PerspectiveDivide();
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var rot = Matrix4D.CreateTranslate(0, 0, -4) * Matrix4D.CreateFromYaw(1f);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                model.Vertices[i] = (rot * model.Vertices[i]).PerspectiveDivide();
            }

            DrawArrays(model.Vertices, model.Attributes);
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

            var viewportTransform = new ViewportTransform(w, h);

            int iterations = 100;
            var frameTimer = new Stopwatch();

            bool wireframe = true;

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride);
                var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride);

                var clipSpace = new Vector4D[3];
                var ndc = new Vector3D[3];
                var vp = new Vector3D[3];
                var attribs = new VertexAttributes[3];
 
                frameTimer.Start();

                for (int iter = 0; iter < iterations; iter++)
                for (int i = 0; i < vertices.Length; i += 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        clipSpace[j] = frustum * vertices[i + j];
                        ndc[j] = clipSpace[j].PerspectiveDivide();
                        vp[j] = viewportTransform * ndc[j];
                        attribs[j] = attributes[i + j];
                        attribs[j].Z = clipSpace[j].W; // -ndc[j].Z;
                    }

                    var normal = Vector3D.CrossProduct(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]);

                    if (Vector3D.DotProduct(normal, vertices[i] - new Vector3D(0, 0, 0)) < 0)
                    {
                        fastRasterizer.Rasterize(vp, attribs, sampler);
                        // simpleRasterizer.Rasterize(vp, attribs, sampler);

                        if (wireframe)
                        {
                            ctx.DrawLine((int)vp[0].X, (int)vp[0].Y, (int)vp[1].X, (int)vp[1].Y, new ColorRGB(255, 255, 255));
                            ctx.DrawLine((int)vp[1].X, (int)vp[1].Y, (int)vp[2].X, (int)vp[2].Y, new ColorRGB(255, 255, 255));
                            ctx.DrawLine((int)vp[2].X, (int)vp[2].Y, (int)vp[0].X, (int)vp[0].Y, new ColorRGB(255, 255, 255));
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
    }
}