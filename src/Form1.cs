using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public Bitmap ConvertToFormat(System.Drawing.Image image, System.Drawing.Imaging.PixelFormat format)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            }
            return copy;
        }

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte vk, byte scan, int flags, int extrainfo);
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const byte KEYBDEVENTF_SHIFTVIRTUAL = 0x10;
        public const byte KEYBDEVENTF_SHIFTSCANCODE = 0x2A;
        public const byte KEYBDEVENTF_NUM1VIRTUAL = 0x31;
        public const byte KEYBDEVENTF_NUM1SCANCODE = 0x02;
        public const int KEYBDEVENTF_KEYDOWN = 0;
        public const int KEYBDEVENTF_KEYUP = 2;

        int imageX = 0;
        int imageY = 0;

        public delegate void ControlStringConsumer(Control control, string text);  // defines a delegate type

        public void SetText(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new ControlStringConsumer(SetText), new object[] { control, text });  // invoking itself
            }
            else
            {
                control.Text = text; // the "functional part", executing only on the main thread
            }
        }

        void Contains(Bitmap template, Bitmap bmp)
        {
            const Int32 divisor = 4;
            const Int32 epsilon = 10;

            ExhaustiveTemplateMatching etm = new ExhaustiveTemplateMatching(0.99f); //

            TemplateMatch[] tm = etm.ProcessImage(
                new ResizeNearestNeighbor(template.Width / divisor, template.Height / divisor).Apply(template),
                new ResizeNearestNeighbor(bmp.Width / divisor, bmp.Height / divisor).Apply(bmp)
            );
            label5.Text = tm.Length.ToString();
            if (tm.Length >= 1)
            {
                Rectangle tempRect = tm[0].Rectangle;

                imageX = tempRect.Location.X * divisor;
                imageY = tempRect.Location.Y * divisor;
                
                SetText(imageXY, "X:" + (imageX).ToString() + " Y:" + (imageY).ToString());

                if (Math.Abs(bmp.Width / divisor - tempRect.Width) < epsilon && Math.Abs(bmp.Height / divisor - tempRect.Height) < epsilon)
                {
                    SetText(findImage, "True");
                    Loot(imageX, imageY);
                }
            }
            else
            {
                SetText(findImage, "False");
                SetText(imageXY, "");
            }
        }

        public Boolean stopWorker = true;

        protected override void WndProc(ref Message m) //hotbuttons
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == 1)
                    startStop();
            }
            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            RegisterHotKey(this.Handle, 1, 2, (int)'S'); // start/stop toggle hotkey
            if (!backgroundWorker1.IsBusy)
                backgroundWorker1.RunWorkerAsync();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            cursorXY.Text = Cursor.Position.ToString();
        }

        private void startStop()
        {
            // this only stops our image comparison function
            if (stopWorker)
            {
                stopWorker = false;
                button1.Text = "Stop Fishing.";
            }
            else
            {
                stopWorker = true;
                button1.Text = "Start Fishing!";
            }
        }

        private void Loot(int x, int y)
        {
            // press and hold shift button (loot all)
            keybd_event(KEYBDEVENTF_SHIFTVIRTUAL, KEYBDEVENTF_SHIFTSCANCODE, KEYBDEVENTF_KEYDOWN, 0);
            // right click
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, Convert.ToUInt32(x), Convert.ToUInt32(y), 0, 0);
            // release shift button
            keybd_event(KEYBDEVENTF_SHIFTVIRTUAL, KEYBDEVENTF_SHIFTSCANCODE, KEYBDEVENTF_KEYUP, 0);
            // start recast our rod timer
            recast.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            startStop();
        }

        Bitmap find;
        Bitmap template2;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int screenWidth = Screen.GetBounds(new Point(0, 0)).Width;
            int screenHeight = Screen.GetBounds(new Point(0, 0)).Height;
            Bitmap bmpScreenShot = new Bitmap(screenWidth, screenHeight);
            Bitmap findimg = new Bitmap(Application.StartupPath + @"\splash.bmp");
            Graphics gfx = Graphics.FromImage(bmpScreenShot);
            gfx.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
            //bmpScreenShot.Save("Screenshot.bmp", System.Drawing.Imaging.ImageFormat.Bmp); //DEBUG

            Bitmap template = ConvertToFormat(bmpScreenShot, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            find = ConvertToFormat(findimg, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            pictureBox1.Image = null;
            pictureBox1.Image = template;

            template2 = ConvertToFormat(bmpScreenShot, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            bmpScreenShot.Dispose();
            findimg.Dispose();
            gfx.Dispose();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (imageX == 0 && imageY == 0)
                return;

            Rectangle ee = new Rectangle(Convert.ToInt32(imageX / 4.99), Convert.ToInt32(imageY / 5.5), 30, 30);
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, ee);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!stopWorker)
                Contains(template2, find);

            if (!backgroundWorker1.IsBusy)
                backgroundWorker1.RunWorkerAsync();
        }

        private void recast_Tick(object sender, EventArgs e)
        {
            // press 1 (assumed to be skill fish)
            keybd_event(KEYBDEVENTF_NUM1VIRTUAL, KEYBDEVENTF_NUM1SCANCODE, KEYBDEVENTF_KEYDOWN, 0);
            keybd_event(KEYBDEVENTF_NUM1VIRTUAL, KEYBDEVENTF_NUM1SCANCODE, KEYBDEVENTF_KEYUP, 0);
            recast.Enabled = false;
        }
    }
}
