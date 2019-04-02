# Masterproef
Rendering light field images on mobile devices

## Getting Started
### Foutenanalyse
Script neemt telkens een beeld uit orignal-folder en een uit changed-folder om te vergelijken. PSNR- en SSIM-waarden worden berekend en op grafiek geplot. 
Via commandolijn oproepen: 
> python analyse.py *originalfolder testfolder1 testfolder2 ...*

### OpenGL_Project 
Het project bevat een testprogramma met geometry instancing in OpenGL in c++.

### Unity_Shadertoy
Het project is een omzetting van shadertoy project naar Unity. Alle shaders zitten in [Assets/Shaders](https://github.com/kurogga/masterproef/tree/master/Unity_Shadertoy/Assets/Shaders). De gewenste shader downloaden en toevoegen in eigen projects asset is voldoende.

### Unity_LightFieldRecon
Dit bevat de reconstructie van SMoE in Unity. De C#-script en shader zitten in de Asset-folder. Dus enkel die folder importeren in eigen project is genoeg. Binnen het project hoeft er zoals in Shadertoy enkel een quad-object voor de camera te hebben en de juiste script en shader op het object toepassen. Gebouwde applicatie is ook beschikbaar in de Build-folder.