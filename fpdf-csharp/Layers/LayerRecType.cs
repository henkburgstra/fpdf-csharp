using System;
using System.Collections.Generic;
using System.Text;

namespace FpdfCsharp.Layers
{
    public struct LayerRecType
    {
        LayerType[] list;
        int currentLayer;
        bool openLayerPane;
    }
}
