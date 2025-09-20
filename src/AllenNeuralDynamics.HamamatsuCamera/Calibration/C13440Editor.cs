using Bonsai.Design;

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    /// <summary>
    /// Used to open the <see cref="CalibrationForm"/> from the 
    /// <see cref="C13440"/> node within Bonsai
    /// </summary>
    class C13440Editor : WorkflowComponentEditor
    {
        /// <summary>
        /// Verifies the service provider exists with a non-null state. 
        /// If the workflow is not running, opens the 
        /// <see cref="EditorForm"/>
        /// </summary>
        /// <param name="context">Unused in this Override function</param>
        /// <param name="component"><see cref="C13440"/> instance.</param>
        /// <param name="provider">Service provider</param>
        /// <param name="owner">Windows that contains the modal dialog used to display the <see cref="CalibrationForm"/></param>
        /// <returns></returns>
        public override bool EditComponent(ITypeDescriptorContext context, object component, IServiceProvider provider, IWin32Window owner)
        {
            // Verify provider exists
            if (provider != null)
            {
                // Get the service and verify it exists
                var editorState = (IWorkflowEditorState)provider.GetService(typeof(IWorkflowEditorState));
                if (editorState != null)
                {
                    // Only open when the workflow is stopped
                    if (editorState.WorkflowRunning)
                    {
                        MessageBox.Show(Resources.MsgBox_Error_WorkflowRunning, "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Show a Splash Screen on separate thread while form loading and camera connecting
                    SplashScreen.ShowSplash();

                    // Verify the camera is not acquiring on initialization
                    var capture = (C13440)component;
                    capture.Acquiring = false;
                    var includeTiff = capture.TiffProperties.IncludeTIFF;
                    var includeProcessing = capture.ImageProcessingProperties.IncludeProcessing;
                    capture.TiffProperties.IncludeTIFF = false;
                    capture.ImageProcessingProperties.IncludeProcessing = false;

                    using (var editorForm = new CalibrationForm(capture, provider))
                    {
                        try
                        {
                            editorForm.ShowDialog(owner);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: editorForm.ShowDialog\nMessage: {ex.Message}");
                            editorForm.Close();
                        }
                    }
                    capture.Acquiring = true; 
                    capture.TiffProperties.IncludeTIFF = includeTiff;
                    capture.ImageProcessingProperties.IncludeProcessing = includeProcessing;
                }
            }

            return false;
        }
    }
}
