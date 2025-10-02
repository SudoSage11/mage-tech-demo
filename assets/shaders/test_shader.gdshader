shader_type spatial;

uniform float time_scale;

uniform float add;
uniform float mult;

uniform float amplitude;

uniform sampler3D noise;

uniform float sway_amount : hint_range(0.0, 1.0) = 0.1;
// Maximum sway offset

uniform float sway_scale : hint_range(0.0, 10.0) = 1.0;
// Controls how tightly sway oscillates over the mesh's Y axis

uniform float sway_duration : hint_range(0.01, 10.0) = 2.0;
// Time (in seconds) for one full sway cycle

uniform vec3 sway_direction = vec3(1.0, 0.0, 0.0);
// Direction of sway movement (X, Y, Z)

uniform float test_value;

// Decode from [0,1]^2 to unit vector
vec3 decode_octahedron(vec2 e) {
	e = e * 2.0 - 1.0;
	vec3 n = vec3(e.x, e.y, 1.0 - abs(e.x) - abs(e.y));
	float t = clamp(-n.z, 0.0, 1.0);
	n.x += (n.x >= 0.0 ? -t : t);
	n.y += (n.y >= 0.0 ? -t : t);
	return normalize(n);
}

void vertex() {
	// Sway (from https://godotshaders.com/shader/swaying-material/
    float sway_time = (TIME / sway_duration) * 6.28318; // 2π = full sine wave cycle
    float sway = sin(VERTEX.y * sway_scale + sway_time) * sway_amount;
    VERTEX.xyz += sway * sway_direction;
	
	// Wiggle
	
	// Get the smooth normals encoded in the UV2 property
	vec3 smoothNormal = decode_octahedron(UV2);
	
	VERTEX += (smoothNormal * (normalize(texture(noise, VERTEX + TIME * time_scale)).xyz - add) * mult);
	
	//VERTEX += test_value;
}

void fragment() {
	METALLIC = 0.0;
	ROUGHNESS = 0.2;
	ALBEDO = vec3(0.1, 0.3, 0.5);
}

