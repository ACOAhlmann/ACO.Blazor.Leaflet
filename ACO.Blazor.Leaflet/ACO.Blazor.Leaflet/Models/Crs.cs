namespace ACO.Blazor.Leaflet.Models
{
    using System;
    using System.ComponentModel.Design;

    public class Crs
	{
        //https://kartena.github.io/Proj4Leaflet/api/

        public Crs(string code, string proj4def, LatLng origin, Transformation transformation, double[] resolutions, Bounds bounds)
        {
            Code = code;
            Proj4def = proj4def;
            Origin = origin;
            Transformation = transformation;
            Resolutions = resolutions;
            Bounds = bounds;
        }
        /// <summary>
        /// CRS identifier
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Proj4 definition for the projection
        /// </summary>
        public string Proj4def { get; set; }

        /// <summary>
        /// Tile origin, in projected coordinates; 
        /// if set, this overrides the transformation option
        /// </summary>
        public LatLng Origin { get; set; }

        public Transformation Transformation { get; set; }

        /// <summary>
        /// Resolution factors (projection units per pixel, for example meters/pixel) for zoom levels
        /// </summary>
        public double[] Resolutions { get; set; }

        /// <summary>
        /// Bounds of the CRS, in projected coordinates
        /// if defined, Proj4Leaflet will use this in the getSize method, otherwise defaulting to Leaflet’s default CRS size
        /// </summary>
        public Bounds Bounds { get; set; }


        //crs EPSG28992 taken from: https://github.com/arbakker/pdok-js-map-examples/blob/master/leaflet-geojson-wmts-epsg28992/index.js
        public static Crs EPSG28992 => new Crs(
            code: "EPSG:28992", 
            proj4def: "+proj=sterea +lat_0=52.15616055555555 +lon_0=5.38763888888889 +k=0.9999079 +x_0=155000 +y_0=463000 +ellps=bessel +units=m +towgs84=565.2369,50.0087,465.658,-0.406857330322398,0.350732676542563,-1.8703473836068,4.0812 +no_defs",
            origin: new LatLng(-285401.920f, 903401.920f),
            transformation: new Transformation(-1, -1, 0, 0),
            resolutions: new double[] { 3440.640, 1720.320, 860.160, 430.080, 215.040, 107.520, 53.760, 26.880, 13.440, 6.720, 3.360, 1.680, 0.840, 0.420, 0.210, 0.105, 0.0525 },
            bounds: new Bounds(new LatLng(-285401.920f, 903401.920f), new LatLng(595401.920f, 22598.080f))
            );

    }
}
