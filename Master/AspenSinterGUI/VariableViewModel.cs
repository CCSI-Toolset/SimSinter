using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using sinter;

namespace SinterConfigGUI
{
    public class VariableViewModel : INotifyPropertyChanged, IEquatable<VariableViewModel>
    {
        #region data
        sinter_Variable o_variable;
        bool _isSelected;

        #endregion data

        public VariableViewModel(sinter_Variable variable)
        {
            o_variable = variable;
        }

        #region SinterProperties

        public bool isDynamic
        {
            get { return o_variable.isDynamicVariable; }
            set
            {
                if (!o_variable.isSetting)  //You can't change settings dynamisism.  (Actually they are all not dynamic)
                {
                    if (value != o_variable.isDynamicVariable)
                    {
                        if (value == true)
                        {
                            if (o_variable.isScalar)
                            {
                                o_variable = new sinter_DynamicScalar(o_variable);
                            }
                            else
                            { //is Vector
                                o_variable = new sinter_DynamicVector((sinter_Vector)o_variable);
                            }
                        }
                        else  //Make it not dynamic
                        {
                            if (o_variable.isScalar)
                            {
                                o_variable = new sinter_Variable((sinter_DynamicScalar)o_variable);
                            }
                            else
                            { //is Vector
                                o_variable = new sinter_Vector((sinter_DynamicVector)o_variable);
                            }

                        }

                    }
                    //                _isDynamic = value; 
                    OnPropertyChanged("isDynamic");
                }
            }
        }

        public sinter_Variable variable
        {
            get
            {
                return o_variable;
            }
        }

        public string name
        {
            get { return o_variable.name; }
            set
            {
                o_variable.name = value;
                OnPropertyChanged("name");
            }
        }

        public string type
        {
            get { return o_variable.typeString; }
            set { o_variable.typeString = value; }
        }

        public string path
        {
            get { return o_variable.addressStrings[0]; }
        }

        public string units
        {
            get { return o_variable.units; }
            set
            {
                o_variable.units = value;
                o_variable.defaultUnits = value;
                OnPropertyChanged("units");
            }
        }

        public string description
        {
            get { return o_variable.description; }
            set
            {
                o_variable.description = value;
                OnPropertyChanged("description");
            }
        }

        public object dfault
        {
            get { return o_variable.dfault; }
            set
            {
                o_variable.Value = value;
                o_variable.dfault = value;
                OnPropertyChanged("default");
                OnPropertyChanged("value");
            }
        }

        public object value
        {
            get { return o_variable.Value; }
            set
            {
                o_variable.Value = value;
                o_variable.dfault = value;
                OnPropertyChanged("default");
                OnPropertyChanged("value");
            }
        }


        public object minimum
        {
            get { return o_variable.minimum; }
            set
            {
                o_variable.minimum = value;
                OnPropertyChanged("minimum");
            }
        }

        public object maximum
        {
            get { return o_variable.maximum; }
            set
            {
                o_variable.maximum = value;
                OnPropertyChanged("maximum");
            }
        }

        public sinter_Variable.sinter_IOMode mode
        {
            get { return o_variable.mode; }
            set
            {
                o_variable.mode = value;
                OnPropertyChanged("mode");
            }
        }

        public bool isSetting
        {
            get { return o_variable.isSetting; }
        }

        public bool isVec
        {
            get { return o_variable.isVec; }
        }

        #endregion SinterProperties

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the Variable 
        /// associated with this object is selected in the datagrid.
        /// I DON'T THINK THIS IS EVER CALLED
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; }
        }
        #endregion IsSelected

        #region IEquatable

        public bool Equals(VariableViewModel other)
        {
            if (other == null)
                return false;
            return other.name == this.name;
        }


        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            VariableViewModel otherVM = obj as VariableViewModel;
            if (otherVM == null)
                return false;
            else
                return Equals(otherVM);
        }   

        #endregion IEquatable

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
    }
}
