using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Tpl.Native
{
    /// <summary>The tpl_* exports. Every function catches everything: an exception must never reach C.</summary>
    static unsafe class Exports
    {
        public const int ApiVersion = 1;

        [ThreadStatic] static string? _lastError;

        static int Fail(string message)
        {
            _lastError = message;
            return 0;
        }

        static World? Get(void* w) => w == null ? null : (World?)GCHandle.FromIntPtr((IntPtr)w).Target;

        /// <summary>
        /// Reads a versioned struct: min(size, sizeof(T)) bytes, the rest zero.
        /// In API version 1 every struct is at its first version, so the smallest size is sizeof(T).
        /// </summary>
        static bool ReadIn<T>(void* p, out T value, string name) where T : unmanaged
        {
            value = default;
            if (p == null) { Fail(name + " is NULL"); return false; }
            uint size = *(uint*)p;
            if (size < (uint)sizeof(T)) { Fail($"{name}.size is {size}, expected at least {sizeof(T)}"); return false; }
            fixed (T* v = &value) Buffer.MemoryCopy(p, v, sizeof(T), Math.Min(size, (uint)sizeof(T)));
            return true;
        }

        /// <summary>Writes a versioned struct: min(size, sizeof(T)) bytes, leaving the host's size field alone.</summary>
        static bool WriteOut<T>(void* p, T value, string name) where T : unmanaged
        {
            if (p == null) return Fail(name + " is NULL") != 0;
            uint size = *(uint*)p;
            if (size < (uint)sizeof(T)) return Fail($"{name}.size is {size}, expected at least {sizeof(T)}") != 0;
            *(uint*)&value = size;
            Buffer.MemoryCopy(&value, p, size, sizeof(T));
            return true;
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_api_version", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int ApiVersionFn() => ApiVersion;

        [UnmanagedCallersOnly(EntryPoint = "tpl_last_error", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int LastError(byte* buf, int size)
        {
            string e = _lastError ?? "";
            byte[] bytes = Encoding.UTF8.GetBytes(e);
            if (buf != null && size > 0)
            {
                int n = Math.Min(bytes.Length, size - 1);
                while (n > 0 && n < bytes.Length && (bytes[n] & 0xC0) == 0x80) n--; // do not cut a UTF-8 sequence
                Marshal.Copy(bytes, 0, (IntPtr)buf, n);
                buf[n] = 0;
            }
            return bytes.Length;
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_units", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int UnitsFn(CUnitsInfo* output)
        {
            var u = new CUnitsInfo
            {
                UpAxis = Units.UpAxis, UpSign = Units.UpSign, Handedness = Units.Handedness,
                VelocityPerTick = Units.VelocityPerTick, TickRate = new Tuning().TickRate,
                MetresPerUnit = Units.MetresPerUnit, RadiansPerUnit = Units.RadiansPerUnit,
                TickSeconds = new Tuning().TickSeconds, InputDelayTicks = new Tuning().InputDelayTicks, SimBits = Units.SimBits,
            };
            return WriteOut(output, u, "out") ? 1 : 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_world_create", CallConvs = new[] { typeof(CallConvCdecl) })]
        static void* WorldCreate(CWorldConfig* config, CCollision* collision)
        {
            try
            {
                CWorldConfig cfg = default;
                if (config != null && !ReadIn(config, out cfg, "config")) return null;
                if (cfg.Units != (int)UnitMode.Native && cfg.Units != (int)UnitMode.Shared) { Fail("config.units must be TPL_UNITS_NATIVE or TPL_UNITS_SHARED"); return null; }
                if (!ReadIn(collision, out CCollision col, "collision")) return null;
                if (col.Ground == null || col.Segment == null || col.Overlap == null) { Fail("collision needs ground, segment and overlap"); return null; }
                var world = new World(new Tuning(), new CallbackCollision(col), (UnitMode)cfg.Units);
                return (void*)GCHandle.ToIntPtr(GCHandle.Alloc(world));
            }
            catch (Exception e) { Fail(e.Message); return null; }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_world_destroy", CallConvs = new[] { typeof(CallConvCdecl) })]
        static void WorldDestroy(void* w)
        {
            if (w != null) GCHandle.FromIntPtr((IntPtr)w).Free();
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_char_add", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int CharAdd(void* w, CCharConfig* config)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (!ReadIn(config, out CCharConfig c, "config")) return 0;
                return world.AddCharacter(c.Position, c.Yaw, camera: (c.Flags & 1u) == 0, variant: c.Variant, inputDelay: (c.Flags & 2u) == 0);
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_char_remove", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int CharRemove(void* w, int id)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                return world.RemoveCharacter(id) ? 1 : Fail($"no character {id}");
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_char_input", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int CharInput(void* w, int id, CInput* input)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (!ReadIn(input, out CInput i, "input")) return 0;
                var pad = new PadInput { Buttons = (Buttons)i.Buttons, MoveX = i.MoveX, MoveY = i.MoveY, LookX = i.LookX, LookY = i.LookY };
                return world.SetInput(id, pad) ? 1 : Fail($"no character {id}");
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_world_tick", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int WorldTick(void* w, int ticks)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (ticks < 0) return Fail("ticks must be >= 0");
                world.Step(ticks);
                return 1;
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_char_state", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int CharState(void* w, int id, CCharState* output)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (!world.GetState(id, out CharacterState s)) return Fail($"no character {id}");
                var c = new CCharState
                {
                    State = (int)s.State, Anim = s.Anim, Grounded = s.Grounded ? 1 : 0, Tick = s.Tick,
                    Position = s.Position, Velocity = s.Velocity, Yaw = s.Yaw, GroundSurface = (uint)s.GroundSurface,
                };
                return WriteOut(output, c, "out") ? 1 : 0;
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_char_camera", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int CharCamera(void* w, int id, CCamera* output)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (!world.GetCamera(id, out CameraView v)) return Fail($"no camera for character {id}");
                var c = new CCamera { Style = (int)v.Style, Position = v.Position, Target = v.Target, Up = v.Up, FovY = v.FovY };
                return WriteOut(output, c, "out") ? 1 : 0;
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_events", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int Events(void* w, Event* output, int max)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (output == null || max <= 0) return 0;
                return world.DrainEvents(new Span<Event>(output, max));
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_events_dropped", CallConvs = new[] { typeof(CallConvCdecl) })]
        static long EventsDropped(void* w)
        {
            World? world = Get(w);
            return world == null ? 0 : world.EventsDropped;
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_state_size", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int StateSize(void* w)
        {
            World? world = Get(w);
            return world == null ? Fail("world is NULL") : world.StateSize;
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_save_state", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int SaveState(void* w, void* buf, int size)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (buf == null || size < world.StateSize) return Fail($"buffer needs {world.StateSize} bytes");
                return world.SaveState(new Span<byte>(buf, size));
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_load_state", CallConvs = new[] { typeof(CallConvCdecl) })]
        static int LoadState(void* w, void* buf, int size)
        {
            try
            {
                World? world = Get(w);
                if (world == null) return Fail("world is NULL");
                if (buf == null || size <= 0) return Fail("buf is NULL or empty");
                string? error = world.LoadState(new ReadOnlySpan<byte>(buf, size));
                return error == null ? 1 : Fail(error);
            }
            catch (Exception e) { return Fail(e.Message); }
        }

        [UnmanagedCallersOnly(EntryPoint = "tpl_state_hash", CallConvs = new[] { typeof(CallConvCdecl) })]
        static ulong StateHash(void* w)
        {
            try
            {
                World? world = Get(w);
                if (world == null) { Fail("world is NULL"); return 0; }
                return world.StateHash();
            }
            catch (Exception e) { Fail(e.Message); return 0; }
        }

        static CApi* _api;

        [UnmanagedCallersOnly(EntryPoint = "tpl_get_api", CallConvs = new[] { typeof(CallConvCdecl) })]
        static CApi* GetApi()
        {
            if (_api != null) return _api;
            var api = (CApi*)NativeMemory.AllocZeroed((nuint)sizeof(CApi));
            api->Size = (uint)sizeof(CApi);
            api->ApiVersion = ApiVersion;
            api->Prefix = (byte*)Marshal.StringToCoTaskMemUTF8("tpl");
            api->ApiVersionFn = &ApiVersionFn;
            api->LastError = &LastError;
            api->Units = &UnitsFn;
            api->WorldCreate = &WorldCreate;
            api->WorldDestroy = &WorldDestroy;
            api->CharAdd = &CharAdd;
            api->CharRemove = &CharRemove;
            api->CharInput = &CharInput;
            api->WorldTick = &WorldTick;
            api->CharState = &CharState;
            api->CharCamera = &CharCamera;
            api->Events = &Events;
            api->EventsDropped = &EventsDropped;
            api->StateSize = &StateSize;
            api->SaveState = &SaveState;
            api->LoadState = &LoadState;
            api->StateHash = &StateHash;
            _api = api; // two threads racing here leak one small table; both are valid
            return _api;
        }
    }
}
