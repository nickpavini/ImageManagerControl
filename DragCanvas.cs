using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Instrumentation;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CustomControls
{
	/*
	 * canvas where you can drag the elements within
	 * maybe later we will add the ability to individually decide indiviually which elements can be dragged
	 */
	public class DragCanvas : Canvas
	{

		private UIElement elementBeingDragged; // current element being dragged	
		private Point pntMouseLeftDown;
		private double origHorizOffset, origVertOffset; // original horizontal and vertical values
		private bool modifyLeftOffset, modifyTopOffset; // flag to know if we should modify left, right, top, or bottom
		private bool isDragInProgress;

		// element that is being dragged
		public UIElement ElementBeingDragged
		{
			get
			{
				return this.elementBeingDragged;
			}
			protected set
			{
				if (this.elementBeingDragged != null)
					this.elementBeingDragged.ReleaseMouseCapture();
				if (value != null)
				{
					this.elementBeingDragged = value;
					this.elementBeingDragged.CaptureMouse();
				}
			}
		}

		public DragCanvas() { } //default constructor

		#region Public Member Functions

		public void BringToFront(UIElement element) { this.UpdateZOrder(element, true); } // put element on top of all elements
		public void SendToBack(UIElement element) { this.UpdateZOrder(element, false); } // put element behind all elements

		// get a direct child of the canvas in case of layered elements
		public UIElement FindCanvasChild(DependencyObject depObj)
		{
			while (depObj != null)
			{
				// If the current object is a UIElement which is a child of the
				// Canvas, exit the loop and return it.
				UIElement elem = depObj as UIElement;
				if (elem != null && base.Children.Contains(elem))
					break;

				// make sure this is a rederable object
				if (depObj is Visual || depObj is Visual3D)
					depObj = VisualTreeHelper.GetParent(depObj);
				else
					depObj = LogicalTreeHelper.GetParent(depObj);
			}
			return depObj as UIElement;
		}

        #endregion

        #region Overrides

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			this.pntMouseLeftDown = e.GetPosition(this); // store click location

			// Walk up the visual tree from the element that was clicked, 
			// looking for an element that is a direct child of the Canvas.
			this.ElementBeingDragged = this.FindCanvasChild(e.Source as DependencyObject);
			if (this.ElementBeingDragged == null)
				return;

			// Get the element's offsets from the four sides of the Canvas.
			double left = Canvas.GetLeft(this.ElementBeingDragged);
			double right = Canvas.GetRight(this.ElementBeingDragged);
			double top = Canvas.GetTop(this.ElementBeingDragged);
			double bottom = Canvas.GetBottom(this.ElementBeingDragged);

			// Calculate the offset deltas and determine for which sides
			// of the Canvas to adjust the offsets.
			this.origHorizOffset = ResolveOffset(left, right, out this.modifyLeftOffset);
			this.origVertOffset = ResolveOffset(top, bottom, out this.modifyTopOffset);

			// Set the Handled flag so that a control being dragged 
			// does not react to the mouse input.
			e.Handled = true;
			this.isDragInProgress = true;
		}


		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);

			// If no element is being dragged, there is nothing to do.
			if (this.ElementBeingDragged == null || !this.isDragInProgress)
				return;

			Point cursorLocation = e.GetPosition(this); // position relative to canvas
			double newHorizontalOffset, newVerticalOffset; // new offsets

			// Determine the horizontal offset (Left or Right).
			if (this.modifyLeftOffset)
				newHorizontalOffset = this.origHorizOffset + (cursorLocation.X - this.pntMouseLeftDown.X);
			else
				newHorizontalOffset = this.origHorizOffset - (cursorLocation.X - this.pntMouseLeftDown.X);

			// Determine the vertical offset (Top or Bottom).
			if (this.modifyTopOffset)
				newVerticalOffset = this.origVertOffset + (cursorLocation.Y - this.pntMouseLeftDown.Y);
			else
				newVerticalOffset = this.origVertOffset - (cursorLocation.Y - this.pntMouseLeftDown.Y);


			VerifyNewElementLocation(ref newHorizontalOffset, ref newVerticalOffset); // verify elements new dragging location

			// set new left or right offset
			if (this.modifyLeftOffset)
				Canvas.SetLeft(this.ElementBeingDragged, newHorizontalOffset);
			else
				Canvas.SetRight(this.ElementBeingDragged, newHorizontalOffset);

			// set new top or bottom offset
			if (this.modifyTopOffset)
				Canvas.SetTop(this.ElementBeingDragged, newVerticalOffset);
			else
				Canvas.SetBottom(this.ElementBeingDragged, newVerticalOffset);
		}

		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseUp(e);

			// no element being dragged now
			this.ElementBeingDragged = null;
			this.isDragInProgress = false; // reset
		}

        #endregion

        #region Private Member Functions

		private void VerifyNewElementLocation(ref double newHorizontalOffset, ref double newVerticalOffset)
		{
			// Get the bounding rect of the drag element.
			Rect elemRect = this.CalculateDragElementRect(newHorizontalOffset, newVerticalOffset);

			bool leftAlign = elemRect.Left < 0;
			bool rightAlign = elemRect.Right > this.ActualWidth;

			if (leftAlign)
				newHorizontalOffset = modifyLeftOffset ? 0 : this.ActualWidth - elemRect.Width;
			else if (rightAlign)
				newHorizontalOffset = modifyLeftOffset ? this.ActualWidth - elemRect.Width : 0;

			bool topAlign = elemRect.Top < 0;
			bool bottomAlign = elemRect.Bottom > this.ActualHeight;

			if (topAlign)
				newVerticalOffset = modifyTopOffset ? 0 : this.ActualHeight - elemRect.Height;
			else if (bottomAlign)
				newVerticalOffset = modifyTopOffset ? this.ActualHeight - elemRect.Height : 0;
		}

        // Returns a Rect which describes the bounds of the element being dragged at its new location.
        private Rect CalculateDragElementRect(double newLeftOffset, double newTopOffset)
		{
			if (this.ElementBeingDragged == null)
				throw new InvalidOperationException("ElementBeingDragged is null.");

			Size elemSize = this.ElementBeingDragged.RenderSize;

			double x, y;

			if (this.modifyLeftOffset)
				x = newLeftOffset;
			else
				x = this.ActualWidth - newLeftOffset - elemSize.Width;

			if (this.modifyTopOffset)
				y = newTopOffset;
			else
				y = this.ActualHeight - newTopOffset - elemSize.Height;

			Point elemLoc = new Point(x, y);

			return new Rect(elemLoc, elemSize);
		}

		// works with right, left, top, or bottom to help determine if the sides have their margins set.. default to side1 if available
		private static double ResolveOffset(double side1, double side2, out bool useSide1)
		{
			useSide1 = true; // flag for choosing side
			double result;

			if (Double.IsNaN(side1))
			{
				if (Double.IsNaN(side2))
					result = 0; // both values are nan, return 0 for side1
				else
				{
					result = side2;
					useSide1 = false;
				}
			}
			else
				result = side1;

			return result;
		}

		// either move element to the from or back based on the boolean value
		private void UpdateZOrder(UIElement element, bool bringToFront)
		{

			if (element == null)
				throw new ArgumentNullException("element", "UIElement has value of null.");

			if (!base.Children.Contains(element))
				throw new ArgumentException("Must be a child element of the Canvas.", "element");


			// Determine the Z-Index for the target UIElement.
			int elementNewZIndex = -1;
			if (bringToFront)
			{
				foreach (UIElement elem in base.Children)
					if (elem.Visibility != Visibility.Collapsed)
						++elementNewZIndex;
			}
			else
				elementNewZIndex = 0;

			int offset = (elementNewZIndex == 0) ? +1 : -1; // push other elements up one or down one
			int elementCurrentZIndex = Canvas.GetZIndex(element); // current z

			// Update the Z-Index of every UIElement in the Canvas.
			foreach (UIElement childElement in base.Children)
			{
				if (childElement == element) // if this is the element to move from or back
					Canvas.SetZIndex(element, elementNewZIndex);
				else
				{
					int zIndex = Canvas.GetZIndex(childElement); // z index of current element

					// Only modify the z-index of an element if it is in between the target element's old and new z-index
					// AKA. Moving the desired element has to jump over this one
					if (bringToFront && elementCurrentZIndex < zIndex || !bringToFront && zIndex < elementCurrentZIndex)
						Canvas.SetZIndex(childElement, zIndex + offset);
				}
			}

		}

        #endregion
    }
}
 