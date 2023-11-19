

namespace rt
{
    public class Line
    {
        public Vector X0 { get; set; }
        public Vector Dx { get; set; }

        public Line()
        {
            X0 = new Vector(0.0, 0.0, 0.0);
            Dx = new Vector(1.0, 0.0, 0.0);
        }

        public Line(Vector x0, Vector x1)
        {
            X0 = new Vector(x0);
            Dx = new Vector(x1 - x0);
            Dx.Normalize();
        }

        public Vector CoordinateToPosition(double t)
        {
            return new Vector(Dx * t + X0);
        }

        public double T_for_X(double x)
        {
            return (x - X0.X) / Dx.X;
        }

        public double T_for_Y(double y)
        {
            return (y - X0.Y) / Dx.Y;
        }

        public double T_for_Z(double z)
        {
            return (z - X0.Z) / Dx.Z;
        }
    }
}