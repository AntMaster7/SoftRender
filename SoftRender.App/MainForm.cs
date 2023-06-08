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

        private Vector2D[] tri = new Vector2D[]
        {
            new Vector2D(0.0f, 0.3f),
            new Vector2D(-0.3f, -0.7f),
            new Vector2D(0.7f, -0.7f)
        };

        public MainForm()
        {
            InitializeComponent();

            sampler = LoadTexture("brickwall-512x512.jpg");

            bitmap = new Bitmap(renderPictureBox.ClientSize.Width, renderPictureBox.ClientSize.Height);
            renderPictureBox.Image = bitmap;

            p1 = new Point(bitmap.Width / 2, bitmap.Height / 2);
            p2 = new Point(p1.X + 100, p1.Y);
            pr = new Vector2D(p2);

            // animationTimer.Start();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            WinNative.AllocConsole();

            Draw();
        }

        private void RenderPictureBox_SizeChanged(object sender, EventArgs e)
        {
            Draw();
        }

        private unsafe void Draw()
        {
            using var ctx = new BitmapContext(bitmap);

            int w = bitmap.Width;
            int h = bitmap.Height;

            ctx.Clear(0);

            var viewportTransform = new ViewportTransform(w, h);

            var polygon = new Point[3];
            polygon[0] = viewportTransform * tri[0];
            polygon[1] = viewportTransform * tri[1];
            polygon[2] = viewportTransform * tri[2];

            var fastRasterizer = new FastRasterizer(ctx.Scan0, ctx.Stride);
            var simpleRasterizer = new SimpleRasterizer(ctx.Scan0, ctx.Stride);

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100; i++)
            {
                fastRasterizer.Rasterize(polygon);
                // simpleRasterizer.Rasterize(polygon);
            }

            sw.Stop();

            Console.WriteLine(fastRasterizer.Dummy);
            Console.WriteLine(simpleRasterizer.Dummy);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");


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
            var textureImage = Image.FromFile(filename);

            using (var textureBitmap = new Bitmap(textureImage.Width, textureImage.Height, PixelFormat.Format24bppRgb))
            {
                var bitmapData = textureBitmap.LockBits(new Rectangle(Point.Empty, textureBitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                
                var texture = new byte[textureImage.Width * textureImage.Height * 3];
                Marshal.Copy(bitmapData.Scan0, texture, 0, texture.Length);

                textureBitmap.UnlockBits(bitmapData);

                return new NearestSampler(texture, textureBitmap.Size);
            }
        }
    }
}