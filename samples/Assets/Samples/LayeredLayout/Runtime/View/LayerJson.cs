using System;
using UnityEngine;

namespace GameplaySamples.LayeredLayout
{
    public static class LayerJson
    {
        public static bool TryRead(string json, out LayerMap layout, out string error)
        {
            try
            {
                return LayerMap.TryCreate(JsonUtility.FromJson<LayerDocument>(json), out layout, out error);
            }
            catch (ArgumentException)
            {
                layout = null;
                error = "The source must contain a JSON layout object.";
                return false;
            }
        }
    }
}
