using OpenDotSpacePrograms.UI.Controls;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace OpenDotSpacePrograms.Programs.StorageInformation
{
    public class Program : MyGridProgram
    {
        /// <summary>
        /// This program shows storage information of this grid.
        /// 
        /// If you want a text panel to visualize storage, write `StorageInformation` into the custom data of the text panel.
        /// </summary>

        private sealed class StorageDisplay
        {
            private static readonly StringBuilder wStringBuilder = new StringBuilder("W");

            private readonly Display display;

            private readonly Label titleLabel;

            private readonly Label currentVolumeLabel;

            private readonly Label maximalVolumeLabel;

            private readonly Label currentMassLabel;

            private readonly ProgressBar volumeProgressBar;

            private MyFixedPoint lastCurrentVolume;

            private MyFixedPoint lastMaximalVolume;

            private MyFixedPoint lastCurrentMass;

            public StorageDisplay(IMyTextSurface textSurface)
            {
                display = new Display(textSurface);
                display.Font = "DarkBlue";
                display.BackgroundColor = Color.Black;
                Vector2 position = display.Size * 0.5f;
                Vector2 offset = textSurface.MeasureStringInPixels(wStringBuilder, display.Font, display.FontSize);
                float image_side_length = display.Size.Y * 0.375f;
                display.AddControl
                (
                    new Image
                    (
                        "StoreBlock2",
                        TextAlignment.CENTER,
                        display.Size * 0.5f,
                        new Vector2(image_side_length, image_side_length),
                        Color.White
                    )
                );
                titleLabel =
                    new Label
                    (
                        "Storage: 0%",
                        display.Font,
                        display.ForegroundColor,
                        display.FontSize,
                        TextAlignment.CENTER,
                        position + (Vector2.UnitY * offset * 2.0f)
                    );
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
                currentMassLabel =
                    new Label
                    (
                        "Storage mass: 0 t",
                        display.Font,
                        display.ForegroundColor,
                        display.FontSize,
                        TextAlignment.LEFT,
                        new Vector2(offset.X, position.Y + (offset.Y * 6.0f))
                    );
                display.AddControl(currentMassLabel);
                volumeProgressBar =
                    new ProgressBar
                    (
                        position + (Vector2.UnitY * (offset.Y * 8.0f)),
                        new Vector2(display.Size.X - (offset.X * 2.0f), offset.Y * 0.5f),
                        Color.Darken(Color.Cyan, 0.875f),
                        Color.Green,
                        Color.Darken(Color.Cyan, 0.75f),
                        offset.Y * 0.125f, 0.0f
                    );
                display.AddControl(volumeProgressBar);
            }

            public void UpdateValues(MyFixedPoint currentVolume, MyFixedPoint maximalVolume, MyFixedPoint currentMass)
            {
                if ((currentVolume != lastCurrentVolume) || (maximalVolume != lastMaximalVolume) || (currentMass != lastCurrentMass))
                {
                    titleLabel.Text = $"Storage: {((maximalVolume.RawValue > 0L) ? (currentVolume.RawValue * 100L / maximalVolume.RawValue) : 0L)}%";
                    currentVolumeLabel.Text = $"Current volume: {(currentVolume.RawValue / 1000L)} L";
                    maximalVolumeLabel.Text = $"Maximal volume: {(maximalVolume.RawValue / 1000L)} L";
                    currentMassLabel.Text = $"Storage mass: {(currentMass.RawValue / 1000000L)} t";
                    float progress = (maximalVolume.RawValue > 0L) ? ((float)currentVolume.RawValue / maximalVolume.RawValue) : 0.0f;
                    volumeProgressBar.Value = progress;
                    volumeProgressBar.ForegroundColor =
                        (progress < 0.5f) ? Color.Lerp(Color.Cyan, Color.Orange, progress * 2.0f) : Color.Lerp(Color.Orange, Color.Red, (progress - 0.5f) * 2.0f);
                    lastCurrentVolume = currentVolume;
                    lastMaximalVolume = maximalVolume;
                    lastCurrentMass = currentMass;
                    display.Refresh(null);
                }
            }
        }

        private static readonly string storageInformationCustomData = "storageinformation";

        private readonly Dictionary<long, StorageDisplay> storageDisplays = new Dictionary<long, StorageDisplay>();

        private readonly HashSet<long> missingStorageDisplays = new HashSet<long>();

        private readonly List<IMyTextPanel> textPanels = new List<IMyTextPanel>();

        private readonly List<IMyCargoContainer> cargoContainerBlocks = new List<IMyCargoContainer>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private void Main(string argument, UpdateType updateType)
        {
            MyFixedPoint current_volume = MyFixedPoint.Zero;
            MyFixedPoint maximal_volume = MyFixedPoint.Zero;
            MyFixedPoint current_mass = MyFixedPoint.Zero;
            textPanels.Clear();
            GridTerminalSystem.GetBlocksOfType(textPanels, (text_panel) => text_panel.CustomData.Trim().ToLower() == storageInformationCustomData);
            foreach (long key in storageDisplays.Keys)
            {
                missingStorageDisplays.Add(key);
            }
            foreach (IMyTextPanel text_panel in textPanels)
            {
                if (!missingStorageDisplays.Remove(text_panel.EntityId))
                {
                    storageDisplays.Add(text_panel.EntityId, new StorageDisplay(text_panel));
                }
            }
            foreach (long key in missingStorageDisplays)
            {
                storageDisplays.Remove(key);
            }
            missingStorageDisplays.Clear();
            cargoContainerBlocks.Clear();
            GridTerminalSystem.GetBlocksOfType(cargoContainerBlocks);
            foreach (IMyCargoContainer cargo_container_block in cargoContainerBlocks)
            {
                for (int i = 0; i < cargo_container_block.InventoryCount; i++)
                {
                    IMyInventory inventory = cargo_container_block.GetInventory();
                    current_volume += inventory.CurrentVolume;
                    maximal_volume += inventory.MaxVolume;
                    current_mass += inventory.CurrentMass;
                }
            }
            foreach (StorageDisplay storage_display in storageDisplays.Values)
            {
                storage_display.UpdateValues(current_volume, maximal_volume, current_mass);
            }
        }
    }
}
