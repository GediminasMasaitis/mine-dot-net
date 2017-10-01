using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace MineDotNet.GUI.UserControls
{
    public partial class ValueEditorBase : UserControl
    {
        public ValueEditorBase()
        {
            InitializeComponent();
        }

        public string LabelText
        {
            get => NameLabel.Text;
            set => NameLabel.Text = value;
        }

        private Type ValueType { get; set; }

        public Control InnerEditor { get; private set; }

        protected void SetupEditorObject(object currentValue)
        {
            if(currentValue == null)
            {
                throw new ArgumentNullException(nameof(currentValue));
            }

            ValueType = currentValue.GetType();
            if (currentValue is bool)
            {
                var checkBox = new CheckBox();
                checkBox.Checked = (bool) currentValue;
                InnerEditor = checkBox;
            }
            else
            {
                var textBox = new TextBox();
                textBox.Width = 200;
                textBox.Text = currentValue.ToString();
                InnerEditor = textBox;
            }
            Relayout();
            //InnerEditor.Location = new Point(350, 0);
            //InnerEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            //InnerEditor.Dock = DockStyle.Right;
            Controls.Add(InnerEditor);
        }

        protected object GetValueObject()
        {
            if (InnerEditor is CheckBox checkBox)
            {
                return checkBox.Checked;
            }
            if (InnerEditor is TextBox textBox)
            {
                // Us reflection to invoke a static method "Parse" on the object.
                var parseMethod = ValueType.GetMethod("Parse", new[] {typeof(string)});
                var value = parseMethod.Invoke(null, new object[] {textBox.Text});
                return value;
            }
            throw new Exception("Editor not set up properly");
        }

        public void Relayout()
        {
            const int totalWidth = 600;
            const int left = 350; //NameLabel.Right + 10;
            InnerEditor.Location = new Point(left, 0);
            InnerEditor.Width = totalWidth - left;
        }
    }
}
