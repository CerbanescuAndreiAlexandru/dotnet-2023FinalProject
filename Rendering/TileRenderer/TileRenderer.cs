using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public static class TileRenderer
{
    public static BaseShape Tessellate(this MapFeatureData feature, ref BoundingBox boundingBox, ref PriorityQueue<BaseShape, int> shapes)
    {
        BaseShape? baseShape = null;

        var featureType = feature.Type;

        // We compare int instead of string and we do so over a single iteration
        if (Border.ShouldBeBorder(feature))
        {
            var coordinates = feature.Coordinates;
            var border = new Border(coordinates);
            baseShape = border;
            shapes.Enqueue(border, border.ZIndex);
        }
        else if (PopulatedPlace.ShouldBePopulatedPlace(feature))
        {
            var coordinates = feature.Coordinates;
            var popPlace = new PopulatedPlace(coordinates, feature);
            baseShape = popPlace;
            shapes.Enqueue(popPlace, popPlace.ZIndex);
        }
        else
        {
            // Use variable to exit iteration after one case

            bool exit = false;
            foreach (var property in feature.Properties)
            {
                if (exit)
                    break;

                ReadOnlySpan<Coordinate> coordinates;

                switch (property.Key)
                {
                    case MapFeatureData.Types.Highway:
                        if (MapFeature.HighwayTypes.Any(v => property.Value.StartsWith(v)))
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var road = new Road(coordinates);
                            baseShape = road;
                            shapes.Enqueue(road, road.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Water:
                        if (feature.Type != GeometryType.Point)
                        {
                            exit = true;
                            coordinates = feature.Coordinates;

                            var waterway = new Waterway(coordinates, feature.Type == GeometryType.Polygon);
                            baseShape = waterway;
                            shapes.Enqueue(waterway, waterway.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Railway:
                        exit = true;
                        coordinates = feature.Coordinates;
                        var railway = new Railway(coordinates);
                        baseShape = railway;
                        shapes.Enqueue(railway, railway.ZIndex);

                        break;
                    case MapFeatureData.Types.Natural:
                        if (featureType == GeometryType.Polygon)
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, feature);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Boundary:
                        if (property.Value.StartsWith("forest"))
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Landuse:
                        if (property.Value.StartsWith("forest") || property.Value.StartsWith("orchard"))
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        else if (feature.Type == GeometryType.Polygon)
                        {
                            if (property.Value.StartsWith("residential") || property.Value.StartsWith("cemetery") ||
                                property.Value.StartsWith("industrial") || property.Value.StartsWith("commercial") ||
                                property.Value.StartsWith("square") || property.Value.StartsWith("construction") ||
                                property.Value.StartsWith("military") || property.Value.StartsWith("quarry") ||
                                property.Value.StartsWith("brownfield"))
                            {
                                exit = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }
                            else if (property.Value.StartsWith("farm") || property.Value.StartsWith("meadow") ||
                                     property.Value.StartsWith("grass") || property.Value.StartsWith("greenfield") ||
                                     property.Value.StartsWith("recreation_ground") || property.Value.StartsWith("winter_sports")
                                     || property.Value.StartsWith("allotments"))
                            {
                                exit = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Plain);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }
                            else if (property.Value.StartsWith("reservoir") || property.Value.StartsWith("basin"))
                            {
                                exit = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Water);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }
                        }

                        break;
                    case MapFeatureData.Types.Building:
                        if (feature.Type == GeometryType.Polygon)
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Leisure:
                        if (feature.Type == GeometryType.Polygon)
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.Types.Amenity:
                        if (feature.Type == GeometryType.Polygon)
                        {
                            exit = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;

                    default:
                        break;
                }
            }
        }
            if (baseShape != null)
            {
                for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j)
                {
                    boundingBox.MinX = Math.Min(boundingBox.MinX, baseShape.ScreenCoordinates[j].X);
                    boundingBox.MaxX = Math.Max(boundingBox.MaxX, baseShape.ScreenCoordinates[j].X);
                    boundingBox.MinY = Math.Min(boundingBox.MinY, baseShape.ScreenCoordinates[j].Y);
                    boundingBox.MaxY = Math.Max(boundingBox.MaxY, baseShape.ScreenCoordinates[j].Y);
                }
            }

       return baseShape;
        
    }

    public static Image<Rgba32> Render(this PriorityQueue<BaseShape, int> shapes, BoundingBox boundingBox, int width, int height)
    {
        var canvas = new Image<Rgba32>(width, height);

        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (boundingBox.MaxX - boundingBox.MinX);
        var scaleY = canvas.Height / (boundingBox.MaxY - boundingBox.MinY);
        var scale = Math.Min(scaleX, scaleY);

        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));
        while (shapes.Count > 0)
        {
            var entry = shapes.Dequeue();
            // FIXME: Hack
            if (entry.ScreenCoordinates.Length < 2)
            {
                continue;
            }
            entry.TranslateAndScale(boundingBox.MinX, boundingBox.MinY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }

    public struct BoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
