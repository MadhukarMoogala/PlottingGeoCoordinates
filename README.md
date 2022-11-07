# How to Plot Geocoordinates in AutoCAD

Geographical Coordinates with respective Lat\Long for given [Cooridnate Reference System]((https://en.wikipedia.org/wiki/Spatial_reference_system)) the point data-set maybe very large, it is need not require to place with same magnitude in AutoCAD drawing, we can use [GeoCoordinateTransformer](https://help.autodesk.com/view/OARX/2022/ENU/?guid=OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_GeoCoordinateTransformer) API to transform the GeoCooridnate Point to WCS Point.

This project reads Point dataset from supplement JSON, each file contains json serializable data with this data (CRS, prediction, probability, lat, lon). There is 256*256=65,536 data points in each file. The points are of very large coordinates as seen from sample dataset.

```
    /************
     * 
     * Geo Marker
     * 
        Long = -87.961526837297455
        Lat = 41.948856487413423
 
        Y=5153321.469906129,
        X=-9791832.37692682
     * 
     * 
     * **********/

```

### Dependencies

This application is tested on AutoCAD 2022, but it should work for previous versions and new versions of AutoCAD as well.

- Requires NET4.8 Framework

- Requires [Netwonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) to process Json data.

### Steps To Build

```bash
git clone https://github.com/MadhukarMoogala/PlottingGeoCoordinates.git
cd PlottingGeoCoordinates
nuget install packages.config -o packages
msbuild /t:build PlottingCoord.csproj -p:Configuration=Debug;Platform=x64
```

NOTE: You may have nuget the packages from packages.config before msbuild

### Steps To Use

- Launch AutoCAD 2022

- Open FloodBlock.dwg

- Netload PlottingCoord\bin\Debug\PlottingCoord.dll

- Run Command "PLOTCOORDS"

### Demo


https://user-images.githubusercontent.com/6602398/200293583-525fc8a6-d8f0-41d4-8a14-9fe072548569.mp4



### License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

### Written by

Madhukar Moogala, [Forge Partner Development](http://forge.autodesk.com)  @galaka

 
