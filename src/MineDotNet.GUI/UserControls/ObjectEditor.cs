using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MineDotNet.GUI.UserControls
{
    class ObjectEditor : UserControl
    {
        public ObjectEditor()
        {
            AutoScroll = true;
        }

        private object TargetObject { get; set; }
        private IList<ObjectEditorEntry> Entries { get; set; }

        private class ObjectEditorEntry
        {
            public ObjectEditorEntry(ValueEditor<object> editor, Action<object> apply)
            {
                Editor = editor;
                Apply = apply;
            }

            public ValueEditor<object> Editor { get; }
            public Action<object> Apply { get; }
        }

        public void SetupObject(object obj)
        {
            if (Entries != null)
            {
                foreach (var innerEditor in Entries)
                {
                    Controls.Remove(innerEditor.Editor);
                }
            }
            var type = obj.GetType();
            var properties = type.GetProperties();
            Entries = new List<ObjectEditorEntry>();
            var offset = 30;
            var currentOffset = 0;
            foreach (var property in properties)
            {
                var value = property.GetValue(obj, null);
                var editor = new ValueEditor<object>();
                editor.LabelText = property.Name;
                editor.Location = new Point(0, currentOffset);
                editor.SetupEditor(value);
                currentOffset += offset;
                var entry = new ObjectEditorEntry(editor, o => property.SetValue(o, editor.GetValue(), null));
                Entries.Add(entry);
            }
            var controlsArr = Entries.Select(x => (Control)x.Editor).ToArray();
            Controls.AddRange(controlsArr);
            TargetObject = obj;
        }

        public object GetObject()
        {
            foreach (var entry in Entries)
            {
                entry.Apply(TargetObject);
            }
            return TargetObject;
        }
    }
}
