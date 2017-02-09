using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;

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
            GC.Collect(); //not the best way to collect 'copy' but it works
            GC.WaitForPendingFinalizers();
            Bitmap copy = new Bitmap(image.Width, image.Height, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            }
            return copy;
        }

        [DllImport("user32.dll", CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
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
        Bitmap[] find = new Bitmap[100];
        int _findLen = 0;
        Bitmap template2;

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

        public float sensitivity = 0.99f; //somewhere between 0.98 - 0.99 is perfect

        void Contains(Bitmap template, Bitmap bmp, int tmpltnum)
        {
            const Int32 divisor = 4;
            const Int32 epsilon = 10;

            ExhaustiveTemplateMatching etm = new ExhaustiveTemplateMatching(sensitivity);

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

                if (Math.Abs(bmp.Width / divisor - tempRect.Width) < epsilon &&
                    Math.Abs(bmp.Height / divisor - tempRect.Height) < epsilon)
                {
                    SetText(findImage, "Got Fish!");
                }
                Loot(imageX, imageY);
                label11.Text = "Template: splash" + tmpltnum.ToString() + ".bmp"; //debug, shows which template matched the bobber (or other stuff on your screen)
            }
            else
            {
                SetText(findImage, "Waiting for splash...");
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
            if (!File.Exists(Application.StartupPath + @"\splash0.bmp"))
            {
                MessageBox.Show(
                    "splash0.bmp image file is missing!" + "\n" + "It must be in the same directory as wowFishing.exe",
                    "CRITICAL ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            _findLen = 0;
//start Templates initialization
//moved it to Form1_Load to not reinitialized it during each worker selfinvokation
            for (int i = 0;; i++)
            {
                string path = Application.StartupPath + @"\splash" + i.ToString() + ".bmp";

                if (!File.Exists(path))
                {
                    break;
                }

                Bitmap findimg = new Bitmap(path);
                find[i] = ConvertToFormat(findimg, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                findimg.Dispose();
                _findLen++;
            }
//end Templates initialization
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            RegisterHotKey(this.Handle, 1, 2, (int) 'S'); // start/stop toggle hotkey
            if (!backgroundWorker1.IsBusy)
                backgroundWorker1.RunWorkerAsync();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            cursorXY.Text = Cursor.Position.ToString();
        }

        private void castRod()
        {
            // press 1 (assumed to be skill fish)
            keybd_event(KEYBDEVENTF_NUM1VIRTUAL, KEYBDEVENTF_NUM1SCANCODE, KEYBDEVENTF_KEYDOWN, 0);
            keybd_event(KEYBDEVENTF_NUM1VIRTUAL, KEYBDEVENTF_NUM1SCANCODE, KEYBDEVENTF_KEYUP, 0);
        }

        private void startStop()
        {
            // this only stops our image comparison function
            if (stopWorker)
            {
                recast.Enabled = true;
                stopWorker = false;
                runTimer.Enabled = true;
                button1.Text = "Stop Fishing.";
            }
            else
            {
                stopWorker = true;
                button1.Text = "Start Fishing!";
                recast.Enabled = false;
                dudTimer.Enabled = false;
                runTimer.Enabled = false;
            }
        }

        private void Loot(int x, int y)
        {
            Cursor.Position = new Point(x, (y + 10));
            // press and hold shift button (loot all)
            System.Threading.Thread.Sleep(200);
            keybd_event(KEYBDEVENTF_SHIFTVIRTUAL, KEYBDEVENTF_SHIFTSCANCODE, KEYBDEVENTF_KEYDOWN, 0);
            // right click
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, Convert.ToUInt32(x), Convert.ToUInt32(y), 0, 0);
            // release shift button
            keybd_event(KEYBDEVENTF_SHIFTVIRTUAL, KEYBDEVENTF_SHIFTSCANCODE, KEYBDEVENTF_KEYUP, 0);
            // start recast our rod timer
            recast.Enabled = true;
            dudTimer.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            startStop();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.Sleep(100);
            int screenWidth = Screen.GetBounds(new Point(0, 0)).Width;
            int screenHeight = Screen.GetBounds(new Point(0, 0)).Height;
            Bitmap bmpScreenShot = new Bitmap(screenWidth, screenHeight);
            Graphics gfx = Graphics.FromImage(bmpScreenShot);
            gfx.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
            //bmpScreenShot.Save("Screenshot.bmp", System.Drawing.Imaging.ImageFormat.Bmp); //DEBUG

            //  Bitmap template = ConvertToFormat(bmpScreenShot, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            template2 = ConvertToFormat(bmpScreenShot, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            pictureBox1.Image = null;
            pictureBox1.Image = template2;
            bmpScreenShot.Dispose();
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
            {
                for (int i = 0; i < _findLen; i++)
                {
                    Contains(template2, find[i], i);
                }
            }

            if (dudTimer.Enabled)
                dudTimerLive.Text = "enabled";
            else
                dudTimerLive.Text = "disabled";

            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void recast_Tick(object sender, EventArgs e)
        {
            castRod();
            recast.Enabled = false;
            Cursor.Position = new Point(imageX, (imageY + 500)); //cursor might get in the way
            dudTimer.Enabled = true;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float trackBarValue = trackBar1.Value;
            float percentage = Convert.ToSingle(trackBarValue / 100.0);
            sensitivity = Convert.ToSingle(0.90 + percentage);
            sensLabel.Text = sensitivity.ToString();
        }

        private void dudTimer_Tick(object sender, EventArgs e)
        {
            recast.Enabled = true;
        }

        private void runTimer_Tick(object sender, EventArgs e)
        {
            if (Convert.ToInt32(runTimerText.Text) > 1)
                runTimerText.Text = (Convert.ToInt32(runTimerText.Text) - 1).ToString();
            else
                startStop(); // camp and hearth needs to be added later
        }
    }
}
