using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace SinterConfigGUI
{
    public class VariableTreeViewModel
    {
        #region Data

        readonly ReadOnlyCollection<VariableTreeNodeViewModel> _firstGeneration;
        readonly VariableTreeNodeViewModel _rootNode;
        readonly ICommand _searchCommand;

        IEnumerator<VariableTreeNodeViewModel> _matchingNodeEnumerator;
        string _searchText = String.Empty;

        #endregion // Data

        #region Constructor

        public VariableTreeViewModel(VariableTree.VariableTree dataTree)
        {
            _rootNode = new VariableTreeNodeViewModel(dataTree.rootNode);

            _firstGeneration = new ReadOnlyCollection<VariableTreeNodeViewModel>(
                new VariableTreeNodeViewModel[] 
                { 
                    _rootNode 
                });

            _rootNode.IsExpanded = true;

            _searchCommand = new SearchFamilyTreeCommand(this);
        }

        #endregion // Constructor

        #region Properties

        public VariableTreeNodeViewModel rootNode
        {
            get
            {
                return _rootNode;
            }
        }

        #region FirstGeneration

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ReadOnlyCollection<VariableTreeNodeViewModel> FirstGeneration
        {
            get { return _firstGeneration; }
        }

        #endregion // FirstGeneration

        #region SearchCommand

        /// <summary>
        /// Returns the command used to execute a search in the family tree.
        /// </summary>
        public ICommand SearchCommand
        {
            get { return _searchCommand; }
        }

        private class SearchFamilyTreeCommand : ICommand
        {
            readonly VariableTreeViewModel _familyTree;

            public SearchFamilyTreeCommand(VariableTreeViewModel familyTree)
            {
                _familyTree = familyTree;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because
                // this command never raises the event, and
                // not using the WeakEvent pattern here can
                // cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _familyTree.PerformSearch();
            }
        }

        #endregion // SearchCommand

        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value == _searchText)
                    return;

                _searchText = value;

                _matchingNodeEnumerator = null;
            }
        }

        #endregion // SearchText

        #endregion // Properties

        public void ExpandNode(string path)
        {
            Presenter presenter = Presenter.presenter_singleton;
            //First make sure all the children are loaded on the Variable Tree
            presenter.o_sim.findDataTreeNode(presenter.o_sim.parsePath(path));

            
        
        }

        #region Search Logic

        void PerformSearch()
        {
            if (_matchingNodeEnumerator == null || !_matchingNodeEnumerator.MoveNext())
                this.VerifyMatchingPeopleEnumerator();

            var person = _matchingNodeEnumerator.Current;

            if (person == null)
                return;

            // Ensure that this person is in view.
            if (person.Parent != null)
                person.Parent.IsExpanded = true;

            person.IsSelected = true;
        }

        void VerifyMatchingPeopleEnumerator()
        {
            var matches = this.FindMatches(_searchText, _rootNode);
            _matchingNodeEnumerator = matches.GetEnumerator();

            if (!_matchingNodeEnumerator.MoveNext())
            {
                MessageBox.Show(
                    "No matching names were found.",
                    "Try Again",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
            }
        }

        IEnumerable<VariableTreeNodeViewModel> FindMatches(string searchText, VariableTreeNodeViewModel person)
        {
            if (person.NameContainsText(searchText))
                yield return person;

            foreach (VariableTreeNodeViewModel child in person.Children)
                foreach (VariableTreeNodeViewModel match in this.FindMatches(searchText, child))
                    yield return match;
        }

        #endregion // Search Logic


    }
}
