using System;
using Xunit;

namespace Tpl.Tests
{
    /// <summary>Snapshots, hashes and event ids for rollback hosts (SPEC.md section 6).</summary>
    public class RollbackTests
    {
        static PadInput Pad(int i) => new PadInput
        {
            MoveX = (float)((i % 9) / 9.0 - 0.4), MoveY = 1, LookX = (i % 50) < 10 ? 0.7f : 0,
            Buttons = i % 37 == 0 ? Buttons.Jump : 0,
        };

        static World MakeWorld(out int a, out int b)
        {
            var w = new World(new Tuning(), TestWorld.Make("step"), UnitMode.Native);
            a = w.AddCharacter(new Vec3(0, 0, 0), 0);                                 // with the game's input delay
            b = w.AddCharacter(new Vec3(0, 50, 300), 1, camera: false, inputDelay: false);
            return w;
        }

        static void Run(World w, int a, int b, int from, int ticks)
        {
            for (int i = from; i < from + ticks; i++)
            {
                w.SetInput(a, Pad(i));
                w.SetInput(b, Pad(i * 3 + 1));
                w.Step(1);
            }
        }

        static byte[] Save(World w)
        {
            var buf = new byte[w.StateSize];
            Assert.Equal(buf.Length, w.SaveState(buf));
            return buf;
        }

        [Fact]
        public void SaveStepLoadStepGivesTheSameBits()
        {
            World w = MakeWorld(out int a, out int b);
            Run(w, a, b, 0, 50);
            byte[] snap = Save(w);
            Run(w, a, b, 50, 120);
            ulong first = w.StateHash();
            byte[] firstBytes = Save(w);

            Assert.Null(w.LoadState(snap));
            Assert.Equal(snap, Save(w));
            Run(w, a, b, 50, 120);
            Assert.Equal(first, w.StateHash());
            Assert.Equal(firstBytes, Save(w));
        }

        [Fact]
        public void ASnapshotLoadsIntoAFreshWorld()
        {
            World w = MakeWorld(out int a, out int b);
            Run(w, a, b, 0, 80);
            byte[] snap = Save(w);
            Run(w, a, b, 80, 60);

            var fresh = new World(new Tuning(), TestWorld.Make("step"), UnitMode.Native);
            Assert.Null(fresh.LoadState(snap));
            Run(fresh, a, b, 80, 60);
            Assert.Equal(w.StateHash(), fresh.StateHash());
            Assert.Equal(w.AddCharacter(Vec3.Zero, 0), fresh.AddCharacter(Vec3.Zero, 0)); // the id counter came along
        }

        [Fact]
        public void DifferentInputsGiveADifferentHash()
        {
            World w1 = MakeWorld(out int a, out int b), w2 = MakeWorld(out _, out _);
            Run(w1, a, b, 0, 30);
            Run(w2, a, b, 0, 30);
            Assert.Equal(w1.StateHash(), w2.StateHash());
            w2.SetInput(a, new PadInput { MoveX = 1 });
            w1.Step(1);
            w2.Step(1);
            Assert.NotEqual(w1.StateHash(), w2.StateHash());
        }

        [Fact]
        public void ResimulatedEventsKeepTheirIds()
        {
            World w = MakeWorld(out int a, out int b);
            Run(w, a, b, 0, 10);
            byte[] snap = Save(w);
            w.DrainEvents(new Event[1000]);
            Run(w, a, b, 10, 100);
            var first = new Event[1000];
            int n1 = w.DrainEvents(first);

            Assert.Null(w.LoadState(snap));
            Run(w, a, b, 10, 100);
            var again = new Event[1000];
            int n2 = w.DrainEvents(again);

            Assert.True(n1 > 0);
            Assert.Equal(n1, n2);
            for (int i = 0; i < n1; i++)
            {
                Assert.Equal(first[i].Id, again[i].Id);
                Assert.Equal(first[i].Tick, again[i].Tick);
                Assert.Equal(first[i].Type, again[i].Type);
            }
            for (int i = 1; i < n1; i++) Assert.True(first[i].Id > first[i - 1].Id, "ids rise and never repeat");
        }

        /// <summary>Inputs set but not yet read by the game are part of the snapshot.</summary>
        [Fact]
        public void DelayedInputsAreInTheSnapshot()
        {
            var w = new World(new Tuning { InputDelayTicks = 3 }, TestWorld.Make("flat"), UnitMode.Native);
            int id = w.AddCharacter(Vec3.Zero, 0);
            w.SetInput(id, new PadInput { MoveY = 1 });
            w.Step(2);                         // the pad is set but the game has not read it yet
            byte[] snap = Save(w);
            w.Step(10);
            ulong first = w.StateHash();
            Assert.Null(w.LoadState(snap));
            w.Step(10);
            Assert.Equal(first, w.StateHash());
            Assert.True(State(w, id).Position.X > 0);
        }

        static CharacterState State(World w, int id)
        {
            Assert.True(w.GetState(id, out CharacterState s));
            return s;
        }

        [Fact]
        public void LoadDropsPendingEvents()
        {
            World w = MakeWorld(out int a, out int b);
            byte[] snap = Save(w);
            Run(w, a, b, 0, 40);
            Assert.Null(w.LoadState(snap));
            Assert.Equal(0, w.DrainEvents(new Event[10]));
        }

        [Fact]
        public void BadSnapshotsAreRefusedAndChangeNothing()
        {
            World w = MakeWorld(out int a, out int b);
            Run(w, a, b, 0, 20);
            byte[] snap = Save(w);
            ulong before = w.StateHash();

            Assert.NotNull(w.LoadState(snap.AsSpan(0, snap.Length - 1)));
            Assert.NotNull(w.LoadState(new byte[3]));
            byte[] broken = (byte[])snap.Clone();
            broken[0] ^= 1;
            Assert.NotNull(w.LoadState(broken));
            var other = new World(new Tuning { WalkSpeed = 6 }, TestWorld.Make("step"), UnitMode.Native);
            Assert.NotNull(other.LoadState(snap));
            Assert.Equal(before, w.StateHash());
            Assert.Equal(0, w.SaveState(new byte[w.StateSize - 1]));
        }

        [Fact]
        public void SticksAreQuantisedLikeAPad()
        {
            Assert.Equal(0f, World.Quantise(0.003f));
            Assert.Equal(1f, World.Quantise(5f));
            Assert.Equal(-1f, World.Quantise(float.NegativeInfinity));
            Assert.Equal(0f, World.Quantise(float.NaN));
            Assert.Equal((float)(64 / 127.0), World.Quantise(0.5f));
        }
    }
}
