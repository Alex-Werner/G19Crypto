using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;


namespace G19Crypto
{
   

    public partial class HomeScreen : Form
    {
        private const string TRAY_TEXT = "G19 - Crypto Monitor";

        private const int LCD_SCREEN_LOADING = 0;
        private const int LCD_SCREEN_INFO = 1;
        private const int LCD_SCREEN_ABOUT = 2;
        private int currentScreen = LCD_SCREEN_LOADING;

        //The fastest you should send updates to the LCD is around 30fps or 34ms.
        //100ms is probably a good typical update speed.
        private int connection = DMcLgLCD.LGLCD_INVALID_CONNECTION;
        private int device = DMcLgLCD.LGLCD_INVALID_DEVICE;
        private int deviceType = DMcLgLCD.LGLCD_INVALID_DEVICE;
        private Bitmap LCD;
        private uint button;

        private Bitmap[] Screens = new Bitmap[3];
        CancellationTokenSource tokenSource1 = new CancellationTokenSource();
        private bool parsed = false;
        private string LCDScreenshotFolder;

        public HomeScreen()
        {
            InitializeComponent();
            this.notifyIcon1.Text = TRAY_TEXT;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.Hide();
        }
        private void HomeScreen_Load(object sender, EventArgs e)
        {
            if (DMcLgLCD.ERROR_SUCCESS == DMcLgLCD.LcdInit())
            {
                this.connection = DMcLgLCD.LcdConnectEx("Crypto Monitor Info", 0, 0);

                if (this.connection != DMcLgLCD.LGLCD_INVALID_CONNECTION)
                {
                    this.device = DMcLgLCD.LcdOpenByType(this.connection, DMcLgLCD.LGLCD_DEVICE_QVGA);

                    if (this.device == DMcLgLCD.LGLCD_INVALID_DEVICE)
                    {
                        this.device = DMcLgLCD.LcdOpenByType(this.connection, DMcLgLCD.LGLCD_DEVICE_BW);
                        if (this.device != DMcLgLCD.LGLCD_INVALID_DEVICE)
                        {
                            this.deviceType = DMcLgLCD.LGLCD_DEVICE_BW;
                        }
                    }
                    else
                    {
                        this.deviceType = DMcLgLCD.LGLCD_DEVICE_QVGA;
                    }                    
               
                    if (this.deviceType == DMcLgLCD.LGLCD_DEVICE_QVGA)
                    {
                        this.LCD = new Bitmap(320, 240);

                        this.Screens[LCD_SCREEN_LOADING] = getImage(@"splash.jpg");
                        this.Screens[LCD_SCREEN_INFO] = getImage(@"background.jpg");
                        this.Screens[LCD_SCREEN_ABOUT] = getImage(@"background.jpg");
                        
                        Graphics g = Graphics.FromImage(this.Screens[LCD_SCREEN_LOADING]);
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        g.DrawString("'How much is enough ?' - Bud Fox", new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 15, 5);
                        g.Dispose();

                        this.Render();
                    }

                    if (this.deviceType > 0)
                    {
                        this.MainLoopTask(this.tokenSource1.Token);
                    }
                }
            }
        }

        private void Render(int LCDType = DMcLgLCD.LGLCD_DEVICE_QVGA)
        {
            if (this.Screens[this.currentScreen] == null)
            {
                return;
            }

            this.LCD = this.Screens[this.currentScreen];

            if (this.currentScreen > LCD_SCREEN_LOADING)
            {
                addHintText(ref this.LCD);
            }

            DMcLgLCD.LcdUpdateBitmap(this.device, this.LCD.GetHbitmap(), LCDType);
            DMcLgLCD.LcdSetAsLCDForegroundApp(this.device, DMcLgLCD.LGLCD_FORE_YES);
        }



        private void MainLoopTask(CancellationToken token)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        if (this.connection != DMcLgLCD.LGLCD_INVALID_CONNECTION)
                        {
                            this.LCDButtonAction();

                            if (!this.parsed)
                            {
                                this.Render();
                                this.getMonitorInfo();
                                this.parsed = true;
                            }
                        }
                        else
                        {
                            this.parsed = false;
                            this.currentScreen = LCD_SCREEN_LOADING;
                            Thread.Sleep(300);
                            if (this.timer1.Enabled)
                            {
                                this.Invoke((MethodInvoker)delegate { this.timer1.Enabled = false; });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }, token);
        }



        private void lCDScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.LCD != null)
            {
                string filename = String.Format("{0}LCD-Screenshot-{1:dd-MM-yyyy_HH-mm-ss}.png", this.LCDScreenshotFolder, DateTime.Now);
                this.LCD.Save(filename, ImageFormat.Png);
                MessageBox.Show("Screenshot saved to " + filename, "LCD screenshot saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Can't get image from LCD!", "No image on LCD", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AssemblyCopyrightAttribute copyright = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute;
            string info = Application.ProductName + "\n" + "version " + Application.ProductVersion + "\n" + copyright.Copyright;
            MessageBox.Show(info);
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
       
        private void LCDButtonAction()
        {
            if (this.currentScreen == LCD_SCREEN_LOADING)
            {
                return;
            }

            uint LCDbtn = DMcLgLCD.LcdReadSoftButtons(this.device);
            if (this.button != LCDbtn)
            {
                this.button = LCDbtn;
                if (this.button == DMcLgLCD.LGLCD_BUTTON_OK)
                {
                    if ((this.currentScreen != this.Screens.Length - 1) && (this.currentScreen >= LCD_SCREEN_INFO))
                    {
                        this.currentScreen++;
                    }
                    else
                    {
                        this.currentScreen--;
                    }
                }
            }
        }
       
        private static void addHintText(ref Bitmap bmp)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                StringFormat strFormat = new StringFormat();
                strFormat.Alignment = StringAlignment.Far;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString("OK - switch screen", new Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel),
                    Brushes.White, new Rectangle(0, 2, bmp.Width - 2, bmp.Height), strFormat);
                strFormat.Dispose();
            }
        }

        private static Bitmap getImage(string path, bool notFoundMark = false, int width = 320, int height = 240)
        {
            if (File.Exists(path))
            {
                Image img = Image.FromFile(path);
                if (img.Width == width && img.Height == height)
                {
                    return (Bitmap)img;
                }
                else
                {
                    return new Bitmap(img, new Size(width, height));
                }
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);
            if (notFoundMark)
            {
                Pen pen = new Pen(Brushes.White);
                g.DrawLine(pen, new Point(0, 0), new Point(width, height));
                g.DrawLine(pen, new Point(width, 0), new Point(0, height));
                pen.Dispose();
            }
            g.Dispose();

            return bmp;
        }
        private void refreshInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var stuffIwantToCheck = false;
            if (!stuffIwantToCheck)
            {
                MessageBox.Show("Stuff is not running.", "Stuff is not running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                this.getMonitorInfo();
            }
        }

        private void getMonitorInfo()
        {
            MarketParcer parser = new MarketParcer();
            parser.Parse();
            var data = parser.getData();

            Thread.Sleep(1000);
            int i = LCD_SCREEN_INFO;
            this.currentScreen = LCD_SCREEN_INFO;
            //string[,] market = new string[,] { { "bitcoin", "bitcoin", "ripple", "litecoin" }, { "320$", "320$", "320$", "320$" } };

            /*foreach (List<string> el in data)
            {
                var u = el;
            }
                    g.DrawString("Hello GDI+", new Font("Times New Roman", 20), 
            */
            Graphics g = Graphics.FromImage(this.Screens[currentScreen]);
            g.Clear(Color.Black);
            string s = DateTime.Now.ToString("HH:mm:ss");
            g.DrawString("'Last check-up : "+s, new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 15, 5);
            

            Pen myPen = new Pen(System.Drawing.Color.AliceBlue, 1);

            Rectangle myRectangle = new Rectangle(25, 25, 270, 190);//SCreen : 320*240

            g.DrawString(data[0][0], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 35, 30);
            g.DrawString(data[0][1], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 100, 30);

            g.DrawLine(myPen, 25, 50, 295, 50);

            g.DrawString(data[1][0], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 35, 55);
            g.DrawString(data[1][1], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 100, 55);

            g.DrawLine(myPen, 25, 75, 295, 75);

            //g.DrawString(data[2][0], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 35, 80);
            //g.DrawString(data[2][1], new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.White, 100, 80);

            g.DrawLine(myPen, 25, 100, 295, 100);
            g.DrawLine(myPen, 25, 125, 295, 125);
            g.DrawLine(myPen, 25, 150, 295, 150);
            g.DrawLine(myPen, 25, 175, 295, 175);

            g.DrawRectangle(myPen, myRectangle);
       

           /* Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawLine(new Pen(Color.Red), 0, 0, 10, 10);
            }
            pictureBox1.Image = bmp;*/
           /* Rectangle[] Inventoryslots = new Rectangle[24]; // 24 slots
            for (int i = 0; i < Inventoryslots.Length; i++)
            {
                Inventoryslots[i] = new Rectangle(i * 33, 0, box.Width, box.Height);

                spriteBatch.Draw(box, Inventoryslots[i], Color.White);
            }
            */
            if(!this.timer1.Enabled)
            {
                this.Invoke((MethodInvoker)delegate { this.timer1.Enabled = true; });
            }

            /*LoLNexusParser parser = new LoLNexusParser();
            parser.Parse(this.summoner, this.region);

            if (parser.Success)
            {
                Array data = parser.getData();

                int i = LCD_SCRERN_INFO_TEAM1;
                foreach (LoLNexusInfo[] summoners in data)
                {
                    if (summoners.Length > 0)
                    {
                        this.Screens[i] = getTeamInfoImage(summoners);
                    }
                    i++;
                }
                this.currentScreen = LCD_SCRERN_INFO_TEAM1;
                if (!this.timer1.Enabled)
                {
                    this.Invoke((MethodInvoker)delegate { this.timer1.Enabled = true; });
                }
            }*/
        }

        private void DisconnectLCD()
        {
            if (this.LCD != null)
            {
                this.LCD.Dispose();
                DMcLgLCD.LcdClose(this.device);
                DMcLgLCD.LcdDisconnect(this.connection);
                DMcLgLCD.LcdDeInit();
            }
        }
         private void HomeScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DisconnectLCD();
            this.tokenSource1.Cancel();
        }
         private void timer1_Tick(object sender, EventArgs e)
         {
             this.Render();
         }


    }
}
