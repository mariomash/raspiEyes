using System;
using System.IO;
using System.Text;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using SharpCifs.Smb;
using Android.Locations;
using Android.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sharpen;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace raspiEyesAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {

        Location currentLocation = null;
        LocationManager locationManager;
        string locationProvider;
        System.Timers.Timer Timer;
        DateTime LastUpdate;
        TextView infoText;
        TextView infoText2;
        TextView infoText3;
        TextView infoText4;
        String LastLocation;
        String LastTemperature;
        String LastHumidity;
        Double totalDistanceInKm;
        bool isDark = true;
        string key = "27OtkDxArEqki7qITqKQbtPgfAtHaWOe";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            infoText = FindViewById<TextView>(Resource.Id.textView1);
            infoText2 = FindViewById<TextView>(Resource.Id.textView2);
            infoText3 = FindViewById<TextView>(Resource.Id.textView3);
            infoText4 = FindViewById<TextView>(Resource.Id.textView4);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            FloatingActionButton light = FindViewById<FloatingActionButton>(Resource.Id.light);
            light.Click += LightOnClick;

            this.Timer = new System.Timers.Timer();
            // Timer1.Start();
            this.Timer.Interval = 1000 * 30; // each 30 seconds * 60; // each minute
            this.Timer.Enabled = true;
            //Timer1.Elapsed += OnTimedEvent;
            Timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                Console.WriteLine("Updating now");
                StartUpdateAsync();
            };
            InitializeLocationManager();
            this.Timer.Start();
        }

        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            using (var criteriaForLocationService = new Criteria { Accuracy = Accuracy.Fine })
            {
                IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
                if (acceptableLocationProviders.Any())
                {
                    locationProvider = acceptableLocationProviders.First();
                }
                else
                {
                    locationProvider = string.Empty;
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void LightOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;

            if (isDark == true)
            {
                this.infoText.SetTextColor(Android.Graphics.Color.White);
                this.infoText2.SetTextColor(Android.Graphics.Color.White);
                this.infoText3.SetTextColor(Android.Graphics.Color.White);
                this.infoText4.SetTextColor(Android.Graphics.Color.White);
                this.isDark = false;
            } else
            {
                this.infoText.SetTextColor(Android.Graphics.Color.DarkGray);
                this.infoText2.SetTextColor(Android.Graphics.Color.DarkGray);
                this.infoText3.SetTextColor(Android.Graphics.Color.DarkGray);
                this.infoText4.SetTextColor(Android.Graphics.Color.DarkGray);
                this.isDark = true;
            }
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;

            const string locationPermission = Manifest.Permission.AccessFineLocation;
            string[] PermissionsLocation = { locationPermission };

            if (ContextCompat.CheckSelfPermission(this, locationPermission) == Permission.Granted)
            {
                // You already have permission, so copy your files...
                InitializeLocationManager();
                StartUpdateAsync(true);
                Snackbar
                    .Make(view, "Coordinates Posted to GitHub", Snackbar.LengthLong)
                    .SetAction("Action", (Android.Views.View.IOnClickListener)null)
                    .Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, PermissionsLocation, 999);
            }

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, locationPermission))
            {
                // Displaying a dialog would make sense, but for an example this works...
                Toast
                    .MakeText(this, "Will need permission to location", ToastLength.Long)
                    .Show();
                return;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case 999:
                    if (grantResults[0] == Permission.Granted)
                    {
                        // you have permission, you are allowed to read/write to external storage go do it...
                        InitializeLocationManager();
                        StartUpdateAsync(true);
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task GenerateData(SmbFile file)
        {
            try
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
                if (currentLocation == null)
                {
                    RunOnUiThread(() => this.infoText.Text = "Location is null");
                    return;
                }
                Console.WriteLine($"return? -> {currentLocation}");

                var coordinates = "";

                Console.WriteLine($"file?-> {file}");
                if (file != null)
                {
                    //Create reading buffer.
                    using (var memStream = new MemoryStream())
                    {
                        //Get readable stream.
                        using (var readStream = file.GetInputStream())
                        {
                            //Get bytes.
                            ((Stream)readStream).CopyTo(memStream);
                        }
                        coordinates = Encoding.UTF8.GetString(memStream.ToArray());
                    }

                    coordinates = $"{coordinates}{System.Environment.NewLine}{DateTime.Now:yyyy/MM/dd HH:mm},{currentLocation.Latitude.ToString()},{currentLocation.Longitude.ToString()}";
                    // Console.WriteLine($"coordinates?-> {coordinates}");
                    //Get writable stream.
                    using (var writeStream = file.GetOutputStream())
                    {
                        //Write bytes.
                        writeStream.Write(Encoding.UTF8.GetBytes(coordinates));
                    }

                    this.LastUpdate = DateTime.Now;
                    this.LastLocation = $"Lat: {Math.Round(currentLocation.Latitude, 2)}{System.Environment.NewLine}Long: {Math.Round(currentLocation.Longitude, 2)}";

                    var totalDistance = 0.0;

                    var coordinatesList = coordinates.Split(
                        new[] { System.Environment.NewLine },
                        StringSplitOptions.None);

                    for (var i = 1; i < coordinatesList.Length; i++)
                    {
                        try
                        {
                            var thisLat = Convert.ToDouble(coordinatesList[i].Split(',')[1]);
                            var thisLong = Convert.ToDouble(coordinatesList[i].Split(',')[2]);
                            var prevLat = Convert.ToDouble(coordinatesList[i - 1].Split(',')[1]);
                            var prevLong = Convert.ToDouble(coordinatesList[i - 1].Split(',')[2]);
                            var distance = new Coordinates(thisLat, thisLong)
                                .DistanceTo(new Coordinates(prevLat, prevLong), UnitOfLength.Kilometers);

                            totalDistance = totalDistance + distance;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }

                    this.totalDistanceInKm = totalDistance;

                    RunOnUiThread(() =>
                    {
                        try
                        {
                            this.infoText4.Text = $"{Math.Round(this.totalDistanceInKm, 1)}km";
                        }
                        catch (Exception)
                        {
                            Console.WriteLine($"errot gen data");
                        }
                    });

                    var mapquestUrl = $"http://www.mapquestapi.com/geocoding/v1/reverse?key={key}&location={currentLocation.Latitude},{currentLocation.Longitude}&includeRoadMetadata=false&includeNearestIntersection=false&thumbmaps=true";
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetStringAsync(mapquestUrl);
                        dynamic data = JObject.Parse(result);
                        Console.WriteLine($"{result}");
                        string city = data.results[0].locations[0].adminArea5;
                        if (city == "")
                        {
                            city = data.results[0].locations[0].adminArea4;
                        }
                        if (city == "")
                        {
                            city = data.results[0].locations[0].adminArea3;
                        }
                        if (city == "")
                        {
                            city = data.results[0].locations[0].adminArea2;
                        }
                        string thumbUrl = data.results[0].locations[0].mapUrl;
                        RunOnUiThread(() => this.infoText3.Text = city);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"errot gen data");
            }
        }

        private static List<string> TakeLastLines(string text, int count)
        {
            List<string> lines = new List<string>();
            Match match = Regex.Match(text, "^.*$", RegexOptions.Multiline | RegexOptions.RightToLeft);

            while (match.Success && lines.Count < count)
            {
                lines.Insert(0, match.Value);
                match = match.NextMatch();
            }

            return lines;
        }

        private void CaptureTemperatures(SmbFile file)
        {
            try
            {
                var temperatures = "";

                Console.WriteLine($"file?-> {file}");
                if (file != null)
                {
                    //Create reading buffer.
                    using (var memStream = new MemoryStream())
                    {
                        //Get readable stream.
                        using (var readStream = file.GetInputStream())
                        {
                            //Get bytes.
                            ((Stream)readStream).CopyTo(memStream);
                        }
                        temperatures = Encoding.UTF8.GetString(memStream.ToArray());
                        string[] stringSeparators = { "\r\n" };
                        string[] lines = temperatures.Split(stringSeparators, StringSplitOptions.None);
                        var lastTemperature = lines.First(l => l != "");
                        lastTemperature = lastTemperature.Split(",").Last();
                        this.LastTemperature = $"{Convert.ToDecimal(lastTemperature).ToString("0.##")}º C";
                        RunOnUiThread(() => this.infoText2.Text = $"{this.LastTemperature}");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"errot gen data");
            }
        }

        private async Task StartUpdateAsync(Boolean force = false)
        {
            RunOnUiThread(() => this.infoText.Text = $"{DateTime.Now.ToString("HH:mm")}");
            // { System.Environment.NewLine}{this.LastLocation}{System.Environment.NewLine}{this.LastTemperature}";

            //Set Local UDP-Broadcast Port.
            //When using the host name when connecting,
            //Change default local port(137) to a value larger than 1024.
            //In many cases, use of the well-known port is restricted.
            //
            // ** If possible, using IP addresses instead of host names 
            // ** to get better performance.
            //
            // IPAddress addr = System.Net.IPAddress.Parse("172.20.10.4");

            SharpCifs.Config.SetProperty("jcifs.smb.client.lport", "8137");
            // SharpCifs.Config.SetProperty("jcifs.smb.client.lport", "137");

            var ojitos = new SmbFile("smb://192.168.43.108", "");

            try
            {
                //Get shared folders in server.
                var shares = ojitos.ListFiles();
                foreach (var share in shares)
                {
                    try
                    {
                        if (share.GetName() == "share/")
                        {
                            try
                            {
                                //List items
                                foreach (SmbFile dir in share.ListFiles())
                                {
                                    try
                                    {
                                        if (dir.GetName() == "raspiEyes/")
                                        {

                                            try
                                            {
                                                //List items
                                                foreach (SmbFile file in dir.ListFiles())
                                                {
                                                    try
                                                    {
                                                        if (file.GetName() == "coordinates.txt")
                                                        {
                                                            if (this.LastLocation == null ||
                                                                this.LastUpdate.AddMinutes(30) < DateTime.Now ||
                                                                force == true)
                                                            {
                                                                await this.GenerateData(file);
                                                            }
                                                        }
                                                        if (file.GetName() == "last_temperature.txt")
                                                        {
                                                            this.CaptureTemperatures(file);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine($"{e}");
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine($"{e}");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine($"{e}");
                                    }
                                }

                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"{e}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error OnResume");
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            try
            {
                locationManager.RemoveUpdates(this);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error OnPause");
            }
        }

        public void OnLocationChanged(Location location)
        {
            currentLocation = location;
            if (currentLocation == null)
            {
                //Error Message  
            }
            else
            {
                // txtlatitu.Text = currentLocation.Latitude.ToString();
                // txtlong.Text = currentLocation.Longitude.ToString();
            }
        }

        public void OnProviderDisabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            // throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            // throw new NotImplementedException();
        }
    }

    public class Coordinates
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public Coordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public static class CoordinatesDistanceExtensions
    {
        public static double DistanceTo(this Coordinates baseCoordinates, Coordinates targetCoordinates)
        {
            return DistanceTo(baseCoordinates, targetCoordinates, UnitOfLength.Kilometers);
        }

        public static double DistanceTo(this Coordinates baseCoordinates, Coordinates targetCoordinates, UnitOfLength unitOfLength)
        {
            var baseRad = Math.PI * baseCoordinates.Latitude / 180;
            var targetRad = Math.PI * targetCoordinates.Latitude / 180;
            var theta = baseCoordinates.Longitude - targetCoordinates.Longitude;
            var thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;

            return unitOfLength.ConvertFromMiles(dist);
        }
    }

    public class UnitOfLength
    {
        public static UnitOfLength Kilometers = new UnitOfLength(1.609344);
        public static UnitOfLength NauticalMiles = new UnitOfLength(0.8684);
        public static UnitOfLength Miles = new UnitOfLength(1);

        private readonly double _fromMilesFactor;

        private UnitOfLength(double fromMilesFactor)
        {
            _fromMilesFactor = fromMilesFactor;
        }

        public double ConvertFromMiles(double input)
        {
            return input * _fromMilesFactor;
        }
    }
}