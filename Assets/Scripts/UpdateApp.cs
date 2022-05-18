using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public class UpdateApp : MonoBehaviour
{
    [Header("使用本地AB包")]
    public bool _UseLocalABundle = false;

    private void Start()
    {
        SRDebug.Init();
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        Versions.VerifyMode = VerifyMode.Size;
        var operation = Versions.InitializeAsync();

        yield return operation;
        string path = "UI/Updater/UI_MessageBox.prefab";
        var _asset = AssetHelper.Load(path, typeof(GameObject));
        yield return _asset;

        StartCheck();
    }

    public void StartCheck()
    {
        StartCoroutine(Checking());
    }
    private IEnumerator Checking()
    {
        SetMessage("获取版本信息...", 0);
        var checking = Versions.CheckForUpdatesAsync();
        yield return checking;

        if (checking.status == OperationStatus.Failed)
        {
            MessageBox.Show("提示", "更新版本信息失败，请检测网络链接后重试。", ok =>
            {
                if (ok)
                {
                    StartCheck();
                }
                else
                {
                    OnComplete();
                }
            }, "重试", "跳过");
            yield break;
        }

        SetMessage("获取数据准备下载...", 0.1f);
        if (checking.downloadSize > 0)
        {
            DebugHelper.Log("download size.");
            DownloadFiles download = checking.DownloadAsync();
            StartDownloading(download);
            yield break;
        }

        StartCoroutine(OnComplete());
    }
    public void StartDownloading(DownloadFiles download)
    {
        StartCoroutine(Downloading(download));
    }
    private string FormatBytes(long bytes)
    {
        return Utility.FormatBytes(bytes);
    }
    public void ShowProgress(DownloadFiles download)
    {
        download.completed += o => { };
        download.updated += o =>
        {
            var current = Download.TotalDownloadedBytes;
            var max = Download.TotalSize;
            var speed = Download.TotalBandwidth;
            SetMessage($"加载中...{FormatBytes(current)}/{FormatBytes(max)}(速度 {FormatBytes(speed)}/s)", current * 1f / max);
        };
    }
    private IEnumerator Downloading(DownloadFiles download)
    {
        ShowProgress(download);
        yield return download;
        if (download.status == OperationStatus.Failed)
        {
            var messageBox = MessageBox.Show("提示！", "下载失败！请检查网络状态后重试。");
            yield return messageBox;
            if (messageBox.ok)
            {
                download.Retry();
                StartDownloading(download);
            }
            else
            {
                Application.Quit();
            }

            yield break;
        }

        GetDownloadSizeAsync();
    }

    private void GetDownloadSizeAsync()
    {
        StartCoroutine(GetDownloadSize());
    }
    private IEnumerator GetDownloadSize()
    {
        var getDownloadSize = Versions.GetDownloadSizeAsync();
        yield return getDownloadSize;
        if (getDownloadSize.downloadSize > 0)
        {
            StartDownloading(getDownloadSize.DownloadAsync());
            yield break;
        }

        StartCoroutine(OnComplete());
    }
    private IEnumerator OnComplete()
    {
        SetVersion();
        SetMessage("更新完成", 1);

        yield return new WaitForSeconds(1);

        StartCoroutine(LoadScene());
    }
    private IEnumerator LoadScene()
    {
        var scene = Scene.LoadAsync("Assets/Scenes/SampleScene.unity");
        yield return scene;
    }

    //=========================================================================
    //=========================================================================
    public Slider progressBar;
    public Text version;
    public Text message;
    public Text toolTip;
    private void SetVersion()
    {
        version.text = $"资源版本:{Versions.ManifestVersion}";
    }

    private void SetMessage(string desc, float processVal)
    {
        message.text = $"{desc}";

        progressBar.value = processVal;
    }

}
