using System;
using Xunit;

namespace Tpl.Tests
{
    public class WorldTests
    {
        static readonly PadInput Forward = new PadInput { MoveY = 1 };
        static readonly PadInput JumpPad = new PadInput { Buttons = Buttons.Jump };

        static (World w, int id) Make(string world = "flat", UnitMode mode = UnitMode.Native, double y = 0)
        {
            ICollisionWorld col = TestWorld.Make(world);
            if (mode == UnitMode.Shared) col = new SharedHost(col);
            var w = new World(new Tuning(), col, mode);
            return (w, w.AddCharacter(mode == UnitMode.Native ? new Vec3(0, y, 0) : new Vec3(0, 0, y / 100), 0));
        }

        static CharacterState State(World w, int id)
        {
            Assert.True(w.GetState(id, out CharacterState s));
            return s;
        }

        [Fact]
        public void StartsOnTheFloor()
        {
            var (w, id) = Make();
            Assert.Equal(MoveState.Idle, State(w, id).State);
        }

        [Fact]
        public void FallsAndLands()
        {
            var (w, id) = Make(y: 100);
            Assert.Equal(MoveState.Fall, State(w, id).State);
            w.Step(60);
            CharacterState s = State(w, id);
            Assert.True(s.Grounded);
            Assert.Equal(0, s.Position.Y);
        }

        [Fact]
        public void WalksAtWalkSpeed()
        {
            var (w, id) = Make();
            w.SetInput(id, Forward);
            w.Step(60);
            CharacterState s = State(w, id);
            Assert.Equal(MoveState.Walk, s.State);
            Assert.Equal(new Tuning().WalkSpeed * (60 - new Tuning().InputDelayTicks), s.Position.X, 9);
        }

        [Fact]
        public void JumpApexMatchesTuning()
        {
            var t = new Tuning();
            var (w, id) = Make();
            w.SetInput(id, JumpPad);
            double top = 0;
            for (int i = 0; i < 120; i++) { w.Step(1); top = Math.Max(top, State(w, id).Position.Y); }
            // Discrete steps: v, v - g, v - 2g, ... summed while positive.
            double expect = 0, v = t.JumpSpeed;
            while (v > 0) { expect += v; v -= t.Gravity; }
            Assert.Equal(expect, top, 9);
            Assert.True(State(w, id).Grounded);
        }

        [Fact]
        public void HoldingJumpDoesNotJumpAgain()
        {
            var (w, id) = Make();
            w.SetInput(id, JumpPad);
            w.Step(200);
            Assert.Equal(MoveState.Idle, State(w, id).State);
        }

        [Fact]
        public void StepsUpSmallRisesButNotWalls()
        {
            var (w, id) = Make("step");
            w.SetInput(id, Forward);
            w.Step(200);
            CharacterState s = State(w, id);
            Assert.Equal(20, s.Position.Y);
            Assert.Equal(SurfaceFlags.Slide, s.GroundSurface);
            Assert.True(s.Position.X < 400 - new Tuning().BodyRadius + 1e-9, $"walked into the wall: x = {s.Position.X}");
        }

        [Fact]
        public void LookStickTurnsTheCameraAndTheWalk()
        {
            var (w, id) = Make();
            w.SetInput(id, new PadInput { LookX = -1 });          // turn the camera left
            w.Step((int)Math.Round(Math.PI / 2 / new Tuning().CameraTurnSpeed));
            w.SetInput(id, Forward);
            w.Step(60);
            CharacterState s = State(w, id);
            Assert.True(s.Position.Z < -200, $"expected to walk towards -Z (shared +Y), z = {s.Position.Z}");
        }

        [Fact]
        public void SharedUnitsGiveTheSameMotion()
        {
            var (wn, idn) = Make();
            var (ws, ids) = Make(mode: UnitMode.Shared);
            foreach (var (w, id) in new[] { (wn, idn), (ws, ids) })
            {
                w.SetInput(id, Forward);
                w.Step(30);
                w.SetInput(id, JumpPad);
                w.Step(20);
            }
            CharacterState n = State(wn, idn), s = State(ws, ids);
            Assert.Equal(n.Position.X / 100, s.Position.X, 9);
            Assert.Equal(n.Position.Y / 100, s.Position.Z, 9);
            Assert.Equal(n.Velocity.Y * 60 / 100, s.Velocity.Z, 9);
            Assert.Equal(n.State, s.State);
            Assert.True(ws.GetCamera(ids, out CameraView cam));
            Assert.Equal(new Vec3(0, 0, 1), cam.Up);
            Assert.Equal(CameraStyle.ThirdPerson, cam.Style);
        }

        [Fact]
        public void SameInputsGiveSameStates()
        {
            CharacterState Run()
            {
                var (w, id) = Make("step");
                for (int i = 0; i < 300; i++)
                {
                    w.SetInput(id, new PadInput { MoveX = (float)((i % 7) / 7.0), MoveY = 1, LookX = (i % 50) < 10 ? 1 : 0, Buttons = i % 40 == 0 ? Buttons.Jump : 0 });
                    w.Step(1);
                }
                return State(w, id);
            }
            CharacterState a = Run(), b = Run();
            Assert.Equal(a.Position, b.Position);
            Assert.Equal(a.Velocity, b.Velocity);
            Assert.Equal(a.Yaw, b.Yaw);
        }

        [Fact]
        public void EventsArePolledAndOverflowIsCounted()
        {
            var (w, id) = Make();
            w.SetInput(id, JumpPad);
            w.Step(1 + new Tuning().InputDelayTicks);
            var buf = new Event[16];
            int n = w.DrainEvents(buf);
            Assert.Contains(buf[..n], e => e.Type == (int)EventType.Sound && e.A == SoundIds.Jump);
            Assert.Contains(buf[..n], e => e.Type == (int)EventType.State && e.A == (int)MoveState.Idle && e.B == (int)MoveState.Jump);
            Assert.Equal(0, w.DrainEvents(buf));

            // Jump over and over without polling: the buffer fills and the rest are counted.
            for (int i = 0; i < 400; i++) { w.SetInput(id, i % 2 == 0 ? JumpPad : default); w.Step(40); }
            Assert.True(w.EventsDropped > 0);
            Assert.Equal(World.EventCapacity, w.DrainEvents(new Event[1000]));
        }

        [Fact]
        public void TheGameReadsThePadLateLikeTheOriginal()
        {
            int delay = new Tuning().InputDelayTicks;
            var w = new World(new Tuning(), new TestWorld(), UnitMode.Native);
            int late = w.AddCharacter(Vec3.Zero, 0);
            int now = w.AddCharacter(new Vec3(0, 0, 500), 0, inputDelay: false);
            w.SetInput(late, JumpPad);
            w.SetInput(now, JumpPad);
            for (int t = 1; t <= delay; t++)
            {
                w.Step(1);
                Assert.Equal(MoveState.Idle, State(w, late).State);
            }
            w.Step(1);
            Assert.Equal(MoveState.Jump, State(w, late).State);
            Assert.True(State(w, now).Position.Y > State(w, late).Position.Y, "the character without delay jumped first");
        }

        [Fact]
        public void InputDelayCanBeZeroInTuning()
        {
            var w = new World(new Tuning { InputDelayTicks = 0 }, new TestWorld(), UnitMode.Native);
            int id = w.AddCharacter(Vec3.Zero, 0);
            w.SetInput(id, JumpPad);
            w.Step(1);
            Assert.Equal(MoveState.Jump, State(w, id).State);
        }

        [Fact]
        public void CharactersComeAndGo()
        {
            var (w, id) = Make();
            int second = w.AddCharacter(new Vec3(500, 0, 0), 0, camera: false);
            Assert.Equal(id + 1, second);
            Assert.False(w.GetCamera(second, out _));
            Assert.True(w.RemoveCharacter(id));
            Assert.False(w.GetState(id, out _));
            Assert.False(w.RemoveCharacter(id));
            Assert.Equal(second + 1, w.AddCharacter(Vec3.Zero, 0));
        }

        [Fact]
        public void BadTuningIsRefused()
        {
            Assert.Throws<ArgumentException>(() => new World(new Tuning { TickRate = 0 }, new TestWorld(), UnitMode.Native));
            Assert.Throws<ArgumentException>(() => new World(new Tuning { Gravity = -1 }, new TestWorld(), UnitMode.Native));
            Assert.Throws<ArgumentException>(() => new World(new Tuning { TickSeconds = 0 }, new TestWorld(), UnitMode.Native));
            Assert.Throws<ArgumentException>(() => new World(new Tuning { InputDelayTicks = 9 }, new TestWorld(), UnitMode.Native));
        }
    }
}
