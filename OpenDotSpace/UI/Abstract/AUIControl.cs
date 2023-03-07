using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace OpenDotSpacePrograms.UI
{
    public abstract class AUIControl
    {
        private readonly List<AUIControl> controls = new List<AUIControl>();

        private byte? backgroundAlpha;

        private Color? backgroundColor;

        private Color? borderColor;

        private float? borderSize;

        private string font;

        private float? fontSize;

        private Color? foregroundColor;

        private AUIControl parent;

        private string text = string.Empty;

        public virtual TextAlignment Alignment { get; set; }

        public virtual byte BackgroundAlpha
        {
            get
            {
                return (backgroundAlpha == null) ? ((parent == null) ? (byte)0xFF : parent.BackgroundAlpha) : backgroundAlpha.Value;
            }
            set
            {
                backgroundAlpha = value;
            }
        }

        public virtual Color BackgroundColor
        {
            get
            {
                return (backgroundColor == null) ? ((parent == null) ? Color.Black : parent.BackgroundColor) : backgroundColor.Value;
            }
            set
            {
                backgroundColor = value;
            }
        }

        public virtual Color BorderColor
        {
            get
            {
                return ((borderColor == null) ? Color.White : borderColor.Value);
            }
            set
            {
                borderColor = value;
            }
        }

        public virtual float BorderSize
        {
            get
            {
                return (borderSize == null) ? 0.0f : borderSize.Value;
            }
            set
            {
                borderSize = value;
            }
        }

        public virtual IReadOnlyList<AUIControl> Controls => controls;

        public virtual string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                text = value;
            }
        }

        public virtual string Font
        {
            get
            {
                return font ?? ((parent == null) ? "DEBUG" : parent.Font);
            }
            set
            {
                font = value;
            }
        }

        public virtual IReadOnlyList<string> Fonts => (parent == null) ? Array.Empty<string>() : parent.Fonts;

        public virtual float FontSize
        {
            get
            {
                return (fontSize == null) ? ((parent == null) ? 1.0f : parent.FontSize) : fontSize.Value;
            }
            set
            {
                fontSize = value;
            }
        }

        public virtual Color ForegroundColor
        {
            get
            {
                return (foregroundColor == null) ? ((parent == null) ? Color.Black : parent.ForegroundColor) : foregroundColor.Value;
            }
            set
            {
                foregroundColor = value;
            }
        }

        public AUIControl Parent => parent;

        public virtual Vector2 Position { get; set; }

        public virtual Vector2 Size { get; set; }

        public virtual float SpriteRotation { get; set; }

        public virtual SpriteType SpriteType { get; set; }

        public virtual IReadOnlyList<string> Sprites => (parent == null) ? Array.Empty<string>() : parent.Sprites;

        public bool AddControl(AUIControl control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }
            bool ret = (control != this) && (control.parent == null) && !controls.Contains(control);
            if (ret)
            {
                controls.Add(control);
                control.parent = this;
            }
            return ret;
        }

        public bool RemoveControl(AUIControl control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }
            bool ret = controls.Remove(control);
            if (ret)
            {
                control.parent = null;
            }
            return ret;
        }

        public virtual void Refresh(MySpriteDrawFrame? spriteDrawFrame)
        {
            foreach (AUIControl control in controls)
            {
                control?.Refresh(spriteDrawFrame);
            }
        }

        public virtual void ResetBackgroundAlpha()
        {
            backgroundAlpha = null;
        }

        public virtual void ResetBackgroundColor()
        {
            backgroundColor = null;
        }

        public virtual void ResetBorderColor()
        {
            borderColor = null;
        }

        public virtual void ResetBorderSize()
        {
            borderSize = null;
        }

        public virtual void ResetFontSize()
        {
            fontSize = null;
        }

        public virtual void ResetForegroundColor()
        {
            foregroundColor = null;
        }

        public virtual void ResetPosition()
        {
            Position = Vector2.Zero;
        }

        public virtual void ResetSize()
        {
            Size = Vector2.Zero;
        }
    }
}
