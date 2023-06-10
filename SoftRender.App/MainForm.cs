using SoftRender.Graphics;
using SoftRender.SRMath;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SoftRender.App
{
    public partial class MainForm : Form
    {
        private Bitmap bitmap;
        private ISampler sampler;

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

            model.Vertices = new Vector3D[]
            {
                new Vector3D(0.0f, 0.3f, 0f),
                new Vector3D(-0.3f, -0.7f, 0f),
                new Vector3D(0.7f, -0.7f, 0f)
            };

            sampler = LoadTexture("brickwall-512x512.jpg");
            //sampler = LoadTexture("test.png");

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;

            // animationTimer.Start();
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

            int iterations = 1;
            var sw = new Stopwatch();

            bool wireframe = true;

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride);
                var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride);

                sw.Start();

                var clipSpace = new Vector4D[3];
                var ndc = new Vector3D[3];
                var vp = new Vector3D[3];
                var attribs = new VertexAttributes[3];

                for (int i = 0; i < vertices.Length; i += 3)
                {
                    clipSpace[0] = frustum * vertices[i + 0];
                    clipSpace[1] = frustum * vertices[i + 1];
                    clipSpace[2] = frustum * vertices[i + 2];

                    ndc[0] = clipSpace[0].PerspectiveDivide();
                    ndc[1] = clipSpace[1].PerspectiveDivide();
                    ndc[2] = clipSpace[2].PerspectiveDivide();

                    vp[0] = viewportTransform * ndc[0];
                    vp[1] = viewportTransform * ndc[1];
                    vp[2] = viewportTransform * ndc[2];

                    attribs[0] = attributes[i + 0];
                    attribs[1] = attributes[i + 1];
                    attribs[2] = attributes[i + 2];


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

                sw.Stop();
            }

            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                var fps = (int)(iterations * 1000 / System.Math.Max(1, sw.ElapsedMilliseconds));
                var info = $"{sw.ElapsedMilliseconds} ms / {iterations} iterations = {fps} fps";
                g.DrawString(info, SystemFonts.DefaultFont, Brushes.White, 10, 17);
            }

            //var dimensions = new Point(700, 700);
            //var rect = new Rectangle(w / 2 - dimensions.X / 2, h / 2 - dimensions.Y / 2, dimensions.X, dimensions.Y);
            //for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            //{
            //    for (int x = rect.X; x < rect.X + rect.Width; x++)
            //    {
            //        var u  = (float)(x - rect.X) / rect.Width;
            //        var v = (float)(y - rect.Y) / rect.Height;

            //        var color = sampler.Sample(u, v);

            //        ctx.DrawPixel(x, y, color);
            //    }
            //}

            //ctx.DrawLine(p1, p2, ColorRGB.FromSystemColor(Color.White));
            //ctx.DrawLine(p2, p3, ColorRGB.FromSystemColor(Color.White));
            //ctx.DrawLine(p3, p1, ColorRGB.FromSystemColor(Color.White));

            renderPictureBox.Invalidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            var rot = Matrix4D.CreateTranslate(0, 0, -4) * Matrix4D.CreateFromYaw(0.01f) * Matrix4D.CreateTranslate(0, 0, 4);

            for (int i = 0; i < model.Vertices.Length; i++)
            {
                model.Vertices[i] = (rot * model.Vertices[i]).PerspectiveDivide();
            }

            DrawArrays(model.Vertices, model.Attributes);
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