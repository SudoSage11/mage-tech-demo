shader_type spatial;
render_mode unshaded, depth_test_disabled;

uniform int max_steps = 100;
uniform float max_dist = 100.0;
uniform float surface_dist = 0.01;
uniform float smooth_amount = 0.1;
uniform float sphere_size = 0.5;

const int MAX_NODES = 256;
uniform vec3 node_positions[MAX_NODES];

// Determine the signed distance from the starting origin pos, to the outside of a sphere with a given radius
float sdSphere(vec3 originPos, vec3 spherePos, float radius) {
	return length(spherePos - originPos) - radius;
}

// Returns the circular smoothed minimum of two SDF values.
//
// Source: https://iquilezles.org/articles/smin/
float smoothUnion( float a, float b, float k ) {
    k *= 1.0/(1.0-sqrt(0.5));
    float h = max( k-abs(a-b), 0.0 )/k;
    return min(a,b) - k*0.5*(1.0+h-sqrt(1.0-h*(h-2.0)));
}

float getDist(vec3 pos) {
	float spheres = sdSphere(pos, node_positions[0], sphere_size);
	
	for (int p = 1; p < MAX_NODES; p++) {
		vec3 spherePos = node_positions[p];
		if (spherePos == vec3(0)) break;
		float sphere = sdSphere(pos, spherePos, sphere_size);
		spheres = smoothUnion(spheres, sphere, smooth_amount);
	}
	
	//vec3 sphere1Pos = node_positions[0];
	//vec3 sphere2Pos = node_positions[1];
	//
	//float sphere1 = sdSphere(pos, sphere1Pos, 1.0);
	//float sphere2 = sdSphere(pos, sphere2Pos, 1.0);
	//
	//float value = smoothUnion(sphere1, sphere2, smooth_amount);
	
	return spheres;
}

vec3 getNormal(vec3 pos) {
	float dist = getDist(pos);
	vec2 offset = vec2(0.01, 0.0);
	vec3 normal = dist - vec3(
		getDist(pos - offset.xyy),
		getDist(pos - offset.yxy),
		getDist(pos - offset.yyx)
	);
	return normalize(normal);
}

// Returns the total distance travelled by the ray
float rayMarch(vec3 origin, vec3 dir) {
	float totalDist = 0.0;
	for (int i = 0; i <= max_steps; i++) {
		vec3 pos = origin + dir * totalDist;
		float distToScene = getDist(pos);
		totalDist += distToScene;
		
		// Check whether the ray has gone too far, or gotten close enough to an object
		if (totalDist > max_dist || distToScene < surface_dist) break;
	}
	
	return totalDist;
}

void fragment() {
	vec3 ws_pixel_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 rayOrigin = CAMERA_POSITION_WORLD;
	vec3 rayDir = normalize(ws_pixel_pos - rayOrigin);
	
	float dist = rayMarch(rayOrigin, rayDir);
	ALPHA = step(dist, max_dist);
	
	vec3 normal = getNormal(rayOrigin + rayDir * dist);
	ALBEDO = normal;
}