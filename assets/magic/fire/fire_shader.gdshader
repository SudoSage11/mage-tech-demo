shader_type spatial;
render_mode unshaded, depth_test_disabled;

const int MAX_NODES = 256;

// Core
group_uniforms Core;
uniform vec3 node_positions[MAX_NODES];
uniform sampler2D depth_texture : hint_depth_texture;

// Ray marching
group_uniforms RayMarching;
uniform int max_steps = 100;
uniform float max_dist = 100.0;
uniform float surface_dist = 0.01;

// Base Shape
group_uniforms BaseShape;
uniform float smooth_amount = 0.35;
uniform float sphere_size = 0.5;
uniform float cut_level = 0.25;
uniform float cut_smoothing = 0.1;

// Color
group_uniforms Color;
uniform sampler2D gradient;
uniform float color_mult = 2.0;
uniform float color_add = -0.15;
uniform sampler2D color_noise;
uniform float color_time_scale = 1.0;
uniform float color_anim_intensity = 0.2;

// 2D "cut disk" SDF — basically a circle with a clipped top
float sd_cut_disk_2d(vec2 p, float r, float h, float scale) {
	p /= scale;
	
	// h = how far down from top you cut, in same units as r (0 = no cut, r = full cut)
	p.y = -p.y; // flip vertically so positive y is "up" and we cut from the top
	float w = sqrt(max(r * r - h * h, 0.0));
	p.x = abs(p.x);
	
	float s = max((h - r) * p.x * p.x + w * w * (h + r - 2.0 * p.y), h * p.x - w * p.y);
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

// Round out the shape after cutting to remove sharp corners
float sd_round(float sd_value, float round_radius) {
	return sd_value - round_radius;
}

// Returns the smoothed minimum of two SDF values (circular geometric).
//
// Source: https://iquilezles.org/articles/smin/
float smooth_union(float a, float b, float k) {
	k *= 1.0/(1.0-sqrt(0.5));
    return max(k,min(a,b)) -
           length(max(k-vec2(a,b),0.0));
}

// Distance field using revolution
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

// Determine color (temperature) based on distance from the center of the node, 
// and add a random nature so that the colour warps
vec3 get_color(vec3 final_ray_pos) {
	float dist_from_sphere = 10.0;
	
	for (int i = 0; i < MAX_NODES; i++) {
		vec3 node_pos = node_positions[i];
		if (node_pos == vec3(0)) break;
		
		// Colour randomisation
		float t = (TIME + (float(i) + 1.0) * 100.0) * color_time_scale;
		vec2 noise_pos = (final_ray_pos + vec3(cos(t * -0.2), sin(t * -0.4), sin(t * 0.5))).xy;
		float noise = texture(color_noise, noise_pos).x * color_anim_intensity;
		
		dist_from_sphere = min(dist_from_sphere, distance(final_ray_pos, node_pos) + noise);
	}
	
	float sample_point = (dist_from_sphere * color_mult) + color_add;
	sample_point = clamp(sample_point, 0.01, 0.99); // Must clamp to remain within gradient bounds
	
	vec3 color = texture(gradient, vec2(sample_point, 0.0)).rgb;
	
	return color;
}

// Draws the pixels to the screen
void fragment() {
	// Manual depth test to only draw when not obscured behind an object
	// From https://docs.godotengine.org/en/stable/tutorials/shaders/advanced_postprocessing.html
	float depth = texture(depth_texture, SCREEN_UV).x;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z;
	
	vec3 ws_pixel_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 ray_origin = CAMERA_POSITION_WORLD;
	vec3 ray_dir = normalize(ws_pixel_pos - ray_origin);
	
	float dist = ray_march(ray_origin, ray_dir);
	vec3 final_ray_pos = ray_origin + ray_dir * dist;
	
	ALPHA = step(dist, min(max_dist, linear_depth));
	ALBEDO = get_color(final_ray_pos);
}
