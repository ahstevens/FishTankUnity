# CCOM VisLab FishTank VR Unity

### Overview:
- **FishTankUnity** is the main project.
- **[FishTankCalibrator](https://github.com/ahstevens/FishTankCalibrator)** is a separate Unity project which is used to generate the XML display calibration files for use with the Cave Manager script.
- **[ImageStreamClient-Unity](https://github.com/ahstevens/ImageStreamClient-Unity)** is a streaming client that receives a "video" feed from the FishTankUnity app.

### Notes on Scripts:
- **Ship>Pivot>Mast Camera>Render Texture Stream Server**
  - Sends render textures over the network as JPG or PNG
- **Ship>Pivot>POS M/V>NMEA Emulator**
  - Broadcasts NMEA position and heading information of the GameObject to the network, which OpenCPN can then ingest. A source EPSG can be set and it uses the offset contained in the UnityZeroGeoReference GameObject’s GEOReference script. 
- **Ship>Follow Boat Circuit**
  - Looks for a child object named “Path” attached to the current GameObject  and interpolates between the “BoatCircuitN” waypoints that are children of the Path child.
- **targets>AR Fuel Icon>Label Orienter**
  - Billboards the label GameObject towards the given camera and sets the degrees of visual angle that the label will subtend.
- **CaveManager>Steam VR Tracked Device Assigner**
  - Lets you enter the enumerated OpenVR tracker name in order to use a specific type of tracker and assign it to one or more GameObjects.
- **CaveManager>Cave Manager**
  - Handles most of the CAVE system, including the TV calibration file and other CAVE-specific settings.
