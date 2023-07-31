namespace ACO.Blazor.Leaflet.Models
{
    public class WmsTileLayer : TileLayer
    {
        /// <summary>
        ///(required) Comma-separated list of WMS layers to show.
        /// </summary>
        public string Layers { get; set; } = "";

		/// <summary>
		/// Comma-separated list of WMS styles.
		/// </summary>
		public string Styles { get; set; } = "";

        /// <summary>
        /// WMS image format (use 'image/png' for layers with transparency).
        /// </summary>
        public string Format { get; set; }= "image/jpeg";

		/// <summary>
		/// f true, the WMS service will return images with transparency.
		/// </summary>
		public bool Transparent { get; set; } = false;

		/// <summary>
		/// Version of the WMS service to use
		/// </summary>
		public string Version { get; set; } = "1.1.1";

		/// <summary>
		/// If true, WMS request parameter keys will be uppercase.
		/// </summary>
		public bool Uppercase { get; set; } = false;
    }
}
