namespace ACO.Blazor.Leaflet.Models
{
	public class GeoJsonDataLayer : InteractiveLayer
	{
		public string GeoJsonData { get; set; }

		public GeoJsonStyle Style { get; set; }

	}

	public class GeoJsonStyle
	{
		public string Color { get; set; } = "#ff7800";

		public float Opacity { get; set; } = 1f;
		public float Weight { get; set; } = 3;

		public string FillColor { get; set; } = "#ff7800";
        public float FillOpacity { get; set; } = 0.5f;
	}
}
