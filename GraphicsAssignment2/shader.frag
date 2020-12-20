#version 330 core

// Global constants (for this vertex shader)
vec4 specular_colour = vec4(1, 1, 1, 1);
vec4 global_ambient = vec4(0.05, 0.05, 0.05, 1);
int shininess = 100;

// Inputs from the vertex shader
in vec3 fnormal, fposition;
in vec4 fdiffusecolour;
in vec4 fintensity;

out vec4 OutputColor;

void main()
{
	vec3 lightpos = vec3(0,0,0); // set the light position to be the camera location
	vec4 emissive = vec4(0);				
	vec4 fambientcolour = fdiffusecolour * 0.3f;
	fambientcolour.w = 0.2f;
	vec4 fspecularcolour =  specular_colour * fintensity;
	float distancetolight = length(lightpos-fposition);

	// Normalise interpolated vectors
	vec3 L = normalize(lightpos - fposition);
	vec3 N = normalize(fnormal);		
	
	// diffuse component
	vec4 diffuse = max(dot(N, L), 0.0) * fdiffusecolour * fintensity;

	// specular component - Phong specular reflection
	vec3 V = normalize(lightpos-fposition);	
	vec3 R = reflect(-L, N);
	vec4 specular = pow(max(dot(R, V), 0.0), shininess) * fspecularcolour;

	// attentuation constants
	float attenuation_k1 = 0.5f;
	float attenuation_k2 = 0.5f;
	float attenuation_k3 = 0.5f;
	float attenuation = 10.0f / (attenuation_k1 + attenuation_k2*distancetolight + 
								attenuation_k3 * pow(distancetolight, 2));
								
    OutputColor = attenuation*(fambientcolour + diffuse + specular) + emissive + global_ambient;
}