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

        private bool send = true;

        #endregion


        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            InitializeConnection();
            InitializeKinect();

            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
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

                    _sensor.ElevationAngle = -10;
                    Thread.Sleep(100);
                    _sensor.ElevationAngle = 10;
                    Thread.Sleep(100);
                    _sensor.ElevationAngle = 0;
                    Thread.Sleep(100);

                    this.kinectStatusBarText.Text = "Kinect Status: Ready";
                }

                catch (IOException) 
                { 
                    _sensor = null; 
                }
            }
            
            if(_sensor == null)
            {
                this.kinectStatusBarText.Text = "Kinect Status: No Kinect Ready Found!";
            }
        }

        void _sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (send)
            {
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                    {
                        Skeleton[] skels = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(skels);
                        byte[] data = objectToByteArray(skels);

                        Message message = new Message("SkeletonFrame");
                        message.AddField("Skeletons", data);
                        this._connection.SendMessage(message);

                        this.Dispatcher.Invoke(new Action(delegate()
                        {
                            _renderer = new SkeletonRenderer(skeletonImage, e, _sensor);
                        }));

                    }
                }
                send = false;
            }
            else
                send = true;
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
                        }));

                this._connection.Start();                
            }
            else
            {
                // Through the GUI thread, close the window
                this.Dispatcher.Invoke(
                    new Action(
                        delegate()
                        {
                            this.connectionStatusBarText.Text = "Connection Status: Pending";
                            this.InitializeConnection();
                            //this.Close();
                        }
                ));
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

        #endregion 
    }
}
