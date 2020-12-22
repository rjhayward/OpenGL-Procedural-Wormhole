#version 330 core
out vec4 OutputColor;

in vec3 TexCoords;
uniform samplerCube skybox;


void main()
{    
    OutputColor = texture(skybox, TexCoords);
    //set alpha for skybox
    OutputColor.w = 0.5f;
}