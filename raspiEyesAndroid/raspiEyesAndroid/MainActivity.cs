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

namespace raspiEyesAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {

        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;
        System.Timers.Timer Timer;
        DateTime LastUpdate;
        TextView infoText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            infoText = FindViewById<TextView>(Resource.Id.textView1);
            

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            this.Timer = new System.Timers.Timer();
            // Timer1.Start();
            this.Timer.Interval = 1000; // each second
            this.Timer.Enabled = true;
            //Timer1.Elapsed += OnTimedEvent;
            this.Timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                this.infoText.Text = this.LastUpdate.ToString();
                if (this.LastUpdate.AddMinutes(3) < DateTime.Now)
                {
                    // this.Timer.Stop();
                    this.AccessShare();
                    /*
                    RunOnUiThread(() =>
                    {
                        SetContentView(Resource.Layout.Main);
                    });
                    */
                    //Delete time since it will no longer be used.
                    //this.Timer.Dispose();
                    this.LastUpdate = DateTime.Now;
                }
            };
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
                Console.WriteLine($"Using {locationProvider}");
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
                AccessShare();
                Snackbar.Make(view, "Coordinates Posted to GitHub", Snackbar.LengthLong)
                    .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
            }

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, locationPermission))
            {
                // Displaying a dialog would make sense, but for an example this works...
                Toast.MakeText(this, "Will need permission to location", ToastLength.Long).Show();
                return;
            }
            ActivityCompat.RequestPermissions(this, PermissionsLocation, 999);
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
                        AccessShare();
                    }
                    break;
                default:
                    break;
            }
        }

        private void GenerateData(SmbFile file)
        {
            if (currentLocation == null)
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            }
            Console.WriteLine($"return? -> {currentLocation}");
            if (currentLocation == null)
            {
                return;
            }

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

                //Get the SmbFile specifying the file name to be created.
                // var file = new SmbFile("smb://UserName:Password@ServerIP/ShareName/Folder/NewFileName.txt");

                //Create file.
                //file.CreateNewFile();
                coordinates = $"{coordinates}{System.Environment.NewLine}{DateTime.Now:yyyy/MM/dd HH:mm},{currentLocation.Latitude.ToString()},{currentLocation.Longitude.ToString()}";

                Console.WriteLine($"coordinates?-> {coordinates}");

                //Get writable stream.
                using (var writeStream = file.GetOutputStream())
                {
                    //Write bytes.
                    writeStream.Write(Encoding.UTF8.GetBytes(coordinates));
                }
            }
        }

        private void AccessShare()
        {
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

            //Get local workgroups.
            var lan = new SmbFile("smb://", "");

            try
            {
                var workgroups = lan.ListFiles();
                foreach (var workgroup in workgroups)
                {
                    try
                    {
                        var servers = workgroup.ListFiles();
                        foreach (var server in servers)
                        {
                            try
                            {
                                if (server.GetName() == "ojitos/")
                                {
                                    //Get shared folders in server.
                                    var shares = server.ListFiles();
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
                                                                                this.GenerateData(file);
                                                                            }
                                                                        }
                                                                        catch (Exception)
                                                                        {
                                                                            Console.WriteLine($"Access Denied");
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    Console.WriteLine($"Access Denied");
                                                                }
                                                            }
                                                        }
                                                        catch (Exception)
                                                        {
                                                            Console.WriteLine($"Access Denied");
                                                        }
                                                    }

                                                }
                                                catch (Exception)
                                                {
                                                    Console.WriteLine($"Access Denied");
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            Console.WriteLine($"Access Denied");
                                        }
                                    }

                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"Access Denied");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Access Denied");
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine($"Access Denied");
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            } catch (Exception)
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