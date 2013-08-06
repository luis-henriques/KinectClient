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
        private Connection _connection;
        private KinectSensor _sensor;
        private SkeletonRenderer _renderer;
        private bool connectionReady = false;

        private bool send = true;

        #endregion


        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            InitializeConnection();
            InitializeKinect();

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

            if (_sensor != null)
            {
                _sensor.SkeletonStream.Enable();
                _sensor.DepthStream.Enable();
                _sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(_sensor_SkeletonFrameReady);

                try 
                { 
                    _sensor.Start();

                    //_sensor.ElevationAngle = -10;
                    //Thread.Sleep(100);
                    //_sensor.ElevationAngle = 10;
                    //Thread.Sleep(100);
                    //_sensor.ElevationAngle = 0;
                    //Thread.Sleep(100);

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
                this.kinectStatusBarText.Text = "Kinect Status: No Kinect Ready Found!";
                kinectOffRadioButton.IsChecked = true;
            }
        }

        void _sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    this.Dispatcher.Invoke(new Action(delegate()
                    {
                        _renderer = new SkeletonRenderer(skeletonImage, e, _sensor);
                    }));

                    if (send && connectionReady)
                    {
                        Skeleton[] skels = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(skels);
                        byte[] data = objectToByteArray(skels);

                        Message message = new Message("SkeletonFrame");
                        message.AddField("Skeletons", data);
                        message.AddField("KinectID", System.Environment.MachineName);
                        this._connection.SendMessage(message);
                    }
                }
            }
        }
        #endregion

        #region Helper Functions
        private byte[] objectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
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
                send = true;
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
                            //this.Close();
                        }
                ));
                connectionReady = false;
                this.InitializeConnection();
            }
        }
        
        //private void OnConnectionDiscovered(Connection connection)
        //{
        //    this._connection = connection;

        //    if (this._connection != null)
        //    {
        //        this._connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);
                
        //        this.Dispatcher.Invoke(
        //            new Action(
        //                delegate()
        //                {
        //                    this.connectionStatusBarText.Text = "Connection Status: Connected";
        //                    connectionOnRadionButton.IsChecked = true;
        //                }));

        //        this._connection.Start();
        //        connectionReady = true;
        //    }
        //    else
        //    {
        //        // Through the GUI thread, close the window
        //        this.Dispatcher.Invoke(
        //            new Action(
        //                delegate()
        //                {
        //                    this.connectionStatusBarText.Text = "Connection Status: Pending";
        //                    connectionOnRadionButton.IsChecked = false;
        //                    this.InitializeConnection();
        //                    //this.Close();
        //                }
        //        ));
        //    }
        //}
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
                        //_connection.Stop();
                        _connection = null;
                        InitializeConnection();
                        break;
                }
            }
        }
        #endregion

        #region Window Event Handlers
        
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_connection != null) { _connection.Stop(); }
            if (_sensor != null) { _sensor.Stop(); }
        }

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

        void connectionOffRadionButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _connection.Stop();
                this.connectionStatusBarText.Text = "Connection Status: Closed";
            }
            catch(Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }
        }

        void connectionOnRadionButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeConnection();
            }
            catch(Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }
        }

        void kinectOnRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!_sensor.IsRunning)
            {
                InitializeKinect();
            }
        }

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

    }
}
