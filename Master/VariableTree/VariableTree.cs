using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VariableTree
{
    public class VariableTree
    {
        public VariableTreeNode rootNode {get; set;}
        public delegate IList<string> ParsePath(string path);

        ParsePath parsePath;

        char pathSeperator;

        public VariableTree(ParsePath i_parsePath, char seperator)
        {
            parsePath = i_parsePath;
            pathSeperator = seperator;
        }

        public VariableTreeNode resolveNode(string path)
        {
            IList<string> pathArray = parsePath(path);
            return rootNode.resolveNode(pathArray);
        }


    }
}
