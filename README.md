# Universal Windows Vector Tile Renderer

Example universal windows apps that renders vector tiles to a [Win2D](https://github.com/Microsoft/Win2D) canvas.

This is currently just for demonstration purposes and is not suitable for production use.



## Screenshot

![Vector Map](Docs/VectorTileRender.png?raw=true)

## Map Source
The app makes calls to the [Mapzen](https://mapzen.com) api to retrieve the vector tiles. In order to run the app, apply for an api key from the Mapzen website and place it in the placeholder in MainPage.xaml.cs.

## Libraries/Inspiration

The app makes use of or drew inspiration from:

* [Mapbox vector tile cs](https://github.com/bertt/mapbox-vector-tile-cs)
* [Mapsui](https://github.com/pauldendulk/Mapsui)
* [BruTile](https://github.com/BruTile/BruTile)
* [VectorTileToBitmapRenderer](https://github.com/OsmSharp/VectorTileToBitmapRenderer)

