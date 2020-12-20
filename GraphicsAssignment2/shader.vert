#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec4 colour;
layout (location = 2) in vec3 normal;

out vec3 fnormal;
out vec3 fposition;
out vec4 fdiffusecolour;
out float fintensity;
out mat4 fview;

uniform mat4 model;
uniform mat4 projection;
uniform mat4 view;
uniform float intensity;
uniform float timeElapsed;
uniform int partyMode;

float rand(float n){return sin(n + 234.34323434);}

void main()
{
    vec4 position_h = vec4(position, 1.0);
    fdiffusecolour = vec4(0.05, 0, 0.05, 1);
    fintensity = intensity;

    fview = view;

	mat4 mv_matrix = view * model;
	mat3 n_matrix = transpose(inverse(mat3(mv_matrix)));

    fposition = (mv_matrix * position_h).xyz;
    fnormal = normalize(n_matrix * normal);

    
    vec3 position_nh = position_h.xyz;
    if (partyMode != 1)
    {
        if (partyMode == 2)
        {
            float moveAmount = 0.4f * sin(timeElapsed/2) * rand(gl_VertexID);
            position_nh += moveAmount*normalize(-position_nh);

        }
        if (partyMode == 3)
        {
            float moveAmount = 1f * sin(timeElapsed/2) * rand(gl_VertexID);
            position_nh += moveAmount*normalize(-position_nh);

        }
        if (partyMode == 4)
        {
            float moveAmount = 2f * sin(timeElapsed/2) * rand(gl_VertexID);
            position_nh += moveAmount*normalize(-position_nh);

        }
    }

    position_h = vec4(position_nh, 1.0);

    gl_Position = projection * view * model * position_h;
}