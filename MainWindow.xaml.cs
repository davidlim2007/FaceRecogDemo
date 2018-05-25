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
using System.Windows.Controls;  // For the definition of common controls like Button.

namespace FaceTutorial
{
    public partial class MainWindow : Window
    {        
        // The FaceServiceClient used to perform calls to the Face API.
        private IFaceServiceClient faceServiceClient = null;

        Face[] faces;                   // The list of detected faces.
        String[] faceDescriptions;      // The list of descriptions for the detected faces.
        double resizeFactor;            // The resize factor for the displayed image.
        
        // There will be 2 images hence we must have duplicate properties
        // for the 2nd image.
        Face[] faces2;                  // The list of detected faces on FacePhoto2 image control.
        String[] faceDescriptions2;     // The list of descriptions for the detected faces FacePhoto2 image control.
        double resizeFactor2;           // The resize factor for the displayed image on the FacePhoto2 image control.

        public MainWindow()
        {
            InitializeComponent();
        }

        // Displays the image and calls Detect Faces.
        // BrowseButton_Click() is the event handler shared by both "Browse" buttons.
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
            
            // Define an array of Face objects for current detection.
            // This array will later be assigned to either face or face2.
            //
            // Also define an array of detectedFaceDescriptions for the current detection.
            // This arrat will later be assigned to either faceDescriptions or faceDescriptions2.
            //
            // Also define a double for the ResizeFactor for the current image.
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
                /*resizeFactor*/ currentResizeFactor = 96 / dpi;
                /*faceDescriptions*/ detectedFaceDescriptions = new String[/*faces*/detectedfaces.Length];

                for (int i = 0; i < /*faces*/detectedfaces.Length; ++i)
                {
                    Face face = /*faces*/detectedfaces[i];

                    // Draw a rectangle on the face.
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
                    /*faceDescriptions[i]*/ detectedFaceDescriptions[i] = FaceDescription(face);
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
        // FacePhoto_MouseMove() is an event handler shared by both "FacePhoto" 
        // and "FacePhoto2" image controls.
        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
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
            // We must also include check for faces2.
            if (/*faces*/ currentFaces == null)
                return;


            // Find the mouse position relative to the image.
            Point mouseXY = e.GetPosition(/*FacePhoto*/ imgControl);

            ImageSource imageSource = /*FacePhoto.Source*/ imgControl.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;

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
                    currentTextBlock.Text = currentFaceDescriptions[i];
                    mouseOverFace = true;
                    break;
                }
            }

            // If the mouse is not over a face rectangle.
            if (!mouseOverFace)
            {
                currentTextBlock.Text = "Place the mouse pointer over a face to see the face description.";
            }
        }

        private void MSCognitiveServicesLogin_Click(object sender, RoutedEventArgs e)
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
            faceServiceClient = new FaceServiceClient(FaceRecogKey.Text, "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
            if (faceServiceClient != null)
            {
                FaceRecogKey.Text = "";
                MSCognitiveServicesLogin.IsEnabled = false;
            }
        }

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

        void PerformFaceMatch()
        {
            MatchResult.Text = "";

            if (faces2.Length == 1)
            {
                Perform1To1FaceMatch();
            }
            else
            {
                Perform1ToNFaceMatch();
            }
        }

        async void Perform1To1FaceMatch()
        {
            VerifyResult result = await faceServiceClient.VerifyAsync(faces[0].FaceId, faces2[0].FaceId);

            string msg = string.Format("Identical Images : {0}.\r\nConfidence : {1}.", result.IsIdentical, result.Confidence);
            MatchResult.Text = msg;
        }

        async void Perform1ToNFaceMatch()
        {
            double matchThreshold;

            try
            {
                matchThreshold = Convert.ToDouble(OneToNMatchingThreshold.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Match Threshold Value Invalid.");
                return;
            }

            try
            {

                VerifyResult[] result = new VerifyResult[faces2.Length];
                int i;

                for (i = 0; i < faces2.Length; i++)
                {
                    result[i] = await faceServiceClient.VerifyAsync(faces[0].FaceId, faces2[i].FaceId);
                }

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
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        // DrawRectOnMatchedFace() will draw a recangle on a face image on a specified Image control.
        void DrawRectOnMatchedFace(Image imgControl, double currentResizeFactor,  Face detectedface, SolidColorBrush brushColor, double thickness)
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
