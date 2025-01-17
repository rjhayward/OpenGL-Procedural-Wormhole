﻿#version 330 core

// Global constants (for this vertex shader)
vec4 specular_colour = vec4(1, 1, 1, 0.3);
vec4 global_ambient = vec4(0.05, 0.05, 0.05, 0.1);
int shininess = 1000;

// Inputs from the vertex shader
in vec3 fnormal, fposition;
in vec4 fdiffusecolour;
in float fintensity;
in mat4 fview;

out vec4 OutputColor;

void main()
{
	vec4 lightpos4 = vec4(0,0,0,1); 
	lightpos4 *= fview; // set the light position to be the camera location

	vec3 lightpos = lightpos4.xyz;

	vec4 emissive = fdiffusecolour * 0.1f;				
	vec4 fambientcolour = fdiffusecolour * 0.3f;
	vec4 fspecularcolour =  specular_colour * fintensity;
	float distancetolight = length(lightpos-fposition);
	
	// Normalise interpolated vectors for our shading functions to use
	vec3 L = normalize(lightpos - fposition);
	vec3 I = normalize(fposition - lightpos);
	vec3 N = normalize(fnormal);
	

	// Fresnel Shading
	float fresnelR;
	{
		// set params for fresnel function
		float fresnelbias = 0.0f;
		float fresnelscale = 0.7f;
		float fresnelpower = 0.9f;

		fresnelR = fresnelbias + fresnelscale * pow(1.0 + dot(I, N), fresnelpower);
	}

	// diffuse component
	vec4 diffuse = max(dot(N, L), 0.0) * fdiffusecolour * fintensity;

	// specular component - Phong specular reflection
	vec3 V = normalize(lightpos-fposition);	
	vec3 R = reflect(-L, N);
	vec4 specular = pow(max(dot(R, V), 0.0), shininess) * fspecularcolour;

	// attentuation constants
	float attenuation_k1 = 50.0f;
	float attenuation_k2 = 0.001f;
	float attenuation_k3 = 0.001f;
	float attenuation = 150.0f / (attenuation_k1 + attenuation_k2*distancetolight + 
								attenuation_k3 * pow(distancetolight, 2));
								
    OutputColor = (fambientcolour + diffuse + specular);
	
	OutputColor.w = 0.25f;

	// linear interpolation using fresnel value
	OutputColor = attenuation*mix(OutputColor, OutputColor * diffuse, fresnelR);
	//OutputColor = OutputColor + emissive + global_ambient; // uncomment to add emission and global ambience
}