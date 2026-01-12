---
uid: installation
---
# Installation

This section describes how to install all required dependencies and the AllenNeuralDynamics.HamamatsuCamera package.

## Bonsai-rx Installation

The AllenNeuralDynamics.HamamatsuCamera package is used inside of Bonsai-rx.

Please follow the official Bonsai installation instructions:
https://bonsai-rx.org/docs/installation

---

## DCAM-API Installation

Hamamatsu cameras require the DCAM-API driver.

1. Download the DCAM-API from the Hamamatsu website: https://www.hamamatsu.com/eu/en/product/cameras/software/driver-software/dcam-api-for-windows.html.
2. Install the driver following Hamamatsu’s instructions.
3. Verify camera detection using the vendor-provided tools.

---

## CameraLink Installation (Optional)

A CameraLink will greatly increase the throughput of the camera and can be used for advanced acquisition scenarios.

1. Ensure the card is compatible with the version of DCAM-API being used.
2. Follow the card's documentation for physically installing it into the PC as well as for installing any required drivers.
3. Connect the camera to the CameraLink and ensure it can be detected by the DCAM-API tools

---

## Package Installation

### Online Installation

1. Open Bonsai.
2. Navigate to **Manage Packages**.
3. Specify nuget.org as the package source
4. Search for `AllenNeuralDynamics.HamamatsuCamera`.
5. If using Bonsai version 2.9.0 or newer, select `Include Advanced`
6. Click **Install**.

---

### Offline Installation

An offline installation of the package can be done by following the steps outlined below:

- [Offline Installation](~/articles/offline_installation.md)