using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace UnoGame1
{
    // main game window (Player vs AI)
    public class Singleplayer : Form
    {
        // player and AI card lists (each card represented by a Button)
        private readonly List<Button> playerHand = new();
        private readonly List<Button> aiHand = new();

        // visual elements
        private PictureBox discardPile = null!;
        private Label colorLabel = null!;

        // game state variables
        private string currentColor = "";   // currently active color
        private string currentValue = "";   // currently active card value
        private readonly Random rng = new(); // for randomization (deck shuffle, AI color choice)
        private Queue<Card> deck = new();    // deck of remaining cards

        // turn control variables
        private bool isPlayerTurn = true;   // true if it's the player's turn
        private bool hasDrawnThisTurn = false; // ensures player can only draw once per turn
        private int drawPenalty = 0;        // for +2 / +4 penalties
        private bool skipNext = false;      // skip flag (for Skip/Reverse cards)

        // game initialization
        public Singleplayer()
        {
            this.Text = "DR_F UNO Game - Singleplayer";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackgroundImage = Image.FromFile("assets/bg2.png");
            this.BackgroundImageLayout = ImageLayout.Stretch;

            SetupGame();    // create all game UI + setup deck
            AddExitButton();
            AddDrawButton();
        }

        // "X" button to exit the game
        private void AddExitButton()
        {
            Button exitBtn = new()
            {
                Text = "X",
                Width = 40,
                Height = 40,
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Top = 10,
                Left = this.ClientSize.Width - 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            exitBtn.Click += (s, e) => this.Close();
            this.Controls.Add(exitBtn);
        }

        // "Draw" button for the player
        private void AddDrawButton()
        {
            Button drawBtn = new()
            {
                Text = "Draw",
                Width = 100,
                Height = 50,
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                Left = 50,
                Top = this.ClientSize.Height - 150,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            drawBtn.Click += (s, e) =>
            {
                if (!isPlayerTurn) return;
                if (hasDrawnThisTurn) return;

                // use SafeDequeue when drawing
                if (deck.Count > 0)
                {
                    AddCardToHand(playerHand, true, 0, this.ClientSize.Height - 200);
                    hasDrawnThisTurn = true;

                    var lastBtn = playerHand.LastOrDefault();
                    if (lastBtn?.Tag is Card drawnCard && IsPlayable(drawnCard))
                    {
                        PlayCard(lastBtn);
                        return;
                    }

                    NextTurn();
                }

                // if deck is empty, it's a draw
                else
                {
                    MessageBox.Show("The deck is empty — it's a draw! No one wins this round.", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // disable all buttons and end the game
                    foreach (Button btn in this.Controls.OfType<Button>().ToList())
                        btn.Enabled = false;

                    return;
                }
            };

            this.Controls.Add(drawBtn);
        }

        // game layout, deck, and first card
        private void SetupGame()
        {
            deck = GenerateDeck();

            discardPile = new PictureBox
            {
                Width = 120,
                Height = 180,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Left = (this.ClientSize.Width - 120) / 2,
                Top = (this.ClientSize.Height - 180) / 2,
                BackColor = Color.White
            };
            this.Controls.Add(discardPile);

            colorLabel = new Label
            {
                Text = "",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.FromArgb(160, 0, 0, 0)
            };
            this.Controls.Add(colorLabel);

            Label playerLabel = new()
            {
                Text = "Your Cards:",
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                Font = new Font("Arial", 20, FontStyle.Bold),
                AutoSize = true,
                Top = this.ClientSize.Height - 300,
                Left = 20
            };
            this.Controls.Add(playerLabel);

            Label aiLabel = new()
            {
                Text = "AI Cards:",
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                Font = new Font("Arial", 20, FontStyle.Bold),
                AutoSize = true,
                Top = 20,
                Left = 20
            };
            this.Controls.Add(aiLabel);

            for (int i = 0; i < 7; i++)
            {
                AddCardToHand(aiHand, false, 0, 80);
                AddCardToHand(playerHand, true, 0, this.ClientSize.Height - 200);
            }

            // use SafeDequeue for first card
            var startCard = SafeDequeue();
            currentColor = startCard.Color;
            currentValue = startCard.Value;
            SetDiscardImage(startCard);
            UpdateColorLabel();

            this.Resize += (s, e) =>
            {
                discardPile.Left = (this.ClientSize.Width - discardPile.Width) / 2;
                discardPile.Top = (this.ClientSize.Height - discardPile.Height) / 2;
                colorLabel.Left = (this.ClientSize.Width - colorLabel.Width) / 2;
                colorLabel.Top = discardPile.Top - 50;
                playerLabel.Top = this.ClientSize.Height - 220;
                RepositionHand(playerHand, true, this.ClientSize.Height - 200);
                RepositionHand(aiHand, false, 80);
            };
        }

        // safeguard deck draws (ensures all are valid cards)
        private Card SafeDequeue()
        {
            if (deck.Count == 0) deck = GenerateDeck();

            Card card = deck.Dequeue();

            while (card == null || string.IsNullOrEmpty(card.Color) || string.IsNullOrEmpty(card.Value))
            {
                if (deck.Count == 0) deck = GenerateDeck();
                card = deck.Dequeue();
            }

            return card;
        }

        // limited to 108 cards (usual UNO deck)
        private Queue<Card> GenerateDeck()
        {
            string[] colors = { "Red", "Blue", "Green", "Yellow" };
            string[] values = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "Skip", "Reverse", "Draw2" };

            var deckList = new List<Card>();
            foreach (string color in colors)
            {
                deckList.Add(new Card(color, "0"));
                foreach (string value in values.Skip(1))
                {
                    deckList.Add(new Card(color, value));
                    deckList.Add(new Card(color, value));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                deckList.Add(new Card("Wild", "Wild"));
                deckList.Add(new Card("Wild", "Draw4"));
            }

            deckList = deckList
                .Where(c => !string.IsNullOrEmpty(c.Color) && !string.IsNullOrEmpty(c.Value))
                .ToList();

            return new Queue<Card>(deckList.OrderBy(x => rng.Next()));
        }

        // adds a single card button to either player or AI hand
        private void AddCardToHand(List<Button> hand, bool isPlayer, int x, int y)
        {
            // use SafeDequeue
            Card card = SafeDequeue();

            Button cardBtn = new()
            {
                Width = 100,
                Height = 150,
                Tag = card,
                FlatStyle = FlatStyle.Flat,
                BackgroundImageLayout = ImageLayout.Zoom,
                ForeColor = Color.Black,
                Font = new Font("Arial", 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            string imagePath = GetCardImagePath(card);
            try
            {
                Image img = Image.FromFile(imagePath);
                cardBtn.BackgroundImage = new Bitmap(img, new Size(100, 150));
            }
            catch
            {
                // strong fallback (visual text)
                Bitmap fallback = new(100, 150);
                using Graphics g = Graphics.FromImage(fallback);
                g.Clear(Color.Gray);
                using Font f = new("Arial", 16, FontStyle.Bold);
                using Brush b = new SolidBrush(Color.Black);
                g.DrawString($"{card.Color}\n{card.Value}", f, b, new RectangleF(5, 40, 90, 100));
                cardBtn.BackgroundImage = fallback;
            }

            if (card.Type == CardType.Number)
            {
                cardBtn.Text = card.Number.ToString();
                cardBtn.ForeColor = Color.Black;
            }
            else
            {
                cardBtn.Text = "";
            }

            if (!isPlayer)
            {
                try { cardBtn.BackgroundImage = Image.FromFile("assets/uno-back.jpg"); }
                catch { }
                cardBtn.Text = "";
            }
            else
            {
                cardBtn.Click += (s, e) => { if (isPlayerTurn) PlayCard(cardBtn); };
            }

            hand.Add(cardBtn);
            this.Controls.Add(cardBtn);
            cardBtn.BringToFront();
            RepositionHand(hand, isPlayer, y);
        }

        // gets correct image path based on color or value
        private string GetCardImagePath(Card card)
        {
            string colorPrefix = card.Color.ToLower()[0].ToString();
            return card switch
            {
                { Color: "Wild", Value: "Wild" } => "assets/wild-card.jpg",
                { Color: "Wild", Value: "Draw4" } => "assets/wild-draw-four.jpg",
                { Value: "Reverse" } => $"assets/{colorPrefix}-reverse.jpg",
                { Value: "Skip" } => $"assets/{colorPrefix}-skip.jpg",
                { Value: "Draw2" } => $"assets/{colorPrefix}-draw-two.jpg",
                { Type: CardType.Number } => $"assets/{colorPrefix}-template.jpg",
                _ => "assets/uno-back.jpg"
            };
        }

        // arranges the cards horizontally in player's and AI's hand
        private void RepositionHand(List<Button> hand, bool isPlayer, int startY)
        {
            if (hand.Count == 0) return;
            int spacing = 110;
            int startX = 200;
            for (int i = 0; i < hand.Count; i++)
            {
                hand[i].Left = startX + i * spacing;
                hand[i].Top = startY;
            }
        }

        // checks if card can be played
        private bool IsPlayable(Card card)
        {
            if (card.Color == "Wild") return true;
            if (string.Equals(card.Color, currentColor, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(card.Value, currentValue, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // called when player clicks a card to play
        private void PlayCard(Button cardBtn)
        {
            if (cardBtn?.Tag is not Card cardData) return;
            if (!IsPlayable(cardData))
            {
                MessageBox.Show("Invalid move! Must match color or number.");
                return;
            }
            ApplyPlayedCard(cardBtn, cardData, true);
        }

        // applies effects of played card (for both player and AI)
        private void ApplyPlayedCard(Button cardBtn, Card cardData, bool isPlayer)
        {
            currentValue = cardData.Value;

            // handle color change for Wild and WildDraw4
            if (cardData.Color == "Wild")
                currentColor = isPlayer ? ChooseColorDialog() : new[] { "Red", "Blue", "Green", "Yellow" }[rng.Next(4)];
            else
                currentColor = cardData.Color;

            SetDiscardImage(cardData);
            UpdateColorLabel();

            // remove card from hand
            if (isPlayer) playerHand.Remove(cardBtn);
            else aiHand.Remove(cardBtn);
            this.Controls.Remove(cardBtn);

            // apply special card effects
            if (cardData.Type == CardType.DrawTwo)
            {
                drawPenalty = 2; NextTurn(); return;
            }
            else if (cardData.Type == CardType.WildDrawFour)
            {
                drawPenalty = 4; NextTurn(); return;
            }
            else if (cardData.Type == CardType.Skip || cardData.Type == CardType.Reverse)
            {
                skipNext = true; NextTurn(); return;
            }
            else
            {
                NextTurn();
            }

            // win conditions
            if (playerHand.Count == 0)
            {
                MessageBox.Show("Congratulations! You won!");
                this.Close();
            }
            else if (aiHand.Count == 0)
            {
                MessageBox.Show("AI wins! Better luck next time.");
                this.Close();
            }
        }

        // handles switching turns and applying penalties
        private void NextTurn()
        {
            hasDrawnThisTurn = false;
            isPlayerTurn = !isPlayerTurn;

            // Apply draw penalties
            if (drawPenalty > 0)
            {
                if (isPlayerTurn)
                    DrawCards(playerHand, drawPenalty, true, this.ClientSize.Height - 200);
                else
                    DrawCards(aiHand, drawPenalty, false, 80);
                drawPenalty = 0;
                isPlayerTurn = !isPlayerTurn; // undo turn after drawing
            }
            else if (skipNext)
            {
                skipNext = false;
                isPlayerTurn = !isPlayerTurn; // skip a turn
            }

            // if it's AI's turn, trigger AI logic
            if (!isPlayerTurn)
                AiTurn();
        }

        // play a valid card or draw one for AI
        private void AiTurn()
        {
            foreach (var aiCardBtn in aiHand.ToArray())
            {
                if (aiCardBtn?.Tag is not Card cardData) continue;
                if (IsPlayable(cardData))
                {
                    ApplyPlayedCard(aiCardBtn, cardData, false);
                    return;
                }
            }

            // if no playable card then draw
            if (deck.Count > 0)
            {
                AddCardToHand(aiHand, false, 0, 80);
                var lastBtn = aiHand.LastOrDefault();
                if (lastBtn?.Tag is Card drawnCard && IsPlayable(drawnCard))
                {
                    ApplyPlayedCard(lastBtn, drawnCard, false);
                    return;
                }
            }

            NextTurn();
        }

        // helper to draw multiple cards at once (for +2 and +4)
        private void DrawCards(List<Button> hand, int count, bool isPlayer, int y)
        {
            for (int i = 0; i < count; i++)
            {
                if (deck.Count == 0) deck = GenerateDeck();
                AddCardToHand(hand, isPlayer, 0, y);
            }
        }

        // when Wild or WildDraw4 is played, let player choose a color
        private string ChooseColorDialog()
        {
            using Form colorForm = new()
            {
                Text = "Choose a color",
                Width = 400,
                Height = 150,
                StartPosition = FormStartPosition.CenterParent
            };

            string chosenColor = "Red";
            string[] colors = { "Red", "Blue", "Green", "Yellow" };
            FlowLayoutPanel panel = new() { Dock = DockStyle.Fill };

            // create color selection buttons
            foreach (string color in colors)
            {
                Button btn = new()
                {
                    Text = color,
                    BackColor = ColorFromName(color),
                    ForeColor = Color.White,
                    Width = 80,
                    Height = 50
                };
                btn.Click += (s, e) =>
                {
                    chosenColor = color;
                    colorForm.DialogResult = DialogResult.OK;
                    colorForm.Close();
                };
                panel.Controls.Add(btn);
            }

            colorForm.Controls.Add(panel);
            colorForm.ShowDialog();
            return chosenColor;
        }

        // updates discard pile image after a card is played
        private void SetDiscardImage(Card card)
        {
            try
            {
                string path = GetCardImagePath(card);
                Image img = Image.FromFile(path);
                Bitmap resized = new(img, new Size(120, 180));

                // draw number if it's a number card
                if (card.Type == CardType.Number)
                {
                    using Graphics g = Graphics.FromImage(resized);
                    using Font f = new("Arial", 20, FontStyle.Bold);
                    using Brush b = new SolidBrush(Color.Black);
                    StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(card.Number.ToString(), f, b, new RectangleF(0, 0, resized.Width, resized.Height), sf);
                }

                discardPile.Image = resized;
                colorLabel.Left = (this.ClientSize.Width - colorLabel.Width) / 2;
                colorLabel.Top = discardPile.Top - 50;
            }
            catch { }
        }

        // updates text display for current color
        private void UpdateColorLabel()
        {
            colorLabel.Text = $"Current Color: {currentColor}";
            colorLabel.ForeColor = ColorFromName(currentColor);
            colorLabel.Left = (this.ClientSize.Width - colorLabel.Width) / 2;
            colorLabel.Top = discardPile.Top - 50;
        }

        // converts string color to System.Drawing.Color
        private Color ColorFromName(string color) =>
            color.ToLower() switch
            {
                "red" => Color.Red,
                "blue" => Color.DodgerBlue,
                "green" => Color.Green,
                "yellow" => Color.Goldenrod,
                "wild" => Color.Black,
                _ => Color.LightGray
            };
    }
}