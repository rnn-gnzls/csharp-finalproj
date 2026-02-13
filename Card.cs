using System;

namespace UnoGame1
{
    // defines all possible types of UNO cards
    public enum CardType
    {
        Number,         // regular number cards (0–9)
        Skip,           // skips next player's turn
        Reverse,        // reverses the order of play
        DrawTwo,        // next player draws two cards
        Wild,           // player chooses the next color
        WildDrawFour    // player chooses color and opponent draws four
    }

    // represents a single UNO card with color, value, type, and number
    public class Card
    {
        // color of the card (Red, Blue, Green, Yellow, or "None" for wild cards)
        public string Color { get; set; }

        // display value (Numbers and special cards)
        public string Value { get; set; }

        // logical type of the card based on its value (see CardType enum)
        public CardType Type { get; set; }

        // numeric value if the card is a number card (0–9), otherwise -1
        public int Number { get; set; } = -1;

        // constructor determines card type automatically from the given value
        public Card(string color, string value)
        {
            Color = color;
            Value = value;

            // If value is a number, it's a regular number card
            if (int.TryParse(value, out int num))
            {
                Type = CardType.Number;
                Number = num;
            }
            else
            {
                // otherwise, determine special card type
                switch (value)
                {
                    case "Skip":
                        Type = CardType.Skip;
                        break;
                    case "Reverse":
                        Type = CardType.Reverse;
                        break;
                    case "Draw2":
                        Type = CardType.DrawTwo;
                        break;
                    case "Wild":
                        Type = CardType.Wild;
                        break;
                    case "Draw4":
                        Type = CardType.WildDrawFour;
                        break;
                    default:
                        // throw an error for invalid card names
                        throw new ArgumentException($"Invalid card value: {value}");
                }
            }
        }

        // returns a readable representation of the card
        public override string ToString()
        {
            return $"{Color} {Value}";
        }
    }
}