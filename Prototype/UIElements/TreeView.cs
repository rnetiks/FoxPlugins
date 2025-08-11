using System.Collections.Generic;
using UnityEngine;

namespace Prototype.UIElements
{
    /// <summary>
    /// Represents a generic tree view structure that can display hierarchical data with expandable
    /// and collapsible nodes. It includes features such as dynamic rendering of the nodes based
    /// on a virtual scroll view and selection tracking.
    /// </summary>
    /// <typeparam name="T">The data type stored in each node of the tree view. Must be a reference type.</typeparam>
    public class TreeView<T> where T : class
    {
        public class TreeNode
        {
            public T data;
            public string label;
            public bool isExpanded;
            public List<TreeNode> children = new List<TreeNode>();
            public TreeNode parent;
            public int depth;

            public TreeNode(T data, string label)
            {
                this.data = data;
                this.label = label;
            }
        }

        private TreeNode _root;
        private VirtualScrollView _scrollView;
        private List<TreeNode> _flattenedNodes = new List<TreeNode>();
        private TreeNode _selectedNode;

        public TreeNode Root => _root;
        public TreeNode SelectedNode => _selectedNode;

        public TreeView()
        {
            _scrollView = new VirtualScrollView(20f);
        }

        public void SetRoot(TreeNode root)
        {
            _root = root;
            RefreshFlattenedNodes();
        }

        public void OnGUI(Rect rect)
        {
            RefreshFlattenedNodes();

            _scrollView.BeginScrollView(rect, _flattenedNodes.Count, out var viewRect);

            for (int i = _scrollView.VisibleStartIndex; i <= _scrollView.VisibleEndIndex && i < _flattenedNodes.Count; i++)
            {
                var node = _flattenedNodes[i];
                var itemRect = _scrollView.GetItemRect(i);
                itemRect.width = viewRect.width;

                DrawNode(itemRect, node);
            }

            _scrollView.EndScrollView();
        }

        private void DrawNode(Rect rect, TreeNode node)
        {
            var indentWidth = node.depth * 20f;
            var expandRect = new Rect(rect.x + indentWidth, rect.y, 20f, rect.height);
            var labelRect = new Rect(rect.x + indentWidth + 20f, rect.y, rect.width - indentWidth - 20f, rect.height);

            if (node.children.Count > 0)
            {
                string expandSymbol = node.isExpanded ? "▼" : "▶";
                if (GUI.Button(expandRect, expandSymbol))
                {
                    node.isExpanded = !node.isExpanded;
                    RefreshFlattenedNodes();
                }
            }

            // var style = node == _selectedNode ? GUI.skin.label /* IMGUIManager.Themes.GetThemedStyle("label", "selected")*/ : GUI.skin.button;

            if (GUI.Button(labelRect, node.label/*, style*/))
            {
                _selectedNode = node;
            }
        }

        private void RefreshFlattenedNodes()
        {
            _flattenedNodes.Clear();
            if (_root != null)
            {
                FlattenNode(_root, 0);
            }
        }

        private void FlattenNode(TreeNode node, int depth)
        {
            node.depth = depth;
            _flattenedNodes.Add(node);

            if (node.isExpanded)
            {
                foreach (var child in node.children)
                {
                    FlattenNode(child, depth + 1);
                }
            }
        }
    }
}