# Masterproef
Rendering light field images on mobile devices

## Getting Started

### Requirements
* Python 3.0 of hoger. Download python compiler en zet python-PATH in de environment variables. [Handleiding](https://docs.python.org/2/using/windows.html).
* C++11 of hoger en OpenGL 3.3 of hoger. Configureer c++ en OpenGL via [deze handleiding](http://www.opengl-tutorial.org/beginners-tutorials/tutorial-1-opening-a-window/).
* Android 4.0 of hoger. Download Android Studio met Android SDK via [deze link](https://developer.android.com/studio).
* UnityEngine 2018.3.9f1 of hoger. Download UnityEngine via [deze link](https://unity3d.com/get-unity/download) en maak een gratis account. 

### Foutenanalyse
Script neemt telkens een beeld uit orignal-folder en een uit changed-folder om te vergelijken. PSNR- en SSIM-waarden worden berekend en op grafiek geplot. 
Via commandolijn oproepen: 
> python analyse.py *originalfolder testfolder1 testfolder2 ...*

### OpenGL_Project 
Het project bevat een testprogramma met geometry instancing in OpenGL in c++.

### Unity_Shadertoy
Het project is een omzetting van shadertoy project naar Unity. Alle shaders zitten in [Assets/Shaders](https://github.com/kurogga/masterproef/tree/master/Unity_Shadertoy/Assets/Shaders). De gewenste shader downloaden en toevoegen in eigen projects asset is voldoende.

### Unity_LightFieldRecon
Dit bevat de reconstructie van SMoE in Unity. De C#-scripts en shaders zitten in de Asset-folder. Dus enkel die folder importeren in eigen project is genoeg. Binnen het project moet er enkel een quad-object voor de camera te hebben en de juiste script en shader op het object toepassen. De ProjectSettings-folder geeft alle bijkomende configuraties van de engine weer. Gebouwde applicatie is ook beschikbaar in de Build-folder. 