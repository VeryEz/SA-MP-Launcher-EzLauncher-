using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace WpfApp1
{
    class DontMindMe
    {
        private DataGridRow? FindParentDataGridRow(DependencyObject obj)
        {
            while (obj != null && obj.GetType() != typeof(DataGridRow))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            return obj as DataGridRow;
        }
    }
}
