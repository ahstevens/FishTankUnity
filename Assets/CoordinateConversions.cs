using System;

public class LatLonConversions
{
	public static Datum wgs84 = new Datum(6378137.0, 6356752.3142);
	public static Datum nad83 = new Datum(6378137.0, 6356752.314);
	public static Datum grs80 = new Datum(6378137.0, 6356752.314);
	public static Datum wgs72 = new Datum(6378135.0, 6356750.5);
	public static Datum australian1965 = new Datum(6378160.0, 6356774.7);
	public static Datum krasovsky1940	= new Datum(6378245.0, 6356863.0);
	public static Datum na1927 = new Datum(6378206.4, 6356583.8);
	public static Datum international1924 = new Datum(6378388.0, 6356911.9);
	public static Datum hayford1909 = new Datum(6378388.0, 6356911.9);
	public static Datum clarke1880 = new Datum(6378249.1, 6356514.9);
	public static Datum clarke1866 = new Datum(6378206.4, 6356583.8);
	public static Datum airy1830 = new Datum(6377563.4, 6356256.9);
	public static Datum bessel1841 = new Datum(6377397.2, 6356079.0);
	public static Datum everest1830 = new Datum(6377276.3, 6356075.4);

	// false easting
	const double fe = 500000.0;

	private LatLonConversions() { }

	private static double Deg2Rad(double x)
	{
		return x * (Math.PI / 180);
	}

	private static double Rad2Deg(double x)
	{
		return x * (180 / Math.PI);
	}

	private static double SinSquared(double x)
	{
		return Math.Sin(x) * Math.Sin(x);
	}

	private static double TanSquared(double x)
	{
		return Math.Tan(x) * Math.Tan(x);
	}

	private static double Sec(double x)
	{
		return 1.0 / Math.Cos(x);
	}
	private static double Arctanh(double x)
	{
		if (Math.Abs(x) > 1)
			throw new ArgumentException("x");

		return 0.5 * Math.Log((1 + x) / (1 - x));
	}

	public static LatLon ConvertUTMtoLatLon(double easting, double northing, int zone, bool isNorth, Datum datum)
    {
		// calc zone's central meridian
		double zoneCM = 6.0 * zone - 183.0;

		double xi = (isNorth ? northing : (10000000.0 - northing)) / (datum.k0 * datum.AA);
		double eta = (easting - fe) / (datum.k0 * datum.AA);

		double tmp;
		
		tmp  = datum.beta[0] * Math.Sin(2.0 * xi) * Math.Cosh(2.0 * eta);
		tmp += datum.beta[1] * Math.Sin(4.0 * xi) * Math.Cosh(4.0 * eta);
		tmp += datum.beta[2] * Math.Sin(6.0 * xi) * Math.Cosh(6.0 * eta);
		tmp += datum.beta[3] * Math.Sin(8.0 * xi) * Math.Cosh(8.0 * eta);
		tmp += datum.beta[4] * Math.Sin(10.0 * xi) * Math.Cosh(10.0 * eta);
		tmp += datum.beta[5] * Math.Sin(12.0 * xi) * Math.Cosh(12.0 * eta);
		tmp += datum.beta[6] * Math.Sin(14.0 * xi) * Math.Cosh(14.0 * eta);

		double xi_prime = xi - tmp;

		tmp  = datum.beta[0] * Math.Cos(2.0 * xi) * Math.Sinh(2.0 * eta);
		tmp += datum.beta[1] * Math.Cos(4.0 * xi) * Math.Sinh(4.0 * eta);
		tmp += datum.beta[2] * Math.Cos(6.0 * xi) * Math.Sinh(6.0 * eta);
		tmp += datum.beta[3] * Math.Cos(8.0 * xi) * Math.Sinh(8.0 * eta);
		tmp += datum.beta[4] * Math.Cos(10.0 * xi) * Math.Sinh(10.0 * eta);
		tmp += datum.beta[5] * Math.Cos(12.0 * xi) * Math.Sinh(12.0 * eta);
		tmp += datum.beta[6] * Math.Cos(14.0 * xi) * Math.Sinh(14.0 * eta);		

		double eta_prime = eta - tmp;

		// absolute longitude in radians and degrees relative to central meridian of zone
		double absLonRad = Math.Atan(Math.Sinh(eta_prime) / Math.Cos(xi_prime));
		double absLonDeg = absLonRad * 180.0 / Math.PI;

		// geographic longitude
		double lon = zoneCM + absLonDeg;

		double tau_prime = Math.Sin(xi_prime) / Math.Sqrt(Math.Pow(Math.Sinh(eta_prime), 2) + Math.Pow(Math.Cos(xi_prime), 2));

		double tau0 = tau_prime;
		double sigma0 = Math.Sinh(datum.e * Arctanh(datum.e * tau0 / Math.Sqrt(1.0 + tau0 * tau0)));
		double f_tau0 = tau0 * Math.Sqrt(1.0 + sigma0 * sigma0) - sigma0 * Math.Sqrt(1.0 + tau0 * tau0) - tau_prime;
		double tau_delta0 = (Math.Sqrt((1.0 + sigma0 * sigma0) * (1.0 + tau0 * tau0)) - sigma0 * tau0) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau0 * tau0) / (1.0 + (1.0 - datum.e * datum.e) * tau0 * tau0);

		double tau1 = (tau0 - f_tau0) / tau_delta0;
		double sigma1 = Math.Sinh(datum.e * Arctanh(datum.e * tau1 / Math.Sqrt(1.0 + tau1 * tau1)));
		double f_tau1 = tau1 * Math.Sqrt(1.0 + sigma1 * sigma1) - sigma1 * Math.Sqrt(1.0 + tau1 * tau1) - tau_prime;
		double tau_delta1 = (Math.Sqrt((1.0 + sigma1 * sigma1) * (1.0 + tau1 * tau1)) - sigma1 * tau1) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau1 * tau1) / (1.0 + (1.0 - datum.e * datum.e) * tau1 * tau1);

		double tau2 = (tau1 - f_tau1) / tau_delta1;
		double sigma2 = Math.Sinh(datum.e * Arctanh(datum.e * tau2 / Math.Sqrt(1.0 + tau2 * tau2)));
		double f_tau2 = tau2 * Math.Sqrt(1.0 + sigma2 * sigma2) - sigma2 * Math.Sqrt(1.0 + tau2 * tau2) - tau_prime;
		double tau_delta2 = (Math.Sqrt((1.0 + sigma2 * sigma2) * (1.0 + tau2 * tau2)) - sigma2 * tau2) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau2 * tau2) / (1.0 + (1.0 - datum.e * datum.e) * tau2 * tau2);

		double tau3 = (tau2 - f_tau2) / tau_delta2;
		double sigma3 = Math.Sinh(datum.e * Arctanh(datum.e * tau3 / Math.Sqrt(1.0 + tau3 * tau3)));
		double f_tau3 = tau3 * Math.Sqrt(1.0 + sigma3 * sigma3) - sigma3 * Math.Sqrt(1.0 + tau3 * tau3) - tau_prime;
		double tau_delta3 = (Math.Sqrt((1.0 + sigma3 * sigma3) * (1.0 + tau3 * tau3)) - sigma3 * tau3) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau3 * tau3) / (1.0 + (1.0 - datum.e * datum.e) * tau3 * tau3);

		double tau4 = (tau3 - f_tau3) / tau_delta3;
		double sigma4 = Math.Sinh(datum.e * Arctanh(datum.e * tau4 / Math.Sqrt(1.0 + tau4 * tau4)));
		double f_tau4 = tau4 * Math.Sqrt(1.0 + sigma4 * sigma4) - sigma4 * Math.Sqrt(1.0 + tau4 * tau4) - tau_prime;
		double tau_delta4 = (Math.Sqrt((1.0 + sigma4 * sigma4) * (1.0 + tau4 * tau4)) - sigma4 * tau4) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau4 * tau4) / (1.0 + (1.0 - datum.e * datum.e) * tau4 * tau4);

		double tau5 = (tau4 - f_tau4) / tau_delta4;
		double sigma5 = Math.Sinh(datum.e * Arctanh(datum.e * tau5 / Math.Sqrt(1.0 + tau5 * tau5)));
		double f_tau5 = tau5 * Math.Sqrt(1.0 + sigma5 * sigma5) - sigma5 * Math.Sqrt(1.0 + tau5 * tau5) - tau_prime;
		double tau_delta5 = (Math.Sqrt((1.0 + sigma5 * sigma5) * (1.0 + tau5 * tau5)) - sigma4 * tau5) * (1.0 - datum.e * datum.e) * Math.Sqrt(1.0 + tau5 * tau5) / (1.0 + (1.0 - datum.e * datum.e) * tau5 * tau5);

		double latRad = Math.Atan((tau5 - f_tau5) / tau_delta5);
		double latDeg = latRad * 180.0 / Math.PI;

		double lat = isNorth ? Math.Abs(latDeg) : -Math.Abs(latDeg);

		UnityEngine.Debug.Log("k0: " + datum.k0);
		UnityEngine.Debug.Log("AA: " + datum.AA);
		UnityEngine.Debug.Log("Zone CM: " + zoneCM);
		UnityEngine.Debug.Log("xi: " + xi);
		UnityEngine.Debug.Log("eta: " + eta);
		UnityEngine.Debug.Log("xi': " + xi_prime);
		UnityEngine.Debug.Log("eta': " + eta_prime);
		UnityEngine.Debug.Log("tau': " + tau_prime);
		UnityEngine.Debug.Log("tau0: " + tau0);
		UnityEngine.Debug.Log("sigma0: " + sigma0);
		UnityEngine.Debug.Log("f(tau0): " + f_tau0);
		UnityEngine.Debug.Log("df(tau)/dtau: " + tau_delta0);
		UnityEngine.Debug.Log("tau1: " + tau1);
		UnityEngine.Debug.Log("lat: " + lat);

		return new LatLon(lat, lon);
    }
}

public class Datum
{
	// major axis / equatorial radius
	public double a;

	// minor axis / polar radius
	public double b;

	// flattening
	public double f;

	// inverse flattening
	public double f_inv;

	// mean radius
	public double rm;

	// scale factor
	public double k0 = 0.9996012717;

	// eccentricity
	public double e;

	// eccentricity squared
	public double e_sq;

	// 3D flattening
	public double n;

	// Meridian radius
	public double AA;

	// Kruger series
	public double[] beta = new double[10];

	public Datum(double major, double minor)
	{
		a = major;
		b = minor;
		f = (a - b) / a;
		f_inv = 1.0 / f;
		rm = Math.Sqrt(a * b);
		e_sq = ((a * a) - (b * b)) / (a * a);
		e = Math.Sqrt(e_sq);
		n = (a - b) / (a + b);

		double tmp = 1.0;
		tmp += (1.0 / 4.0) * Math.Pow(n, 2);
		tmp += (1.0 / 64.0) * Math.Pow(n, 4);
		tmp += (1.0 / 256.0) * Math.Pow(n, 6);
		tmp += (25.0 / 16384.0) * Math.Pow(n, 8);
		tmp += (49.0 / 65536.0) * Math.Pow(n, 10);
		AA = (a / (1.0 + n)) * tmp;

		CalculateKrugerCoefficients();
	}

	void CalculateKrugerCoefficients()
    {
		beta[0] = (1.0 / 2.0) * n;
		beta[0] -= (2.0 / 3.0) * Math.Pow(n, 2);
		beta[0] += (37.0 / 96.0) * Math.Pow(n, 3);
		beta[0] -= (1.0 / 360.0) * Math.Pow(n, 4);
		beta[0] -= (81.0 / 512.0) * Math.Pow(n, 5);
		beta[0] += (96199.0 / 604800.0) * Math.Pow(n, 6);
		beta[0] -= (5406467.0 / 38707200.0) * Math.Pow(n, 7);
		beta[0] += (7944359.0 / 67737600.0) * Math.Pow(n, 8);
		beta[0] -= (7378753979.0 / 97542144000.0) * Math.Pow(n, 9);
		beta[0] += (25123531261.0 / 804722688000.0) * Math.Pow(n, 10);

		beta[1] = (1.0 / 48.0) * Math.Pow(n, 2);
		beta[1] += (1.0 / 15.0) * Math.Pow(n, 3);
		beta[1] -= (437.0 / 1440.0) * Math.Pow(n, 4);
		beta[1] += (46.0 / 105.0) * Math.Pow(n, 5);
		beta[1] -= (1118711.0 / 3870720.0) * Math.Pow(n, 6);
		beta[1] += (51841.0 / 1209600.0) * Math.Pow(n, 7);
		beta[1] += (24749483.0 / 348364800.0) * Math.Pow(n, 8);
		beta[1] -= (115295683.0 / 1397088000.0) * Math.Pow(n, 9);
		beta[1] += (5487737251099.0 / 51502252032000.0) * Math.Pow(n, 10);

		beta[2] = (17.0 / 480.0) * Math.Pow(n, 3);
		beta[2] -= (37.0 / 840.0) * Math.Pow(n, 4);
		beta[2] -= (209.0 / 4480.0) * Math.Pow(n, 5);
		beta[2] += (5569.0 / 90720.0) * Math.Pow(n, 6);
		beta[2] += (9261899.0 / 58060800.0) * Math.Pow(n, 7);
		beta[2] -= (6457463.0 / 17740800.0) * Math.Pow(n, 8);
		beta[2] += (2473691167.0 / 9289728000.0) * Math.Pow(n, 9);
		beta[2] -= (852549456029.0 / 20922789888000.0) * Math.Pow(n, 10);

		beta[3] = (4397.0 / 161280.0) * Math.Pow(n, 4);
		beta[3] -= (11.0 / 504.0) * Math.Pow(n, 5);
		beta[3] -= (830251.0 / 7257600.0) * Math.Pow(n, 6);
		beta[3] += (466511.0 / 2494800.0) * Math.Pow(n, 7);
		beta[3] += (324154477.0 / 7664025600.0) * Math.Pow(n, 8);
		beta[3] -= (937932223.0 / 3891888000.0) * Math.Pow(n, 9);
		beta[3] -= (89112264211.0 / 5230697472000.0) * Math.Pow(n, 10);

		beta[4] = (4583.0 / 161280.0) * Math.Pow(n, 5);
		beta[4] -= (108847.0 / 3991680.0) * Math.Pow(n, 6);
		beta[4] -= (8005831.0 / 63866880.0) * Math.Pow(n, 7);
		beta[4] += (22894433.0 / 124540416.0) * Math.Pow(n, 8);
		beta[4] += (112731569449.0 / 557941063680.0) * Math.Pow(n, 9);
		beta[4] -= (5391039814733.0 / 10461394944000.0) * Math.Pow(n, 10);

		beta[5] = (20648693.0 / 638668800.0) * Math.Pow(n, 6);
		beta[5] -= (16363163.0 / 518918400.0) * Math.Pow(n, 7);
		beta[5] -= (2204645983.0 / 12915302400.0) * Math.Pow(n, 8);
		beta[5] += (4543317553.0 / 18162144000.0) * Math.Pow(n, 9);
		beta[5] += (54894890298749.0 / 167382319104000.0) * Math.Pow(n, 10);

		beta[6] = (219941297.0 / 5535129600.0) * Math.Pow(n, 7);
		beta[6] -= (497323811.0 / 12454041600.0) * Math.Pow(n, 8);
		beta[6] -= (79431132943.0 / 332107776000.0) * Math.Pow(n, 9);
		beta[6] += (4346429528407.0 / 12703122432000.0) * Math.Pow(n, 10);

		beta[7] = (191773887257.0 / 3719607091200.0) * Math.Pow(n, 8);
		beta[7] -= (17822319343.0 / 336825216000.0) * Math.Pow(n, 9);
		beta[7] -= (497155444501631.0 / 1422749712384000.0) * Math.Pow(n, 10);

		beta[8] = (11025641854267.0 / 158083301376000.0) * Math.Pow(n, 9);
		beta[8] -= (492293158444691.0 / 6758061133824000.0) * Math.Pow(n, 10);

		beta[9] = (7028504530429620.0 / 72085985427456000.0) * Math.Pow(n, 10);
	}
}

public class LatLon
{
	public double Latitude;
	public double Longitude;

	public LatLon()
	{
		Latitude = 0;
		Longitude = 0;
	}

	public LatLon(double lat, double lon)
	{
		Latitude = lat;
		Longitude = lon;
	}
}
