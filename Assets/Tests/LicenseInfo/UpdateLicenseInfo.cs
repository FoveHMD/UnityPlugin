
using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UpdateLicenseInfo : MonoBehaviour
{
    public Text licenseText;

    void Update()
    {
        var licenseInfoResult = FoveManager.QueryLicenses();
        if (!licenseInfoResult.IsValid)
        {
            licenseText.text = licenseInfoResult.error.ToString();
            return;
        }

        licenseText.text = "Licenses: \n";
        foreach (var license in licenseInfoResult.value)
        {
            licenseText.text += "{\n";
            licenseText.text += "\tUUID: " + license.uuid + ",\n";
            licenseText.text += "\tLicensee: " + license.licensee + ",\n";
            licenseText.text += "\tType: " + license.licenseType + ",\n";
            licenseText.text += "\tExpiration: " + license.expirationYear + "/" + license.expirationMonth + "/" + license.expirationDay + ",\n";
            licenseText.text += "},\n";
        }
    }
}
