using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/BuildConfig")]
public class BuildConfig : ScriptableObject
{
    public string ProductName = "TestName";
    [Space(2)]
    public string KeystoreName = "Test.keystore";
    public string KeyaliasPass = "Test2022";
    public string KeyaliasName = "Test";
    public string KeystorePass = "Test2022";
    [Space(2)]
    public bool ExportAsGoogleAndroidProject = true;
    public string BuildNum = "0";
    public bool BuildNumAutoAdd = true;
    [Space(2)]
    public bool _GeneralAPK = true;
}
