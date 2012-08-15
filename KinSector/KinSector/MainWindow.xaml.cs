using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace KinSector
{
    public partial class MainWindow : Window
    {
        #region Properties
        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        KinectSensor _sensor;

        /// <summary>
        /// Maximum number of skeletons to track.
        /// </summary>
        const int skeletonCount = 6;

        /// <summary>
        /// Array of all tracked skeletons
        /// </summary>
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine;

        /// <summary>
        /// Path to grammar file with defined words to recognize
        /// </summary>
        private const string _grammarPath = "Dictionary\\Grammar.xml";

        /// <summary>
        /// Name of taken photo via Kinect device
        /// </summary>
        private const string kinectPhoto = "photo.jpg";

        /// <summary>
        /// Open API application key
        /// </summary>
        private const string AppKey = "";

        /// <summary>
        /// Receiver telephone number
        /// </summary>
        private const string TelNumb = "";

        /// <summary>
        /// MMS title
        /// </summary>
        private const string MMSTitle = "Someone is in your room!";

        /// <summary>
        /// SMS content
        /// </summary>
        private const string SMSContent = "Someone has his hands up!";

        /// <summary>
        /// Path to taken photo
        /// </summary>
        private const string FilePath = "photo.jpg";

        /// <summary>
        /// URL to the Open API service
        /// </summary>
        private const string queryURL = "https://developers.t-mobile.pl/api/messaging/";

        /// <summary>
        /// Set tracking state
        /// </summary>
        bool tracking = false;

        /// <summary>
        /// Check if any skeleton has tracking state active
        /// </summary>
        bool AreSkeletonsBeingTracked
        {
            get { return tracking; }
            set
            {
                if (tracking != value)
                {
                    TakePhoto(kinectPhoto);
                    SendAlertViaMMS(TelNumb, MMSTitle);
                }

                tracking = value;
            }
        }

        /// <summary>
        /// Set hands above head state
        /// </summary>
        bool handsAbove = false;

        /// <summary>
        /// Check if any skeleton has his hands above head
        /// </summary>
        bool AreHandsBeingAbove
        {
            get { return handsAbove; }
            set
            {
                if (handsAbove != value)
                {
                    SendAlertViaSMS(TelNumb, SMSContent);
                }

                handsAbove = value;
            }
        }
        #endregion

        #region Ctor
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closed += new EventHandler(MainWindow_Closed);
        }
        #endregion

        #region Window Events
        /// <summary>
        /// Event method fired when window is loaded
        /// </summary>
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];
                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
                    _sensor.SkeletonStream.Enable();

                    _sensor.Start();
                }

                RecognizerInfo ri = GetKinectRecognizer();

                if (null != ri)
                {
                    this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                    // Create a grammar from grammar definition XML file.

                    var g = new Microsoft.Speech.Recognition.Grammar(_grammarPath);
                    speechEngine.LoadGrammar(g);

                    speechEngine.SpeechRecognized += SpeechRecognized;
                    speechEngine.SpeechRecognitionRejected += SpeechRejected;

                    speechEngine.SetInputToAudioStream(
                        _sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                    speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
                else
                {
                    Console.WriteLine("No speech recognition engine installed!");
                }
            }
        }

        /// <summary>
        /// Event method fired when window is closed
        /// </summary>
        void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
                _sensor.AudioSource.Stop();
            }
        }
        #endregion

        #region Kinect events
        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return;
                }

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                AreSkeletonsBeingTracked = allSkeletons.Any(x =>
                x.TrackingState == SkeletonTrackingState.Tracked);

                AreHandsBeingAbove = allSkeletons.Any(skeleton =>
                    (skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.Head].Position.Y) &&
                            (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.Head].Position.Y));

                foreach (Skeleton data in allSkeletons)
                {
                    if (SkeletonTrackingState.Tracked == data.TrackingState)
                    {
                        tracking = true;
                        break;
                    }
                    else
                    {
                        tracking = false;
                    }
                }

                foreach (Skeleton skeleton in allSkeletons)
                {
                    if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                    {
                        if ((skeleton.Joints[JointType.HandLeft].Position.Y > skeleton.Joints[JointType.Head].Position.Y) &&
                            (skeleton.Joints[JointType.HandRight].Position.Y > skeleton.Joints[JointType.Head].Position.Y))
                        {
                            handsAbove = true;
                            break;
                        }
                        else
                        {
                            handsAbove = false;
                        }
                    }
                }

                skeletonActive.Content = tracking
                ? "Someone is here!"
                : "Clear";
            }

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4;

                kinectColorView.Source =
                    BitmapSource.Create(colorFrame.Width,
                    colorFrame.Height,
                    96,
                    96,
                    PixelFormats.Bgr32,
                    null,
                    pixels,
                    stride);
            }
        }
        #endregion

        #region Speech events
        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.8;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.WriteLine("Recognized: " + e.Result.Text);
                string content = "Someone said one of warning words: " + e.Result.Text;
                SendAlertViaSMS(TelNumb, content);
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Something unrecognized");
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Send a SMS with an alert message
        /// </summary>
        private void SendAlertViaSMS(string TelephoneNumber, string TextToSend)
        {
            string querySMS = string.Format("sms?to={0}&text={1}&appkey={2}", TelephoneNumber, TextToSend, AppKey);

            if (!AreHandsBeingAbove)
            {
                var uri = new Uri(string.Format(queryURL + querySMS));

                if (uri.Scheme == Uri.UriSchemeHttps)
                {
                    var request = WebRequest.Create(uri);
                    request.Method = WebRequestMethods.Http.Get;

                    using (var response = request.GetResponse())
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            string tmp = reader.ReadToEnd();
                            Console.WriteLine(tmp);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Takes photo from Kinect device
        /// </summary>
        private void TakePhoto(string FileNameToSave)
        {
            if (!AreSkeletonsBeingTracked)
            {
                using (FileStream savedSnapshot = new FileStream(FileNameToSave, FileMode.Create))
                {
                    BitmapSource image = (BitmapSource)kinectColorView.Source;
                    JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
                    jpgEncoder.QualityLevel = 70;
                    jpgEncoder.Frames.Add(BitmapFrame.Create(image));
                    jpgEncoder.Save(savedSnapshot);
                    savedSnapshot.Flush();
                    savedSnapshot.Close();
                    savedSnapshot.Dispose();
                }
            }
        }

        /// <summary>
        /// Send a MMS with photo taken by Kinect when someone is tracked
        /// </summary>
        private void SendAlertViaMMS(string TelephoneNumber, string TitleForMMS)
        {
            string queryMMS = string.Format("mms?to={0}&title={1}&appkey={2}", TelephoneNumber, TitleForMMS, AppKey);

            if (!AreSkeletonsBeingTracked)
            {
                byte[] bytes = null;
                FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                long numBytes = new FileInfo(FilePath).Length;
                bytes = br.ReadBytes((int)numBytes);

                var uri = new Uri(string.Format(queryURL + queryMMS));

                if (uri.Scheme == Uri.UriSchemeHttps)
                {
                    var request = WebRequest.Create(uri);
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = "image/jpg";
                    request.ContentLength = bytes.Length;

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    using (var response = request.GetResponse())
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            string tmp = reader.ReadToEnd();
                            Console.WriteLine(tmp);
                        }
                    }
                }
            }
        }
        #endregion

        #region Speech methods
        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }
        #endregion
    }
}