using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Android2Unity : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TestAndroid2Unity(string value) {
        Debug.Log("yush TestAndroid2Unity " + value);
    }

    public void AlipayResult(string value) {
        BoyuePurchase.BoyuePurchaseManager.AlipayResult(value);
    }
}
