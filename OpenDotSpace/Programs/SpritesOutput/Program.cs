using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;

namespace OpenDotSpacePrograms.Programs.SpritesOutput
{
    internal sealed class Program : MyGridProgram
    {
        private readonly List<IMyTextPanel> textPanels = new List<IMyTextPanel>();

        private readonly List<string> sprites = new List<string>();

        public void Main(string argument, UpdateType updateType)
        {
            StringBuilder sprites_string_builder = new StringBuilder();
            GridTerminalSystem.GetBlocksOfType
            (
                textPanels,
                (textPanel) =>
                {
                    if (textPanel.CustomData.Trim().ToLowerInvariant() == "listsprites")
                    {
                        textPanel.GetSprites(sprites);
                        foreach (string sprite in sprites)
                        {
                            sprites_string_builder.AppendLine(sprite);
                        }
                        textPanel.WriteText(sprites_string_builder);
                        sprites_string_builder.Clear();
                    }
                    return false;
                }
            );
        }
    }
}
