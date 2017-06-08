using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace SinterConfigGUI
{
    public class VectorViewModel : INotifyPropertyChanged, IEquatable<VectorViewModel>
    {
        #region data
        sinter.sinter_Vector s_vector;
        bool o_sizeCanChange;
        #endregion data

        public VectorViewModel(sinter.sinter_Vector in_vec, bool sizechangeable)
        {
            s_vector = in_vec;
//            o_data = (System.Array) in_vec.dfault;
            o_sizeCanChange = sizechangeable;
        }

        public bool sizeCanChange
        {
            get { return o_sizeCanChange; }
        }

        public int size
        {
            get { return s_vector.size; }
            set
            {
                if (sizeCanChange)
                {
                    s_vector.dfault = ResizeArray((System.Array)s_vector.dfault, value);
                    s_vector.Value = ResizeArray((System.Array)s_vector.Value, value);
                    OnPropertyChanged("size");
                    OnPropertyChanged("dataArray");
                }
            }
        }

        public string name
        {
            get { return s_vector.name; }
            set
            {
                s_vector.name = value;
                OnPropertyChanged("name");
            }
        }

        public System.Array dataArray
        {
            get { return (System.Array)s_vector.Value; }
            set {
                s_vector.dfault = value;
                s_vector.Value = value;
                OnPropertyChanged("size");
                OnPropertyChanged("dataArray");
            }
        }

        // Reallocates an array with a new size, and copies the contents
        // of the old array to the new array.
        // Arguments:
        //   oldArray  the old array, to be reallocated.
        //   newSize   the new array size.
        // Returns     A new array with the same contents.
        public static System.Array ResizeArray(System.Array oldArray, int newSize)
        {
            int oldSize = oldArray.Length;
            System.Type elementType = oldArray.GetType().GetElementType();
            System.Array newArray = System.Array.CreateInstance(elementType, newSize);
            int preserveLength = System.Math.Min(oldSize, newSize);
            if (preserveLength > 0)
                System.Array.Copy(oldArray, newArray, preserveLength);
            return newArray;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string strPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
            }
        }
        #endregion INotifyPropertyChanged Members

        #region IEquatable

        public bool Equals(VectorViewModel other)
        {
            if (other == null)
                return false;
            return other.name == this.name;
        }


        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            VectorViewModel otherVM = obj as VectorViewModel;
            if (otherVM == null)
                return false;
            else
                return Equals(otherVM);
        }

        #endregion IEquatable
    }
}
