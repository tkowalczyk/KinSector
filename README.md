KinSector
=========

This application use [Kinect for Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/ "Kinect for Windows SDK") with [Twilio](https://www.twilio.com/docs/csharp/install "Twilio"). 

## Quick overview ##

**KinSector** has three great features and it could be used as a part of home automation systems. It shows how to integrate Kinect device and features from mobile operator to increase security in your home.

1) **KinSector** sends a MMS when some skeleton is being tracked in the room. It take a photo of the person, perform a message and send it to your mobile phone.

2) **KinSector** sends a SMS when someone puts his hands above head in the room. 

3) **KinSector** sends a SMS when someone said one of the previous defined word in dictionary for example: *help*

## How does it work? ##

[Twilio](https://www.twilio.com/docs/csharp/install "Twilio") is very simple to use so I just had to use its REST API with helpful NuGet package.

**Taking a photo using Kinect:**

	using (FileStream savedSnapshot = new FileStream(FileNameToSave, FileMode.Create)  
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


**Recognize *help* word and sends SMS**

	private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.5;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Console.WriteLine("Recognized: " + e.Result.Text);
                string content = "Someone said one of warning words: " + e.Result.Text;
                SendAlertViaSMS(MyMobileNumber, content);
            }
        }

**Sending SMS using Twilio REST API in .NET**

	private void SendAlertViaSMS(string TelephoneNumber, string TextToSend)
        {
            if(!AreHandsBeingAbove)
            {
                if(!string.IsNullOrEmpty(AccountSid) && !string.IsNullOrEmpty(AuthToken))
                {
                    var client = new TwilioRestClient(AccountSid, AuthToken);

                    var people = new Dictionary<string, string>() 
                    { 
                        {MyMobileNumber,"Tomasz Kowalczyk"}
                    };

                    foreach (var person in people)
                    {
                        client.SendMessage(
                            MyTwilioNumber, 
                            person.Key,     
                            string.Format("Hey {0}, {1}", person.Value, SMSContent)
                        );

                        Console.WriteLine(string.Format("Sent message to {0}", person.Value));
                    }   
                }
                else
                {
                    MessageBox.Show("Please provide your Twilio credentials: AccountSid and AuthToken");
                }
            }
        }

**More examples**

Feel free to visit my homepage [Tomasz Kowalczyk](http://tomek.kownet.info/ "Tomasz Kowalczyk") to see more Kinect examples.