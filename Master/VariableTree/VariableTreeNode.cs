using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VariableTree
{

    public delegate void VariableTreeVisitor(VariableTreeNode node);

    public class VariableTreeNode
    {

        public Dictionary<string, VariableTreeNode> o_children = new Dictionary<string, VariableTreeNode>();
        public VariableTreeNode parent { get; set; }
        public string prettyName { get; set; }  //The name users see.  Usually the same as name, but not for Aspen 2D objects
        public string name { get; set; }
        public string path { get; set;}
        public char pathSeperator { get; set;}
        public static readonly VariableTreeNode DummyChild = new VariableTreeNode("Loading", "Loading", '.');
//        public int vectorIndexMin = int.MaxValue;
//        public int vectorIndexMax = -1;

        public IDictionary<string, VariableTreeNode> children
        {
            get
            {
                return o_children;
            }
        }

        public VariableTreeNode(string printName, string nodeName, string nodePath, char seperator)
        {
            prettyName = printName;
            name = nodeName;
            path = nodePath;
            pathSeperator = seperator;
            o_children.Add("DummyChild", DummyChild);
        }

        public VariableTreeNode(string nodeName, string nodePath, char seperator)
        {
            name = nodeName;
            prettyName = name;
            path = nodePath;
            pathSeperator = seperator;
            o_children.Add("DummyChild", DummyChild);
        }

        public void addChild(VariableTreeNode child) {
            child.parent = this;
            children.Add(child.name, child);
        }

        public void traverse(VariableTreeNode node, VariableTreeVisitor visitor)
        {
            visitor(node);
            foreach (KeyValuePair<string, VariableTreeNode> kid in node.children)
                traverse(kid.Value, visitor);
        }

//        public bool isVector() {
//            return vectorIndexMax > 0;
//       }

        public bool isLeaf()
        {
            return o_children == null || o_children.Count <= 0;
        }

        public VariableTreeNode resolveNode(IList<String> pathArray)
        {
            if (pathArray.Count == 0 || pathArray[0] == "")
            {
                return this;
            }

            string childName = pathArray[0];
            VariableTreeNode child = o_children[childName];

            if (pathArray.Count == 1)
            {
                return child;
            }
            else
            {
                pathArray.RemoveAt(0); 
                return child.resolveNode(pathArray);
            }
        }

        public void addNode(IList<String> pathArray)
        {
            if (pathArray.Count == 0)
            {
                return;
            }

            string childName = pathArray[0];
            if (o_children.ContainsKey(childName))
            {
                VariableTreeNode child = o_children[childName];
                pathArray.RemoveAt(0);
                child.addNode(pathArray);
            }
            else
            {
                VariableTreeNode child = new VariableTreeNode(childName, this.path + pathSeperator + childName, pathSeperator); 
                this.addChild(child);
                pathArray.RemoveAt(0);
                child.addNode(pathArray);
            }

        }

    }
}
