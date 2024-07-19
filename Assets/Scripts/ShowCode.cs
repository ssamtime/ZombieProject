using Fusion.Menu;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using static Fusion.Allocator;
using TMPro;
using UnityEngine.UI;

public class ShowCode : FusionMenuUIScreen
{
    public TextMeshProUGUI textMeshProUGUI;
    
    void Update()
    {
        if(Connection != null)
            textMeshProUGUI.text = Connection.SessionName;
    }
}
