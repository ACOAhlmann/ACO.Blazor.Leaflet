namespace ACO.Blazor.Leaflet.Models
{
    public class Transformation
    {
        public Transformation(double a, double b, double c, double d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
    }
}
