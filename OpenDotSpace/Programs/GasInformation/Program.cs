using OpenDotSpacePrograms.UI.Controls;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace OpenDotSpacePrograms.GasInformation
{
    internal class Program : MyGridProgram
    {
        /// <summary>
        /// This program shows gas information of this grid.
        /// 
        /// If you want a text panel to visualize gas, write `GasInformation` into the custom data of the text panel.
        /// </summary>

        private sealed class GasDisplay
        {
            private static readonly StringBuilder wStringBuilder = new StringBuilder("W");

            private readonly Display display;

            private readonly Label titleLabel;

            private readonly Label currentVolumeLabel;

            private readonly Label maximalVolumeLabel;

            private readonly ProgressBar volumeProgressBar;

            private double lastCurrentVolume;

            private float lastMaximalVolume;

            public GasDisplay(IMyTextSurface textSurface)
            {
                display = new Display(textSurface);
                display.Font = "DarkBlue";
                display.BackgroundColor = Color.Black;
                Vector2 position = display.Size * 0.5f;
                Vector2 offset = textSurface.MeasureStringInPixels(wStringBuilder, display.Font, display.FontSize);
                float image_side_length = display.Size.Y * 0.25f;
                display.AddControl
                (
                    new Image
                    (
                        "IconHydrogen",
                        TextAlignment.CENTER,
                        new Vector2((display.Size.X * 0.5f) - image_side_length, display.Size.Y * 0.5f),
                        new Vector2(image_side_length, image_side_length),
                        Color.Red
                    )
                );
                display.AddControl
                (
                    new Image
                    (
                        "IconOxygen",
                        TextAlignment.CENTER,
                        new Vector2((display.Size.X * 0.5f) + image_side_length, display.Size.Y * 0.5f),
                        new Vector2(image_side_length, image_side_length),
                        Color.Cyan
                    )
                );
                titleLabel =
                    new Label("Gas: 0%", display.Font, display.ForegroundColor, display.FontSize, TextAlignment.CENTER, position + (Vector2.UnitY * offset * 2.0f));
                display.AddControl(titleLabel);
                currentVolumeLabel =
                    new Label
                    (
                        "Current volume: 0 L",
                        display.Font,
                        display.ForegroundColor,
                        display.FontSize,
                        TextAlignment.LEFT,
                        new Vector2(offset.X, position.Y + (offset.Y * 4.0f))
                    );
                display.AddControl(currentVolumeLabel);
                maximalVolumeLabel =
                    new Label
                    (
                        "Maximal volume: 0 L",
                        display.Font,
                        display.ForegroundColor,
                        display.FontSize,
                        TextAlignment.LEFT,
                        new Vector2(offset.X, position.Y + (offset.Y * 5.0f))
                    );
                display.AddControl(maximalVolumeLabel);
                volumeProgressBar =
                    new ProgressBar
                    (
                        position + (Vector2.UnitY * (offset.Y * 8.0f)),
                        new Vector2(display.Size.X - (offset.X * 2.0f), offset.Y * 0.5f),
                        Color.Darken(Color.Cyan, 0.875f),
                        Color.Red,
                        Color.Darken(Color.Cyan, 0.75f),
                        offset.Y * 0.125f,
                        0.0f
                    );
                display.AddControl(volumeProgressBar);
            }

            public void UpdateValues(double currentVolume, float maximalVolume)
            {
                if ((currentVolume != lastCurrentVolume) || (maximalVolume != lastMaximalVolume))
                {
                    titleLabel.Text = $"Gas: {((Math.Abs(maximalVolume) > float.Epsilon) ? (currentVolume * 100.0 / maximalVolume) : 0L):N2}%";
                    currentVolumeLabel.Text = $"Current volume: {currentVolume:N2} L";
                    maximalVolumeLabel.Text = $"Maximal volume: {maximalVolume:N2} L";
                    float progress = (Math.Abs(maximalVolume) > float.Epsilon) ? (float)(currentVolume / maximalVolume) : 0.0f;
                    volumeProgressBar.Value = progress;
                    volumeProgressBar.ForegroundColor =
                        (progress < 0.5f) ? Color.Lerp(Color.Red, Color.Orange, progress * 2.0f) : Color.Lerp(Color.Orange, Color.Green, (progress - 0.5f) * 2.0f);
                    lastCurrentVolume = currentVolume;
                    lastMaximalVolume = maximalVolume;
                    display.Refresh(null);
                }
            }
        }

        private static readonly string gasInformationCustomData = "gasinformation";

        private readonly Dictionary<long, GasDisplay> storageDisplays = new Dictionary<long, GasDisplay>();

        private readonly HashSet<long> missingStorageDisplays = new HashSet<long>();

        private readonly List<IMyTextPanel> textPanels = new List<IMyTextPanel>();

        private readonly List<IMyGasTank> gasTanks = new List<IMyGasTank>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateType)
        {
            double current_volume = 0.0f;
            float maximal_volume = 0.0f;
            textPanels.Clear();
            GridTerminalSystem.GetBlocksOfType(textPanels, (text_panel) => text_panel.CustomData.Trim().ToLower() == gasInformationCustomData);
            foreach (long key in storageDisplays.Keys)
            {
                missingStorageDisplays.Add(key);
            }
            foreach (IMyTextPanel text_panel in textPanels)
            {
                if (!missingStorageDisplays.Remove(text_panel.EntityId))
                {
                    storageDisplays.Add(text_panel.EntityId, new GasDisplay(text_panel));
                }
            }
            foreach (long key in missingStorageDisplays)
            {
                storageDisplays.Remove(key);
            }
            missingStorageDisplays.Clear();
            gasTanks.Clear();
            GridTerminalSystem.GetBlocksOfType(gasTanks);
            foreach (IMyGasTank gas_tank in gasTanks)
            {
                current_volume += gas_tank.Capacity * gas_tank.FilledRatio;
                maximal_volume += gas_tank.Capacity;
            }
            foreach (GasDisplay storage_display in storageDisplays.Values)
            {
                storage_display.UpdateValues(current_volume, maximal_volume);
            }
        }
    }
}
