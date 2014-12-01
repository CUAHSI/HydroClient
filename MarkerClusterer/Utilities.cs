using System;
using System.Collections.Generic;
using System.Text;


namespace HISWebClient.MarkerClusterer
{
    public static class Utilities
    {
        private const double earthRadius = 6378137; //The radius of the earth - should never change!
        private const double earthCircum = earthRadius * 2.0 * Math.PI; //calulated circumference of the earth
        private const double earthHalfCirc = earthCircum / 2; //calulated half circumference of the earth
        private const int pixelsPerTile = 256;


        public static int LatitudeToYAtZoom(double lat, int zoom)
        {
            int y;
            double arc = earthCircum / ((1 << zoom) * pixelsPerTile);
            double sinLat = Math.Sin(DegToRad(lat));
            double metersY = earthRadius / 2 * Math.Log((1 + sinLat) / (1 - sinLat));
            y = (int)Math.Round((earthHalfCirc - metersY) / arc);
            return y;
        }

        public static double LatitudeToYAtZoomGoogle(double lat, int zoom)
        {

            double exp = Math.Sin(lat * Math.PI / 180);
            if (exp < -.9999) { exp = -.9999; }
            if (exp > .9999) { exp = .9999; }
            double T1 = 256 * (Math.Pow(2, zoom - 1));
            double T2 = (0.5 * Math.Log((1 + exp) / (1 - exp)));
            double T3 = -256 * (Math.Pow(2, zoom)) / (2 * Math.PI);

            double y = T1 + (T2) * (T3);
            //y = round (256*(2^(zoom-1)))+((.5*log((1+$exp)/(1-$exp)))*((-256*(2^zoom))/(2*pi)))  
            return Math.Round(y);
        }
        /*
         public static int LToY(int z,double y) 
         { 
            int d= (offset-radius*Math.Log((1+Math.Sin(y*Math.PI/180))/(1-Math.Sin(y*Math.PI/180)))/2) >> z; 
            return d;
         } 
 */


        public static int LongitudeToXAtZoom(double lon, int zoom)
        {
            int x;
            double arc = earthCircum / ((1 << zoom) * pixelsPerTile);
            double metersX = earthRadius * DegToRad(lon);
            x = (int)Math.Round((earthHalfCirc + metersX) / arc);
            return x;
        }

        public static int LongitudeToXAtZoomGoogle(double lon, int zoom)
        {
            double x;

            x = Math.Round(256 * (Math.Pow(2, zoom - 1)) + (lon * ((256 * (Math.Pow(2, zoom))) / 360)));
            return Convert.ToInt32(x);
        }

        private static double DegToRad(double d)
        {
            return d * Math.PI / 180.0;
        }

        private const int minASCII = 63;
        private const int binaryChunkSize = 5;

        public static Bounds DecodeBounds(string encoded)
        {
            List<LatLong> locs = DecodeLatLong(encoded);
            //OverSize the bounds to allow for rounding errors in the encoding process.
            locs[0].Lat += 0.00001;
            locs[0].Lon -= 0.00001;
            locs[1].Lat -= 0.00001;
            locs[1].Lon += 0.00001;
            return new Bounds(locs[0], locs[1]);
        }

        public static List<LatLong> DecodeLatLong(string encoded)
        {
            List<LatLong> locs = new List<LatLong>();

            int index = 0;
            int lat = 0;
            int lng = 0;

            int len = encoded.Length;
            while (index < len)
            {
                lat += decodePoint(encoded, index, out index);
                lng += decodePoint(encoded, index, out index);

                locs.Add(new LatLong((lat * 1e-5), (lng * 1e-5)));
            }

            return locs;
        }

        private static int decodePoint(string encoded, int startindex, out int finishindex)
        {
            int b;
            int shift = 0;
            int result = 0;
            do
            {
                //get binary encoding
                b = Convert.ToInt32(encoded[startindex++]) - minASCII;
                //binary shift
                result |= (b & 0x1f) << shift;
                //move to next chunk
                shift += binaryChunkSize;
            } while (b >= 0x20); //see if another binary value
            //if negivite flip
            int dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));
            //set output index
            finishindex = startindex;
            return dlat;
        }

        public static string EncodeCluster(List<ClusteredPin> pins)
        {
            StringBuilder encoded = new StringBuilder();
            //encode the locations
            List<LatLong> points = new List<LatLong>();
            foreach (ClusteredPin pin in pins)
            {
                points.Add(pin.Loc);
            }

            encoded.Append(EncodeLatLong(points));


            //encode the bounds per cluster
            foreach (ClusteredPin pin in pins)
            {
                //comma seperated
                encoded.Append(',');
                points = new List<LatLong>();
                points.Add(pin.ClusterArea.NW);
                points.Add(pin.ClusterArea.SE);
                encoded.Append(EncodeLatLong(points));
            }


            return encoded.ToString();
        }

        public static string EncodeLatLong(List<LatLong> points)
        {
            int plat = 0;
            int plng = 0;
            int len = points.Count;

            StringBuilder encoded_points = new StringBuilder();

            for (int i = 0; i < len; ++i)
            {
                //Round to 5 decimal places and drop the decimal
                int late5 = (int)(points[i].Lat * 1e5);
                int lnge5 = (int)(points[i].Lon * 1e5);

                //encode the differences between the points
                encoded_points.Append(encodeSignedNumber(late5 - plat));
                encoded_points.Append(encodeSignedNumber(lnge5 - plng));

                //store the current point
                plat = late5;
                plng = lnge5;
            }
            return encoded_points.ToString();
        }

        private static string encodeSignedNumber(int num)
        {
            int sgn_num = num << 1; //shift the binary value
            if (num < 0) //if negative invert
            {
                sgn_num = ~(sgn_num);
            }
            return (encodeNumber(sgn_num));
        }

        private static string encodeNumber(int num)
        {
            StringBuilder encodeString = new StringBuilder();
            while (num >= 0x20)
            {
                //while another chunk follows
                encodeString.Append((char)((0x20 | (num & 0x1f)) + minASCII)); //OR value with 0x20, convert to decimal and add 63
                num >>= binaryChunkSize; //shift to next chunk
            }
            encodeString.Append((char)(num + minASCII));
            return encodeString.ToString();
        }




    }

}