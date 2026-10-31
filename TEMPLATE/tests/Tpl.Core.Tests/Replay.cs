using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tpl.Tests
{
    /// <summary>
    /// Reads, runs and writes inklib replay files (format "inklib-replay 1", Docs/REPLAY.md).
    /// The format is the same in every inklib; only the button and state names are this lib's.
    /// </summary>
    sealed class Replay
    {
        public sealed class Row
        {
            public int Line, Ticks;
            public PadInput Input;
            /// <summary>Expected x, y, z, yaw after the row; null = do not check.</summary>
            public double?[] Expect = new double?[4];
            public MoveState? State;
        }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly string[] Columns = { "ticks", "buttons", "move_x", "move_y", "look_x", "look_y", "|", "x", "y", "z", "yaw", "state" };

        public string Name = "";
        public string Source = "";
        public UnitMode Units = UnitMode.Native;
        public string World = "flat";
        public double[] Start = new double[4];
        public double[]? Origin;
        public double Tolerance = 0.01;
        public readonly List<(string Field, string Value)> TuningOverrides = new List<(string, string)>();
        public readonly List<Row> Rows = new List<Row>();

        public static Replay Load(string path)
        {
            var r = new Replay { Name = Path.GetFileNameWithoutExtension(path) };
            string[] lines = File.ReadAllLines(path);
            bool table = false, sawFormat = false, sawLib = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0) continue;
                string where = $"{path}:{i + 1}";

                if (!table)
                {
                    if (line.StartsWith("ticks", StringComparison.Ordinal))
                    {
                        string[] cols = Split(line);
                        if (!cols.SequenceEqual(Columns)) throw new FormatException($"{where}: table header must be: {string.Join(" ", Columns)}");
                        table = true;
                        continue;
                    }
                    int colon = line.IndexOf(':');
                    if (colon < 0) throw new FormatException($"{where}: expected 'key: value'");
                    string key = line.Substring(0, colon).Trim(), value = line.Substring(colon + 1).Trim();
                    if (!sawFormat && key != "format") throw new FormatException($"{where}: the first line must be 'format: inklib-replay 1'");
                    switch (key)
                    {
                        case "format":
                            if (value != "inklib-replay 1") throw new FormatException($"{where}: unknown format '{value}'");
                            sawFormat = true; break;
                        case "lib":
                            if (Split(value)[0] != "tpl") throw new FormatException($"{where}: replay is for lib '{value}', this is tpl");
                            sawLib = true; break;
                        case "name": r.Name = value; break;
                        case "source": r.Source = value; break;
                        case "units":
                            r.Units = value == "native" ? UnitMode.Native : value == "shared" ? UnitMode.Shared
                                : throw new FormatException($"{where}: units must be native or shared"); break;
                        case "world": r.World = value; break;
                        case "start": r.Start = Numbers(value, 4, where); break;
                        case "origin": r.Origin = Numbers(value, 3, where); break;
                        case "tolerance": r.Tolerance = double.Parse(value, Inv); break;
                        case "tuning":
                            string[] kv = Split(value);
                            if (kv.Length != 2) throw new FormatException($"{where}: tuning: Field value");
                            r.TuningOverrides.Add((kv[0], kv[1])); break;
                        default: throw new FormatException($"{where}: unknown key '{key}'");
                    }
                    continue;
                }

                string[] t = Split(line);
                if (t.Length != Columns.Length || t[6] != "|") throw new FormatException($"{where}: expected {Columns.Length} columns");
                var row = new Row { Line = i + 1, Ticks = int.Parse(t[0], Inv) };
                if (row.Ticks < 1) throw new FormatException($"{where}: ticks must be 1 or more");
                row.Input = new PadInput
                {
                    Buttons = ParseButtons(t[1], where),
                    MoveX = float.Parse(t[2], Inv), MoveY = float.Parse(t[3], Inv),
                    LookX = float.Parse(t[4], Inv), LookY = float.Parse(t[5], Inv),
                };
                for (int k = 0; k < 4; k++) row.Expect[k] = t[7 + k] == "-" ? (double?)null : double.Parse(t[7 + k], Inv);
                row.State = t[11] == "-" ? (MoveState?)null : (MoveState)Enum.Parse(typeof(MoveState), t[11]);
                r.Rows.Add(row);
            }
            if (!sawFormat || !sawLib) throw new FormatException($"{path}: needs 'format:' and 'lib:' lines");
            if (!table) throw new FormatException($"{path}: no table");
            return r;
        }

        static string[] Split(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        static double[] Numbers(string s, int count, string where)
        {
            string[] p = Split(s);
            if (p.Length != count) throw new FormatException($"{where}: expected {count} numbers");
            return p.Select(x => double.Parse(x, Inv)).ToArray();
        }

        static Buttons ParseButtons(string s, string where)
        {
            if (s == "0") return Buttons.None;
            if (s.StartsWith("0x", StringComparison.Ordinal)) return (Buttons)uint.Parse(s.Substring(2), NumberStyles.HexNumber, Inv);
            Buttons b = Buttons.None;
            foreach (string name in s.Split('+'))
            {
                if (!Enum.TryParse(name, true, out Buttons one)) throw new FormatException($"{where}: unknown button '{name}'");
                b |= one;
            }
            return b;
        }

        static string FormatButtons(Buttons b) =>
            b == Buttons.None ? "0" : string.Join("+", Enum.GetValues(typeof(Buttons)).Cast<Buttons>()
                .Where(x => x != Buttons.None && (b & x) == x).Select(x => x.ToString().ToLowerInvariant()));

        public Tuning MakeTuning()
        {
            var t = new Tuning();
            foreach (var (field, value) in TuningOverrides)
            {
                FieldInfo f = typeof(Tuning).GetField(field) ?? throw new FormatException($"{Name}: no Tuning.{field}");
                f.SetValue(t, Convert.ChangeType(value, f.FieldType, Inv));
            }
            return t;
        }

        public (World world, int id) MakeWorld()
        {
            ICollisionWorld col = TestWorld.Make(World);
            if (Units == UnitMode.Shared) col = new SharedHost(col);
            var w = new World(MakeTuning(), col, Units);
            int id = w.AddCharacter(new Vec3(Start[0], Start[1], Start[2]), Start[3]);
            return (w, id);
        }

        /// <summary>Runs the replay. Returns null if it matches, else what went wrong and where.</summary>
        public string? Run()
        {
            var (w, id) = MakeWorld();
            foreach (Row row in Rows)
            {
                w.SetInput(id, row.Input);
                w.Step(row.Ticks);
                w.GetState(id, out CharacterState s);
                double[] got = { s.Position.X, s.Position.Y, s.Position.Z, s.Yaw };
                string[] names = { "x", "y", "z", "yaw" };
                for (int k = 0; k < 4; k++)
                {
                    if (row.Expect[k] is not double e) continue;
                    if (k < 3 && Origin != null) e = e - Origin[k] + Start[k];
                    if (Math.Abs(got[k] - e) > Tolerance)
                        return $"{Name} line {row.Line}, tick {s.Tick}: {names[k]} is {got[k].ToString("R", Inv)}, expected {e.ToString("R", Inv)} (tolerance {Tolerance.ToString(Inv)})";
                }
                if (row.State is MoveState st && st != s.State)
                    return $"{Name} line {row.Line}, tick {s.Tick}: state is {s.State}, expected {st}";
            }
            return null;
        }

        /// <summary>
        /// Runs <paramref name="rows"/> (inputs only) and writes a regression replay with the positions
        /// the lib produces now. Only for UNVERIFIED regression files; never for measured ones.
        /// </summary>
        public string Record(string source)
        {
            var (w, id) = MakeWorld();
            var sb = new StringBuilder();
            sb.Append("format: inklib-replay 1\nlib: tpl 1\n");
            sb.Append($"name: {Name}\nsource: {source}\nunits: {(Units == UnitMode.Native ? "native" : "shared")}\nworld: {World}\n");
            sb.Append("start: " + string.Join(" ", Start.Select(F)) + "\n");
            sb.Append($"tolerance: {F(Tolerance)}\n");
            foreach (var (field, value) in TuningOverrides) sb.Append($"tuning: {field} {value}\n");
            sb.Append("\n" + string.Join(" ", Columns) + "\n");
            foreach (Row row in Rows)
            {
                w.SetInput(id, row.Input);
                w.Step(row.Ticks);
                w.GetState(id, out CharacterState s);
                PadInput p = row.Input;
                sb.Append($"{row.Ticks} {FormatButtons(p.Buttons)} {F(p.MoveX)} {F(p.MoveY)} {F(p.LookX)} {F(p.LookY)} | ");
                sb.Append($"{F(Math.Round(s.Position.X, 6))} {F(Math.Round(s.Position.Y, 6))} {F(Math.Round(s.Position.Z, 6))} {F(Math.Round(s.Yaw, 6))} {s.State}\n");
            }
            return sb.ToString();
        }

        static string F(double v) => (v == 0 ? 0.0 : v).ToString("0.######", Inv);
    }
}
