namespace FaceDiff.Models
{
    public class OvalRegion
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }

        /// <summary>Rotation angle in degrees.</summary>
        public double Angle { get; set; }

        public bool ContainsPoint(double x, double y)
        {
            double dx = x - CenterX;
            double dy = y - CenterY;
            if (RadiusX <= 0 || RadiusY <= 0) return false;
            return (dx * dx) / (RadiusX * RadiusX) + (dy * dy) / (RadiusY * RadiusY) <= 1.0;
        }

        public OvalRegion Clone()
        {
            return new OvalRegion
            {
                CenterX = CenterX,
                CenterY = CenterY,
                RadiusX = RadiusX,
                RadiusY = RadiusY,
                Angle = Angle
            };
        }
    }
}
