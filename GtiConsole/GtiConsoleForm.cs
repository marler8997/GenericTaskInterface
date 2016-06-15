using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GtiConsole
{
    // Prevents visual studio from opening this file in designer mode
    [System.ComponentModel.DesignerCategory("")]
    public partial class GtiConsoleForm : Form
    {
        int cursorX, cursorY;
        int consoleCharWidth, consoleCharHeight;
        Font consoleFont;
        int charPixelWidth, charPixelHeight;
        int cursorPixelHeight;

        StringBuilder[] rows;

        public GtiConsoleForm()
        {
            //
            // TODO: implement color schemes
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Gti Console";
            this.BackColor = Color.Black;

            KeyDown += KeyDownHandler;


            //
            // Initialize Console Data
            //
            cursorX = 0;
            cursorY = 0;
            consoleCharWidth = 80;
            consoleCharHeight = 50;

            SetConsoleFont(new Font(FontFamily.GenericMonospace, 12));

            //
            // Setup strings for each row for paint method
            //
            rows = new StringBuilder[consoleCharHeight];
            for (int i = 0; i < consoleCharHeight; i++)
            {
                rows[i] = new StringBuilder(consoleCharWidth, consoleCharWidth);
                rows[i].Length = consoleCharWidth;
            }
        }

        void SetConsoleFont(Font consoleFont)
        {
            this.consoleFont = consoleFont;
            var charSizeOne = TextRenderer.MeasureText("_", consoleFont, default(Size), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var charSizeTwo = TextRenderer.MeasureText("__", consoleFont, default(Size), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            charPixelWidth = charSizeTwo.Width - charSizeOne.Width;
            charPixelHeight = charSizeOne.Height;
            cursorPixelHeight = (int)(charPixelHeight * .2);
            if (cursorPixelHeight < 1)
            {
                cursorPixelHeight = 1;
            }

            Width = consoleCharWidth * charPixelWidth;
            Height = consoleCharHeight * charPixelHeight;
        }

        void MoveCursorRight()
        {
            Invalidate(new Rectangle(cursorX * charPixelWidth, cursorY * charPixelHeight,
                charPixelWidth, charPixelHeight));
            cursorX++;
            if (cursorX >= consoleCharWidth)
            {
                cursorY++;
                cursorX = 0;
            }
            Invalidate(new Rectangle(cursorX * charPixelWidth, cursorY * charPixelHeight,
                charPixelWidth, charPixelHeight));
        }
        void MoveCursorToBeginningOfNextLine()
        {
            Invalidate(new Rectangle(cursorX * charPixelWidth, cursorY * charPixelHeight,
                charPixelWidth, charPixelHeight));
            cursorX = 0;
            cursorY++;
            Invalidate(new Rectangle(cursorX * charPixelWidth, cursorY * charPixelHeight,
                charPixelWidth, charPixelHeight));
        }

        void Input(Char c)
        {
            rows[cursorY][cursorX] = c;
            MoveCursorRight(); // Will invalidate the gui for the character
        }
        void Input(String s)
        {
            foreach (var c in s)
            {
                Input(c);
            }
        }
        void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                MoveCursorToBeginningOfNextLine();
            }
            else
            {
                Char c = KeyToChar.GetKeyChar(e);
                if (c != '\0')
                {
                    Input(c);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            var rect = e.ClipRectangle;
            //Console.WriteLine("Clip Rectangle ({0},{1}) {2} x {3}",
            //    rect.X, rect.Y, rect.Width, rect.Height);

            //
            // TODO: I should use a different method
            // if the invalidation is more of a vertical box instead of
            // a horizontal box
            //
            int row = rect.Y / charPixelHeight;
            int bottom = rect.Y + rect.Height;
            int y = row * charPixelHeight;
            for (;; row++)
            {
                TextRenderer.DrawText(graphics, rows[row].ToString(), consoleFont, new Point(0, y), Color.White, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                y += charPixelHeight;
                if (row >= consoleCharHeight || y >= bottom)
                {
                    break;
                }
            }

            //
            // Draw Cursor
            //
            graphics.FillRectangle(Brushes.White,
                cursorX * charPixelWidth,
                ((cursorY + 1) * charPixelHeight) - cursorPixelHeight,
                charPixelWidth, cursorPixelHeight);
        }
    }

    [Flags]
    public enum KeyFlags
    {
        None = 0,
        IsAlpha = 0x100,
    }

    static class KeyToChar
    {
        public static Char GetKeyChar(KeyEventArgs e)
        {
            UInt32 info;

            if ((int)e.KeyCode < KeyInfoTable.Length)
            {
                info = KeyInfoTable[(int)e.KeyCode];
            }
            else
            {
                if (!KeyInfoMap.TryGetValue(e.KeyCode, out info))
                {
                    KeyInfoMap.Add(e.KeyCode, 0);
                    info = 0;
                }
            }

            Char c = (Char)(info & 0xFF);
            if ((info & (uint)KeyFlags.IsAlpha) != 0)
            {
                if (e.Shift)
                {
                    c = (Char)(c + 'A' - 'a');
                }
            }
            return c;
        }

        static void SetInfo(Keys key)
        {
            if ((int)key < KeyInfoTable.Length)
            {
                KeyInfoTable[(int)key] = 0;
            }
            else
            {
                KeyInfoMap.Add(key, 0);
            }
        }
        static void SetInfo(Keys key, Char c)
        {
            if ((int)key < KeyInfoTable.Length)
            {
                KeyInfoTable[(int)key] = c;
            }
            else
            {
                KeyInfoMap.Add(key, c);
            }
        }
        static void SetInfo(Keys key, Char c, KeyFlags flags)
        {
            if ((int)key < KeyInfoTable.Length)
            {
                KeyInfoTable[(int)key] = (uint)flags | c;
            }
            else
            {
                KeyInfoMap.Add(key, (uint)flags | c);
            }
        }
        static readonly UInt32[] KeyInfoTable = new UInt32[256];
        static readonly Dictionary<Keys, UInt32> KeyInfoMap = new Dictionary<Keys, UInt32>();
        static KeyToChar()
        {
            SetInfo(Keys.None);
            SetInfo(Keys.LButton);
            SetInfo(Keys.RButton);
            SetInfo(Keys.Cancel);
            SetInfo(Keys.MButton);
            SetInfo(Keys.XButton1);
            SetInfo(Keys.XButton2);
            SetInfo(Keys.Back);
            SetInfo(Keys.Tab);
            SetInfo(Keys.LineFeed);
            SetInfo(Keys.Clear);
            SetInfo(Keys.Enter);
            SetInfo(Keys.Return);
            SetInfo(Keys.ShiftKey);
            SetInfo(Keys.ControlKey);
            SetInfo(Keys.Menu);
            SetInfo(Keys.Pause);
            SetInfo(Keys.CapsLock);
            SetInfo(Keys.Capital);
            SetInfo(Keys.KanaMode);
            SetInfo(Keys.HanguelMode);
            SetInfo(Keys.HangulMode);
            SetInfo(Keys.JunjaMode);
            SetInfo(Keys.FinalMode);
            SetInfo(Keys.KanjiMode);
            SetInfo(Keys.HanjaMode);
            SetInfo(Keys.Escape);
            SetInfo(Keys.IMEConvert);
            SetInfo(Keys.IMENonconvert);
            SetInfo(Keys.IMEAceept);
            SetInfo(Keys.IMEAccept);
            SetInfo(Keys.IMEModeChange);
            SetInfo(Keys.Space, ' ');
            SetInfo(Keys.Prior);
            SetInfo(Keys.PageUp);
            SetInfo(Keys.Next);
            SetInfo(Keys.PageDown);
            SetInfo(Keys.End);
            SetInfo(Keys.Home);
            SetInfo(Keys.Left);
            SetInfo(Keys.Up);
            SetInfo(Keys.Right);
            SetInfo(Keys.Down);
            SetInfo(Keys.Select);
            SetInfo(Keys.Print);
            SetInfo(Keys.Execute);
            SetInfo(Keys.PrintScreen);
            SetInfo(Keys.Snapshot);
            SetInfo(Keys.Insert);
            SetInfo(Keys.Delete);
            SetInfo(Keys.Help);
            SetInfo(Keys.D0);
            SetInfo(Keys.D1);
            SetInfo(Keys.D2);
            SetInfo(Keys.D3);
            SetInfo(Keys.D4);
            SetInfo(Keys.D5);
            SetInfo(Keys.D6);
            SetInfo(Keys.D7);
            SetInfo(Keys.D8);
            SetInfo(Keys.D9);
            SetInfo(Keys.A, 'a', KeyFlags.IsAlpha);
            SetInfo(Keys.B, 'b', KeyFlags.IsAlpha);
            SetInfo(Keys.C, 'c', KeyFlags.IsAlpha);
            SetInfo(Keys.D, 'd', KeyFlags.IsAlpha);
            SetInfo(Keys.E, 'e', KeyFlags.IsAlpha);
            SetInfo(Keys.F, 'f', KeyFlags.IsAlpha);
            SetInfo(Keys.G, 'g', KeyFlags.IsAlpha);
            SetInfo(Keys.H, 'h', KeyFlags.IsAlpha);
            SetInfo(Keys.I, 'i', KeyFlags.IsAlpha);
            SetInfo(Keys.J, 'j', KeyFlags.IsAlpha);
            SetInfo(Keys.K, 'k', KeyFlags.IsAlpha);
            SetInfo(Keys.L, 'l', KeyFlags.IsAlpha);
            SetInfo(Keys.M, 'm', KeyFlags.IsAlpha);
            SetInfo(Keys.N, 'n', KeyFlags.IsAlpha);
            SetInfo(Keys.O, 'o', KeyFlags.IsAlpha);
            SetInfo(Keys.P, 'p', KeyFlags.IsAlpha);
            SetInfo(Keys.Q, 'q', KeyFlags.IsAlpha);
            SetInfo(Keys.R, 'r', KeyFlags.IsAlpha);
            SetInfo(Keys.S, 's', KeyFlags.IsAlpha);
            SetInfo(Keys.T, 't', KeyFlags.IsAlpha);
            SetInfo(Keys.U, 'u', KeyFlags.IsAlpha);
            SetInfo(Keys.V, 'v', KeyFlags.IsAlpha);
            SetInfo(Keys.W, 'w', KeyFlags.IsAlpha);
            SetInfo(Keys.X, 'x', KeyFlags.IsAlpha);
            SetInfo(Keys.Y, 'y', KeyFlags.IsAlpha);
            SetInfo(Keys.Z, 'z', KeyFlags.IsAlpha);
            SetInfo(Keys.LWin);
            SetInfo(Keys.RWin);
            SetInfo(Keys.Apps);
            SetInfo(Keys.Sleep);
            SetInfo(Keys.NumPad0);
            SetInfo(Keys.NumPad1);
            SetInfo(Keys.NumPad2);
            SetInfo(Keys.NumPad3);
            SetInfo(Keys.NumPad4);
            SetInfo(Keys.NumPad5);
            SetInfo(Keys.NumPad6);
            SetInfo(Keys.NumPad7);
            SetInfo(Keys.NumPad8);
            SetInfo(Keys.NumPad9);
            SetInfo(Keys.Multiply);
            SetInfo(Keys.Add);
            SetInfo(Keys.Separator);
            SetInfo(Keys.Subtract);
            SetInfo(Keys.Decimal);
            SetInfo(Keys.Divide);
            SetInfo(Keys.F1);
            SetInfo(Keys.F2);
            SetInfo(Keys.F3);
            SetInfo(Keys.F4);
            SetInfo(Keys.F5);
            SetInfo(Keys.F6);
            SetInfo(Keys.F7);
            SetInfo(Keys.F8);
            SetInfo(Keys.F9);
            SetInfo(Keys.F10);
            SetInfo(Keys.F11);
            SetInfo(Keys.F12);
            SetInfo(Keys.F13);
            SetInfo(Keys.F14);
            SetInfo(Keys.F15);
            SetInfo(Keys.F16);
            SetInfo(Keys.F17);
            SetInfo(Keys.F18);
            SetInfo(Keys.F19);
            SetInfo(Keys.F20);
            SetInfo(Keys.F21);
            SetInfo(Keys.F22);
            SetInfo(Keys.F23);
            SetInfo(Keys.F24);
            SetInfo(Keys.NumLock);
            SetInfo(Keys.Scroll);
            SetInfo(Keys.LShiftKey);
            SetInfo(Keys.RShiftKey);
            SetInfo(Keys.LControlKey);
            SetInfo(Keys.RControlKey);
            SetInfo(Keys.LMenu);
            SetInfo(Keys.RMenu);
            SetInfo(Keys.BrowserBack);
            SetInfo(Keys.BrowserForward);
            SetInfo(Keys.BrowserRefresh);
            SetInfo(Keys.BrowserStop);
            SetInfo(Keys.BrowserSearch);
            SetInfo(Keys.BrowserFavorites);
            SetInfo(Keys.BrowserHome);
            SetInfo(Keys.VolumeMute);
            SetInfo(Keys.VolumeDown);
            SetInfo(Keys.VolumeUp);
            SetInfo(Keys.MediaNextTrack);
            SetInfo(Keys.MediaPreviousTrack);
            SetInfo(Keys.MediaStop);
            SetInfo(Keys.MediaPlayPause);
            SetInfo(Keys.LaunchMail);
            SetInfo(Keys.SelectMedia);
            SetInfo(Keys.LaunchApplication1);
            SetInfo(Keys.LaunchApplication2);
            SetInfo(Keys.Oem1);
            SetInfo(Keys.OemSemicolon);
            SetInfo(Keys.Oemplus);
            SetInfo(Keys.Oemcomma, ',');
            SetInfo(Keys.OemMinus);
            SetInfo(Keys.OemPeriod);
            SetInfo(Keys.OemQuestion);
            SetInfo(Keys.Oem2);
            SetInfo(Keys.Oemtilde);
            SetInfo(Keys.Oem3);
            SetInfo(Keys.Oem4);
            SetInfo(Keys.OemOpenBrackets);
            SetInfo(Keys.OemPipe);
            SetInfo(Keys.Oem5);
            SetInfo(Keys.Oem6);
            SetInfo(Keys.OemCloseBrackets);
            SetInfo(Keys.Oem7);
            SetInfo(Keys.OemQuotes);
            SetInfo(Keys.Oem8);
            SetInfo(Keys.Oem102);
            SetInfo(Keys.OemBackslash);
            SetInfo(Keys.ProcessKey);
            SetInfo(Keys.Packet);
            SetInfo(Keys.Attn);
            SetInfo(Keys.Crsel);
            SetInfo(Keys.Exsel);
            SetInfo(Keys.EraseEof);
            SetInfo(Keys.Play);
            SetInfo(Keys.Zoom);
            SetInfo(Keys.NoName);
            SetInfo(Keys.Pa1);
            SetInfo(Keys.OemClear);
            SetInfo(Keys.KeyCode);
            SetInfo(Keys.Shift);
            SetInfo(Keys.Control);
            SetInfo(Keys.Alt);
        }
    }
}
