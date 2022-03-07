using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static float GetPixelsPerPoint() {
        Type utilityType = typeof(GUIUtility);
        PropertyInfo[] allProps = utilityType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
        PropertyInfo property = allProps.First(m => m.Name == "pixelsPerPoint");
        return (float)property.GetValue(null);
    }
}
