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
        String LastLocation;
        String LastTemperature;
        String LastHumidity;
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

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

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

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View)sender;

            const string locationPermission = Manifest.Permission.AccessFineLocation;
            string[] PermissionsLocation = { locationPermission };

            if (ContextCompat.CheckSelfPermission(this, locationPermission) == Permission.Granted)
            {
                // You already have permission, so copy your files...
                InitializeLocationManager();
                StartUpdateAsync();
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
                        StartUpdateAsync();
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
                    Console.WriteLine($"coordinates?-> {coordinates}");
                    //Get writable stream.
                    using (var writeStream = file.GetOutputStream())
                    {
                        //Write bytes.
                        writeStream.Write(Encoding.UTF8.GetBytes(coordinates));
                    }

                    this.LastUpdate = DateTime.Now;
                    this.LastLocation = $"Lat: {Math.Round(currentLocation.Latitude, 2)}{System.Environment.NewLine}Long: {Math.Round(currentLocation.Longitude, 2)}";
                    var mapquestUrl = $"http://www.mapquestapi.com/geocoding/v1/reverse?key={key}&location={currentLocation.Latitude},{currentLocation.Longitude}&includeRoadMetadata=false&includeNearestIntersection=false&thumbmaps=true";
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetStringAsync(mapquestUrl);
                        dynamic data = JObject.Parse(result);
                        string city = data.results[0].locations[0].adminArea5;
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
                        var lastTemperature = lines.Last(l => l != "");
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

        private async Task StartUpdateAsync()
        {
            RunOnUiThread(() => this.infoText.Text = $"{DateTime.Now.ToString("HH:mm")}");
            // { System.Environment.NewLine}{this.LastLocation}{System.Environment.NewLine}{this.LastTemperature}";

            if (this.LastLocation != null &&
                this.LastUpdate.AddMinutes(30) > DateTime.Now)
            {
                return;
            }

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
                                                            await this.GenerateData(file);
                                                        }
                                                        if (file.GetName() == "temperatures.txt")
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

}