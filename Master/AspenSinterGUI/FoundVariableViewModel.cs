using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SinterConfigGUI
{
    public class FoundVariableViewModel : INotifyPropertyChanged, IEquatable<FoundVariableViewModel>
    {
        #region data
        String o_name = "";

        bool o_isVector = false;

        bool _isSelected;

        #endregion data

        public FoundVariableViewModel(string name)
        {
            o_name = name;
        }

        #region Properties

        public string name
        {
            get
            {
                return o_name;
            }
            set
            {
                o_name = value;
                OnPropertyChanged("name");
            }
        }

        public bool isVector
        {
            get
            {
                return o_isVector;
            }
            set
            {
                o_isVector = value;
                OnPropertyChanged("isVector");
            }
        }

        //public int indexMin
        //{
        //    get
        //    {
        //        return o_indexMin;
        //    }
        //    set
        //    {
        //        o_indexMin = value;
        //        OnPropertyChanged("indexMin");
        //    }
        //}

        //public int indexMax
        //{
        //    get
        //    {
        //        return o_indexMax;
        //    }
        //    set
        //    {
        //        o_indexMax = value;
        //        OnPropertyChanged("indexMax");
        //    }
        //}


        #endregion Properties

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the Variable 
        /// associated with this object is selected in the datagrid.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { 
                _isSelected = value;
                this.OnPropertyChanged("IsSelected");
                if (_isSelected)
                {
                    Presenter presenter = Presenter.presenter_singleton;
                    presenter.previewVariablePath = name;
                }
            }
        }
        #endregion IsSelected

        #region IEquatable

        public bool Equals(FoundVariableViewModel other)
        {
            if (other == null)
                return false;
            return other.name == this.name;
        }


        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            FoundVariableViewModel otherVM = obj as FoundVariableViewModel;
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
