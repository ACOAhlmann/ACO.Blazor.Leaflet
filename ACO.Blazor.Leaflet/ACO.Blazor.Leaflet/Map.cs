using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ACO.Blazor.Leaflet.Exceptions;
using ACO.Blazor.Leaflet.Models;
using ACO.Blazor.Leaflet.Models.Events;
using ACO.Blazor.Leaflet.Utils;
using Rectangle = ACO.Blazor.Leaflet.Models.Rectangle;

namespace ACO.Blazor.Leaflet
{
    public class Map
    {
        /// <summary>
        /// Initial geographic center of the map
        /// </summary>
        public LatLng Center { get; set; } = new();

        /// <summary>
        /// Initial map zoom level
        /// </summary>
        public float Zoom { get; set; }


        /// <summary>
        /// Map bounds
        /// </summary>
        public Bounds Bounds { get; private set; }

        /// <summary>
        /// Minimum zoom level of the map. If not specified and at least one 
        /// GridLayer or TileLayer is in the map, the lowest of their minZoom
        /// options will be used instead.
        /// </summary>
        public float? MinZoom { get; set; }

        /// <summary>
        /// Maximum zoom level of the map. If not specified and at least one
        /// GridLayer or TileLayer is in the map, the highest of their maxZoom
        /// options will be used instead.
        /// </summary>
        public float? MaxZoom { get; set; }

        /// <summary>
        /// When this option is set, the map restricts the view to the given
        /// geographical bounds, bouncing the user back if the user tries to pan
        /// outside the view.
        /// </summary>
        public Tuple<LatLng, LatLng> MaxBounds { get; set; }

        /// <summary>
        /// Whether a zoom control is added to the map by default.
        /// <para/>
        /// Defaults to true.
        /// </summary>
        public bool ZoomControl { get; set; } = true;

        /// <summary>
        /// Event raised when the component has finished its first render.
        /// </summary>
        public event Action OnInitialized;

        public string Id { get; }

        private readonly ObservableCollection<Layer> _layers = new();
        private readonly ConcurrentDictionary<string, IDisposable> _layerReferences = new();

        private readonly IJSRuntime _jsRuntime;

        private static readonly string JsZoomOut = $"{JsInterops.JsInteropConfig.BasePath}.zoomOut";
        private static readonly string JsZoomIn = $"{JsInterops.JsInteropConfig.BasePath}.zoomIn";
        private static readonly string JsGetBounds = $"{JsInterops.JsInteropConfig.BasePath}.getBounds";
        private static readonly string JsGetZoom = $"{JsInterops.JsInteropConfig.BasePath}.getZoom";
        private static readonly string JsGetCenter = $"{JsInterops.JsInteropConfig.BasePath}.getCenter";
        private static readonly string JsPanTo = $"{JsInterops.JsInteropConfig.BasePath}.panTo";
        private static readonly string JsGetBoundsFromMarker =
            $"{JsInterops.JsInteropConfig.BasePath}.getBoundsFromMarker";
        
        private static readonly string JsFitBounds = $"{JsInterops.JsInteropConfig.BasePath}.fitBounds";
        private static readonly string JsOpenLayerPopup = $"{JsInterops.JsInteropConfig.BasePath}.openLayerPopup";
        
        private static readonly string JsAddTileLayer = $"{JsInterops.JsInteropConfig.BasePath}.addTileLayer";
        private static readonly string JsAddMbTilesLayer = $"{JsInterops.JsInteropConfig.BasePath}.addMbTilesLayer";
        private static readonly string JsAddShapefileLayer = $"{JsInterops.JsInteropConfig.BasePath}.addShapefileLayer";
        private static readonly string JsAddMarker = $"{JsInterops.JsInteropConfig.BasePath}.addMarker";
        private static readonly string JsAddRectangle = $"{JsInterops.JsInteropConfig.BasePath}.addRectangle";
        private static readonly string JsAddCircle = $"{JsInterops.JsInteropConfig.BasePath}.addCircle";
        private static readonly string JsAddPolyline = $"{JsInterops.JsInteropConfig.BasePath}.addPolyline";
        private static readonly string JsAddPolygon = $"{JsInterops.JsInteropConfig.BasePath}.addPolygon";
        private static readonly string JsAddImageRotatedLayer =
            $"{JsInterops.JsInteropConfig.BasePath}.addImageRotatedLayer";
        private static readonly string JsAddImageLayer = $"{JsInterops.JsInteropConfig.BasePath}.addImageLayer";
        private static readonly string JsAddGeoJsonLayer = $"{JsInterops.JsInteropConfig.BasePath}.addGeoJsonLayer";
        private static readonly string JsAddHeatLayer = $"{JsInterops.JsInteropConfig.BasePath}.addHeatLayer";
        private static readonly string JsRemoveLayer = $"{JsInterops.JsInteropConfig.BasePath}.removeLayer";

        public bool IsInitialized { get; private set; }

        public Map(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            Id = StringHelper.GetRandomString(10);

            _layers.CollectionChanged += OnLayersChanged;
        }

        /// <summary>
        /// This method MUST be called only once by the Blazor component upon rendering, and never by the user.
        /// </summary>
        public void RaiseOnInitialized()
        {
            IsInitialized = true;
            OnInitialized?.Invoke();
            RunTaskInBackground(UpdateBounds);
        }

        private async void RunTaskInBackground(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                NotifyBackgroundExceptionOccurred(ex);
            }
        }

        /// <summary>
        /// Add a layer to the map.
        /// </summary>
        /// <param name="layer">The layer to be added.</param>
        /// <exception cref="System.ArgumentNullException">Throws when the layer is null.</exception>
        /// <exception cref="UninitializedMapException">Throws when the map has not been yet initialized.</exception>
        public void AddLayer(Layer layer)
        {
            if (layer is null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            if (!IsInitialized)
            {
                throw new UninitializedMapException();
            }

            _layers.Add(layer);
        }

        public ValueTask OpenMarkerPopup(Marker marker) => _jsRuntime.InvokeVoidAsync(JsOpenLayerPopup, Id, marker.Id);

        /// <summary>
        /// Remove a layer from the map.
        /// </summary>
        /// <param name="layer">The layer to be removed.</param>
        /// <exception cref="System.ArgumentNullException">Throws when the layer is null.</exception>
        /// <exception cref="UninitializedMapException">Throws when the map has not been yet initialized.</exception>
        public void RemoveLayer(Layer layer)
        {
            if (layer is null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            if (!IsInitialized)
            {
                throw new UninitializedMapException();
            }

            _layers.Remove(layer);
        }

        public void RemoveAllLayersOfType<TLayer>() where TLayer : Layer
        {
            if (!IsInitialized)
            {
                throw new UninitializedMapException();
            }

            var rm = _layers.Where(IsSameOrSubclass<TLayer>).ToArray();
            foreach (var layer in rm)
            {
                _layers.Remove(layer);
            }
        }

        private static bool IsSameOrSubclass<TType>(object o)
        {
            var type = o.GetType();
            var ttype = typeof(TType);
            return type == ttype || type.IsSubclassOf(ttype);
        }

        /// <summary>
        /// Get a read only collection of the current layers.
        /// </summary>
        /// <returns>A read only collection of layers.</returns>
        public IEnumerable<Layer> GetLayers()
        {
            return _layers.ToList().AsReadOnly();
        }

        private async void OnLayersChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    if (args.NewItems != null)
                        foreach (var item in args.NewItems)
                        {
                            if (item is not Layer layer) continue;
                            await AddLayerJs(layer);
                        }

                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    if (args.OldItems != null)
                        foreach (var item in args.OldItems)
                        {
                            if (item is not Layer layer) continue;
                            await RemoveLayerJs(layer);
                        }

                    break;
                }
                case NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move:
                {
                    if (args.OldItems != null)
                        foreach (var oldItem in args.OldItems)
                        {
                            if (oldItem is not Layer layer) continue;
                            await RemoveLayerJs(layer);
                        }

                    if (args.NewItems != null)
                        foreach (var newItem in args.NewItems)
                        {
                            {
                                if (newItem is not Layer layer) continue;
                                await AddLayerJs(layer);
                            }
                        }

                    break;
                }
            }
        }

        private void DisposeLayerReference(string layerId)
        {
            if (_layerReferences.TryRemove(layerId, out var value))
                value.Dispose();
        }

        private async Task RemoveLayerJs(Layer layer)
        {
            await _jsRuntime.InvokeVoidAsync(JsRemoveLayer, Id, layer.Id);
            DisposeLayerReference(layer.Id);
        }

        /// <summary>
        /// Should be moved to <see cref="ACO.Blazor.Leaflet.Models.Layer"/>.
        /// <p>
        /// Possible usage then is:
        ///
        /// <code>
        /// var map = new Map(...);
        /// var layer = new Layer(...);
        /// layer.AddTo(map);
        /// </code>
        /// </p>
        ///  
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private ValueTask AddLayerJs(Layer layer)
        {
            return layer switch
            {
                TileLayer tileLayer => _jsRuntime.InvokeVoidAsync(JsAddTileLayer, Id, tileLayer,
                    CreateLayerReference(tileLayer)),
                MbTilesLayer mbTilesLayer => _jsRuntime.InvokeVoidAsync(JsAddMbTilesLayer, Id,
                    mbTilesLayer, CreateLayerReference(mbTilesLayer)),
                ShapefileLayer shapefileLayer => _jsRuntime.InvokeVoidAsync(JsAddShapefileLayer,
                    Id, shapefileLayer, CreateLayerReference(shapefileLayer)),
                Marker marker => _jsRuntime.InvokeVoidAsync(JsAddMarker, Id, marker,
                    CreateLayerReference(marker)),
                Rectangle rectangle => _jsRuntime.InvokeVoidAsync(JsAddRectangle, Id, rectangle,
                    CreateLayerReference(rectangle)),
                Circle circle => _jsRuntime.InvokeVoidAsync(JsAddCircle, Id, circle,
                    CreateLayerReference(circle)),
                Polygon polygon => _jsRuntime.InvokeVoidAsync(JsAddPolygon, Id, polygon,
                    CreateLayerReference(polygon)),
                Polyline polyline => _jsRuntime.InvokeVoidAsync(JsAddPolyline, Id, polyline,
                    CreateLayerReference(polyline)),
                ImageRotatedLayer imageRotated => _jsRuntime.InvokeVoidAsync(JsAddImageRotatedLayer, Id, imageRotated,
                    CreateLayerReference(imageRotated)),
                ImageLayer image => _jsRuntime.InvokeVoidAsync(JsAddImageLayer, Id, image,
                    CreateLayerReference(image)),
                GeoJsonDataLayer geo => _jsRuntime.InvokeVoidAsync(JsAddGeoJsonLayer, Id, geo,
                    CreateLayerReference(geo)),
                HeatmapLayer heat => _jsRuntime.InvokeVoidAsync(JsAddHeatLayer, Id, heat,
                    CreateLayerReference(heat)),
                _ => throw new NotImplementedException($"The layer {nameof(Layer)} has not been implemented."),
            };
        }

        private DotNetObjectReference<Layer> CreateLayerReference(Layer layer)
        {
            var reference = DotNetObjectReference.Create(layer);

            _layerReferences.TryAdd(layer.Id, reference);

            return reference;
        }

        public ValueTask FitBounds(PointF corner1, PointF corner2, PointF? padding = null, float? maxZoom = null)
            => _jsRuntime.InvokeVoidAsync(JsFitBounds, Id, corner1, corner2, padding, maxZoom);

        public ValueTask FitBounds(Bounds bounds, PointF? padding = null, float? maxZoom = null)
            => _jsRuntime.InvokeVoidAsync(JsFitBounds, Id, new PointF(bounds.NorthEast.Lat, bounds.NorthEast.Lng),
                new PointF(bounds.SouthWest.Lat, bounds.SouthWest.Lng), padding, maxZoom);

        public async ValueTask<Bounds> GetBoundsFromMarkers(params Marker[] markers)
            => (await _jsRuntime.InvokeAsync<BoundsDto>(JsGetBoundsFromMarker, new object[] { markers })).AsBounds();

        public async ValueTask FitBounds(params Marker[] markers) => await FitBounds(markers, null, null);

        public async ValueTask FitBounds(Marker[] markers, PointF? padding = null, float? maxZoom = null)
        {
            var bounds = await GetBoundsFromMarkers(markers);
            await FitBounds(bounds, padding, maxZoom);
        }

        public async ValueTask PanTo(PointF position, bool animate = false, float duration = 0.25f,
            float easeLinearity = 0.25f, bool noMoveStart = false) =>
            await _jsRuntime.InvokeVoidAsync(JsPanTo, Id, position, animate, duration, easeLinearity, noMoveStart);

        public async Task<LatLng> GetCenter() => await _jsRuntime.InvokeAsync<LatLng>(JsGetCenter, Id);
        public async Task<float> GetZoom() => await _jsRuntime.InvokeAsync<float>(JsGetZoom, Id);
        public async Task<Bounds> GetBounds() => (await _jsRuntime.InvokeAsync<BoundsDto>(JsGetBounds, Id)).AsBounds();

        private async Task UpdateBounds()
        {
            Bounds = await GetBounds();
            Center = await GetCenter();
            Zoom = await GetZoom();
            OnBoundsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Increases the zoom level by one notch.
        /// 
        /// If <c>shift</c> is held down, increases it by three.
        /// </summary>
        public async Task ZoomIn(MouseEventArgs e) => await _jsRuntime.InvokeVoidAsync(JsZoomIn, Id, e);

        /// <summary>
        /// Decreases the zoom level by one notch.
        /// 
        /// If <c>shift</c> is held down, decreases it by three.
        /// </summary>
        public async Task ZoomOut(MouseEventArgs e) => await _jsRuntime.InvokeVoidAsync(JsZoomOut, Id, e);

        #region events

        public delegate void MapEventHandler(object sender, Event e);

        public delegate void MapResizeEventHandler(object sender, ResizeEvent e);

        public event MapEventHandler OnZoomLevelsChange;

        [JSInvokable]
        public void NotifyZoomLevelsChange(Event e) => OnZoomLevelsChange?.Invoke(this, e);

        public event MapResizeEventHandler OnResize;

        [JSInvokable]
        public void NotifyResize(ResizeEvent e) => OnResize?.Invoke(this, e);

        public event MapEventHandler OnUnload;

        [JSInvokable]
        public void NotifyUnload(Event e) => OnUnload?.Invoke(this, e);

        public event MapEventHandler OnViewReset;

        [JSInvokable]
        public void NotifyViewReset(Event e) => OnViewReset?.Invoke(this, e);

        public event MapEventHandler OnLoad;

        [JSInvokable]
        public void NotifyLoad(Event e) => OnLoad?.Invoke(this, e);

        public event MapEventHandler OnZoomStart;

        [JSInvokable]
        public void NotifyZoomStart(Event e) => OnZoomStart?.Invoke(this, e);

        public event MapEventHandler OnMoveStart;

        [JSInvokable]
        public void NotifyMoveStart(Event e) => OnMoveStart?.Invoke(this, e);

        public event MapEventHandler OnZoom;

        [JSInvokable]
        public void NotifyZoom(Event e) => OnZoom?.Invoke(this, e);

        public event MapEventHandler OnMove;

        [JSInvokable]
        public void NotifyMove(Event e) => OnMove?.Invoke(this, e);

        public event MapEventHandler OnZoomEnd;

        [JSInvokable]
        public async void NotifyZoomEnd(Event e)
        {
            try
            {
                await UpdateBounds();
            }
            catch (Exception ex)
            {
                NotifyBackgroundExceptionOccurred(ex);
            }
            finally
            {
                OnZoomEnd?.Invoke(this, e);
            }
        }

        public event MapEventHandler OnMoveEnd;

        [JSInvokable]
        public async void NotifyMoveEnd(Event e)
        {
            try
            {
                await UpdateBounds();
            }
            catch (Exception ex)
            {
                NotifyBackgroundExceptionOccurred(ex);
            }
            finally
            {
                OnMoveEnd?.Invoke(this, e);
            }
        }

        public event EventHandler OnBoundsChanged;

        public event MouseEventHandler OnMouseMove;

        [JSInvokable]
        public void NotifyMouseMove(MouseEvent eventArgs) => OnMouseMove?.Invoke(this, eventArgs);

        public event MapEventHandler OnKeyPress;

        [JSInvokable]
        public void NotifyKeyPress(Event eventArgs) => OnKeyPress?.Invoke(this, eventArgs);

        public event MapEventHandler OnKeyDown;

        [JSInvokable]
        public void NotifyKeyDown(Event eventArgs) => OnKeyDown?.Invoke(this, eventArgs);

        public event MapEventHandler OnKeyUp;

        [JSInvokable]
        public void NotifyKeyUp(Event eventArgs) => OnKeyUp?.Invoke(this, eventArgs);

        public event MouseEventHandler OnPreClick;

        [JSInvokable]
        public void NotifyPreClick(MouseEvent eventArgs) => OnPreClick?.Invoke(this, eventArgs);

        public event EventHandler<Exception> BackgroundExceptionOccurred;

        private void NotifyBackgroundExceptionOccurred(Exception exception) =>
            BackgroundExceptionOccurred?.Invoke(this, exception);

        #endregion events

        #region InteractiveLayerEvents

        // Has the same events as InteractiveLayer, but it is not a layer. 
        // Could place this code in its own class and make Layer inherit from that, but not every layer is interactive...
        // Is there a way to not duplicate this code?

        public delegate void MouseEventHandler(Map sender, MouseEvent e);

        public event MouseEventHandler OnClick;

        [JSInvokable]
        public void NotifyClick(MouseEvent eventArgs) => OnClick?.Invoke(this, eventArgs);

        public event MouseEventHandler OnDblClick;

        [JSInvokable]
        public void NotifyDblClick(MouseEvent eventArgs) => OnDblClick?.Invoke(this, eventArgs);

        public event MouseEventHandler OnMouseDown;

        [JSInvokable]
        public void NotifyMouseDown(MouseEvent eventArgs) => OnMouseDown?.Invoke(this, eventArgs);

        public event MouseEventHandler OnMouseUp;

        [JSInvokable]
        public void NotifyMouseUp(MouseEvent eventArgs) => OnMouseUp?.Invoke(this, eventArgs);

        public event MouseEventHandler OnMouseOver;

        [JSInvokable]
        public void NotifyMouseOver(MouseEvent eventArgs) => OnMouseOver?.Invoke(this, eventArgs);

        public event MouseEventHandler OnMouseOut;

        [JSInvokable]
        public void NotifyMouseOut(MouseEvent eventArgs) => OnMouseOut?.Invoke(this, eventArgs);

        public event MouseEventHandler OnContextMenu;

        [JSInvokable]
        public void NotifyContextMenu(MouseEvent eventArgs) => OnContextMenu?.Invoke(this, eventArgs);

        #endregion InteractiveLayerEvents
    }
}