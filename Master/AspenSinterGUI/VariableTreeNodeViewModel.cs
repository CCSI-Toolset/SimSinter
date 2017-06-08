using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using sinter;

namespace SinterConfigGUI
{
    public class VariableTreeNodeViewModel : INotifyPropertyChanged
    {

        #region Data
        readonly ObservableCollection<VariableTreeNodeViewModel> _children;
        readonly VariableTreeNodeViewModel _parent;
        readonly VariableTree.VariableTreeNode _node;
        //Dummy Child is a place holder for where we think there may be children, but they haven't been loaded yet
        static readonly VariableTreeNodeViewModel DummyChild = new VariableTreeNodeViewModel(VariableTree.VariableTreeNode.DummyChild);

        bool _isExpanded;
        bool _isSelected;


        #endregion // Data

        #region constructors 

        public VariableTreeNodeViewModel(VariableTree.VariableTreeNode node)
            : this(node, null)
        {
        }

        private VariableTreeNodeViewModel(VariableTree.VariableTreeNode node, VariableTreeNodeViewModel parent)
        {
            _node = node;
            _parent = parent;
            _children = new ObservableCollection<VariableTreeNodeViewModel>();
            _children.Add(DummyChild);

            //            _children = new ReadOnlyCollection<VariableTreeNodeViewModel>(
//                    (from KeyValuePair<String, VariableTree.VariableTreeNode> entry in _node.children
//                     select new VariableTreeNodeViewModel(entry.Value, this))
//                     .ToList<VariableTreeNodeViewModel>());
        }

        #endregion constructors

        #region properties
        public ObservableCollection<VariableTreeNodeViewModel> Children
        {
            get { return _children; }
        }

        public string PrettyName
        {
            get { return _node.prettyName; }
        }

        public string Name
        {
            get { return _node.name; }
        }

        public string Path
        {
            get { return _node.path; }
        }

        #endregion properties

        #region Presentation Members

        #region HasLoadedChildren

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        #endregion // HasLoadedChildren

        public VariableTreeNodeViewModel resolveNode(IList<String> pathArray)
        {
            if (pathArray.Count == 0 || pathArray[0] == "")
            {
                return this;
            }

            if (HasDummyChild)
            {
                LoadChildren();
            }


            string childName = pathArray[0];
            foreach (VariableTreeNodeViewModel child in _children) { //inefficent search, optimize later if necessary
                if (child.Name == childName)
                {
                    pathArray.RemoveAt(0);
                    return child.resolveNode(pathArray);
                }
            }
            throw new System.Collections.Generic.KeyNotFoundException(String.Format("Node {0} not a child of {1}", childName, Path));
        }

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (this.HasDummyChild)
                {
                    this.LoadChildren();
                }
            }
        }

        #endregion // IsExpanded

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
            Presenter presenter = Presenter.presenter_singleton;

            presenter.o_sim.findDataTreeNode(presenter.o_sim.parsePath(_node.path));

            this.Children.Remove(DummyChild);

            foreach (KeyValuePair<String, VariableTree.VariableTreeNode> entry in _node.children) {
                _children.Add(new VariableTreeNodeViewModel(entry.Value, this));
            }
        }

        #endregion LoadChildren

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                    if (_isSelected)
                    {
                        IsExpanded = true;
                        Presenter presenter = Presenter.presenter_singleton;
                        presenter.previewVariablePath = Path;
                    }
                }
            }
        }

        #endregion // IsSelected

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region Parent

        public VariableTreeNodeViewModel Parent
        {
            get { return _parent; }
        }

        #endregion // Parent

        #endregion Presentation Members

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
