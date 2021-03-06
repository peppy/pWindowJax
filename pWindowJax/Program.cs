﻿using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using Gma.UserActivityMonitor;
using System.Diagnostics;
using System.Reflection;

namespace pWindowJax
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new MainController());
        }
    }

    class MainController : Form
    {
        
        public MainController()
        {
            HookManager.KeyDown += keyDown;
            HookManager.KeyUp += keyUp;

            cm = new ContextMenu();
            cm.MenuItems.Add("Quit", delegate { Application.Exit(); });
            
            ni = new NotifyIcon();
            ni.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("pWindowJax.poop.ico"));
            ni.Text = "pWindowJax";
            ni.Visible = true;

            ni.ContextMenu = cm;

            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams pm = base.CreateParams;
                pm.ExStyle |= 0x80;
                return pm;
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
            /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
            /// blocking its execution while other threads process the request.</summary>
            /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
            SynchronousWindowPosition = 0x4000,
            /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
            /// <remarks>SWP_DEFERERASE</remarks>
            DeferErase = 0x2000,
            /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
            /// <remarks>SWP_DRAWFRAME</remarks>
            DrawFrame = 0x0020,
            /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
            /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
            /// is sent only when the window's size is being changed.</summary>
            /// <remarks>SWP_FRAMECHANGED</remarks>
            FrameChanged = 0x0020,
            /// <summary>Hides the window.</summary>
            /// <remarks>SWP_HIDEWINDOW</remarks>
            HideWindow = 0x0080,
            /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
            /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
            /// parameter).</summary>
            /// <remarks>SWP_NOACTIVATE</remarks>
            DoNotActivate = 0x0010,
            /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
            /// contents of the client area are saved and copied back into the client area after the window is sized or 
            /// repositioned.</summary>
            /// <remarks>SWP_NOCOPYBITS</remarks>
            DoNotCopyBits = 0x0100,
            /// <summary>Retains the current position (ignores X and Y parameters).</summary>
            /// <remarks>SWP_NOMOVE</remarks>
            IgnoreMove = 0x0002,
            /// <summary>Does not change the owner window's position in the Z order.</summary>
            /// <remarks>SWP_NOOWNERZORDER</remarks>
            DoNotChangeOwnerZOrder = 0x0200,
            /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
            /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
            /// window uncovered as a result of the window being moved. When this flag is set, the application must 
            /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
            /// <remarks>SWP_NOREDRAW</remarks>
            DoNotRedraw = 0x0008,
            /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
            /// <remarks>SWP_NOREPOSITION</remarks>
            DoNotReposition = 0x0200,
            /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
            /// <remarks>SWP_NOSENDCHANGING</remarks>
            DoNotSendChangingEvent = 0x0400,
            /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
            /// <remarks>SWP_NOSIZE</remarks>
            IgnoreResize = 0x0001,
            /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
            /// <remarks>SWP_NOZORDER</remarks>
            IgnoreZOrder = 0x0004,
            /// <summary>Displays the window.</summary>
            /// <remarks>SWP_SHOWWINDOW</remarks>
            ShowWindow = 0x0040,
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetWindowModuleFileName(IntPtr hwnd, StringBuilder lpszFileName, uint cchFileNameMax);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler)
                : this()
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }

        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        void keyUp(object sender, KeyEventArgs e)
        {
            ctrlPressed &= e.KeyValue != 162;
            altPressed &= e.KeyValue != 164;
            winPressed &= e.KeyValue != 91;
            shiftPressed &= e.KeyValue != 0xA0;

            if (!ctrlPressed)
                isOperating = false;
            else
                isOperationResizing = false;
        }

        bool ctrlPressed;
        bool altPressed;
        bool winPressed;
        bool shiftPressed;

        void keyDown(object sender, KeyEventArgs e)
        {
            ctrlPressed |= e.KeyValue == 162;
            altPressed |= e.KeyValue == 164;
            winPressed |= e.KeyValue == 91;
            shiftPressed |= e.KeyValue == 0xA0;

            if (ctrlPressed && winPressed)
                startOp(false);
            if ((altPressed && winPressed) || (ctrlPressed && winPressed && shiftPressed))
                startOp(true);
        }

        /// <summary>
        /// Position of the window when move/resize operation began.
        /// </summary>
        Point initialPosition;

        /// <summary>
        /// Size of the window when move/resize operation began.
        /// </summary>
        RECT windowSize;

        bool isOperating;
        bool isOperationResizing;
        private NotifyIcon ni;
        private ContextMenu cm;

        void startOp(bool resize)
        {
            if (isOperating && isOperationResizing == resize)
                return; //an operation is in progress and is the same type as the one requested.

            //Make sure the window underneath the cursor is foregrounded.
            POINT p;
            if (GetCursorPos(out p))
            {
                IntPtr hWnd = WindowFromPoint(p);
                IntPtr hWndSuccess = IntPtr.Zero;

                while (hWnd != IntPtr.Zero)
                {
                    hWndSuccess = hWnd;
                    hWnd = GetParent(hWnd);
                }

                if (hWndSuccess != IntPtr.Zero && hWndSuccess != GetForegroundWindow())
                {
                    {
                        //first set the main form window as active...
                        uint cursorWindowThreadId = GetWindowThreadProcessId(Handle, IntPtr.Zero);
                        uint foregroundWindowThreadId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);

                        if (foregroundWindowThreadId != cursorWindowThreadId)
                            AttachThreadInput(foregroundWindowThreadId, cursorWindowThreadId, true);

                        SetForegroundWindow(Handle);

                        if (foregroundWindowThreadId != cursorWindowThreadId)
                            AttachThreadInput(foregroundWindowThreadId, cursorWindowThreadId, false);
                    }

                    {
                        //then switch to the target window.
                        uint cursorWindowThreadId = GetWindowThreadProcessId(hWndSuccess, IntPtr.Zero);
                        uint foregroundWindowThreadId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);

                        if (foregroundWindowThreadId != cursorWindowThreadId)
                            AttachThreadInput(foregroundWindowThreadId, cursorWindowThreadId, true);

                        SetForegroundWindow(hWndSuccess);

                        if (foregroundWindowThreadId != cursorWindowThreadId)
                            AttachThreadInput(foregroundWindowThreadId, cursorWindowThreadId, false);
                    }
                }
            }

            isOperationResizing = resize;

            IntPtr window = GetForegroundWindow();
            WINDOWINFO info = new WINDOWINFO();
            GetWindowInfo(window, ref info);

            initialPosition = Cursor.Position;
            windowSize = info.rcWindow;

            lock (this)
            {
                if (isOperating)
                    return; //an operation has just switch modes from move <-> resize, but is already in progress.

                isOperating = true;
            }

            new Thread(t =>
            {
                Point lastCursorPosition = Point.Empty;

                while (isOperating)
                {
                    if (Cursor.Position == lastCursorPosition)
                    {
                        Thread.Sleep(16);
                        continue;
                    }

                    lastCursorPosition = Cursor.Position;

                    if (isOperationResizing)
                    {
                        SetWindowPos(window, IntPtr.Zero, windowSize.X, windowSize.Y, windowSize.Width + (lastCursorPosition.X - initialPosition.X), windowSize.Height + (lastCursorPosition.Y - initialPosition.Y), 0);
                    }
                    else
                    {
                        SetWindowPos(window, IntPtr.Zero, windowSize.X + (lastCursorPosition.X - initialPosition.X), windowSize.Y + (lastCursorPosition.Y - initialPosition.Y), windowSize.Width, windowSize.Height, 0);
                    }
                }
            }).Start();
        }

        private void linkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://twitter.com/peppyhax");
        }
    }
}


