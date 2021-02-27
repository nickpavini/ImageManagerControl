using System;
using System.Collections.Generic;
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

namespace CustomControls
{
    /*
     * Special Canvas for allowing the user to
     * draw a box anywhere on the underlying elements
     */
    public partial class SelectionCanvas : Canvas
    {

        private Point pntMouseLeftDown;
        private Shape area = null;

        // event for clicking and dragging on the canvas
        public readonly RoutedEvent AreaSelectedEvent = EventManager.RegisterRoutedEvent( "AreaSelected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectionCanvas));

        // Provide CLR accessors for the event
        public event RoutedEventHandler AreaSelected
        {
            add { AddHandler(AreaSelectedEvent, value); }
            remove { RemoveHandler(AreaSelectedEvent, value); }
        }

        public SelectionCanvas() { } // default constructor


        #region Backend Functionality

        // implement virtual function
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e); // call the base implementation
            if (!this.IsMouseCaptured) // make canvas is recieving mouse input
            {
                pntMouseLeftDown = e.GetPosition(this);
                this.CaptureMouse(); //start recieving input
            }

        }

        // draw rectangle based on new size
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (this.IsMouseCaptured) // make sure onMouseLeftDown was invoked
            {
                Point currentPoint = e.GetPosition(this);

                // if an area doesnt exist
                if (area == null)
                {
                    area = new Rectangle();
                    area.Stroke = new SolidColorBrush(Colors.Black);
                    area.StrokeThickness = 2;
                    area.Fill = Brushes.Yellow;
                    area.Opacity = 0.20;
                    this.Children.Add(area);
                }

                if (!this.Children.Contains(area)) // just in case all children were deleted
                    this.Children.Add(area);

                // positioning and size of square area made by the user
                double width = Math.Abs(pntMouseLeftDown.X - currentPoint.X);
                double height = Math.Abs(pntMouseLeftDown.Y - currentPoint.Y);
                double left = Math.Min(pntMouseLeftDown.X, currentPoint.X);
                double top = Math.Min(pntMouseLeftDown.Y, currentPoint.Y);

                //set size and location for visual feedback
                area.Width = width;
                area.Height = height;
                Canvas.SetLeft(area, left);
                Canvas.SetTop(area, top);
            }
        }

        // finalize by rougint to provided function
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture(); // release upon letting button up
                RaiseEvent(new RoutedEventArgs(this.AreaSelectedEvent, this)); // raise event if dragged
            }
        }

        #endregion
    }
}