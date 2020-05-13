using MorphxLibs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerlinNoiseFlowField {
    public partial class FormMain : Form {
        // Customizable parameters (I'm too lazy to create an UI)
        private const int scale = 10;
        private const int numParticles = 6_000;
        private const bool persistRender = true;
        private const bool renderField = false && !persistRender;
        private const bool renderParticles = true;
        private const bool renderFieldHits = false && !persistRender;
        private const double forceFieldMagnitude = 2.0;
        private const double vectorAngleMultiplier = 2.0;
        private const double noiseXYInc = 0.1;
        private const double noiseZIncDivider = 12;
        private readonly Pen fieldColor = new Pen(Color.FromArgb(200, Color.Blue));
        private readonly Pen fieldHitColor = new Pen(Color.FromArgb(200, Color.Red));
        private readonly Pen particleColor = new Pen(
            persistRender ? Color.FromArgb(1, Color.GhostWhite) : Color.White
            );
        // End of Customizable parameters

        private List<Particle> particles = new List<Particle>();
        private Vector[] vectors;

        Graphics g;
        private Bitmap bmp;
        private int bw;
        private int bh;
        private double rw;
        private double rh;

        private double zOffset = 0.0;

        private readonly object syncObj = new object();

        public FormMain() {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);

            PerlinNoise.Init();
            CreateBitmapAndParticles();

            Task.Run(() => {
                while(true) {
                    this.Invalidate();
                    Thread.Sleep(30);
                    Render();
                }
            });

            this.SizeChanged += (_, __) => CreateBitmapAndParticles();
        }

        private void CreateBitmapAndParticles() {
            lock(syncObj) {
                g?.Dispose();
                bmp?.Dispose();

                bmp = new Bitmap(this.DisplayRectangle.Width, this.DisplayRectangle.Height);
                bw = bmp.Width - 1;
                bh = bmp.Height - 1;
                g = Graphics.FromImage(bmp);
                rw = bmp.Width / scale;
                rh = bmp.Height / scale;

                vectors = new Vector[(int)(rw * rh)];

                CreateParticles();
            }
        }

        private void CreateParticles() {
            Random rnd = new Random();
            for(int i = 0; i < numParticles; i++) {
                Particle p = new Particle(rnd.NextDouble() * bw,
                                          rnd.NextDouble() * bh);

                particles.Add(p);
            }
        }

        private void Render() {
            double xOffset = 0;
            double yOffset;
            int i;

            lock(syncObj) {
                if(!persistRender) g.Clear(this.BackColor);

                for(int x = 0; x < rw; x++) {
                    xOffset += noiseXYInc;
                    yOffset = 0;
                    for(int y = 0; y < rh; y++) {
                        yOffset += noiseXYInc;
                        i = (int)(x * rh + y);

                        double p = PerlinNoise.Perlin(xOffset, yOffset, zOffset);

                        Vector v = new Vector(forceFieldMagnitude, vectorAngleMultiplier * Constants.PI360 * p,
                            scale + x * scale,
                            scale + y * scale);
                        vectors[i] = v;
                        if(renderField) v.Paint(g, fieldColor, 1 / forceFieldMagnitude * scale);
                    }
                }

                particles.ForEach(p => {
                    double x = Math.Floor(p.X1 / scale);
                    double y = Math.Floor(p.Y1 / scale);
                    i = (int)(x * rh + y);
                    if(i < vectors.Length) { // FIXME: This handles an issue when resizing the window
                        p.ApplyForce(vectors[i]);

                        if(renderFieldHits) {
                            g.DrawRectangle(fieldHitColor,
                                            (float)vectors[i].X1 - scale, (float)vectors[i].Y1 - scale,
                                            scale, scale);
                        }
                    }

                    p.Update(bw, bh);
                    if(renderParticles) {
                        if(persistRender) {
                            g.DrawLine(particleColor,
                                        (float)p.LastPosition.X1,
                                        (float)p.LastPosition.Y1,
                                        (float)p.X1,
                                        (float)p.Y1);
                        } else
                            p.Render(g, particleColor);
                    }
                });
            }

            zOffset += noiseXYInc / noiseZIncDivider;
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            e.Graphics.DrawImageUnscaled(bmp, 0, 0);
        }
    }
}