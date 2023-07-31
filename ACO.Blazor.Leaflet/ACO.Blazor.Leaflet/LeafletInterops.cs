using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;
using ACO.Blazor.Leaflet.Models;
using Rectangle = ACO.Blazor.Leaflet.Models.Rectangle;
using System.Runtime.InteropServices;

namespace ACO.Blazor.Leaflet
{
    public static class LeafletInterops
    {
        private static ConcurrentDictionary<string, (IDisposable, string, Layer)> LayerReferences { get; }
            = new ConcurrentDictionary<string, (IDisposable, string, Layer)>();

        private static readonly string _BaseObjectContainer = "window.leafletBlazor";

        public static ValueTask Create(IJSRuntime js, Map map) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.create", map, DotNetObjectReference.Create(map));

        private static DotNetObjectReference<T> CreateLayerReference<T>(string mapId, T layer) where T : Layer
        {
            var result = DotNetObjectReference.Create(layer);
            LayerReferences.TryAdd(layer.Id, (result, mapId, layer));
            return result;
        }

        private static void DisposeLayerReference(string layerId)
        {
            if (LayerReferences.TryRemove(layerId, out var value))
                value.Item1.Dispose();
        }

        public static ValueTask AddLayer(IJSRuntime js, string mapId, Layer layer)
        {
            return layer switch
            {
                WmsTileLayer wmsTileLayer => js.InvokeVoidAsync($"{_BaseObjectContainer}.addWmsTilesLayer", mapId, wmsTileLayer, CreateLayerReference(mapId, wmsTileLayer)),
                TileLayer tileLayer => js.InvokeVoidAsync($"{_BaseObjectContainer}.addTilelayer", mapId, tileLayer, CreateLayerReference(mapId, tileLayer)),
                MbTilesLayer mbTilesLayer => js.InvokeVoidAsync($"{_BaseObjectContainer}.addMbTilesLayer", mapId, mbTilesLayer, CreateLayerReference(mapId, mbTilesLayer)),
                ShapefileLayer shapefileLayer => js.InvokeVoidAsync($"{_BaseObjectContainer}.addShapefileLayer", mapId, shapefileLayer, CreateLayerReference(mapId, shapefileLayer)),
                Marker marker => js.InvokeVoidAsync($"{_BaseObjectContainer}.addMarker", mapId, marker, CreateLayerReference(mapId, marker)),
                Rectangle rectangle => js.InvokeVoidAsync($"{_BaseObjectContainer}.addRectangle", mapId, rectangle, CreateLayerReference(mapId, rectangle)),
                Circle circle => js.InvokeVoidAsync($"{_BaseObjectContainer}.addCircle", mapId, circle, CreateLayerReference(mapId, circle)),
                Polygon polygon => js.InvokeVoidAsync($"{_BaseObjectContainer}.addPolygon", mapId, polygon, CreateLayerReference(mapId, polygon)),
                Polyline polyline => js.InvokeVoidAsync($"{_BaseObjectContainer}.addPolyline", mapId, polyline, CreateLayerReference(mapId, polyline)),
                ImageRotatedLayer imageRotated => js.InvokeVoidAsync($"{_BaseObjectContainer}.addImageRotatedLayer", mapId, imageRotated, CreateLayerReference(mapId, imageRotated)),
                ImageLayer image => js.InvokeVoidAsync($"{_BaseObjectContainer}.addImageLayer", mapId, image, CreateLayerReference(mapId, image)),
                GeoJsonDataLayer geo => js.InvokeVoidAsync($"{_BaseObjectContainer}.addGeoJsonLayer", mapId, geo, CreateLayerReference(mapId, geo)),
                HeatmapLayer heat => js.InvokeVoidAsync($"{_BaseObjectContainer}.addHeatLayer", mapId, heat, CreateLayerReference(mapId, heat)),
                _ => throw new NotImplementedException($"The layer {typeof(Layer).Name} has not been implemented."),
            };
        }

        public static async Task RemoveLayer(IJSRuntime js, string mapId, string layerId)
        {
            await js.InvokeVoidAsync($"{_BaseObjectContainer}.removeLayer", mapId, layerId);
            DisposeLayerReference(layerId);
        }

        public static ValueTask UpdatePopupContent(IJSRuntime js, string mapId, Layer layer) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.updatePopupContent", mapId, layer.Id, layer.Popup?.Content);

        public static ValueTask UpdateTooltipContent(IJSRuntime js, string mapId, Layer layer) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.updateTooltipContent", mapId, layer.Id, layer.Tooltip?.Content);

        public static ValueTask UpdateShape(IJSRuntime js, string mapId, Layer layer) =>
            layer switch
            {
                Models.Rectangle rectangle => js.InvokeVoidAsync($"{_BaseObjectContainer}.updateRectangle", mapId, rectangle),
                Circle circle => js.InvokeVoidAsync($"{_BaseObjectContainer}.updateCircle", mapId, circle),
                Polygon polygon => js.InvokeVoidAsync($"{_BaseObjectContainer}.updatePolygon", mapId, polygon),
                Polyline polyline => js.InvokeVoidAsync($"{_BaseObjectContainer}.updatePolyline", mapId, polyline),
                _ => throw new NotImplementedException($"The layer {typeof(Layer).Name} has not been implemented."),
            };
        public static ValueTask SetOpacity(IJSRuntime js, string mapId, Layer layer)
                => layer switch
                {
                    Models.GridLayer gridLayer => js.InvokeVoidAsync($"{_BaseObjectContainer}.setOpacity", mapId, gridLayer.Id,gridLayer.Opacity),
                    _ => throw new NotImplementedException($"The layer {typeof(Layer).Name} has not been implemented."),
                };



        public static ValueTask BringPathToFront(this Path path, IJSRuntime js, string mapId)
                => js.InvokeVoidAsync($"{_BaseObjectContainer}.bringPathToFront", mapId, path);

        public static ValueTask BringPathToBack(this Path path, IJSRuntime js, string mapId)
            => js.InvokeVoidAsync($"{_BaseObjectContainer}.bringPathToBack", mapId, path);


        public static ValueTask OpenLayerPopup(IJSRuntime js, string mapId, Marker marker)
            => js.InvokeVoidAsync($"{_BaseObjectContainer}.openLayerPopup", mapId, marker.Id);

        public static ValueTask FitBounds(IJSRuntime js, string mapId, PointF corner1, PointF corner2, PointF? padding, float? maxZoom) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.fitBounds", mapId, corner1, corner2, padding, maxZoom);


        public static ValueTask PanTo(IJSRuntime js, string mapId, PointF position, bool animate, float duration, float easeLinearity, bool noMoveStart) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.panTo", mapId, position, animate, duration, easeLinearity, noMoveStart);


        public static async ValueTask<Bounds> GetBoundsFromMarkers(IJSRuntime js, params Marker[] markers)
            => (await js.InvokeAsync<_Bounds>($"{_BaseObjectContainer}.getBoundsFromMarker", new[] { markers })).AsBounds();

        public static ValueTask<LatLng> GetCenter(IJSRuntime js, string mapId) =>
            js.InvokeAsync<LatLng>($"{_BaseObjectContainer}.getCenter", mapId);

        public static ValueTask<float> GetZoom(IJSRuntime js, string mapId) =>
            js.InvokeAsync<float>($"{_BaseObjectContainer}.getZoom", mapId);

        // Private class only for deserialization from JSON (since the JSON names on the bounds are "_southWest"
        // with the _). Since deserialization in JSRuntime is non-customizable, this is a good solution for now.
        private class _Bounds
        {
            public LatLng _southWest { get; set; }
            public LatLng _northEast { get; set; }

            public Bounds AsBounds() => new Bounds(_southWest, _northEast);
        }

        public static async Task<Bounds> GetBounds(IJSRuntime js, string mapId)
        {
            return (await js.InvokeAsync<_Bounds>($"{_BaseObjectContainer}.getBounds", mapId)).AsBounds();
        }

        public static ValueTask ZoomIn(IJSRuntime js, string mapId, MouseEventArgs e) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.zoomIn", mapId, e);

        public static ValueTask ZoomOut(IJSRuntime js, string mapId, MouseEventArgs e) =>
            js.InvokeVoidAsync($"{_BaseObjectContainer}.zoomOut", mapId, e);

        public static ValueTask<LatLng> ForwardProj(IJSRuntime js, string mapId, LatLng latlon)
            => js.InvokeAsync<LatLng>($"{_BaseObjectContainer}.forwardProj", mapId, latlon);

    }
}