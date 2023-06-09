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
        private Point p1, p2;
        private Vector2D pr;
        private ISampler sampler;

        private Vector3D[] triNDC = new Vector3D[]
        {
            new Vector3D(0.0f, 0.3f, 1f),
            new Vector3D(-0.3f, -0.7f,1f),
            new Vector3D(0.7f, -0.7f, 1f)
        };

        public MainForm()
        {
            InitializeComponent();

            sampler = LoadTexture("brickwall-512x512.jpg");
            //sampler = LoadTexture("test.png");

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;

            p1 = new Point(bitmap.Width / 2, bitmap.Height / 2);
            p2 = new Point(p1.X + 100, p1.Y);
            pr = new Vector2D(p2);

            // animationTimer.Start();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Draw();
        }

        private void RenderPictureBox_SizeChanged(object sender, EventArgs e)
        {
            Draw();
        }

        private unsafe void Draw()
        {
            int w = bitmap.Width;
            int h = bitmap.Height;

            var viewportTransform = new ViewportTransform(w, h);

            var polygon = new Vector3D[3];
            polygon[0] = viewportTransform * triNDC[0];
            polygon[1] = viewportTransform * triNDC[1];
            polygon[2] = viewportTransform * triNDC[2];

            var attribs = new VertexAttributes[3];
            attribs[0] = new VertexAttributes(1, 255, 0, 0, 0.5f, 0.9f);
            attribs[1] = new VertexAttributes(1, 0, 255, 0, 0, 0);
            attribs[2] = new VertexAttributes(1, 0, 0, 255, 1, 0);

            int iterations = 100;
            var sw = new Stopwatch();

            using (var ctx = new BitmapContext(bitmap))
            {
                ctx.Clear(0);

                var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride);
                var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride);


                sw.Start();

                for (int i = 0; i < iterations; i++)
                {
                    fastRasterizer.Rasterize(polygon, attribs, sampler);
                    // simpleRasterizer.Rasterize(polygon, attribs, sampler);
                    // simpleRasterizer.DrawTexture(sampler, new Rectangle(0,0, w, h));
                }

                sw.Stop();
            }

            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.DrawString($"{sw.ElapsedMilliseconds} ms / {iterations} iterations", SystemFonts.DefaultFont, Brushes.White, 10, 17);
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
            var rot = Matrix2D.Rotate(0.1f);
            pr.Translate(-p1.X, -p1.Y);
            pr = rot * pr;
            pr.Translate(p1.X, p1.Y);
            p2 = pr.ToPoint();

            Draw();
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