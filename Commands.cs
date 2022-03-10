using System;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace PlottingCoord
{
    /************
     * 
     * Geo Marker
     * 
        Long = -87.961526837297455
        Lat = 41.948856487413423
 
        Y=5153321.469906129,
        X=-9791832.37692682
     * 
     * 
     * **********/

    public partial class FloodData
    {
        [JsonProperty("dims", NullValueHandling = NullValueHandling.Ignore)]
        public List<long> Dims { get; set; }

        [JsonProperty("prediction", NullValueHandling = NullValueHandling.Ignore)]
        public List<long> Prediction { get; set; }

        [JsonProperty("probability", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> Probability { get; set; }

        [JsonProperty("y", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> Y { get; set; }

        [JsonProperty("x", NullValueHandling = NullValueHandling.Ignore)]
        public List<double> X { get; set; }

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        public string Crs { get; set; }
    }

    public partial class FloodPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public long Prediction { get; set; }
        public double Probability { get; set; }
    }

    public partial class FloodData
    {
        private static FloodData _floodData;
        public static FloodData FromJson(string json)
        {
            using (StreamReader file = File.OpenText(json))
            {
                  JsonSerializer serializer = new JsonSerializer();
                 _floodData = serializer.Deserialize(file, typeof(FloodData)) as FloodData;
            }
            return _floodData;
        }

        public List<FloodPoint> GetFloodPoints()
        {
            var points = new List<FloodPoint>();
            if(_floodData is null)
            {
                return null;
            }

            long _count = _floodData.Dims[0] * _floodData.Dims[1];

            for(int i = 0; i < _count; i++)
            {
                points.Add(new FloodPoint()
                {
                    X = _floodData.X[i],
                    Y = _floodData.Y[i],
                    Prediction = _floodData.Prediction[i],
                    Probability = _floodData.Probability[i]
                });
                
            }            
            return points;
        }
    }


    public class Commands
    {
        private static readonly Dictionary<Handle, double> FLoodProbablityMapper = new Dictionary<Handle, double>();
        [CommandMethod("PlotCoords")]
        public static void PlotCoords()
        {

            var files = Directory.GetFiles(@"D:\Work\Forge\LatLongC3D\PlottingCoord\sample_x_y\", "*.json");
           
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            ed.PointMonitor += Ed_PointMonitor;
            var db = doc.Database;
            if (!HasGeoData(db)) { return; }
            GeoCoordinateTransformer transformer = GeoCoordinateTransformer.Create("WGS84.PseudoMercator", "LL84");
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                if(FLoodProbablityMapper != null)
                {
                    FLoodProbablityMapper.Clear();
                }
                foreach (var file in files)
                {
                    var floodData = FloodData.FromJson(file);
                    var floodPoints = floodData.GetFloodPoints();
                    // Get the drawing's GeoLocation object
                    var gd = tr.GetObject(db.GeoDataObject, OpenMode.ForRead) as GeoLocationData;
                    foreach (var point in floodPoints)
                    {
                        if (point.Prediction == 1)
                        {
                            var geoPoint = new Point3d(point.X, point.Y, 0.0);
                            Point3d targetPt = transformer.TransformPoint(geoPoint);
                            Point3d wcsPt = gd.TransformFromLonLatAlt(targetPt);
                            var c = GetColorIntensity(Color.FromRgb(255, 0, 0), point.Probability);
                            DrawPoint(c, wcsPt, point.Probability);
                        }
                    }
                    tr.Commit();               
                }
            }            
        }

        private static void Ed_PointMonitor(object sender, Autodesk.AutoCAD.EditorInput.PointMonitorEventArgs e)
        {
            var ed = sender as Editor;
            var fullPaths = e.Context.GetPickedEntities();
            if (fullPaths.Length == 0) return;
            ObjectId entId = ObjectId.Null;
            foreach (var path in fullPaths)
            {
                if (!path.IsNull)
                {
                    var ids = path.GetObjectIds();
                    if (ids.Length > 0)
                    {
                        entId = ids[0];
                    }
                }
                if (!entId.IsNull) break;
            }
            if (!entId.IsNull)
            {

                using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
                {
                   using (BlockReference ent = (BlockReference)tr.GetObject(entId, OpenMode.ForRead))
                   {
                        if (ent != null)
                        {
                            if(FLoodProbablityMapper.TryGetValue(ent.Handle, out double factor))
                            {
                                
                                e.AppendToolTipText($"{factor.ToString("P", CultureInfo.InvariantCulture)} Flood Probability!");
                            }
                        }
                    }
                }
            }


        }

        public static Color GetColorIntensity(Color color, double predictionFactor)
        {
            double red = color.Red;
            double green = color.Green;
            double blue = color.Blue;
            predictionFactor = 1 - predictionFactor;
            red = (255 - red) * predictionFactor + red;
            green = (255 - green) * predictionFactor + green;
            blue = (255 - blue) * predictionFactor + blue;

            //red *= predictionFactor;
            //green *= predictionFactor;
            //blue *= predictionFactor;
            return Color.FromRgb(Convert.ToByte(red), Convert.ToByte(green), Convert.ToByte(blue));
        }       
        private static bool HasGeoData(Database db)
        {
            // Check whether the drawing already has geolocation data
            bool hasGeoData = false;
            try
            {
                var gdId = db.GeoDataObject;
                hasGeoData = true;
            }
            catch { }
            return hasGeoData;
        }
        public static ObjectId DrawPoint(Color c, Point3d position, double factor)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var brefId = ObjectId.Null;
            using (OpenCloseTransaction t = new OpenCloseTransaction())
            {
                // Open the Block table for read
                var blockTable = t.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                // Open the Block table record for read
                if(!(t.GetObject(blockTable["FloodPoint"], OpenMode.ForRead) is BlockTableRecord floodBlock))
                {
                    return brefId;
                }
                var bref = new BlockReference(position, floodBlock.ObjectId)
                {
                    Color = c,
                    ScaleFactors = Scale3d.ExtractScale(Matrix3d.Scaling(1000, position))
                };
                var modelSpace = t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite) as BlockTableRecord;
                brefId = modelSpace.AppendEntity(bref);
                t.AddNewlyCreatedDBObject(bref, true);
                FLoodProbablityMapper.Add(brefId.Handle, factor);
                t.Commit();
            }
            return brefId;
        }  


    }
}
