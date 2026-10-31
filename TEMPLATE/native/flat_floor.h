/* A flat floor at height 0 as tpl_collision callbacks, shared by the C test and the example host.
   user points to an int: the up axis (1 = Y for native units, 2 = Z for shared units). */
#ifndef TPL_FLAT_FLOOR_H
#define TPL_FLAT_FLOOR_H

#include "tpl.h"

static double *axis_of(tpl_vec3 *v, int axis) { return axis == 0 ? &v->x : axis == 1 ? &v->y : &v->z; }
static double up_of(const tpl_vec3 *v, int axis) { return axis == 0 ? v->x : axis == 1 ? v->y : v->z; }

static void floor_hit(int axis, tpl_vec3 at, double t, tpl_hit *out)
{
    tpl_vec3 n = { 0.0, 0.0, 0.0 };
    *axis_of(&at, axis) = 0.0;
    *axis_of(&n, axis) = 1.0;
    out->point = at;
    out->normal = n;
    out->t = t;
    out->surface = 0;
    out->actor = 0;
}

static int32_t floor_ground(void *user, const tpl_vec3 *from, double max_drop, tpl_hit *out)
{
    int axis = *(const int *)user;
    double h = up_of(from, axis);
    if (h < 0.0 || h > max_drop) return 0;
    floor_hit(axis, *from, max_drop > 0.0 ? h / max_drop : 0.0, out);
    return 1;
}

static int32_t floor_segment(void *user, const tpl_vec3 *a, const tpl_vec3 *b, uint32_t flags, tpl_hit *out)
{
    int axis = *(const int *)user;
    double ha = up_of(a, axis), hb = up_of(b, axis), t;
    tpl_vec3 p;
    if ((flags & TPL_SEGMENT_SKIP_FLOORS) || ha < 0.0 || hb >= 0.0) return 0;
    t = ha / (ha - hb);
    p.x = a->x + (b->x - a->x) * t;
    p.y = a->y + (b->y - a->y) * t;
    p.z = a->z + (b->z - a->z) * t;
    floor_hit(axis, p, t, out);
    return 1;
}

static int32_t floor_overlap(void *user, const tpl_shape *s, tpl_hit *out)
{
    int axis = *(const int *)user;
    double bottom;
    switch (s->kind) {
    case TPL_SHAPE_SPHERE: bottom = up_of(&s->a, axis) - s->radius; break;
    case TPL_SHAPE_CAPSULE: {
        double ha = up_of(&s->a, axis), hb = up_of(&s->b, axis);
        bottom = (ha < hb ? ha : hb) - s->radius;
        break;
    }
    default: bottom = up_of(&s->a, axis) - up_of(&s->half_extents, axis); break;
    }
    if (bottom >= 0.0) return 0;
    floor_hit(axis, s->a, 0.0, out);
    return 1;
}

/* *up_axis must stay valid while the world lives. */
static tpl_collision flat_floor(int *up_axis)
{
    tpl_collision c = { sizeof(tpl_collision), up_axis, floor_ground, floor_segment, floor_overlap, 0 };
    return c;
}

#endif
