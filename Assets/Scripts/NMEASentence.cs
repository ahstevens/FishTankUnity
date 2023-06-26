
namespace CCOM.NMEA
{
    // Establish common parameters and their data types
    using SatCount = System.UInt16;
    using Coordinate = System.Double;
    using PrecisionDilution = System.Single;
    using Altitude = System.Double;
    using Heading = System.Single;
    using Speed = System.Single;
    using DifferentialCorrectionsAge = System.UInt16;
    using RefStationID = System.UInt16;
    
    enum GNSSQuality
    {
        INVALID = 0,
        STANDARD = 1,
        RTKFloat = 2,
        RTKFixed = 3,
        DGNSS = 4,
        FREEINERTIAL = 5
    }

    public enum LatitudeDirection
    {
        N,
        S
    }

    public enum LongitudeDirection
    { 
        E,
        W
    }

    enum TimeStampControl
    {
        AUTO,
        MANUAL
    }

    // Base class for NMEA Sentence strings
    public class NMEASentence
    {

        // POS MV Manual says "IN" may be more correct, but "GP" is more
        // widely accepted by third-party apps
        public const string device = "GP";

        public static void DegreesToDDM(in double degrees, out int degree, out float decimalMinutes)
        {
            degree = (int)System.Math.Abs(degrees);
            decimalMinutes = (float)(degrees - (double)degree) * 60f;
        }

        public static string GetChecksumString(string sentence)
        {
           byte[] s = System.Text.Encoding.ASCII.GetBytes(sentence);

            byte c = 0;

            foreach (byte b in s)
            {
                c ^= b;
            }

            return "*" + c.ToString("X2");
        }
    }

    // Time and position fix related data
    public class GGA : NMEASentence
    {
        const string header = "GGA";

        // hhmmss.sss (2 Hours | 2 Minutes | 2 Seconds | 3 Decimal Seconds)
        System.DateTime time;

        TimeStampControl timeUpdate;

        // Range: [0, 90]
        // DDMM.ddddd (2 Degrees | 2 Minutes | 5 Decimal Minutes)
        Coordinate latitude;

        // N or S
        LatitudeDirection latDirection;

        // Range: [0, 180]
        // DDDMM.ddddd (3 Degrees | 2 Minutes | 5 Decimal Minutes)
        Coordinate longitude;

        // E or W
        LongitudeDirection lonDirection;

        // 0 to 6
        GNSSQuality quality;

        // Range: [0, 32]
        // nn
        SatCount satellites;

        // v.v
        PrecisionDilution hdp;

        // xxxxx.xx (meters)
        Altitude altitude;

        // Always meters
        const char units = 'M';

        // Range [0, 999] seconds
        // ccc
        DifferentialCorrectionsAge adc;

        // 0000 to 1023
        // rrrr
        RefStationID rsid;

        public GGA()
        {
            time = System.DateTime.Now;
            timeUpdate = TimeStampControl.AUTO;
            latitude = 0.0;
            latDirection = LatitudeDirection.N;
            longitude = 0.0;
            lonDirection = LongitudeDirection.E;
            quality = GNSSQuality.STANDARD;
            satellites = 32;
            hdp = 1f;
            altitude = 0.0;
            adc = 0;
            rsid = 0;
        }

        public GGA(Coordinate latitude, LatitudeDirection latDirection, Coordinate longitude, LongitudeDirection lonDirection) : this()
        {
            SetPosition(latitude, latDirection, longitude, lonDirection);
        }

        public GGA(Coordinate latitude, LatitudeDirection latDirection, Coordinate longitude, LongitudeDirection lonDirection, Altitude altitude) 
        : this(latitude, latDirection, longitude, lonDirection)
        {
            SetAltitude(altitude);
        }

        public void SetPosition(Coordinate latitude, Coordinate longitude)
        {
            this.latitude = System.Math.Min(System.Math.Max(0.0, System.Math.Abs(latitude)), 90.0);
            latDirection = latitude < 0 ? LatitudeDirection.S : LatitudeDirection.N;

            this.longitude = System.Math.Min(System.Math.Max(0.0, System.Math.Abs(longitude)), 180.0);
            lonDirection = longitude < 0 ? LongitudeDirection.W : LongitudeDirection.E;
        }

        public void SetPosition(Coordinate latitude, LatitudeDirection latDirection, Coordinate longitude, LongitudeDirection lonDirection)
        {            
            this.latitude = System.Math.Min(System.Math.Max(0.0, latitude), 90.0);
            this.latDirection = latDirection;
            
            this.longitude = System.Math.Min(System.Math.Max(0.0, longitude), 180.0);
            this.lonDirection = lonDirection;
        }

        public void SetAltitude(Altitude altitude)
        {
            this.altitude = altitude;
        }

        public void SetTimeStamp(System.DateTime ts)
        {
            this.time = ts;
            timeUpdate = TimeStampControl.MANUAL;
        }

        public void SetTimeStampAuto()
        {
            timeUpdate = TimeStampControl.AUTO;
        }

        public void SetTimeStampManual()
        {
            timeUpdate = TimeStampControl.MANUAL;
        }

        // $xxGGA,hhmmss.sss,llll.lllll,a,yyyyy.yyyyy,b,t,nn,v.v,xxxxx.xx,M,,,ccc,rrrr*hh<CRLF>
        public override string ToString()
        {
            if (timeUpdate == TimeStampControl.AUTO)
                time = System.DateTime.Now;

            int latD, lonD;
            float latDM, lonDM;

            DegreesToDDM(latitude, out latD, out latDM);
            DegreesToDDM(longitude, out lonD, out lonDM);
            
            string latDDM = latD.ToString("D2") + latDM.ToString("00.0####");

            string lonDDM = lonD.ToString("D3") + lonDM.ToString("00.0####");

            string sentence = $"{device}{header},{time:HHmmss.fff}," +
                $"{latDDM},{latDirection:G}," +
                $"{lonDDM},{lonDirection:G}," +
                $"{quality:D},{satellites},{hdp:0.0},{altitude:####0.0#},{units},,,{adc},{rsid:0000}";
            
            return $"${sentence}{GetChecksumString(sentence)}";
        }
    }

    // True vessel heading in degrees
    public class HDT : NMEASentence
    {
        const string header = "HDT";

        Heading heading;

        public HDT(Heading heading)
        {
            SetHeading(heading);
        }

        public HDT() : this(0f) {}
        
        public void SetHeading(Heading heading)
        {
            this.heading = System.Math.Min(System.Math.Max(0.0f, heading), 359.9f);            
        }

        // $xxHDT,xxx.x,T*hh<CRLF>
        public override string ToString()
        {
            string sentence = $"{device}{header},{heading:000.0},T";

            return $"${sentence}{GetChecksumString(sentence)}";
        }
    }

    // Actual course and speed relative to the ground
    public class VTG : NMEASentence
    {
        const string header = "VTG";

        Heading vesselTrack;

        // Store as meters per second (m/s) and convert as-needed
        Speed speed;

        public VTG(Heading vesselTrack, Speed speed)
        {
            SetVesselTrack(vesselTrack);
            this.speed = speed;
        }

        public VTG()
        {
            SetVesselTrack(0f);
            speed = 0f;
        }

        public void SetVesselTrack(Heading vesselTrack)
        {
            this.vesselTrack = System.Math.Min(System.Math.Max(0.0f, vesselTrack), 359.9f);            
        }

        public void SetSpeed(Speed speed)
        {
            this.speed = speed;
        }

        public Speed GetSpeedInKnots()
        {
            return speed * 1.94384f;
        }

        public Speed GetSpeedInKMS()
        {
            return speed * 0.001f;
        }

        // $xxVTG,xxx.x,T,,M,n.n,N,k.k,K*hh<CRLF>
        public override string ToString()
        {
            string sentence = $"{device}{header},{vesselTrack:000.0},T,,M,{GetSpeedInKnots():0.0},N,{GetSpeedInKMS():0.0},K";

            return $"${sentence}{GetChecksumString(sentence)}";
        }
    }
}
