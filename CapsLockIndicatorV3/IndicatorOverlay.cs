/*
 * Created 09.07.2017 20:22
 * 
 * Copyright (c) Jonas Kohl <https://jonaskohl.de/>
 */
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapsLockIndicatorV3
{
    /// <summary>
    /// A form that indicates if a keystate has changed.
    /// </summary>
    public partial class IndicatorOverlay : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern int SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        static extern int GetWindowLongInt(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        const uint WS_EX_LAYERED = 0x80000;
        const uint WS_EX_TOPMOST = 0x00000008;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_EX_TRANSPARENT = 0x00000020;
        const uint LWA_ALPHA = 0x2;
        const uint LWA_COLORKEY = 0x1;
        const int GWL_EXSTYLE = -20;
        const int WM_NCHITTEST = 0x84;
        const int HTTRANSPARENT = -1;

        private Size originalSize;

        private IndicatorDisplayPosition pos = IndicatorDisplayPosition.BottomRight;

        const int WINDOW_MARGIN = 16;
        private double lastOpacity = 1;
        double opacity_timer_value = 2.0;

        Color BorderColour = Color.FromArgb(
            0xFF,
            0x34,
            0x4D,
            0xB4
        );
        int BorderSize = 4;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fadeTimer.Stop();
            windowCloseTimer.Stop();
            positionUpdateTimer.Stop();
            base.OnFormClosing(e);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)(WS_EX_TOPMOST | WS_EX_TOOLWINDOW);
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)HTTRANSPARENT;
            else
                base.WndProc(ref m);
        }

        protected override void OnShown(EventArgs e)
        {
            UpdatePosition();

            base.OnShown(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        void UpdatePosition()
        {
            Rectangle workingArea = Screen.GetWorkingArea(Cursor.Position);

            Size = new Size(Width, Height);

            switch (pos)
            {
                case IndicatorDisplayPosition.TopLeft:
                    Left = workingArea.X + WINDOW_MARGIN;
                    Top = workingArea.Y + WINDOW_MARGIN;
                    break;
                case IndicatorDisplayPosition.TopCenter:
                    Left = workingArea.X + (workingArea.Width / 2 - Width / 2);
                    Top = workingArea.Y + WINDOW_MARGIN;
                    break;
                case IndicatorDisplayPosition.TopRight:
                    Left = workingArea.X + workingArea.Left + (workingArea.Width - Width - WINDOW_MARGIN - workingArea.Left);
                    Top = workingArea.Y + WINDOW_MARGIN;
                    break;
                case IndicatorDisplayPosition.MiddleLeft:
                    Left = workingArea.X + WINDOW_MARGIN;
                    Top = workingArea.Y + (workingArea.Height / 2 - Height / 2);
                    break;
                case IndicatorDisplayPosition.MiddleCenter:
                    Left = workingArea.X + (workingArea.Width / 2 - Width / 2);
                    Top = workingArea.Y + (workingArea.Height / 2 - Height / 2);
                    break;
                case IndicatorDisplayPosition.MiddleRight:
                    Left = workingArea.X + workingArea.Left + (workingArea.Width - Width - WINDOW_MARGIN - workingArea.Left);
                    Top = workingArea.Y + (workingArea.Height / 2 - Height / 2);
                    break;
                case IndicatorDisplayPosition.BottomLeft:
                    Left = workingArea.X + WINDOW_MARGIN;
                    Top = workingArea.Y + workingArea.Top + (workingArea.Height - Height - WINDOW_MARGIN - workingArea.Top);
                    break;
                case IndicatorDisplayPosition.BottomCenter:
                    Left = workingArea.X + (workingArea.Width / 2 - Width / 2);
                    Top = workingArea.Y + workingArea.Top + (workingArea.Height - Height - WINDOW_MARGIN - workingArea.Top);
                    break;
                case IndicatorDisplayPosition.BottomRight:
                    Left = workingArea.X + workingArea.Left + (workingArea.Width - Width - WINDOW_MARGIN - workingArea.Left);
                    Top = workingArea.Y + workingArea.Top + (workingArea.Height - Height - WINDOW_MARGIN - workingArea.Top);
                    break;
                default:
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var factor = DPIHelper.GetScalingFactorPercent(Handle);

            if (BorderSize > 0)
                e.Graphics.DrawRectangle(new Pen(BorderColour, (int)(BorderSize * factor)), e.ClipRectangle);

            using (var sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var b = new SolidBrush(contentLabel.ForeColor))
                e.Graphics.DrawString(contentLabel.Text, contentLabel.Font, b, ClientRectangle, sf);
        }

        private int ClickThroughWindow(double opacity = 1d)
        {
            if (IsDisposed)
                return -1;

            uint windowLong = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong32(Handle, GWL_EXSTYLE, windowLong | WS_EX_LAYERED);
            SetLayeredWindowAttributes(Handle, 0, (byte)(opacity * 255), LWA_ALPHA);

            SetWindowStyles();

            return 0;
        }

        private void SetWindowStyles()
        {
            var style = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
        }

        public IndicatorOverlay(string content)
        {
            InitializeComponent();
        }

        public IndicatorOverlay(string content, int timeoutInMs, IndicatorDisplayPosition position)
        {
            pos = position;

            if (IsDisposed)
                return;

            InitializeComponent();

            if (IsDisposed)
                return;

            contentLabel.Text = content;
            originalSize = Size;

            if (timeoutInMs < 1)
            {
                windowCloseTimer.Enabled = false;
                fadeTimer.Enabled = false;
            }
            else
            {
                windowCloseTimer.Interval = timeoutInMs;
                fadeTimer.Interval = (int)Math.Floor((decimal)(timeoutInMs / 20));
            }
            ClickThroughWindow();
        }

        public IndicatorOverlay(string content, int timeoutInMs, Color bgColour, Color fgColour, Color bdColour, int bdSize, Font font, IndicatorDisplayPosition position, int indOpacity, bool alwaysShow)
        {
            var ret = 0;

            pos = position;

            InitializeComponent();


            contentLabel.Text = content;
            Font = font;
            originalSize = Size;

            var op = indOpacity / 100d;
            lastOpacity = op;
            ret |= SetOpacity(op);
            if (timeoutInMs < 0 || alwaysShow)
            {
                windowCloseTimer.Enabled = false;
                fadeTimer.Enabled = false;
            }
            else
            {
                windowCloseTimer.Interval = timeoutInMs;
                fadeTimer.Interval = (int)Math.Floor((decimal)(timeoutInMs / 20));
            }
            BackColor = bgColour;
            ForeColor = fgColour;
            BorderColour = bdColour;
            BorderSize = bdSize;
            ret |= ClickThroughWindow(op);
            if (ret != -1)
                Show();
        }

        private int SetOpacity(double op)
        {
            if (IsDisposed)
                return -1;

            byte opb = 0xFF;

            try
            {
                opb = (byte)Math.Min(255, Math.Max(op * 0xFF, 0));
            }
            catch (OverflowException) { }
            SetLayeredWindowAttributes(Handle, 0, opb, LWA_ALPHA);
            return 0;
        }

        public void UpdateIndicator(string content, IndicatorDisplayPosition position)
        {
            pos = position;
            Opacity = 1;
            contentLabel.Text = content;
            opacity_timer_value = 2.0;
            windowCloseTimer.Stop();
            windowCloseTimer.Start();
            fadeTimer.Stop();
            fadeTimer.Start();
            Show();
            UpdatePosition();
            SetWindowStyles();
        }

        public void UpdateIndicator(string content, int timeoutInMs, IndicatorDisplayPosition position)
        {
            pos = position;
            Opacity = 1;
            contentLabel.Text = content;
            opacity_timer_value = 2.0;
            if (timeoutInMs < 0)
            {
                windowCloseTimer.Enabled = false;
                fadeTimer.Enabled = false;
            }
            else
            {
                windowCloseTimer.Stop();
                windowCloseTimer.Interval = timeoutInMs;
                windowCloseTimer.Start();
                fadeTimer.Stop();
                fadeTimer.Interval = (int)Math.Floor((decimal)(timeoutInMs / 20));
                fadeTimer.Start();
            }
            Show();
            UpdatePosition();
            SetWindowStyles();
        }

        public void UpdateIndicator(string content, int timeoutInMs, Color bgColour, Color fgColour, Color bdColour, int bdSize, Font font, IndicatorDisplayPosition position, int indOpacity, bool alwaysShow)
        {
            pos = position;
            var op = indOpacity / 100d;
            lastOpacity = op;
            SetOpacity(op);
            contentLabel.Text = content;
            Font = font;
            opacity_timer_value = 2.0;
            if (timeoutInMs < 0 || alwaysShow)
            {
                windowCloseTimer.Enabled = false;
                fadeTimer.Enabled = false;
            }
            else
            {
                windowCloseTimer.Stop();
                windowCloseTimer.Interval = timeoutInMs;
                windowCloseTimer.Start();
                fadeTimer.Stop();
                fadeTimer.Interval = (int)Math.Floor((decimal)(timeoutInMs / 20));
                fadeTimer.Start();
            }
            BackColor = bgColour;
            ForeColor = fgColour;
            BorderColour = bdColour;
            BorderSize = bdSize;
            Show();
            Invalidate();
            UpdatePosition();
            SetWindowStyles();
        }

        void WindowCloseTimerTick(object sender, EventArgs e)
        {
            Hide();
        }

        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            if (opacity_timer_value > 0)
            {
                opacity_timer_value -= 0.1;
                opacity_timer_value = Math.Max(opacity_timer_value, 0);
                if (opacity_timer_value <= 1.0)
                    SetOpacity(opacity_timer_value * lastOpacity);
            }
        }

        private void positionUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdatePosition();
            SetWindowStyles();
        }
    }
}
