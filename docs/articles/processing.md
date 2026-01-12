---
uid: processing
---
# Processing

**Description**  
The `Processing` node accepts `IFrameContainer` objects from the `C13440` node and provides image processing utilities, including a dedicated visualizer for inspecting the data.

**Output**  
- `IFrameContainer`  
- Either a `Frame` or a `FrameBundle`, depending on upstream configuration.

**Properties**

- **DeinterleaveCount**  
The number of interleaved channels present in the acquired data.  
This value is used to correctly separate interleaved image signals and is also used by the `C13440` node when frame bundling is enabled.