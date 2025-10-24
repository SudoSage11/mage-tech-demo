shader_type spatial;
render_mode unshaded, depth_test_disabled;

// Ray marching
group_uniforms RayMarching;
uniform int max_steps = 100;
uniform float max_dist = 100.0;
uniform float surface_dist = 0.01;

// Base Shape
group_uniforms BaseShape;
uniform float smooth_amount = 0.1;
uniform float sphere_size = 0.5;
uniform float cut_level = 0.0;
uniform float cut_smoothing = 0.0;

const int MAX_NODES = 256;
uniform vec3 node_positions[MAX_NODES];

// 2D "cut disk" SDF — basically a circle with a clipped top
float sd_cut_disk_2d(vec2 p, float r, float h, float scale) {
	p /= scale;
	
    // h = how far down from top you cut, in same units as r (0 = no cut, r = full cut)
    p.y = -p.y; // flip vertically so positive y is "up" and we cut from the top
    float w = sqrt(max(r * r - h * h, 0.0));
    p.x = abs(p.x);
    float s = max((h - r) * p.x * p.x + w * w * (h + r - 2.0 * p.y),
                  h * p.x - w * p.y);
    float d = (s < 0.0)
        ? length(p) - r
        : (p.x < w)
            ? h - p.y
            : length(p - vec2(w, h));
    return d * scale;
}

// 3D revolution operator: turn a 2D shape into a radial 3D shape
float op_revolution(vec3 pos, float radius, float cut_height, float scale) {
    // offset controls how far from the Y axis the revolution happens (like a torus center)
    vec2 q = vec2(length(pos.xz), pos.y);
    return sd_cut_disk_2d(q, radius, cut_height, scale);
}

float sd_round(float sd_value, float round_radius) {
	return sd_value - round_radius;
}

// Returns the circular smoothed minimum of two SDF values.
//
// Source: https://iquilezles.org/articles/smin/
float smooth_union(float a, float b, float k) {
    k *= 1.0 / (1.0 - sqrt(0.5));
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - k * 0.5 * (1.0 + h - sqrt(1.0 - h * (h - 2.0)));
}

// Example distance field using revolution
float get_dist(vec3 pos) {
	float counter_scale = sphere_size - cut_smoothing;
    float d = sd_round(op_revolution(pos - node_positions[0], sphere_size, cut_level, counter_scale), cut_smoothing);

    for (int i = 1; i < MAX_NODES; i++) {
        vec3 node_pos = node_positions[i];
        if (node_pos == vec3(0)) break;
        float sphere = sd_round(op_revolution(pos - node_pos, sphere_size, cut_level, counter_scale), cut_smoothing);
        d = smooth_union(d, sphere, smooth_amount);
    }

    return d;
}

vec3 get_normal(vec3 pos) {
    float dist = get_dist(pos);
    vec2 offset = vec2(0.01, 0.0);
    vec3 normal = dist - vec3(
        get_dist(pos - offset.xyy),
        get_dist(pos - offset.yxy),
        get_dist(pos - offset.yyx)
    );
    return normalize(normal);
}

// Returns the total distance travelled by the ray
float ray_march(vec3 origin, vec3 dir) {
    float total_dist = 0.0;
    for (int i = 0; i <= max_steps; i++) {
        vec3 pos = origin + dir * total_dist;
        float dist_to_scene = get_dist(pos);
        total_dist += dist_to_scene;
        
        // Check whether the ray has gone too far, or gotten close enough to an object
        if (total_dist > max_dist || dist_to_scene < surface_dist) break;
    }
    
    return total_dist;
}

void fragment() {
    vec3 ws_pixel_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec3 ray_origin = CAMERA_POSITION_WORLD;
    vec3 ray_dir = normalize(ws_pixel_pos - ray_origin);
    
    float dist = ray_march(ray_origin, ray_dir);
    ALPHA = step(dist, max_dist);
    
    vec3 normal = get_normal(ray_origin + ray_dir * dist);
    ALBEDO = normal;
}
