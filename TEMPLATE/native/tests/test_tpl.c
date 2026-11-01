/* C test of the tpl_* API against a flat floor, in plain C99. */
#include <math.h>
#include <stdio.h>
#include <string.h>
#include "tpl.h"
#include "../flat_floor.h"

static int failures = 0;
#define CHECK(cond) do { if (!(cond)) { printf("FAIL %s:%d: %s\n", __FILE__, __LINE__, #cond); failures++; } } while (0)

static tpl_input pad(uint32_t buttons, float mx, float my)
{
    tpl_input in;
    memset(&in, 0, sizeof in);
    in.size = sizeof in;
    in.buttons = buttons;
    in.move_x = mx;
    in.move_y = my;
    return in;
}

int main(void)
{
    char err[256];
    int native_up = TPL_NATIVE_UP_AXIS, shared_up = 2;
    tpl_collision col = flat_floor(&native_up);
    tpl_world_config wc = { sizeof(tpl_world_config), TPL_UNITS_NATIVE, NULL };
    tpl_char_config cc;
    tpl_state s;
    tpl_camera cam;
    tpl_event ev[64];
    tpl_input in;
    tpl_units_info u;
    int32_t id, n, i, saw_land = 0;

    CHECK(tpl_api_version() == TPL_API_VERSION);

    /* Units say what the header says. */
    memset(&u, 0, sizeof u);
    u.size = sizeof u;
    CHECK(tpl_units(&u) == 1);
    CHECK(u.size == sizeof u);
    CHECK(u.up_axis == TPL_NATIVE_UP_AXIS && u.tick_rate == TPL_TICK_RATE);
    CHECK(u.metres_per_unit == TPL_NATIVE_METRES_PER_UNIT);
    CHECK(u.tick_seconds == TPL_TICK_SECONDS && u.input_delay_ticks == TPL_INPUT_DELAY_TICKS && u.sim_bits == TPL_SIM_BITS);

    /* Errors are errors, not crashes, and they say why. */
    CHECK(tpl_world_create(&wc, NULL) == NULL);
    CHECK(tpl_last_error(NULL, 0) > 0);
    CHECK(tpl_last_error(err, sizeof err) > 0 && strlen(err) > 0);
    {
        tpl_world_config small = wc;
        small.size = 4;
        CHECK(tpl_world_create(&small, &col) == NULL);
    }
    CHECK(tpl_char_add(NULL, NULL) == 0);
    CHECK(tpl_world_tick(NULL, 1) == 0);

    tpl_world *w = tpl_world_create(&wc, &col);
    CHECK(w != NULL);
    if (!w) return 1;

    memset(&cc, 0, sizeof cc);
    cc.size = sizeof cc;
    cc.position.y = 100.0; /* 1 m above the floor */
    id = tpl_char_add(w, &cc);
    CHECK(id == 1);
    CHECK(tpl_char_state(w, 99, &s) == 0);

    /* Falls and lands. */
    memset(&s, 0, sizeof s);
    s.size = sizeof s;
    CHECK(tpl_world_tick(w, 60) == 1);
    CHECK(tpl_char_state(w, id, &s) == 1);
    CHECK(s.size == sizeof s);
    CHECK(s.grounded == 1 && s.state == TPL_STATE_IDLE);
    CHECK(fabs(s.position.y) < 1e-9);
    CHECK(s.tick == 60);

    n = tpl_events(w, ev, 64);
    CHECK(n > 0);
    for (i = 0; i < n; i++) if (ev[i].type == TPL_EVENT_LAND) saw_land = 1;
    CHECK(saw_land);
    CHECK(tpl_events(w, ev, 64) == 0);
    CHECK(tpl_events_dropped(w) == 0);

    /* Stick up walks the way the camera looks (+X at yaw 0). */
    in = pad(0, 0.0f, 1.0f);
    CHECK(tpl_char_input(w, id, &in) == 1);
    tpl_world_tick(w, 30);
    tpl_char_state(w, id, &s);
    CHECK(s.state == TPL_STATE_WALK);
    CHECK(s.position.x > 100.0);

    /* Jump. */
    in = pad(TPL_BUTTON_JUMP, 0.0f, 0.0f);
    tpl_char_input(w, id, &in);
    tpl_world_tick(w, 1 + TPL_INPUT_DELAY_TICKS); /* the game reads the pad late, like the original */
    tpl_char_state(w, id, &s);
    CHECK(s.state == TPL_STATE_JUMP && s.velocity.y > 0.0);

    /* Camera. */
    memset(&cam, 0, sizeof cam);
    cam.size = sizeof cam;
    CHECK(tpl_char_camera(w, id, &cam) == 1);
    CHECK(cam.style == TPL_CAMERA_THIRD_PERSON);
    CHECK(cam.fov_y > 0.0 && cam.up.y == 1.0);
    CHECK(cam.position.x < s.position.x);

    /* Rollback: save, step, load, step the same again: same hash, same events. */
    {
        static unsigned char snap[4096];
        int32_t size = tpl_state_size(w), written;
        uint64_t h1, h2;
        tpl_event e1[64], e2[64];
        int32_t n1, n2;
        CHECK(size > 0 && size <= (int32_t)sizeof snap);
        CHECK(tpl_save_state(w, snap, size - 1) == 0);
        written = tpl_save_state(w, snap, sizeof snap);
        CHECK(written == size);
        tpl_events(w, ev, 64);                      /* empty the buffer */
        in = pad(0, 0.5f, 1.0f);
        tpl_char_input(w, id, &in);
        tpl_world_tick(w, 90);
        h1 = tpl_state_hash(w);
        n1 = tpl_events(w, e1, 64);
        CHECK(tpl_load_state(w, snap, written) == 1);
        tpl_char_input(w, id, &in);
        tpl_world_tick(w, 90);
        h2 = tpl_state_hash(w);
        n2 = tpl_events(w, e2, 64);
        CHECK(h1 != 0 && h1 == h2);
        CHECK(n1 > 0 && n1 == n2);
        for (i = 0; i < n1 && i < n2; i++) CHECK(e1[i].id == e2[i].id && e1[i].tick == e2[i].tick && e1[i].type == e2[i].type);
        snap[0] ^= 0xFF;                            /* a broken snapshot is refused */
        CHECK(tpl_load_state(w, snap, written) == 0);
        CHECK(tpl_state_hash(w) == h2);
    }

    /* Removed characters are gone; ids are not reused. */
    CHECK(tpl_char_remove(w, id) == 1);
    CHECK(tpl_char_state(w, id, &s) == 0);
    CHECK(tpl_char_add(w, &cc) == 2);
    tpl_world_destroy(w);

    /* Shared units: the same character in metres, Z up. */
    {
        tpl_world_config sc = { sizeof(tpl_world_config), TPL_UNITS_SHARED, NULL };
        tpl_collision zcol = flat_floor(&shared_up);
        tpl_world *ws = tpl_world_create(&sc, &zcol);
        CHECK(ws != NULL);
        if (ws) {
            memset(&cc, 0, sizeof cc);
            cc.size = sizeof cc;
            cc.position.z = 1.0;
            id = tpl_char_add(ws, &cc);
            in = pad(0, 0.0f, 1.0f);
            tpl_char_input(ws, id, &in);
            tpl_world_tick(ws, 120);
            memset(&s, 0, sizeof s);
            s.size = sizeof s;
            tpl_char_state(ws, id, &s);
            CHECK(s.grounded == 1 && fabs(s.position.z) < 1e-9);
            CHECK(s.position.x > 1.0 && fabs(s.position.y) < 1e-9);
            CHECK(fabs(s.velocity.x - 3.0) < 1e-9); /* 5 cm per tick at 60 Hz = 3 m/s */
            memset(&cam, 0, sizeof cam);
            cam.size = sizeof cam;
            CHECK(tpl_char_camera(ws, id, &cam) == 1 && cam.up.z == 1.0);
            tpl_world_destroy(ws);
        }
    }

    /* The function table. */
    {
        const tpl_api *api = tpl_get_api();
        CHECK(api != NULL && api == tpl_get_api());
        CHECK(api->size == sizeof(tpl_api));
        CHECK(api->api_version == TPL_API_VERSION && strcmp(api->prefix, "tpl") == 0);
        w = api->world_create(&wc, &col);
        CHECK(w != NULL);
        memset(&cc, 0, sizeof cc);
        cc.size = sizeof cc;
        id = api->char_add(w, &cc);
        CHECK(api->world_tick(w, 10) == 1);
        memset(&s, 0, sizeof s);
        s.size = sizeof s;
        CHECK(api->char_state(w, id, &s) == 1 && s.tick == 10);
        CHECK(api->state_hash(w) == tpl_state_hash(w) && api->state_size(w) == tpl_state_size(w));
        api->world_destroy(w);
    }

    /* Same bits on every machine: the inputs of tests/Tpl.Core.Tests/Replays/walk-and-jump.replay
       must end in the hash stored there. If you record that replay again, update this number. */
    {
        static const struct { int ticks; uint32_t buttons; float mx, my; } rows[] = {
            { 10, 0, 0.0f, 0.0f }, { 30, 0, 0.0f, 1.0f }, { 1, TPL_BUTTON_JUMP, 0.0f, 1.0f },
            { 40, 0, 0.0f, 1.0f }, { 30, 0, 1.0f, 0.0f },
        };
        w = tpl_world_create(&wc, &col);
        memset(&cc, 0, sizeof cc);
        cc.size = sizeof cc;
        id = tpl_char_add(w, &cc);
        for (i = 0; i < (int32_t)(sizeof rows / sizeof rows[0]); i++) {
            in = pad(rows[i].buttons, rows[i].mx, rows[i].my);
            tpl_char_input(w, id, &in);
            tpl_world_tick(w, rows[i].ticks);
        }
        if (tpl_state_hash(w) != 0x196007041dc1b243ull)
            printf("state hash 0x%016llx\n", (unsigned long long)tpl_state_hash(w));
        CHECK(tpl_state_hash(w) == 0x196007041dc1b243ull);
        tpl_world_destroy(w);
    }

    if (failures == 0) printf("test_tpl: all checks passed\n");
    return failures == 0 ? 0 : 1;
}
