using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MineDotNet.GUI.Controls
{
    public partial class ObjectEditor : UserControl
    {
        private object _target;
        private readonly List<PropertyRow> _rows = new List<PropertyRow>();

        public ObjectEditor() { InitializeComponent(); }

        public void SetupObject(object obj)
        {
            _target = obj;
            RowsPanel.Children.Clear();
            _rows.Clear();
            if (obj == null) return;

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (!prop.CanRead) continue;
                var value = prop.GetValue(obj);
                var row = BuildRow(prop, value);
                if (row == null) continue;
                _rows.Add(row);
                RowsPanel.Children.Add(row.Host);
            }
        }

        public object GetObject()
        {
            foreach (var row in _rows) row.Apply(_target);
            return _target;
        }

        private static PropertyRow BuildRow(PropertyInfo prop, object value)
        {
            if (value == null) return null;
            var type = value.GetType();

            var grid = new Grid { Margin = new Thickness(12, 4, 12, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = Humanize(prop.Name),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            FrameworkElement editor;
            Func<object> getValue;
            if (value is bool b)
            {
                var cb = new CheckBox { IsChecked = b, VerticalAlignment = VerticalAlignment.Center };
                editor = cb;
                getValue = () => cb.IsChecked == true;
            }
            else
            {
                var tb = new TextBox
                {
                    Text = value.ToString(),
                    VerticalAlignment = VerticalAlignment.Center
                };
                editor = tb;
                getValue = () =>
                {
                    var parse = type.GetMethod("Parse", new[] { typeof(string) });
                    return parse != null ? parse.Invoke(null, new object[] { tb.Text }) : value;
                };
            }
            Grid.SetColumn(editor, 1);
            grid.Children.Add(editor);

            return new PropertyRow(grid, target => prop.SetValue(target, getValue()));
        }

        private static string Humanize(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1])) sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpper(c) : c);
            }
            return sb.ToString();
        }

        private sealed class PropertyRow
        {
            public PropertyRow(FrameworkElement host, Action<object> apply) { Host = host; Apply = apply; }
            public FrameworkElement Host { get; }
            public Action<object> Apply { get; }
        }
    }
}
