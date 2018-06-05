# Greedy Cubical Marching Squares
This is an implimentation of the Cubical Marching Squares algorithm for use with Unity Engine. It can be used in a project by simply calling the CmsMain.Volume.ContourMesh with the desired input. As of yet it lacks lacks the amgiguous case resolution or sharp feature preservation features described in the original [National Taiwan University paper](https://graphics.cmlab.csie.ntu.edu.tw/CMS/), but it does support a WIP version of a mesh simplification feature that is designed to combine voxels with planar surfaces and minimize the number of polygons needed to represent flat areas while retaining a high level of detail.


![Image of unreduced cabinet mesh](https://i.imgur.com/OrYfzpG.jpg)
![Image of reduced cabinet mesh](https://i.imgur.com/EjaXfVo.jpg)
