using DailyTasksReport.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DailyTasksReport.Tasks
{
    public class CropsTask : Task
    {
        private readonly ModConfig _config;
        private readonly CropsTaskId _id;
        private readonly int _index;
        private readonly string _locationName;
        private bool _anyCrop;
        private bool UnharvestedFlowersEnabled = false;

        // 0 = Farm, 1 = Greenhouse
        private static readonly List<Tuple<Vector2, HoeDirt>>[] Crops =
            {new List<Tuple<Vector2, HoeDirt>>(), new List<Tuple<Vector2, HoeDirt>>()};

        private static readonly List<Tuple<Vector2, FruitTree>>[] FruitTrees =
            {new List<Tuple<Vector2, FruitTree>>(), new List<Tuple<Vector2, FruitTree>>()};

        private static CropsTaskId _who = CropsTaskId.None;

        internal CropsTask(ModConfig config, CropsTaskId id)
        {
            _config = config;
            _id = id;

            if (id == CropsTaskId.UnwateredCropFarm || id == CropsTaskId.UnharvestedCropFarm ||
                id == CropsTaskId.DeadCropFarm || id == CropsTaskId.FruitTreesFarm)
            {
                _index = 0;
                _locationName = "Farm";
            }
            else
            {
                _index = 1;
                _locationName = "Greenhouse";
            }

            SettingsMenu.ReportConfigChanged += SettingsMenu_ReportConfigChanged;
        }

        private bool IsUnwatered(HoeDirt dirt)
        {
            return dirt.state.Value == HoeDirt.dry && dirt.needsWatering() && !IsDead(dirt);
        }

        private bool IsFlower(HoeDirt dirt)
        {
            return ObjectsCategory[dirt.crop.indexOfHarvest.Value] == StardewValley.Object.flowersCategory;
        }

        private bool IsUnharvested(HoeDirt dirt)
        {
            if (!dirt.readyForHarvest()) return false;
            if (!UnharvestedFlowersEnabled)
                return !IsFlower(dirt);
            return true;
        }

        private bool IsDead(HoeDirt dirt)
        {
            return dirt.crop.dead.Value;
        }

        private bool HasEnoughFruit(FruitTree tree)
        {
            return tree.fruitsOnTree.Value >= _config.FruitTrees;
        }

        private void SettingsMenu_ReportConfigChanged(object sender, EventArgs e)
        {
            ReReadConfig();
        }

        private void ReReadConfig()
        {
            switch (_id)
            {
                case CropsTaskId.UnwateredCropFarm:
                case CropsTaskId.UnwateredCropGreenhouse:
                    Enabled = _config.UnwateredCrops;
                    break;

                case CropsTaskId.UnharvestedCropFarm:
                case CropsTaskId.UnharvestedCropGreenhouse:
                    Enabled = _config.UnharvestedCrops;
                    break;

                case CropsTaskId.DeadCropFarm:
                case CropsTaskId.DeadCropGreenhouse:
                    Enabled = _config.DeadCrops;
                    break;

                case CropsTaskId.FruitTreesFarm:
                case CropsTaskId.FruitTreesGreenhouse:
                    Enabled = _config.FruitTrees > 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Crop task or location not implemented");
            }
            UnharvestedFlowersEnabled = _config.UnharvestedFlowers;
        }

        protected override void FirstScan()
        {
            if (_who == CropsTaskId.None)
                _who = _id;

            if (ObjectsNames.Count == 0)
                PopulateObjectsNames();

            if (_who != _id) return;

            GameLocation location = Game1.locations.OfType<Farm>().FirstOrDefault();
            foreach (var pair in location.terrainFeatures.Pairs)
                if (pair.Value is FruitTree tree && tree.fruitsOnTree.Value > 0)
                    FruitTrees[0].Add(new Tuple<Vector2, FruitTree>(pair.Key, tree));

            location = Game1.locations.FirstOrDefault(l => l.IsGreenhouse);
            foreach (var pair in location.terrainFeatures.Pairs)
                if (pair.Value is FruitTree tree && tree.fruitsOnTree.Value > 0)
                    FruitTrees[1].Add(new Tuple<Vector2, FruitTree>(pair.Key, tree));
        }

        public override string GeneralInfo(out int usedLines)
        {
            usedLines = 0;

            if (!Enabled) return "";

            _anyCrop = false;
            usedLines = 1;
            int count;

            if (Crops[_index].Count == 0)
            {
                var location = Game1.locations.FirstOrDefault(l => l.Name == _locationName);
                foreach (var keyValuePair in location.terrainFeatures.Pairs)
                    if (keyValuePair.Value is HoeDirt dirt && dirt.crop != null)
                        Crops[_index].Add(new Tuple<Vector2, HoeDirt>(keyValuePair.Key, dirt));
            }

            switch (_id)
            {
                case CropsTaskId.UnwateredCropFarm:
                case CropsTaskId.UnwateredCropGreenhouse:
                    count = Crops[_index].Count(pair => IsUnwatered(pair.Item2));
                    if (count > 0)
                    {
                        _anyCrop = true;
                        return $"{_locationName} crops not watered: {count}^";
                    }
                    break;

                case CropsTaskId.UnharvestedCropFarm:
                case CropsTaskId.UnharvestedCropGreenhouse:
                    count = Crops[_index].Count(pair => IsUnharvested(pair.Item2));
                    if (count > 0)
                    {
                        _anyCrop = true;
                        return $"{_locationName} crops ready to harvest: {count}^";
                    }
                    break;

                case CropsTaskId.DeadCropFarm:
                case CropsTaskId.DeadCropGreenhouse:
                    count = Crops[_index].Count(pair => IsDead(pair.Item2));
                    if (count > 0)
                    {
                        _anyCrop = true;
                        return $"Dead crops in the {_locationName}: {count}^";
                    }
                    break;

                case CropsTaskId.FruitTreesFarm:
                case CropsTaskId.FruitTreesGreenhouse:
                    count = FruitTrees[_index].Count(p => HasEnoughFruit(p.Item2));
                    if (count > 0)
                    {
                        _anyCrop = true;
                        return $"Fruit trees with fruits in the {_locationName}: {count}^";
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Crop task or location not implemented");
            }

            usedLines = 0;
            return "";
        }

        public override string DetailedInfo(out int usedLines, out bool skipNextPage)
        {
            usedLines = 0;
            skipNextPage = false;

            if (!Enabled || !_anyCrop) return "";

            var stringBuilder = new StringBuilder();

            switch (_id)
            {
                case CropsTaskId.UnwateredCropFarm:
                    stringBuilder.Append("Unwatered crops:^");
                    usedLines++;
                    skipNextPage = true;
                    break;

                case CropsTaskId.UnharvestedCropFarm:
                    stringBuilder.Append("Ready to harvest crops:^");
                    usedLines++;
                    skipNextPage = true;
                    break;

                case CropsTaskId.DeadCropFarm:
                    stringBuilder.Append("Dead crops:^");
                    usedLines++;
                    skipNextPage = true;
                    break;

                case CropsTaskId.FruitTreesFarm:
                    stringBuilder.Append("Fruit trees with fruits:^");
                    usedLines++;
                    skipNextPage = true;
                    break;

                default:
                    break;
            }

            switch (_id)
            {
                case CropsTaskId.UnwateredCropFarm:
                case CropsTaskId.UnwateredCropGreenhouse:
                    EchoForCrops(ref stringBuilder, ref usedLines, pair => IsUnwatered(pair.Item2));
                    break;

                case CropsTaskId.UnharvestedCropFarm:
                case CropsTaskId.UnharvestedCropGreenhouse:
                    EchoForCrops(ref stringBuilder, ref usedLines, pair => IsUnharvested(pair.Item2));
                    break;

                case CropsTaskId.DeadCropFarm:
                case CropsTaskId.DeadCropGreenhouse:
                    EchoForCrops(ref stringBuilder, ref usedLines, pair => IsDead(pair.Item2));
                    break;

                case CropsTaskId.FruitTreesFarm:
                case CropsTaskId.FruitTreesGreenhouse:
                    if (_config.FruitTrees == 0) break;
                    foreach (var tuple in FruitTrees[_index].Where(pair => HasEnoughFruit(pair.Item2)))
                    {
                        var s = (tuple.Item2.fruitsOnTree.Value > 1) ? "s" : "";
                        stringBuilder.Append(
                            $"{ObjectsNames[tuple.Item2.indexOfFruit.Value]} tree at {_locationName} with {tuple.Item2.fruitsOnTree} fruit{s} ({tuple.Item1.X}, {tuple.Item1.Y})^");
                        usedLines++;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Crop task or location not implemented");
            }

            return stringBuilder.ToString();
        }

        private void EchoForCrops(ref StringBuilder stringBuilder, ref int count,
            Func<Tuple<Vector2, HoeDirt>, bool> predicate)
        {
            foreach (var item in Crops[_index].Where(predicate))
            {
                stringBuilder.Append(
                    $"{ObjectsNames[item.Item2.crop.indexOfHarvest.Value]} at {_locationName} ({item.Item1.X}, {item.Item1.Y})^");
                count++;
            }
        }

        public override void FinishedReport()
        {
            Crops[_index].Clear();
        }

        public override void Draw(SpriteBatch b)
        {
            if (!(Game1.currentLocation is Farm) && Game1.currentLocation.IsGreenhouse || _who != _id) return;

            var x = Game1.viewport.X / Game1.tileSize;
            var xLimit = (Game1.viewport.X + Game1.viewport.Width) / Game1.tileSize;
            var yStart = Game1.viewport.Y / Game1.tileSize;
            var yLimit = (Game1.viewport.Y + Game1.viewport.Height) / Game1.tileSize + 1;
            for (; x <= xLimit; ++x)
                for (var y = yStart; y <= yLimit; ++y)
                    if (Game1.currentLocation.terrainFeatures.TryGetValue(new Vector2(x, y), out var t) &&
                        t is HoeDirt dirt && dirt.crop != null)
                    {
                        var v = new Vector2(x * Game1.tileSize - Game1.viewport.X + Game1.tileSize / 8,
                            y * Game1.tileSize - Game1.viewport.Y - Game1.tileSize * 2 / 4);

                        if (IsDead(dirt) && _config.DrawBubbleDeadCrops)
                            DrawBubble(b, Game1.mouseCursors, new Rectangle(269, 471, 14, 15), v);
                        else if (IsUnharvested(dirt) && _config.DrawBubbleUnharvestedCrops)
                            DrawBubble(b, Game1.mouseCursors, new Rectangle(32, 0, 10, 10), v);
                        else if (IsUnwatered(dirt) && _config.DrawBubbleUnwateredCrops)
                            DrawBubble(b, Game1.toolSpriteSheet, new Rectangle(49, 226, 15, 13), v);
                    }
        }

        public override void Clear()
        {
            if (_who == _id)
            {
                Crops[0].Clear();
                Crops[1].Clear();
                FruitTrees[0].Clear();
                FruitTrees[1].Clear();
            }

            ReReadConfig();
        }
    }

    public enum CropsTaskId
    {
        None = -1,
        UnwateredCropFarm = 0,
        UnwateredCropGreenhouse = 1,
        UnharvestedCropFarm = 2,
        UnharvestedCropGreenhouse = 3,
        DeadCropFarm = 4,
        DeadCropGreenhouse = 5,
        FruitTreesFarm = 6,
        FruitTreesGreenhouse = 7
    }
}