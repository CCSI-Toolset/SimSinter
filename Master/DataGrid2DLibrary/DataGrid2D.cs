using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using System.Reflection;
using System.Data;
using System.Windows.Media;

namespace DataGrid2DLibrary
{
    public class DataGrid2D : DataGrid
    {
        #region Statics

        private static Style s_dataGridColumnHeaderStyle = null;
        private static Style s_dataGridCellStyle = null;
        private static Style s_dataGridRowHeaderStyle = null;
        private static Style s_dataGridRowStyle = null;

        static DataGrid2D()
        {
            Uri resourceLocater = new Uri("/DataGrid2DLibrary;component/Themes/DataGridStyleDictionary.xaml", System.UriKind.Relative);
            ResourceDictionary resourceDictionary = (ResourceDictionary)Application.LoadComponent(resourceLocater);
            s_dataGridColumnHeaderStyle = resourceDictionary["DataGridColumnHeaderStyle"] as Style;
            s_dataGridCellStyle = resourceDictionary["DataGridCellStyle"] as Style;
            s_dataGridRowHeaderStyle = resourceDictionary["DataGridRowHeaderStyle"] as Style;
            s_dataGridRowStyle = resourceDictionary["DataGridRowStyle"] as Style;
        }

        public static readonly DependencyProperty ItemsSource2DProperty =
            DependencyProperty.Register("ItemsSource2D", typeof(IEnumerable), typeof(DataGrid2D), new UIPropertyMetadata(null,
                ItemsSource2DPropertyChanged));

        private static void ItemsSource2DPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            DataGrid2D dataGrid2D = source as DataGrid2D;
            dataGrid2D.OnItemsSource2DChanged(e.OldValue as IEnumerable, e.NewValue as IEnumerable);
        }

        #endregion //Statics

        #region Constructor

        public DataGrid2D() : base()
        {
            AutoGenerateColumns = true;
            CanUserAddRows = false;
            Background = Brushes.White;
            SelectionUnit = DataGridSelectionUnit.Cell;
            AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(DataGrid2D_AutoGeneratingColumn);
            LoadingRow += new EventHandler<DataGridRowEventArgs>(DataGrid2D_LoadingRow);
        }

        #endregion //Constructor

        #region Private Methods

        protected virtual void OnItemsSource2DChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            // Multi Dimensional Arrays with more than 2 dimensions
            // crash on iList[0].
            if (newValue != null && newValue is IList && newValue.GetType().Name.IndexOf("[,,") == -1)
            {
                Type type = newValue.GetType();
                Type elementType = newValue.GetType().GetElementType();

                IList iList = newValue as IList;
                bool multiDimensionalArray = type.IsArray && type.GetArrayRank() == 2;
                if (multiDimensionalArray == true) // 2D MultiDimensional Array
                {
                    BindingHelper bindingHelper = new BindingHelper();
                    MethodInfo method = typeof(BindingHelper).GetMethod("GetBindableMultiDimensionalArray");
                    MethodInfo generic = method.MakeGenericMethod(elementType);
                    ItemsSource = generic.Invoke(bindingHelper, new object[] { newValue }) as DataView;
                }
                else
                {
                    if (iList.Count == 0)
                    {
                        ItemsSource = null;
                        return;
                    }
                    if (iList[0] is IList) // 2D List
                    {
                        IList iListRow1 = iList[0] as IList;
                        if (iListRow1.Count == 0)
                        {
                            ItemsSource = null;
                            return;
                        }
                        Type listType = iListRow1[0].GetType();
                        BindingHelper bindingHelper = new BindingHelper();
                        MethodInfo method = typeof(BindingHelper).GetMethod("GetBindable2DViewFromIList");
                        MethodInfo generic = method.MakeGenericMethod(listType);
                        ItemsSource = generic.Invoke(bindingHelper, new object[] { iList }) as DataView;
                    }
                    else // 1D List
                    {
                        Type listType = iList[0].GetType();
                        BindingHelper bindingHelper = new BindingHelper();
                        MethodInfo method = typeof(BindingHelper).GetMethod("GetBindable1DViewFromIList");
                        MethodInfo generic = method.MakeGenericMethod(listType);
                        ItemsSource = generic.Invoke(bindingHelper, new object[] { iList }) as DataView;
                    }
                }
            }
            else
            {
                ItemsSource = null;
            }
        }

        #endregion // Private Methods

        #region EventHandlers

        void DataGrid2D_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()).ToString(); 
        }

        void DataGrid2D_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridTextColumn column = e.Column as DataGridTextColumn;
            Binding binding = column.Binding as Binding;
            binding.Path = new PropertyPath(binding.Path.Path + ".Value");
        }

        #endregion //EventHandlers

        #region Properties

        public IEnumerable ItemsSource2D
        {
            get { return (IEnumerable)GetValue(ItemsSource2DProperty); }
            set { SetValue(ItemsSource2DProperty, value); }
        }

        private bool m_useModifiedDataGridStyle;
        public bool UseModifiedDataGridStyle
        {
            get
            {
                return m_useModifiedDataGridStyle;
            }
            set
            {
                m_useModifiedDataGridStyle = value;
                if (m_useModifiedDataGridStyle == true)
                {
                    RowHeaderStyle = s_dataGridRowHeaderStyle;
                    CellStyle = s_dataGridCellStyle;
                    ColumnHeaderStyle = s_dataGridColumnHeaderStyle;
                    RowStyle = s_dataGridRowStyle;
                    GridLinesVisibility = DataGridGridLinesVisibility.None;
                }
                else
                {
                    RowHeaderStyle = null;
                    CellStyle = null;
                    ColumnHeaderStyle = null;
                    RowStyle = null;
                    GridLinesVisibility = DataGridGridLinesVisibility.All;
                }
            }
        }

        #endregion //Properties
    }
}
