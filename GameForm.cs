using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UnoGame1
{
    // main menu form
    public class GameForm : Form
    {
        // define buttons as nullable so compiler won’t warn about initialization
        private Button? singleplayerButton;
        private Button? exitButton;

        public GameForm()
        {
            // title of the window
            this.Text = "DR_F UNO Game";

            // set the form to fullscreen without borders
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            // set the background image
            this.BackgroundImage = Image.FromFile("assets/menu-bg.jpg");
            this.BackgroundImageLayout = ImageLayout.Stretch; // stretches to fill the window

            // initialize all UI elements (buttons)
            SetupUI();

            // draw title text directly on the form
            this.Paint += GameForm_Paint;

            // redraw when resizing
            this.Resize += (s, e) => this.Invalidate();
        }

        // draws the title text centered with outline (no background rectangle)
        private void GameForm_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            string title = "DR_F UNO Game";
            Font titleFont = new Font("Segoe UI", 60, FontStyle.Bold);

            // measure text size for perfect centering
            SizeF textSize = g.MeasureString(title, titleFont);
            float x = (this.ClientSize.Width - textSize.Width) / 2;
            float y = (this.ClientSize.Height / 2) - 300; // slightly above buttons

            // create outline and fill for title text
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddString(title, titleFont.FontFamily, (int)FontStyle.Bold,
                               g.DpiY * titleFont.Size / 72,
                               new PointF(x, y), StringFormat.GenericDefault);

                using (Pen outlinePen = new Pen(Color.Black, 8) { LineJoin = LineJoin.Round })
                    g.DrawPath(outlinePen, path);

                using (Brush fillBrush = new SolidBrush(Color.Gold))
                    g.FillPath(fillBrush, path);
            }
        }

        // helper method for rounded corners
        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        // for creating and positioning UI components
        private void SetupUI()
        {
            // "Single Player" button
            singleplayerButton = new Button();
            singleplayerButton.Text = "Single Player";
            singleplayerButton.Width = 300;
            singleplayerButton.Height = 100;
            singleplayerButton.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            singleplayerButton.BackColor = Color.FromArgb(255, 221, 69); // UNO gold
            singleplayerButton.ForeColor = Color.Black;
            singleplayerButton.Cursor = Cursors.Hand;
            singleplayerButton.FlatStyle = FlatStyle.Flat;
            singleplayerButton.FlatAppearance.BorderSize = 0;

            // "Exit Game" button
            exitButton = new Button();
            exitButton.Text = "Exit Game";
            exitButton.Width = 300;
            exitButton.Height = 100;
            exitButton.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            exitButton.BackColor = Color.FromArgb(220, 50, 50); // Deep red
            exitButton.ForeColor = Color.White;
            exitButton.Cursor = Cursors.Hand;
            exitButton.FlatStyle = FlatStyle.Flat;
            exitButton.FlatAppearance.BorderSize = 0;

            // apply rounded corners (radius = 30)
            int cornerRadius = 30;
            singleplayerButton.Region = new Region(RoundedRect(new Rectangle(0, 0, singleplayerButton.Width, singleplayerButton.Height), cornerRadius));
            exitButton.Region = new Region(RoundedRect(new Rectangle(0, 0, exitButton.Width, exitButton.Height), cornerRadius));

            // hover effects
            singleplayerButton.MouseEnter += (s, e) =>
            {
                singleplayerButton.BackColor = Color.FromArgb(255, 240, 100); // lighter gold
            };
            singleplayerButton.MouseLeave += (s, e) =>
            {
                singleplayerButton.BackColor = Color.FromArgb(255, 221, 69);
            };

            exitButton.MouseEnter += (s, e) =>
            {
                exitButton.BackColor = Color.FromArgb(255, 80, 80); // lighter red
            };
            exitButton.MouseLeave += (s, e) =>
            {
                exitButton.BackColor = Color.FromArgb(220, 50, 50);
            };

            // position buttons in the center area of the screen
            singleplayerButton.Left = (this.ClientSize.Width - singleplayerButton.Width) / 2;
            singleplayerButton.Top = this.ClientSize.Height / 2 - 60;

            exitButton.Left = (this.ClientSize.Width - exitButton.Width) / 2;
            exitButton.Top = this.ClientSize.Height / 2 + 80;

            // re-center buttons whenever the window is resized
            this.Resize += CenterElements;
            CenterElements(null, EventArgs.Empty);

            // to start single-player mode
            singleplayerButton.Click += (s, e) =>
            {
                Singleplayer battle = new Singleplayer();
                battle.ShowDialog();
            };

            // to exit the game
            exitButton.Click += (s, e) => this.Close();

            // add buttons to the main form
            this.Controls.Add(singleplayerButton);
            this.Controls.Add(exitButton);
        }

        // recenters buttons and title when window is resized
        private void CenterElements(object? sender, EventArgs e)
        {
            if (singleplayerButton == null || exitButton == null) return;

            int centerX = (this.ClientSize.Width - singleplayerButton.Width) / 2;
            int centerY = this.ClientSize.Height / 2;

            singleplayerButton.Left = centerX;
            singleplayerButton.Top = centerY - 60;

            exitButton.Left = centerX;
            exitButton.Top = centerY + 80;

            // force repaint to keep title centered
            this.Invalidate();
        }
    }
}