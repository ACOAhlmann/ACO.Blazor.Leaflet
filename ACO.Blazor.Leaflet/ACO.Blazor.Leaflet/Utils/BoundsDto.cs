using ACO.Blazor.Leaflet.Models;

namespace ACO.Blazor.Leaflet.Utils
{
    public class BoundsDto
    {
        public LatLng _southWest { get; set; }
        public LatLng _northEast { get; set; }

        public Bounds AsBounds() => new (_southWest, _northEast);
    }
}