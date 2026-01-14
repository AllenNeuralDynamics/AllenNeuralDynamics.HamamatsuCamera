---
uid: usage
---
# Usage

This section describes how to use the AllenNeuralDynamics.HamamatsuCamera package, including available components and example workflows.

## Components

AllenNeuralDynamics.HamamatsuCamera provides several component types that integrate with Bonsai workflows.

### Nodes

This package provides several Bonsai nodes for camera acquisition, processing, and data export. Most nodes operate on the `IFrameContainer` interface, which represents either a single frame (`Frame`) or a collection of frames (`FrameBundle`).

- [C13440](~/articles/c13440.md)
- [Processing](~/articles/processing.md)
- [CsvWriter](~/articles/csv_writer.md)
- [TiffWriter](~/articles/tiff_writer.md)
- [Frames](~/articles/frames.md)
- [FrameBundles](~/articles/frame_bundles.md)

---

### Editors

Editors provide custom configuration interfaces for complex nodes. Currently, only the C13440 node has an editor attached to it.

- [C13440 Editor](~/articles/c13440_editor.md)

---

### Visualizers

Visualizers enable real-time inspection of acquired data. Currently, only the Processing node has a visualizer attached to it.

- [Processing Visualizer](~/articles/processing_visualizer.md)

## Workflows

At the core of workflows using this package are the C13440 node followed by the Processing node. This provides camera initialization, configuration, and image acquisition followed by signal extraction and visualization.

![Core Workflow](~/workflows/c13440_core.svg)

Typically this is followed up by the CsvWriter and/or the TiffWriter nodes where the CsvWriter is responsible for writing the frame metadata and activity data to a .csv while the TiffWriter is responsible for writing the image data to multi-page split BigTiff files. 

![Standard Workflow](~/workflows/c13440_csv_tiff.svg)

The Frames and FrameBundles nodes can be used to extract the IplImages from the IFrameContainer to be used with other Bonsai-rx operators. 

![IplImage Workflow](~/workflows/c13440_iplimage.svg)