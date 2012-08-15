KinSector
=========

This application use [Kinect for Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/ "Kinect for Windows SDK") with [T-Mobile OpenAPI](https://developers.t-mobile.pl/ "T-Mobile OpenAPI"). 

## Quick overview ##

**KinSector** has three great features and it could be used as a part of home automation systems. It shows how to integrate Kinect device and features from mobile operator to increase security in your home.

1) **KinSector** sends a MMS when some skeleton is being tracked in the room. It take a photo of the person, perform a message and send it to your mobile phone.

2) **KinSector** sends a SMS when someone puts his hands above head in the room. 

3) **KinSector** sends a SMS when someone said one of the previous defined word in dictionary for example: *help*

## How does it work? ##

[T-Mobile OpenAPI](https://developers.t-mobile.pl/ "T-Mobile OpenAPI") is very simple to use so I just had to code it in .NET world.

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


**Reading a photo from specific path and building header for MMS:**

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
                }`

**Sending SMS**

`var uri = new Uri(string.Format(queryURL + querySMS));`

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

**More examples**

Feel free to visit my homepage [Tomasz Kowalczyk](http://tomek.kownet.info/ "Tomasz Kowalczyk") to see more Kinect examples.