/* The smallest inklib host: a flat floor, one character, two seconds of walking and a jump.
   Prints the character's state and the camera, and the events, in shared units (metres, Z up). */
#include <stdio.h>
#include <string.h>
#include "tpl.h"
#include "../flat_floor.h"

int main(void)
{
    int up = 2; /* shared units: Z up */
    tpl_collision col = flat_floor(&up);
    tpl_world_config wc = { sizeof(tpl_world_config), TPL_UNITS_SHARED, NULL };
    tpl_char_config cc;
    tpl_input in;
    tpl_state s;
    tpl_camera cam;
    tpl_event ev[32];
    int32_t id, tick, n, i;
    char err[256];

    tpl_world *w = tpl_world_create(&wc, &col);
    if (!w) {
        tpl_last_error(err, sizeof err);
        printf("tpl_world_create failed: %s\n", err);
        return 1;
    }

    memset(&cc, 0, sizeof cc);
    cc.size = sizeof cc;
    id = tpl_char_add(w, &cc);

    memset(&in, 0, sizeof in);
    in.size = sizeof in;
    memset(&s, 0, sizeof s);
    s.size = sizeof s;
    memset(&cam, 0, sizeof cam);
    cam.size = sizeof cam;

    for (tick = 0; tick < 2 * TPL_TICK_RATE; tick++) {
        in.move_y = 1.0f;                                   /* stick up */
        in.buttons = tick == TPL_TICK_RATE ? TPL_BUTTON_JUMP : 0;
        tpl_char_input(w, id, &in);
        tpl_world_tick(w, 1);                               /* one fixed tick */

        n = tpl_events(w, ev, 32);
        for (i = 0; i < n; i++)
            printf("  event %d at tick %lld (a=%d b=%d)\n", ev[i].type, (long long)ev[i].tick, ev[i].a, ev[i].b);

        if ((tick + 1) % 15 == 0) {
            tpl_char_state(w, id, &s);
            tpl_char_camera(w, id, &cam);
            printf("tick %3lld state %d pos %.3f %.3f %.3f  camera %.2f %.2f %.2f\n", (long long)s.tick, s.state,
                   s.position.x, s.position.y, s.position.z, cam.position.x, cam.position.y, cam.position.z);
        }
    }

    tpl_world_destroy(w);
    return 0;
}
