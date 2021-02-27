using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomControls
{
    /// <summary>
    /// Interaction logic for ImageManagerControl.xaml
    /// </summary>
    public partial class ImageManagerControl : UserControl
    {
        // private member variables
        private bool imgChosen = false;
        private string pathToImage = null; // some images may be able to have paths, and others maybe not, we handle both

        // Dynamically build the control
        private System.Windows.Controls.Image imgSourceImage = null;
        private SelectionCanvas selectionArea = null;
        private DragCanvas selectedArea = null;
        private bool areaSelected = false;

        #region Contructors

        // by default do not load a selection canvas, this is basically the user didnt choose
        public ImageManagerControl()
        {
            InitializeComponent();
            CreateImageSource();
            imgSourceImage.Source = Bitmap2BitmapImage(Properties.Resources.grey); // set image to default grey
            GridTopLevel.Children.Add(imgSourceImage); // end of dynamic decleration
        }
    

        public ImageManagerControl(BitmapImage image)
        {
            try
            {
                InitializeComponent();
                CreateImageSource();
                SetImage(image);
                selectionArea.Children.Add(imgSourceImage);
                GridTopLevel.Children.Add(selectionArea); // end of dynamic build
                imgChosen = true;
            }
            catch
            {

            }
        }

        public ImageManagerControl(string imagePath)
        {
            try
            {
                InitializeComponent();
                CreateImageSource();
                pathToImage = imagePath;
                SetImage(new BitmapImage(new Uri(imagePath)));
                selectionArea.Children.Add(imgSourceImage);
                GridTopLevel.Children.Add(selectionArea); // end of dynamic build
                imgChosen = true;
            }
            catch
            {

            }
        }

        #endregion

        #region Public Member Functions

        public BitmapImage GetImage() { return (BitmapImage)imgSourceImage.Source; } // using bitmapimages only for now because the image may only be in memory

        public string GetImagePath () { return pathToImage; } // logic will return null when there is no associated path

        // eventually will have my own image source that has ability to convert between all types
        public void SetImage(BitmapImage image) 
        {
            ResetControl(); // reset control when new image is chosen

            // this is to make sure image and selection canvas resize as chosen
            imgSourceImage.Width = image.Width;
            imgSourceImage.Height = image.Height;
            imgSourceImage.Source = image;

            if (selectionArea == null) // create if needed
                CreateSelectionCanvas();

            SetSelectionCanvas();
            selectionArea.Children.Add(imgSourceImage);
            GridTopLevel.Children.Add(selectionArea);
        }

        public bool isImageChosen() { return imgChosen; }  // lets user know if the image is set programatically

        public bool isAreaSelected() { return areaSelected; } // let user know if there is a selected area

        #endregion

        #region Private Member Functions

        // basic setup to sit nice on top grid
        private void CreateImageSource()
        {
            imgSourceImage = new System.Windows.Controls.Image();
            imgSourceImage.HorizontalAlignment = HorizontalAlignment.Left;
            imgSourceImage.VerticalAlignment = VerticalAlignment.Top;
            imgSourceImage.Width = GridTopLevel.Width; // in this case I decided to let the Grid be the deciding factor for everything.. basically making it a tool where its a size I want but can be modified if you modify the control to dynamically declare the grid
            imgSourceImage.Height = GridTopLevel.Height;
            imgSourceImage.Stretch = Stretch.UniformToFill; // use this jusut becuase the default resource image of grey needs to stretch to fit
        }

        // overlaying canvas to allowing mouse interactions with the surface below
        private void CreateSelectionCanvas()
        {
            // get handle and set events routing
            selectionArea = new SelectionCanvas();
            selectionArea.AreaSelected += new RoutedEventHandler(SetSelectedArea);

            // set size and placement
            selectionArea.Width = imgSourceImage.Width; 
            selectionArea.Height = imgSourceImage.Height;
            selectionArea.HorizontalAlignment = HorizontalAlignment.Left;
            selectionArea.VerticalAlignment = VerticalAlignment.Top;
        }

        // create a canvas that can be dragged around within the underlying image
        private void CreateDragCanvas()
        {
            selectedArea = new DragCanvas();
            selectedArea.Width = imgSourceImage.Width;
            selectedArea.Height = imgSourceImage.Height;
            selectedArea.HorizontalAlignment = HorizontalAlignment.Left;
            selectedArea.VerticalAlignment = VerticalAlignment.Top;
        }

        private void SetSelectionCanvas()
        {
            selectionArea.Width = imgSourceImage.Width;
            selectionArea.Height = imgSourceImage.Height;

            /*
             *  Here I can make the canvas position itself a specific location on the grid
             */
        }

        private void SetDragCanvas()
        {
            selectedArea.Width = imgSourceImage.Width;
            selectedArea.Height = imgSourceImage.Height;

            /*
             *  Here I can make the canvas position itself a specific location on the grid
             */
        }

        // called when an area has been selected
        private void SetSelectedArea(object sender, RoutedEventArgs e)
        {
            if (selectedArea == null)
                CreateDragCanvas();

            SetDragCanvas();

            // move image and area to drag canvas and reset selection canvas
            var childrenList = selectionArea.Children.Cast<UIElement>().ToArray();
            foreach (var c in childrenList)
            {
                selectionArea.Children.Remove(c); // remove then add to new drag canvas
                selectedArea.Children.Add(c);
            }

            GridTopLevel.Children.Remove(selectionArea);
            GridTopLevel.Children.Add(selectedArea);

            areaSelected = true;
        }

        // reset tool 
        private void ResetControl()
        {
            GridTopLevel.Children.Clear(); // this will always have children... may later want to make logic here more specific

            if (selectedArea != null && selectedArea.Children.Count > 0)
                selectedArea.Children.Clear();

            if (selectionArea != null && selectionArea.Children.Count > 0)
                selectionArea.Children.Clear(); // remove image and area because re-adding the image brings it above the area

            pathToImage = null;
            imgChosen = false;
            areaSelected = false;
        }

        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        #endregion
    }
}
