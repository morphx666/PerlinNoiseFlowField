using MorphxLibs;
using System.Drawing;

namespace PerlinNoiseFlowField {
    public class Particle : Vector {
        private Vector vel = Vector.Empty;
        private Vector acc = Vector.Empty;
        private Vector lastPos;

        public Particle(double x, double y) : base(1, 0, x, y) {
            SetLastPosition();
        }

        public Vector LastPosition {
            get => lastPos;
        }

        private void SetLastPosition() {
            lastPos = new Vector(this);
        }

        public void Update(int w, int h) {
            vel += acc;
            if(vel.Magnitude > 4) vel.Magnitude = 4;
            SetLastPosition();
            base.Move(vel);
            acc.Magnitude = 0;

            bool resetLastPosition = false;
            if(base.X1 < 0) {
                base.X1 = w;
                resetLastPosition = true;
            } else if(base.X1 > w) {
                base.X1 = 0;
                resetLastPosition = true;
            }

            if(base.Y1 < 0) {
                base.Y1 = h;
                resetLastPosition = true;
            } else if(base.Y1 > h) {
                base.Y1 = 0;
                resetLastPosition = true;
            }

            if(resetLastPosition) lastPos.TranslateAbs(this);
        }

        public void ApplyForce(Vector force) {
            acc = new Vector(force);
        }

        public void Render(Graphics g, Pen c) {
            //g.FillRectangle(Brushes.White, (float)this.X1, (float)this.Y1, 2f, 2f);
            g.DrawRectangle(c, (float)this.X1, (float)this.Y1, 1, 1);
        }
    }
}