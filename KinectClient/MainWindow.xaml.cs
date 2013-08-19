using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace KinectClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Class Instance Variables

        /// <summary>
        /// Stores the ID assigned to the Kinect sensor
        /// </summary>
        private String KinectID;

        /// <summary>
        /// The permanent connection to the server
        /// </summary>
        private Connection _connection;

        /// <summary>
        /// Kinect sensor
        /// </summary>
        private KinectSensor _sensor;

        /// <summary>
        /// Renders received skeleton frames
        /// </summary>
        private SkeletonRenderer _skeletonRenderer;

        /// <summary>
        /// Renders received depth frames
        /// </summary>
        private DepthRenderer _depthRenderer;

        /// <summary>
        /// Connection ready flag
        /// </summary>
        private bool connectionReady = false;

        /// <summary>
        /// Start sending flag
        /// </summary>
        private bool send = false;

        /// <summary>
        /// Standby timer 
        /// </summary>
        System.Windows.Threading.DispatcherTimer standbyTimer = new System.Windows.Threading.DispatcherTimer();

        /// <summary>
        /// Stores the location of the Kinect client
        /// </summary>
        private Point? _Location;
        private Point? Location
        {
            set
            {
                _Location = value;
                UpdateConfigurationFile();
            }

            get
            {
                return _Location;
            }
        }

        /// <summary>
        /// Stores the orientation of the Kinect client
        /// </summary>
        private Double? _Orientation;
        private Double? Orientation
        {
            set
            {
                _Orientation = value;
                UpdateConfigurationFile();
            }
            get
            {
                return _Orientation;
            }
        }

        /// <summary>
        /// The Directory of where the config file will be saved
        /// </summary>
        private const string CONFIGFILEDIRECTORY = "../../config/";

        /// <summary>
        /// The name of the config file
        /// </summary>
        private const string CONFIGFILELOCATION = CONFIGFILEDIRECTORY + "config.txt";

        #endregion

        #region Constructor
        public MainWindow()
        {
            KinectID = System.Environment.MachineName;
            TestKinectAvailability();

            //access saved location and orientation data from config.txt file
            LoadConfigurationFile();

            //Connection Lost timer
            standbyTimer.Tick += new EventHandler(InitializeConnectionAfterTimer);
            standbyTimer.Interval = new TimeSpan(0, 0, 1);

            // Initialization
            InitializeComponent();
            InitializeConnection();
            InitializeKinect();

            // Event Handlers
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            kinectOffRadioButton.Checked += new RoutedEventHandler(kinectOffRadioButton_Checked);
            kinectOnRadioButton.Checked += new RoutedEventHandler(kinectOnRadioButton_Checked);
            connectionOnRadionButton.Checked += new RoutedEventHandler(connectionOnRadionButton_Checked);
            connectionOffRadionButton.Checked += new RoutedEventHandler(connectionOffRadionButton_Checked);
        }
        #endregion

        #region Kinect Initialization
        private void InitializeKinect()
        {
            foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors)
            {
                _sensor = potentialSensor;
                break;
            }

            // Check if a Kinect sensor was discovered
            if (_sensor != null)
            {
                _sensor.SkeletonStream.Enable();
                _sensor.DepthStream.Enable();
                _sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(_sensor_DepthFrameReady);
                _sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(_sensor_SkeletonFrameReady);

                try 
                { 
                    _sensor.Start();

                    // Change Kinect status
                    this.kinectStatusBarText.Text = "Kinect Status: Ready";
                    kinectOnRadioButton.IsChecked = true;
                }

                catch (IOException) 
                { 
                    _sensor = null; 
                }
            }
            
            if(_sensor == null)
            {
                // Change Kinect status
                this.kinectStatusBarText.Text = "Kinect Status: No Kinect Ready Found!";
                kinectOffRadioButton.IsChecked = true;
            }
        }

        void _sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                _depthRenderer = new DepthRenderer(depthImage, e, _sensor);
            }));
        }

        void _sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    this.Dispatcher.Invoke(new Action(delegate()
                    {
                        _skeletonRenderer = new SkeletonRenderer(skeletonImage, e, _sensor);
                    }));

                    if (send && connectionReady)
                    {
                        Skeleton[] skels = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(skels);
                        byte[] data = objectToByteArray(skels);

                        Message message = new Message("SkeletonFrame");
                        message.AddField("Skeletons", data);
                        message.AddField("KinectID", KinectID);
                        
                        if(this._connection!=null)
                            this._connection.SendMessage(message);
                    }
                }
            }
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Converts an arbitrary object type to a Byte array
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <returns>Byte array representation of the passed object</returns>
        private byte[] objectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        /// <summary>
        /// Runs a Kinect check and exits if none detected
        /// </summary>
        private void TestKinectAvailability()
        {
            // Checks to see how many Kinects are connected to the system. If none then exit.
            if (KinectSensor.KinectSensors.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("No Kinect detected. Please plug in a Kinect and restart the program", "No Kinect Detected!");
                Environment.Exit(0);
            }
        }
        #endregion

        #region Connection Initialization
        private void InitializeConnection()
        {
            Connection.Discover("KinectServer", new SingleConnectionDiscoveryEventHandler(OnConnectionDiscovered));            
        }

        private void OnConnectionDiscovered(Connection connection)
        {
            this._connection = connection;

            if (this._connection != null)
            {
                this._connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);

                this.Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            this.connectionStatusBarText.Text = "Connection Status: Connected";
                            connectionOnRadionButton.IsChecked = true;
                        }));

                this._connection.Start();
                connectionReady = true;

                //send location and orientation info to server here
                sendKinectIDandLocation();
            }
            else
            {
                // Through the GUI thread, close the window
                this.Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            this.connectionStatusBarText.Text = "Connection Status: Pending";
                            connectionOnRadionButton.IsChecked = false;                            
                        }
                ));
                connectionReady = false;
                send = false;
                this.InitializeConnection();
            }

        }

        private void sendKinectIDandLocation()
        {
            Message message = new Message("NewKinect");
            message.AddField("KinectID", KinectID);

            if (Location != null)
            {
                message.AddField("LocationX", Location.Value.X);
                message.AddField("LocationY", Location.Value.Y);

                if (Orientation != null)
                {
                    message.AddField("Orientation", (double)Orientation);
                }
            }

            _connection.SendMessage(message);

        }

        private void sendLocationAndOrientationToServer()
        {
            if (Location != null)
            {
                Message message = new Message("LocationAndOrientationOfClient");
                message.AddField("ClientID", KinectID); 
                message.AddField("LocationX", Location.Value.X);
                message.AddField("LocationY", Location.Value.Y);

                if (Orientation != null)
                {
                    message.AddField("Orientation", Orientation);
                }

                _connection.SendMessage(message);
            }
        }
        #endregion

        #region Received Messsage Handler
        // Handles and reacts to messages received by the client
        private void OnMessageReceived(object sender, Message msg)
        {
            if (msg != null)
            {
                switch (msg.Name)
                {
                    default:
                        // Do nothing
                        break;

                    case "StartKinectStream":
                        send = true;
                        break;

                    case "StopKinectStream":
                        send = false;
                        break;

                    case "StandBy":
                        send = false;
                        _connection = null;
                        standbyTimer.Start();
                        break;

                    case "UpdateLocation":
                        double xValue = (double)msg.GetDoubleField("xValue");
                        double yValue = (double)msg.GetDoubleField("yValue");
                        Location = new Point(xValue, yValue);
                        break;

                    case "UpdateOrientation":
                        Orientation = (double?)msg.GetDoubleField("orientation");
                        break;
                }
            }
        }

        private void InitializeConnectionAfterTimer(object sender, EventArgs e)
        {
            InitializeConnection();
            standbyTimer.Stop();
        }

        #endregion

        #region Window Event Handlers
        /// <summary>
        /// Handles clean exiting of the program 
        /// </summary>
        /// <param name="sender">Window object</param>
        /// <param name="e">Cancel event args</param>
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_connection != null) { _connection.Stop(); }
            if (_sensor != null) { _sensor.Stop(); }
            Environment.Exit(0);
        }
        #endregion

        #region Kinect Tilt Angle Control
        /// <summary>
        /// Captures the start of slider move
        /// </summary>
        /// <param name="sender">Slider object</param>
        /// <param name="e">Mouse button event args</param>
        private void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;

            if (null != fe)
            {
                if (fe.CaptureMouse())
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Changes the tilt angle of the sensor
        /// </summary>
        /// <param name="sender">Slider object</param>
        /// <param name="e">Mouse button event args</param>
        private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;

            if (null != fe)
            {
                if (fe.IsMouseCaptured)
                {
                    fe.ReleaseMouseCapture();
                    e.Handled = true;
                }

                Int16 newTiltAngle = Int16.Parse(tiltAngleTextBox.Text);

                try
                {
                    _sensor.ElevationAngle = newTiltAngle;
                }
                catch (InvalidOperationException exception)
                {
                    System.Console.WriteLine(exception.Message);
                }
            }
        }

        /// <summary>
        /// Changes tilt angle as the slider moves
        /// </summary>
        /// <param name="sender">Slider object</param>
        /// <param name="e">Mouse event args</param>
        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            var fe = sender as FrameworkElement;

            if (null != fe)
            {
                if (fe.IsMouseCaptured)
                {
                    var position = Mouse.GetPosition(this.SliderTrack);
                    int newAngle = -27 + (int)Math.Round(54.0 * (this.SliderTrack.ActualHeight - position.Y) / this.SliderTrack.ActualHeight);

                    if (newAngle < -27)
                    {
                        newAngle = -27;
                    }
                    else if (newAngle > 27)
                    {
                        newAngle = 27;
                    }
                    RotateTransform rt = new RotateTransform(-2 * newAngle);
                    SliderArrow.RenderTransform = rt;
                    tiltAngleTextBox.Text = newAngle.ToString();
                }
            }
        }
        #endregion

        #region Connection Status Controls

        /// <summary>
        /// Turns the connection off
        /// </summary>
        /// <param name="sender">Radio button object</param>
        /// <param name="e">Routed event args</param>
        void connectionOffRadionButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Stop();
                }
                this.connectionStatusBarText.Text = "Connection Status: Closed";
            }
            catch(Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// Turns the connection on
        /// </summary>
        /// <param name="sender">Radio button object</param>
        /// <param name="e">Routed event args</param>
        void connectionOnRadionButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeConnection();
                this.connectionStatusBarText.Text = "Connection Status: Connected";
            }
            catch(Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }
        }
        #endregion

        #region Kinect Status Controls

        /// <summary>
        /// Turns the Kinect on
        /// </summary>
        /// <param name="sender">Radio button object</param>
        /// <param name="e">Routed event args</param>
        void kinectOnRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!_sensor.IsRunning)
            {
                InitializeKinect();
            }
        }

        /// <summary>
        /// Turns the Kinect off
        /// </summary>
        /// <param name="sender">Radio button object</param>
        /// <param name="e">Routed event args</param>
        void kinectOffRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_sensor.IsRunning)
            {
                try
                {
                    _sensor.Stop();
                }
                catch (Exception exception)
                {
                    System.Console.WriteLine(exception.Message);
                }
            }
        }
        #endregion        

        #region Saving and accessing Location

        private void LoadConfigurationFile()
        {
            if (!Directory.Exists(CONFIGFILEDIRECTORY))
            {
                Directory.CreateDirectory(CONFIGFILEDIRECTORY);
            }

            if (!File.Exists(CONFIGFILELOCATION))
            {
                System.IO.File.Create(CONFIGFILELOCATION).Close();
            }
            else
            {
                string[] lines = File.ReadAllLines(CONFIGFILELOCATION);
                ParseConfigurationFile(lines);
            }
        }

        private void ParseConfigurationFile(string[] lines)
        {
            foreach (string line in lines)
            {
                string[] s = line.Split(':');

                if (s.Length == 2)
                {
                    switch (s[0])
                    {
                        case "location":

                            // Split on space to get multiple parameters per line
                            string[] location = s[1].Split(' ');

                            try
                            {
                                if (location.Length == 2)
                                {
                                    Location = (Point?) new System.Windows.Point(Convert.ToDouble(location[0]), Convert.ToDouble(location[1]));
                                }
                            }
                            catch
                            {
                                Console.Write("Configuration File has invalid data for Location field");
                            }

                            break;
                        case "orientation":
                            try
                            {
                                Orientation = Convert.ToDouble(s[1]);
                            }
                            catch
                            {
                                Console.Write("Configuration File has invalid data for Orientation field");
                            }
                            break;
                    }
                }
            }
        }

        private void UpdateConfigurationFile()
        {
            List<string> outputStrings = new List<string>();

            if (Location != null)
            {
                outputStrings.Add("location:" + Location.Value.X + " " + Location.Value.Y);
            }
            if (Orientation != null)
            {
                outputStrings.Add("orientation:" + Orientation);
            }

            string[] outputString = outputStrings.ToArray();

            try
            {
                File.WriteAllLines(CONFIGFILELOCATION, outputString);
            }
            catch (IOException exception)
            {
                System.Console.WriteLine(exception.Message);
            }

        }

        #endregion

    }
}
