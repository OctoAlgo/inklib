/*
 * tpl.h - C API of libtpl, an inklib (see the inklib SPEC.md).
 *
 * Native units (the game's own; this template's are an example):
 *   length 1 unit = 1 cm, angles in radians, Y up, right-handed,
 *   velocity in units per tick, yaw 0 = +X, forward = (cos yaw, 0, -sin yaw).
 * Shared units (TPL_UNITS_SHARED): metres, seconds, radians, Z up, right-handed,
 *   yaw 0 = +X turning towards +Y, velocity in metres per second.
 * The world config picks one. Every position, direction, velocity and angle that
 * crosses this API, including the collision callbacks, is then in that convention.
 * Field of view is always radians. Sticks are always -1..1.
 *
 * Time: the host calls tpl_world_tick TPL_TICK_RATE times per real second. Each tick simulates
 * exactly TPL_TICK_SECONDS of game time: the game's real step, which need not be 1 / rate
 * (Rayman 3 PC: 60 Hz, 0.016 s, so the game runs at 96 % of real time). Shared-unit
 * velocities are per second of game time. tpl_world_tick(w, n) steps n ticks.
 * Input: the game reads the pad TPL_INPUT_DELAY_TICKS ticks after tpl_char_input sets it,
 * like the original. TPL_CHAR_NO_INPUT_DELAY turns that off for one character.
 * The simulation computes in TPL_SIM_BITS reals, as the game did; the API always uses double.
 * One world is used from one thread at a time. Worlds share nothing.
 * Functions returning int32_t return 1 (or an id) on success, 0 on error;
 * tpl_last_error() then says why.
 *
 * Versioned structs start with uint32_t size: set it to sizeof(the struct).
 * The lib reads and writes only min(size, its own size) bytes; missing fields are zero.
 *
 * Collision callbacks needed: ground, segment, overlap. sweep is not used (may be NULL).
 *
 * Rollback: tpl_save_state / tpl_load_state snapshot the whole world (characters, cameras,
 * inputs including the delayed ones, tick). libtpl keeps no world state of its own (no store). tpl_state_hash checks two machines agree. The lib keeps no history and these
 * cost nothing unless called. The simulation gives the same bits on every machine (no platform
 * maths), and sticks are quantised to steps of 1/127.
 */
#ifndef TPL_H
#define TPL_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define TPL_API_VERSION 1

/* Native units. Same as tpl_units() and Tpl.Units. */
#define TPL_NATIVE_METRES_PER_UNIT   0.01
#define TPL_NATIVE_RADIANS_PER_UNIT  1.0
#define TPL_NATIVE_UP_AXIS           1   /* 0 = X, 1 = Y, 2 = Z */
#define TPL_NATIVE_UP_SIGN           1   /* +1 or -1 */
#define TPL_NATIVE_HANDEDNESS        1   /* +1 right-handed, -1 left-handed */
#define TPL_NATIVE_VELOCITY_PER_TICK 1   /* 1: units per tick, 0: units per second */
#define TPL_TICK_RATE                60  /* ticks per real second the host calls (pacing) */
#define TPL_TICK_SECONDS             0.016666666666666666 /* game time one tick simulates, exact */
#define TPL_INPUT_DELAY_TICKS        1   /* ticks before the game reads a new pad */
#define TPL_TOUCH_TOLERANCE          0.5 /* native length units: closer than this counts as touching (SPEC.md section 4) */
#define TPL_SIM_BITS                 64  /* the simulation's reals: 32 = float32, 64 = double, 0 = integer/fixed */

enum {
    TPL_UNITS_NATIVE = 0,
    TPL_UNITS_SHARED = 1,
};

/* Surface flags. Bits 0..15 are the same in every inklib; 16..31 are this lib's own. */
enum {
    TPL_SURFACE_NO_COLLISION = 1u << 0,
    TPL_SURFACE_WATER        = 1u << 1,
    TPL_SURFACE_HURT         = 1u << 2,
    TPL_SURFACE_DEATH        = 1u << 3,
    TPL_SURFACE_SLIDE        = 1u << 4,
    TPL_SURFACE_LEDGE        = 1u << 5,
    TPL_SURFACE_CLIMB        = 1u << 6,
    TPL_SURFACE_HANG         = 1u << 7,
    TPL_SURFACE_BOUNCE       = 1u << 8,
    TPL_SURFACE_CAMERA_PASS  = 1u << 9,
};

/* tpl_collision.segment flags. */
enum {
    TPL_SEGMENT_SKIP_FLOORS = 1u << 0, /* ignore surfaces facing up */
    TPL_SEGMENT_CAMERA      = 1u << 1, /* the camera asks: ignore TPL_SURFACE_CAMERA_PASS */
};

/* tpl_shape.kind */
enum {
    TPL_SHAPE_SPHERE  = 1, /* a = centre, radius */
    TPL_SHAPE_CAPSULE = 2, /* a, b = ends of the core segment, radius */
    TPL_SHAPE_BOX     = 3, /* a = centre, half_extents along the world axes */
};

/* Pad buttons: the game's pad. */
enum {
    TPL_BUTTON_JUMP = 1u << 0,
};

/* Movement states (Tpl.MoveState). */
enum {
    TPL_STATE_IDLE = 0,
    TPL_STATE_WALK = 1,
    TPL_STATE_JUMP = 2,
    TPL_STATE_FALL = 3,
};

/* Camera styles. */
enum {
    TPL_CAMERA_FIRST_PERSON = 1,
    TPL_CAMERA_THIRD_PERSON = 2,
    TPL_CAMERA_CHASE        = 3,
    TPL_CAMERA_TOP_DOWN     = 4,
    TPL_CAMERA_FIXED        = 5,
};

/* Event types. 1..255 are the same in every inklib; 256 and up are this lib's own. */
enum {
    TPL_EVENT_STATE        = 1, /* a = old state, b = new state */
    TPL_EVENT_LAND         = 2,
    TPL_EVENT_LEAVE_GROUND = 3,
    TPL_EVENT_SOUND        = 4, /* a = sound id; the host plays it */
};

/* Sound ids (TPL_EVENT_SOUND.a). */
enum {
    TPL_SOUND_JUMP = 1,
};

/* tpl_char_config.flags */
enum {
    TPL_CHAR_NO_CAMERA      = 1u << 0,
    TPL_CHAR_NO_INPUT_DELAY = 1u << 1, /* read the pad on the next tick, not the game's delay */
};

/* ---- small fixed structs (change only with TPL_API_VERSION) ---- */

typedef struct tpl_vec3 { double x, y, z; } tpl_vec3;

typedef struct tpl_hit {
    tpl_vec3 point;   /* contact point */
    tpl_vec3 normal;  /* unit, pointing back at the query */
    double t;         /* 0..1, how far along the query */
    uint32_t surface; /* TPL_SURFACE_* */
    int32_t actor;    /* host id of what was hit, 0 = level geometry */
} tpl_hit;

typedef struct tpl_shape {
    int32_t kind;     /* TPL_SHAPE_* */
    uint32_t reserved;
    tpl_vec3 a;
    tpl_vec3 b;
    double radius;
    tpl_vec3 half_extents;
} tpl_shape;

typedef struct tpl_event {
    int64_t id;       /* unique in the world; the same when the tick is simulated again (rollback) */
    int64_t tick;     /* the tick the event belongs to */
    int32_t type;     /* TPL_EVENT_* */
    int32_t char_id;
    int32_t a, b;
    tpl_vec3 position;
} tpl_event;

/* ---- versioned structs (start with size) ---- */

typedef struct tpl_units_info {
    uint32_t size;
    int32_t up_axis;
    int32_t up_sign;
    int32_t handedness;
    int32_t velocity_per_tick;
    int32_t tick_rate;
    double metres_per_unit;
    double radians_per_unit;
    double tick_seconds;       /* TPL_TICK_SECONDS */
    int32_t input_delay_ticks; /* TPL_INPUT_DELAY_TICKS */
    int32_t sim_bits;          /* TPL_SIM_BITS */
} tpl_units_info;

/*
 * Collision. The host answers in the world's unit convention, never reports the
 * character's own body, and returns 1 and fills *out on a hit, else 0.
 * user must stay valid until tpl_world_destroy.
 */
typedef struct tpl_collision {
    uint32_t size;
    void *user;
    /* First walkable ground straight down from *from, within max_drop. */
    int32_t (*ground)(void *user, const tpl_vec3 *from, double max_drop, tpl_hit *out);
    /* First hit on the line a -> b. flags: TPL_SEGMENT_*. */
    int32_t (*segment)(void *user, const tpl_vec3 *a, const tpl_vec3 *b, uint32_t flags, tpl_hit *out);
    /* Does *shape touch solid geometry? out->normal points out of the geometry. */
    int32_t (*overlap)(void *user, const tpl_shape *shape, tpl_hit *out);
    /* Optional: first hit while moving *shape by *move. Not used by libtpl. */
    int32_t (*sweep)(void *user, const tpl_shape *shape, const tpl_vec3 *move, tpl_hit *out);
} tpl_collision;

typedef struct tpl_world_config {
    uint32_t size;
    int32_t units;          /* TPL_UNITS_* */
    const char *data_path;  /* UTF-8 path to the user's own game data; libtpl needs none (NULL) */
} tpl_world_config;

typedef struct tpl_char_config {
    uint32_t size;
    uint32_t flags;         /* TPL_CHAR_* */
    tpl_vec3 position;      /* feet */
    double yaw;
    int32_t variant;        /* 0 = default */
    int32_t reserved;
} tpl_char_config;

typedef struct tpl_input {
    uint32_t size;
    uint32_t buttons;       /* TPL_BUTTON_* held */
    float move_x, move_y;   /* move stick, -1..1, +y = up; camera-relative like the game */
    float look_x, look_y;   /* look stick, -1..1; +x turns the camera right */
} tpl_input;

typedef struct tpl_state {
    uint32_t size;
    int32_t state;          /* TPL_STATE_* */
    int32_t anim;           /* the game's animation id, -1 = none */
    int32_t grounded;       /* 1 on walkable ground */
    int64_t tick;           /* ticks stepped by the world */
    tpl_vec3 position;      /* feet */
    tpl_vec3 velocity;
    double yaw;             /* facing */
    uint32_t ground_surface;/* TPL_SURFACE_* of the ground, 0 in the air */
    uint32_t reserved;
} tpl_state;

typedef struct tpl_camera {
    uint32_t size;
    int32_t style;          /* TPL_CAMERA_* */
    tpl_vec3 position;
    tpl_vec3 target;        /* a point the camera looks at */
    tpl_vec3 up;            /* unit; carries the roll */
    double fov_y;           /* vertical field of view, radians, in both unit modes */
} tpl_camera;

typedef struct tpl_world tpl_world;

/* Function table, for hosts that load the library at run time. */
typedef struct tpl_api {
    uint32_t size;
    int32_t api_version;
    const char *prefix;     /* "tpl" */
    int32_t (*api_version_fn)(void);
    int32_t (*last_error)(char *buf, int32_t size);
    int32_t (*units)(tpl_units_info *out);
    tpl_world *(*world_create)(const tpl_world_config *config, const tpl_collision *collision);
    void (*world_destroy)(tpl_world *world);
    int32_t (*char_add)(tpl_world *world, const tpl_char_config *config);
    int32_t (*char_remove)(tpl_world *world, int32_t id);
    int32_t (*char_input)(tpl_world *world, int32_t id, const tpl_input *input);
    int32_t (*world_tick)(tpl_world *world, int32_t ticks);
    int32_t (*char_state)(const tpl_world *world, int32_t id, tpl_state *out);
    int32_t (*char_camera)(const tpl_world *world, int32_t id, tpl_camera *out);
    int32_t (*events)(tpl_world *world, tpl_event *out, int32_t max);
    int64_t (*events_dropped)(const tpl_world *world);
    int32_t (*state_size)(const tpl_world *world);
    int32_t (*save_state)(const tpl_world *world, void *buf, int32_t size);
    int32_t (*load_state)(tpl_world *world, const void *buf, int32_t size);
    uint64_t (*state_hash)(const tpl_world *world);
} tpl_api;

/* Returns TPL_API_VERSION of the built library. */
int32_t tpl_api_version(void);
/* Copies this thread's last error (UTF-8, NUL-terminated, cut to size). Returns its full
   length without the NUL, 0 if none. buf may be NULL to ask for the length. */
int32_t tpl_last_error(char *buf, int32_t size);
/* Fills the native units. */
int32_t tpl_units(tpl_units_info *out);
/* Makes a world. config may be NULL for native units. collision is copied. NULL on error. */
tpl_world *tpl_world_create(const tpl_world_config *config, const tpl_collision *collision);
void tpl_world_destroy(tpl_world *world);
/* Adds the character. Returns its id (1 or more), 0 on error. Ids are never reused. */
int32_t tpl_char_add(tpl_world *world, const tpl_char_config *config);
int32_t tpl_char_remove(tpl_world *world, int32_t id);
/* Sets the pad. It stays until the next call. */
int32_t tpl_char_input(tpl_world *world, int32_t id, const tpl_input *input);
/* Steps ticks fixed ticks (0 or more). */
int32_t tpl_world_tick(tpl_world *world, int32_t ticks);
int32_t tpl_char_state(const tpl_world *world, int32_t id, tpl_state *out);
/* Returns 0 if the character was added with TPL_CHAR_NO_CAMERA. */
int32_t tpl_char_camera(const tpl_world *world, int32_t id, tpl_camera *out);
/* Moves up to max events out of the buffer, oldest first. Returns how many (0 if empty). */
int32_t tpl_events(tpl_world *world, tpl_event *out, int32_t max);
/* Events lost because the buffer (256) was full, since create. */
int64_t tpl_events_dropped(const tpl_world *world);
/* Bytes tpl_save_state needs now (changes when characters are added or removed). 0 on error. */
int32_t tpl_state_size(const tpl_world *world);
/* Writes a snapshot of the whole world into buf. Returns the bytes written, 0 if size is too small.
   The event buffer is not included. The snapshot only loads into a world of the same lib build. */
int32_t tpl_save_state(const tpl_world *world, void *buf, int32_t size);
/* Puts the world back to a snapshot. Pending events are dropped; ticks simulated again send their
   events again with the same ids. On error the world is unchanged. */
int32_t tpl_load_state(tpl_world *world, const void *buf, int32_t size);
/* 64-bit hash of the snapshot (FNV-1a). Equal on two machines = same world. 0 on error. */
uint64_t tpl_state_hash(const tpl_world *world);
/* The function table. Never NULL. */
const tpl_api *tpl_get_api(void);

#ifdef __cplusplus
}
#endif

#endif /* TPL_H */
