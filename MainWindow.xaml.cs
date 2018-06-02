using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Windows.Controls;  // DavidLim : For the definition of common controls like Button.

namespace FaceTutorial
{
    public partial class MainWindow : Window
    {
        // Replace the first parameter with your valid subscription key.
        //
        // Replace or verify the region in the second parameter.
        //
        // You must use the same region in your REST API call as you used to obtain your subscription keys.
        // For example, if you obtained your subscription keys from the westus region, replace
        // "westcentralus" in the URI below with "westus".
        //
        // NOTE: Free trial subscription keys are generated in the westcentralus region, so if you are using
        // a free trial subscription key, you should not need to change this region.
        //
        // DavidLim : 
        // The original tutorial code instantiated faceServiceClient to a new
        // instance of FaceServiceClient with preset Face API keys and rootAPI.
        // I changed the tutorial code so that faceServiceClient is set to an intial value of null.
        // It will only be instantiated later after a FaceAPI key has been supplied by the User.
        private /*readonly*/ IFaceServiceClient faceServiceClient = null;

        Face[] faces;                   // The list of detected faces.
        String[] faceDescriptions;      // The list of descriptions for the detected faces.
        double resizeFactor;            // The resize factor for the displayed image.

        // DavidLim :
        // As my enhancement for the tutorial, I have added a 2nd (right hand side) image.
        // As there will be 2 images hence we must have duplicate properties
        // for the 2nd image.
        Face[] faces2;                  // The list of detected faces on FacePhoto2 image control.
        String[] faceDescriptions2;     // The list of descriptions for the detected faces FacePhoto2 image control.
        double resizeFactor2;           // The resize factor for the displayed image on the FacePhoto2 image control.

        public MainWindow()
        {
            InitializeComponent();
        }

        // Displays the image and calls Detect Faces.
        // DavidLim :
        // BrowseButton_Click() is the event handler shared by both "Browse" buttons.
        // The sender parameter will tell us which button was the one clicked.
        // Later in this code, we will use the Name property of sender to determine this.
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the image file to scan from the user.
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            // Return if canceled.
            if (!(bool)result)
            {
                return;
            }

            // Display the image file.
            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            // Detect any faces in the image.
            Title = "Detecting...";

            // DavidLim
            // Define an array of Face objects (detectedfaces) for the current detection.
            // This array will later be assigned to either faces or faces2 (these are
            // member variables).
            //
            // Also define an array of strings (detectedFaceDescriptions) for the the current detection.
            // This array will later be assigned to either faceDescriptions or faceDescriptions2.
            //
            // Also define a double (currentResizeFactor) for the ResizeFactor for the current image.
            // The value of currentResizeFactor will later be assigned to either resizeFactor or
            // resizeFactor2. Note that the Resize Factor depends on the resolution of the current
            // image as so is not a constant value. Another important factor is the XAML image tag
            // "Stretch" property.
            //
            // These local variables are used in lieu of member variables (e.g. faces), as is the case in the 
            // original tutorial. This is because BrowseButton_Click() is a generic method that applies 
            // to both BrowseButton and BrowseButton2. Hence, the face data is stored locally before 
            // being transferred to the relevant member variable(s). In the case of the tutorial, however, 
            // there is only one Browse button and one Image control, and hence we are able to transfer the face 
            // data straightaway to the faces array.
            Face[] detectedfaces;
            String[] detectedFaceDescriptions;
            double currentResizeFactor;

            /*faces*/
            detectedfaces = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", /*faces*/detectedfaces.Length);

            if (/*faces*/detectedfaces.Length > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                /*resizeFactor*/
                currentResizeFactor = 96 / dpi;
                /*faceDescriptions*/
                detectedFaceDescriptions = new String[/*faces*/detectedfaces.Length];

                for (int i = 0; i < /*faces*/detectedfaces.Length; ++i)
                {
                    Face face = /*faces*/detectedfaces[i];

                    // Draw a rectangle on the face.
                    // DavidLim :
                    // Note that the dimensions given in FaceRectangle
                    // is relative to the dimensions of the bitmap which
                    // has been sent for face detection.
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * /*resizeFactor*/ currentResizeFactor,
                            face.FaceRectangle.Top * /*resizeFactor*/ currentResizeFactor,
                            face.FaceRectangle.Width * /*resizeFactor*/ currentResizeFactor,
                            face.FaceRectangle.Height * /*resizeFactor*/ currentResizeFactor
                            )
                    );

                    // Store the face description.
                    /*faceDescriptions[i]*/
                    detectedFaceDescriptions[i] = FaceDescription(face);
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * /*resizeFactor*/ currentResizeFactor),
                    (int)(bitmapSource.PixelHeight * /*resizeFactor*/ currentResizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);

                // DavidLim :
                // Set properties according to which "Browse" button was clicked. 
                if (((Button)sender).Name.Equals("BrowseButton"))
                {
                    FacePhoto.Source = faceWithRectBitmap;
                    faces = detectedfaces;
                    faceDescriptions = detectedFaceDescriptions;
                    resizeFactor = currentResizeFactor;
                    // Set the status bar text.
                    faceDescriptionStatusBar.Text = "Place the mouse pointer over a face to see the face description.";
                }
                else
                {
                    FacePhoto2.Source = faceWithRectBitmap;
                    faces2 = detectedfaces;
                    faceDescriptions2 = detectedFaceDescriptions;
                    resizeFactor2 = currentResizeFactor;
                    // Set the status bar text.
                    faceDescriptionStatusBar2.Text = "Place the mouse pointer over a face to see the face description.";
                }
            }
        }

        // Displays the face description when the mouse is over a face rectangle.
        // DavidLim :
        // FacePhoto_MouseMove() is an event handler shared by both "FacePhoto" 
        // and "FacePhoto2" image controls.
        // We use the sender parameter and its Name property to determine which
        // image control was the one involved.
        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            // DavidLim :
            // Make generic references.
            Face[] currentFaces;
            String[] currentFaceDescriptions;
            TextBlock currentTextBlock;
            double currentResizeFactor;

            Image imgControl;
            if (((Image)sender).Name.Equals("FacePhoto"))
            {
                currentFaces = faces;
                currentFaceDescriptions = faceDescriptions;
                imgControl = FacePhoto;
                currentResizeFactor = resizeFactor;
                currentTextBlock = faceDescriptionStatusBar;
            }
            else
            {
                currentFaces = faces2;
                currentFaceDescriptions = faceDescriptions2;
                imgControl = FacePhoto2;
                currentResizeFactor = resizeFactor2;
                currentTextBlock = faceDescriptionStatusBar2;
            }

            // If the REST call has not completed, return from this method.
            // DavidLim :
            // We use currentFaces instead of directly using faces
            // so as to also include check for faces2.
            if (/*faces*/ currentFaces == null)
                return;


            // Find the mouse position relative to the image.
            Point mouseXY = e.GetPosition(/*FacePhoto*/ imgControl);

            ImageSource imageSource = /*FacePhoto.Source*/ imgControl.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;

            // DavidLim :
            // Note that bitmapSource is different from the image that is displayed 
            // on-screen on the image control.
            // The image control may display the image larger or smaller than
            // the actual bitmap itself. imgControl.ActualWidth and imgControl.ActualHeight
            // provides the actual on-screen width and height of the image displayed on 
            // the image control.
            // Also note that the face rectangle, when shown on the image control on the
            // screen, is similarly larger or smaller than the dimensions in 
            // currentFaces.FaceRectangle. This is where scale is important.
            //
            // Scale adjustment between the actual size and displayed size.
            //var scale = FacePhoto.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);
            var scale = imgControl.ActualWidth / (bitmapSource.PixelWidth / currentResizeFactor);

            // Check if this mouse position is over a face rectangle.
            bool mouseOverFace = false;

            for (int i = 0; i < /*faces*/currentFaces.Length; ++i)
            {
                FaceRectangle fr = /*faces[i]*/currentFaces[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                // Display the face description for this face if the mouse is over this face rectangle.
                if (mouseXY.X >= left && mouseXY.X <= left + width && mouseXY.Y >= top && mouseXY.Y <= top + height)
                {
                    // DavidLim :
                    // currentTextBlock generically points to either faceDescriptionStatusBar
                    // or faceDescriptionStatusBar2.
                    currentTextBlock.Text = currentFaceDescriptions[i];
                    mouseOverFace = true;
                    break;
                }
            }

            // If the mouse is not over a face rectangle.
            if (!mouseOverFace)
            {
                // DavidLim :
                // currentTextBlock generically points to either faceDescriptionStatusBar
                // or faceDescriptionStatusBar2.
                currentTextBlock.Text = "Place the mouse pointer over a face to see the face description.";
            }
        }

        // DavidLim :
        // MSCognitiveServicesLogin_Click() is the handler for the Click event of the
        // button named MSCognitiveServicesLogin in the xaml.
        // This handler responds by instantiating a new FaceServiceClient class based
        // on the FaceAPI key supplied in the FaceRecogKey TextBox.
        private void MSCognitiveServicesLogin_Click(object sender, RoutedEventArgs e)
        {
            faceServiceClient = new FaceServiceClient(FaceRecogKey.Text, "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
            if (faceServiceClient != null)
            {
                FaceRecogKey.Text = "";
                MSCognitiveServicesLogin.IsEnabled = false;
            }
        }

        // DavidLim :
        // FaceMatch_Click() is the handler for the Click event of the FaceMatch button.
        private void FaceMatch_Click(object sender, RoutedEventArgs e)
        {
            if (faces == null)
            {
                MessageBox.Show("Error no faces on Left Hand Image.");
                return;
            }

            if (faces2 == null)
            {
                MessageBox.Show("Error no faces on Right Hand Image.");
                return;
            }

            if (faces.Length > 1)
            {
                MessageBox.Show("Left Hand Image contains more than 1 image.\r\nMulti-Face Images must be on the Right.");
                return;
            }

            // DavidLim :
            // The real work is done in PerformFaceMatch().
            PerformFaceMatch();
        }

        // Uploads the image file and calls Detect Faces.

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };

            // Call the Face API.
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    // The await keyword is used so that DetectAsync() will not return until it completes.
                    // The await keyword effectively makes DetectAsync() synchronous.
                    Face[] faces = await faceServiceClient.DetectAsync(imageFileStream, returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: faceAttributes);
                    return faces;
                }
            }
            // Catch and display Face API errors.
            catch (FaceAPIException f)
            {
                MessageBox.Show(f.ErrorMessage, f.ErrorCode);
                return new Face[0];
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return new Face[0];
            }
        }

        // Returns a string that describes the given face.

        private string FaceDescription(Face face)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Face: ");

            // Add the gender, age, and smile.
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            // Add the emotions. Display all emotions over 10%.
            sb.Append("Emotion: ");
            EmotionScores emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            // Add glasses.
            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            // Display all hair color attributes over 10%.
            HairColor[] hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
        }

        // DavidLim :
        // Starting point for Face Matching operations.
        void PerformFaceMatch()
        {
            MatchResult.Text = "";

            // If the number of faces detected in the right hand image
            // is one, we do 1 to 1 matching.
            if (faces2.Length == 1)
            {
                Perform1To1FaceMatch();
            }
            else
            {
                // Else we do 1 to N matching.
                Perform1ToNFaceMatch();
            }
        }

        // DavidLim :
        // 1 to 1 Matching uses the VerifyAsync() method to perform 1 to 1 Matching.
        async void Perform1To1FaceMatch()
        {
            // The await keyword is used so that VerifyAsync() will not return until it completes.
            // The await keyword effectively makes VerifyAsync() synchronous.
            VerifyResult result = await faceServiceClient.VerifyAsync(faces[0].FaceId, faces2[0].FaceId);

            string msg = string.Format("Identical Images : {0}.\r\nConfidence : {1}.", result.IsIdentical, result.Confidence);
            MatchResult.Text = msg;
        }

        // DavidLim :
        // We use the same VerifyAsync() method to perform 1 to N matching.
        // Our simple approach iterates through all the elements of the faces2 array
        // and matches each with faces[0] (faces has only one element).
        async void Perform1ToNFaceMatch()
        {
            double matchThreshold;

            try
            {
                matchThreshold = Convert.ToDouble(OneToNMatchingThreshold.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Match Threshold Value Invalid.");
                return;
            }

            try
            {
                // Define an array of VerifyResult objects.
                VerifyResult[] result = new VerifyResult[faces2.Length];
                int i;

                // Match faces[0] against all elements of faces2.
                // Collect each result in the result array.
                for (i = 0; i < faces2.Length; i++)
                {
                    result[i] = await faceServiceClient.VerifyAsync(faces[0].FaceId, faces2[i].FaceId);
                }

                // Obtain the VerifyResult with the highest Confidence score.
                int iMatchIndex = 0;
                VerifyResult mainResult = result[0];

                for (i = 0; i < result.Length; i++)
                {
                    if (result[i].Confidence > mainResult.Confidence)
                    {
                        mainResult = result[i];
                        iMatchIndex = i;
                    }
                }

                // If a Confidence score that equals or surpasses matchThreshold is found,
                // we draw a green rectangle around the detected face and also display 
                // the result.
                if (mainResult.Confidence >= matchThreshold)
                {
                    DrawRectOnMatchedFace(FacePhoto2, resizeFactor2, faces2[iMatchIndex], Brushes.Green, 4);
                    string msg = string.Format("Identical Images : {0}.\r\nConfidence : {1}.", mainResult.IsIdentical, mainResult.Confidence);
                    MatchResult.Text = msg;
                }
                else
                {
                    MatchResult.Text = "Unable to find any matching face.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // DavidLim :
        // DrawRectOnMatchedFace() is a generic method.
        // It will draw a recangle on a face image on a specified Image control.
        // The code is adapted from the original tutorial code for BrowseButton_Click().
        void DrawRectOnMatchedFace(Image imgControl, double currentResizeFactor, Face detectedface, SolidColorBrush brushColor, double thickness)
        {
            ImageSource imgSrc = imgControl.Source.Clone();
            // Note that the bitmap retrieved from imgControl is exactly
            // the one that is currently displayed. Any red boxes drawn
            // around faces are part of the image itself.
            BitmapSource bitmapSource = (BitmapSource)imgSrc;

            // Prepare to draw rectangles around the faces.
            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();
            drawingContext.DrawImage(bitmapSource,
                new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
            double dpi = bitmapSource.DpiX;

            // Draw a rectangle on the face.
            // Note that the face's rectangular dimensions is relative to
            // the original image file.
            // When we draw the rectangle on the screen, the rectangle dimensions
            // must be adjusted according to currentResizeFactor. 
            drawingContext.DrawRectangle(
                Brushes.Transparent,
                new Pen(brushColor, thickness),
                new Rect(
                    detectedface.FaceRectangle.Left * currentResizeFactor,
                    detectedface.FaceRectangle.Top * currentResizeFactor,
                    detectedface.FaceRectangle.Width * currentResizeFactor,
                    detectedface.FaceRectangle.Height * currentResizeFactor
                    )
            );


            drawingContext.Close();

            // Display the image with the rectangle around the face.
            RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                (int)(bitmapSource.PixelWidth),
                (int)(bitmapSource.PixelHeight),
                96,
                96,
                PixelFormats.Pbgra32);

            faceWithRectBitmap.Render(visual);

            imgControl.Source = faceWithRectBitmap;
        }

    }
}
