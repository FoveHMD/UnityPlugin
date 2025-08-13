using Fove.Unity;
using UnityEngine;
using UnityEngine.UI;

public class VersionAndBuildDateInfo : MonoBehaviour
{
    [SerializeField] private Text infoText;

    private void Update()
    {
        var versionsResult = FoveManager.QuerySoftwareVersions();

        if (!versionsResult.IsValid)
        {
            string errorText = "Failed to query versions. Error: " + versionsResult.error.ToString();
            infoText.text = errorText;
            Debug.LogWarning(errorText);
        }
        else
        {
            string runtimeVersion = versionsResult.value.runtimeMajor.ToString() + "."+ versionsResult.value.runtimeMinor.ToString() + "." + versionsResult.value.runtimeBuild.ToString();
            string runtimeBuildDate = versionsResult.value.runtimeYear.ToString() + "_" + versionsResult.value.runtimeMonth.ToString() + "_" + versionsResult.value.runtimeDay.ToString();
            string clientVersion = versionsResult.value.clientMajor.ToString() + "." + versionsResult.value.clientMinor.ToString() + "." + versionsResult.value.clientBuild.ToString();
            string clientBuildDate = versionsResult.value.clientYear.ToString() + "_" + versionsResult.value.clientMonth.ToString() + "_" + versionsResult.value.clientDay.ToString();

            infoText.text = "Runtime: \n";
            infoText.text += "{\n";
            infoText.text += "\tVersion: " + runtimeVersion + ",\n";
            infoText.text += "\tBuild date(YYYY-MM-DD): " + runtimeBuildDate + ",\n";
            infoText.text += "},\n";

            infoText.text += "Client: \n";
            infoText.text += "{\n";
            infoText.text += "\tVersion: " + clientVersion + ",\n";
            infoText.text += "\tBuild date(YYYY-MM-DD): " + clientBuildDate + ",\n";
            infoText.text += "},\n";
        }
    }
}
