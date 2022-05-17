using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelperManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //===============================================================
    private void Init()
    {
        HotfixHelper.InitInjectFix();
        ResourceHelper.Init();
    }
}
