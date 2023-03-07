using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace OpenDotSpacePrograms.UI.Controls
{
    public sealed class Display : AUIControl
    {
        private readonly List<string> spriteList = new List<string>();

        private string[] fonts;

        private string[] sprites;

        public IMyTextSurface TextSurface { get; }

        public override TextAlignment Alignment
        {
            get
            {
                return (TextSurface == null) ? base.Alignment : TextSurface.Alignment;
            }
            set
            {
                base.Alignment = value;
                if (TextSurface != null)
                {
                    TextSurface.Alignment = value;
                }
            }
        }

        public override byte BackgroundAlpha
        {
            get
            {
                return (TextSurface == null) ? base.BackgroundAlpha : TextSurface.BackgroundAlpha;
            }
            set
            {
                base.BackgroundAlpha = value;
                if (TextSurface != null)
                {
                    TextSurface.BackgroundAlpha = value;
                }
            }
        }

        public override Color BackgroundColor
        {
            get
            {
                return (TextSurface == null) ? base.BackgroundColor : TextSurface.ScriptBackgroundColor;
            }
            set
            {
                base.BackgroundColor = value;
                if (TextSurface != null)
                {
                    TextSurface.ScriptBackgroundColor = value;
                }
            }
        }

        public override string Font
        {
            get
            {
                return (TextSurface == null) ? base.Font : TextSurface.Font;
            }
            set
            {
                base.Font = value;
                if (TextSurface != null)
                {
                    TextSurface.Font = value;
                }
            }
        }

        public override IReadOnlyList<string> Fonts
        {
            get
            {
                IReadOnlyList<string> ret = fonts;
                if (fonts == null)
                {
                    if (TextSurface == null)
                    {
                        ret = Parent.Fonts;
                    }
                    else
                    {
                        List<string> font_list = new List<string>();
                        TextSurface.GetFonts(font_list);
                        fonts = font_list.ToArray();
                        font_list.Clear();
                        ret = fonts;
                    }
                }
                return ret;
            }
        }

        public override Color ForegroundColor
        {
            get
            {
                return (TextSurface == null) ? base.ForegroundColor : TextSurface.ScriptForegroundColor;
            }
            set
            {
                base.ForegroundColor = value;
                if (TextSurface != null)
                {
                    TextSurface.ScriptForegroundColor = value;
                }
            }
        }

        public override float FontSize
        {
            get
            {
                return (TextSurface == null) ? base.FontSize : TextSurface.FontSize;
            }
            set
            {
                base.FontSize = value;
                if (TextSurface != null)
                {
                    TextSurface.FontSize = value;
                }
            }
        }

        public override Vector2 Position
        {
            get
            {
                return Vector2.Zero;
            }
            set
            {
                // ...
            }
        }

        public override Vector2 Size
        {
            get
            {
                return (TextSurface == null) ? base.Size : TextSurface.SurfaceSize;
            }
            set
            {
                // ...
            }
        }

        public override string Text
        {
            get
            {
                return (TextSurface == null) ? base.Text : TextSurface.GetText();
            }
            set
            {
                base.Text = value;
                TextSurface?.WriteText(value, false);
            }
        }

        public override IReadOnlyList<string> Sprites
        {
            get
            {
                IReadOnlyList<string> ret = sprites;
                if (sprites == null)
                {
                    if (TextSurface == null)
                    {
                        ret = Parent.Fonts;
                    }
                    else
                    {
                        TextSurface.GetSprites(spriteList);
                        sprites = spriteList.ToArray();
                        spriteList.Clear();
                        ret = sprites;
                    }
                }
                return ret;
            }
        }

        public Display(IMyTextSurface textSurface)
        {
            if (textSurface == null)
            {
                throw new ArgumentNullException(nameof(textSurface));
            }
            TextSurface = textSurface;
        }

        public override void Refresh(MySpriteDrawFrame? spriteDrawFrame)
        {
            if (TextSurface == null)
            {
                base.Refresh(spriteDrawFrame);
            }
            else
            {
                TextSurface.ContentType = ContentType.SCRIPT;
                using (MySpriteDrawFrame sprite_draw_frame = TextSurface.DrawFrame())
                {
                    base.Refresh(sprite_draw_frame);
                }
            }
        }

        public override void ResetBackgroundAlpha()
        {
            base.ResetBackgroundAlpha();
            if (TextSurface != null)
            {
                TextSurface.BackgroundAlpha = base.BackgroundAlpha;
            }
        }

        public override void ResetBackgroundColor()
        {
            base.ResetBackgroundColor();
            if (TextSurface != null)
            {
                TextSurface.BackgroundColor = base.BackgroundColor;
            }
        }

        public override void ResetFontSize()
        {
            base.ResetFontSize();
            if (TextSurface != null)
            {
                TextSurface.FontSize = base.FontSize;
            }
        }

        public override void ResetForegroundColor()
        {
            base.ResetForegroundColor();
            if (TextSurface != null)
            {
                TextSurface.FontColor = base.ForegroundColor;
            }
        }
    }
}
