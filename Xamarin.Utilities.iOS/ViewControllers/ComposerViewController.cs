using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Utilities.Views;

namespace Xamarin.Utilities.ViewControllers
{
    public class ComposerViewController : UIViewController
    {
        protected UIBarButtonItem SendItem;
        UIViewController _previousController;
        public Action<string> ReturnAction;
        protected readonly UITextView TextView;
        protected UIView ScrollingToolbarView;
        private UIImage _normalButtonImage;
        private UIImage _pressedButtonImage;

        public bool EnableSendButton
        {
            get { return SendItem.Enabled; }
            set { SendItem.Enabled = value; }
        }

        public ComposerViewController()
            : base(null, null)
        {
            Title = "New Comment";
            EdgesForExtendedLayout = UIRectEdge.None;

            TextView = new UITextView(ComputeComposerSize(RectangleF.Empty));
            TextView.Font = UIFont.SystemFontOfSize(18);
            TextView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

            // Work around an Apple bug in the UITextView that crashes
            if (MonoTouch.ObjCRuntime.Runtime.Arch == MonoTouch.ObjCRuntime.Arch.SIMULATOR)
                TextView.AutocorrectionType = UITextAutocorrectionType.No;

            View.AddSubview(TextView);

            _normalButtonImage = ImageFromColor(UIColor.White);
            _pressedButtonImage = ImageFromColor(UIColor.FromWhiteAlpha(0.0f, 0.4f));
        }

        private UIImage ImageFromColor(UIColor color)
        {
            UIGraphics.BeginImageContext(new SizeF(1, 1));
            var context = UIGraphics.GetCurrentContext();
            context.SetFillColor(color.CGColor);
            context.FillRect(new RectangleF(0, 0, 1, 1));
            var image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return image;
        }

        public static UIButton CreateAccessoryButton(UIImage image, Action action)
        {
            var btn = CreateAccessoryButton(string.Empty, action);
            //            btn.AutosizesSubviews = true;
            btn.SetImage(image, UIControlState.Normal);
            btn.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            btn.ImageEdgeInsets = new UIEdgeInsets(6, 6, 6, 6);

            //            var imageView = new UIImageView(image);
            //            imageView.Frame = new RectangleF(4, 4, 24, 24);
            //            imageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            //            btn.Add(imageView);
            return btn;
        }

        public static UIButton CreateAccessoryButton(string title, Action action)
        {
            var fontSize = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone ? 22 : 28f;

            var btn = new UIButton(UIButtonType.System);
            btn.Frame = new RectangleF(0, 0, 32, 32);
            btn.SetTitle(title, UIControlState.Normal);
            btn.BackgroundColor = UIColor.White;
            btn.Font = UIFont.SystemFontOfSize(fontSize);
            btn.Layer.CornerRadius = 7f;
            btn.Layer.MasksToBounds = true;
            btn.AdjustsImageWhenHighlighted = false;
            btn.TouchUpInside += (object sender, System.EventArgs e) => action();
            return btn;
        }

        private float CalculateHeight(UIInterfaceOrientation orientation)
        {
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
                return 44;

            // If  pad
            if (orientation == UIInterfaceOrientation.Portrait || orientation == UIInterfaceOrientation.PortraitUpsideDown)
                return 64;
            return 88f;
        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            base.WillRotate(toInterfaceOrientation, duration);

            if (TextView.InputAccessoryView != null)
            {
                UIView.Animate(duration, 0, UIViewAnimationOptions.BeginFromCurrentState, () =>
                {
                    var frame = TextView.InputAccessoryView.Frame;
                    frame.Height = CalculateHeight(toInterfaceOrientation);
                    TextView.InputAccessoryView.Frame = frame;
                }, null);
            }
        }

        public void SetAccesoryButtons(IEnumerable<UIButton> buttons)
        {
            foreach (var button in buttons)
            {
                button.SetBackgroundImage(_normalButtonImage, UIControlState.Normal);
                button.SetBackgroundImage(_pressedButtonImage, UIControlState.Highlighted);
            }

            var height = CalculateHeight(UIApplication.SharedApplication.StatusBarOrientation);
            ScrollingToolbarView = new ScrollingToolbarView(new RectangleF(0, 0, View.Bounds.Width, height), buttons);
            ScrollingToolbarView.BackgroundColor = UIColor.FromWhiteAlpha(0.7f, 1.0f);
            TextView.InputAccessoryView = ScrollingToolbarView;
        }

        public string Text
        {
            get { return TextView.Text; }
            set { TextView.Text = value; }
        }

        public void CloseComposer()
        {
            SendItem.Enabled = true;
            _previousController.DismissViewController(true, null);
        }

        public void Save()
        {
            SendItem.Enabled = false;
            TextView.ResignFirstResponder();

            try
            {
                if (ReturnAction != null)
                    ReturnAction(Text);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message + " - " + e.StackTrace);
            }
        }

        void KeyboardWillShow(NSNotification notification)
        {
            var nsValue = notification.UserInfo.ObjectForKey(UIKeyboard.BoundsUserInfoKey) as NSValue;
            if (nsValue == null) return;
            var kbdBounds = nsValue.RectangleFValue;
            UIView.Animate(1.0f, 0, UIViewAnimationOptions.CurveEaseIn, () => TextView.Frame = ComputeComposerSize(kbdBounds), null);
        }

        void KeyboardWillHide(NSNotification notification)
        {
            TextView.Frame = ComputeComposerSize(new RectangleF(0, 0, 0, 0));
        }

        RectangleF ComputeComposerSize(RectangleF kbdBounds)
        {
            var view = View.Bounds;
            return new RectangleF(0, 0, view.Width, view.Height - kbdBounds.Height);
        }

        [Obsolete]
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("UIKeyboardWillShowNotification"), KeyboardWillShow);
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("UIKeyboardWillHideNotification"), KeyboardWillHide);
            TextView.BecomeFirstResponder();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        }

        public void NewComment(UIViewController parent, Action<string> action)
        {
            Title = Title;
            ReturnAction = action;
            _previousController = parent;
            TextView.BecomeFirstResponder();
            var nav = new UINavigationController(this);
            parent.PresentViewController(nav, true, null);
        }
    }
}
