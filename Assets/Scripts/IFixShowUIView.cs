using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IFixShowUIView : MonoBehaviour
{
    #region widget
    public Text ifixInfo;
    public Button patchBtn;
    public Toggle interpretTgl;
    public Button attributeBtn;
    public Button mthBtn;
    public Button classBtn;
    public GameObject interpretGroup;
    public Toggle customBridgeTgl;
    public Button interfaceBtn;
    public Button iEnumeratorBtn;
    public Button delegateBtn;
    public GameObject customBridgeGroup;

    private string iFixName;//这个name字段是原生的

    public string IFixName
    {
        [IFix.Interpret]
        set
        {
            iFixName = value;
        }
        [IFix.Interpret]
        get
        {
            return iFixName;
        }
    }

    private void Awake()
    {
        InitWidget();
    }
    private void OnDestroy()
    {
        Clear();
    }
    #endregion
    public void Clear()
    {
        patchBtn.onClick.RemoveListener(PatchBtnClick);
        interpretTgl.onValueChanged.RemoveListener(InterpretClick);
        attributeBtn.onClick.RemoveListener(AttributeBtnClick);
        mthBtn.onClick.RemoveListener(MthBtnClick);
        classBtn.onClick.RemoveListener(ClassBtnClick);
        customBridgeTgl.onValueChanged.RemoveListener(CustomBridgeClick);
        interfaceBtn.onClick.RemoveListener(InterfaceTest);
        iEnumeratorBtn.onClick.RemoveListener(IEnumeratorTest);
        delegateBtn.onClick.RemoveListener(DelegateTest);
    }

    private void InitWidget()
    {
        patchBtn.onClick.AddListener(PatchBtnClick);
        interpretTgl.onValueChanged.AddListener(InterpretClick);
        attributeBtn.onClick.AddListener(AttributeBtnClick);
        mthBtn.onClick.AddListener(MthBtnClick);
        classBtn.onClick.AddListener(ClassBtnClick);
        customBridgeTgl.onValueChanged.AddListener(CustomBridgeClick);
        interfaceBtn.onClick.AddListener(InterfaceTest);
        iEnumeratorBtn.onClick.AddListener(IEnumeratorTest);
        delegateBtn.onClick.AddListener(DelegateTest);
    }

    private void PatchBtnClick()
    {
        ifixInfo.text = string.Format("Add函数结果为:{0}", Add(10, 9));
    }


    private int Add(int a, int b)
    {
        return a + b;
    }

    private void InterpretClick(bool isOn)
    {
        interpretGroup.SetActive(isOn);
    }
    private void CustomBridgeClick(bool isOn)
    {
        customBridgeGroup.SetActive(isOn);
    }

    private void AttributeBtnClick()
    {
        ifixInfo.text = "测试Attribute";
    }

    private void MthBtnClick()
    {
        ifixInfo.text = "测试MthBtn";
    }

    private void ClassBtnClick()
    {
        ifixInfo.text = "测试IFix";
    }

    private void InterfaceTest()
    {
        ifixInfo.text = "测试Interface";
    }

    private void IEnumeratorTest()
    {
        ifixInfo.text = "测试IEnumerator";
    }
    [IFix.Patch]
    private void DelegateTest()
    {
        ifixInfo.text = "测试Delegate";
    }

}