using Accord.Video.DirectShow;

namespace EasyFaceCredentialProvider;

public class Camera
{
    private readonly VideoCaptureDevice _camera;

    public Camera()
    {
        var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        if (videoDevices.Count == 0)
        {
            throw new Exception("No camera found");
        }
        _camera = new VideoCaptureDevice(videoDevices[0].MonikerString);
    }
}