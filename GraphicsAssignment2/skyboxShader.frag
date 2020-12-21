#version 330 core
out vec4 OutputColor;

in vec3 TexCoords;
uniform samplerCube skybox;


void main()
{    
    OutputColor = texture(skybox, TexCoords);
    OutputColor.w = 0.5f;
}