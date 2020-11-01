using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class AssignTextures : MonoBehaviour {

    public RawImage EyeImage;
    public RawImage PositionImage;
    public RawImage MirrorImage;

    void Start()
    {
        var caps = Fove.ClientCapabilities.EyesImage | Fove.ClientCapabilities.PositionImage;
        FoveManager.RegisterCapabilities(caps);
    }

    void Update()
    {
        EyeImage.texture = FoveManager.GetEyesImage();
        PositionImage.texture = FoveManager.GetPositionImage();
        MirrorImage.texture = FoveManager.GetMirrorTexture();
    }
}
