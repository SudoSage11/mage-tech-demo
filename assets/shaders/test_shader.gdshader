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

void vertex() {
	// Sway (from https://godotshaders.com/shader/swaying-material/
    float sway_time = (TIME / sway_duration) * 6.28318; // 2π = full sine wave cycle
    float sway = sin(VERTEX.y * sway_scale + sway_time) * sway_amount;
    VERTEX.xyz += sway * sway_direction;
	
	// Wiggle
	vec4 offset = (texture(noise, VERTEX + TIME * time_scale) + amplitude) * (1.0 / amplitude);
	VERTEX *= offset.xyz;
	
	VERTEX += test_value;
}

void fragment() {
	METALLIC = 0.0;
	ROUGHNESS = 0.2;
	ALBEDO = vec3(0.1, 0.3, 0.5);
}