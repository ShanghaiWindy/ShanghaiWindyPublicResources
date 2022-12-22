README
================================

Contents
--------------------------------
About
Installation Instructions
Usage Instructions
Troubleshooting
Requirements
Support


About
--------------------------------
LSPP is an extremely simple, easy to use, and highly performant screenspace Volumetric Light Scattering solution targeted for lightweight hardware. In short, LSPP intelligently determines areas of potential sun occlusion on your screen. It then uses this information to generate god rays (light shafts) from the sun.

By default, the asset uses the main light color as the tint for the fog. You can adjust this tint using the Sun Color Tint parameter.

There are also several other exposed shader parameters, including fog density, sample count, max ray distance, tint, and several additional appearance settings.
These settings are all described in the Volume Component system workflow. You can hover over the respective label to understand more about the impact each will have on your scene.


Installation Instructions
--------------------------------
1. Import the LSPP asset to your project.
2. Navigate to the active Forward Renderer Data asset in use in your project. This asset is typically named "ForwardRendererData".
3. Click "Add Renderer Feature", and select "Light Scattering Renderer Feature" from the dropdown menu.


Usage Instructions
--------------------------------
1. Navigate to or create a new Global Volume in your scene.
2. Click "Add Override"
3. Navigate to Post-Process/Light Scattering. Click on it.
4. Configure the Light Scattering post process effect in your scene.

You can change this post-process on the fly in the same way you would modify any other Volume Component integration.
I've included an example script, "ChangeLightScatteringColor.cs" to demonstrate an example of randomly changing the color.

N.B. Shuriken Particle Systems using Unity's built-in particle shader must be opaque or alpha-clipped and must uncheck the "Enable Mesh GPU Instancing" option in the Particle System Renderer settings to be included in the occluders pass.


Troubleshooting
--------------------------------
1. Verify that the Light Scattering Render Pass Feature is included in your Forward Renderer Data asset.
2. Verify that you have a Global Volume active in your scene, and that your Camera is not excluding this Volume Layer.
3. Check if your Global Volume includes a Light Scattering volume override.
4. Ensure that the fog density is sufficiently high, and that your camera is roughly facing the sun direction.
5. Verify that you have MergeLightScattering, Occluders, and LightScattering shaders present in your project and that these Shaders have been compiled.


Requirements
--------------------------------
This asset is designed for Unity 2020.3 Universal Render Pipeline. This asset is not guaranteed to work on other versions of Unity. This asset will not work on the Built-In Render Pipeline or the High Definition Render Pipeline.


Support
--------------------------------
If you're not happy, I'm not happy.
Please contact me at occasoftware@gmail.com for any support.

You can join the OccaSoftware developer community to ask questions or get support as well.
Join our Discord: https://occasoftware.com/discord
DM me on Twitter: https://twitter.com/occasoftware
