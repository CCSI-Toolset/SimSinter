using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;

namespace DataGrid2DLibrary
{
    public class BindingHelper
    {
        public DataView GetBindableMultiDimensionalArray<T>(T[,] array)
        {
            DataTable dataTable = new DataTable();
            for (int i = 0; i < array.GetLength(1); i++)
            {
                dataTable.Columns.Add(i.ToString(), typeof(Ref<T>));
            }
            for (int i = 0; i < array.GetLength(0); i++)
            {
                DataRow dataRow = dataTable.NewRow();
                dataTable.Rows.Add(dataRow);
            }
            DataView dataView = new DataView(dataTable);
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    int a = i;
                    int b = j;
                    Ref<T> refT = new Ref<T>(() => array[a, b], z => { array[a, b] = z; });
                    dataView[i][j] = refT;
                }
            }
            return dataView;
        }

        public DataView GetBindable1DViewFromIList<T>(IList<T> list1d)
        {
            DataTable dataTable = new DataTable();
            for (int i = 0; i < list1d.Count; i++)
            {
                dataTable.Columns.Add(i.ToString(), typeof(Ref<T>));
            }
            DataRow dataRow = dataTable.NewRow();
            dataTable.Rows.Add(dataRow);
            DataView dataView = new DataView(dataTable);
            for (int i = 0; i < list1d.Count; i++)
            {
                int a = i;
                Ref<T> refT = new Ref<T>(() => list1d[a], z => { list1d[a] = z; });
                dataView[0][i] = refT;
            }
            return dataView;
        }

        public DataView GetBindable2DViewFromIList<T>(IList list2d)
        {
            DataTable dataTable = new DataTable();
            for (int i = 0; i < ((IList)list2d[0]).Count; i++)
            {
                dataTable.Columns.Add(i.ToString(), typeof(Ref<T>));
            }
            for (int i = 0; i < list2d.Count; i++)
            {
                DataRow dataRow = dataTable.NewRow();
                dataTable.Rows.Add(dataRow);
            }
            DataView dataView = new DataView(dataTable);
            for (int i = 0; i < list2d.Count; i++)
            {
                for (int j = 0; j < ((IList)list2d[i]).Count; j++)
                {
                    int a = i;
                    int b = j;
                    Ref<T> refT = new Ref<T>(() => (list2d[a] as IList<T>)[b], z => { (list2d[a] as IList<T>)[b] = z; });                    
                    dataView[i][j] = refT;
                }
            }
            return dataView;
        }
    }
}
