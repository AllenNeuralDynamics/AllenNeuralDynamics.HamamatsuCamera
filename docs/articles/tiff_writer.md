---
uid: tiff_writer
---
# TiffWriter

**Description**  
The `TiffWriter` node handles writing image data to split, multi-page `.tif` files.

**Properties**

- **FolderName**  
The base name and relative path of the folder containing the output `.tif` files.

- **Suffix**  
The suffix appended to the output folder name.

- **FramesPerTiff**  
The number of frames stored in each `.tif` file.

- **BaseFileName**  
Optional base filename for the output `.tif` files.  
If not specified, the base filename defaults to the containing folder’s base name.