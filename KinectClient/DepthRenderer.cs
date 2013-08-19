using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace KinectClient
{
    class DepthRenderer
    {
        #region Instance Variables
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Image that will view depth data
        /// </summary>
        private Image Image;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private byte[] colorPixels;
        #endregion

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public DepthRenderer(Image Image, DepthImageFrameReadyEventArgs e, KinectSensor sensor)
        {
            // Set the Kinect sensor
            this.sensor = sensor;

            // Turn on the depth stream to receive depth frames
            this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            // Allocate space to put the depth pixels we'll receive
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            // Allocate space to put the color pixels we'll create
            this.colorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];

            // This is the bitmap we'll display on-screen
            this.colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Initialize the image
            this.Image = Image;

            // Set the image we display to point to the bitmap where we'll put the image data
            this.Image.Source = this.colorBitmap;

            // Process the depth data
            SensorDepthFrameReady(e);
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).
                        byte intensity = (byte)(depth >= minDepth && depth <= 7000 ? depth : 0);

                        // Assign colors to distinct players
                        int playerIndex = depthPixels[i].PlayerIndex;
                        if (playerIndex == 0)
                        {
                            // Write out blue byte
                            this.colorPixels[colorPixelIndex++] = (byte)(intensity);

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = (byte)(intensity);

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = (byte)(intensity);
                        }
                        else
                        {
                            // Write out blue byte
                            this.colorPixels[colorPixelIndex++] = (byte)(playerIndex * 33);

                            // Write out green byte
                            this.colorPixels[colorPixelIndex++] = (byte)(0);

                            // Write out red byte                        
                            this.colorPixels[colorPixelIndex++] = (byte)(playerIndex * 150);
                        }
                                                
                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }
    }
}
