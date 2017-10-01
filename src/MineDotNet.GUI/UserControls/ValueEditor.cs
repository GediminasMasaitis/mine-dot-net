using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MineDotNet.GUI.UserControls
{
    class ValueEditor<TValue> : ValueEditorBase
    {
        public void SetupEditor(TValue value)
        {
            SetupEditorObject(value);
        }

        public TValue GetValue()
        {
            return (TValue) GetValueObject();
        }
    }
}
