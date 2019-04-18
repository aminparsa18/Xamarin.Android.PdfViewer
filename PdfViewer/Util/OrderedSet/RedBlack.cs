using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace PdfViewer.Util.OrderedSet
{
    internal enum DuplicatePolicy
    {
        InsertFirst, // Insert a new node before duplicates
        InsertLast, // Insert a new node after duplicates
        ReplaceFirst, // Replace the first of the duplicate nodes
        ReplaceLast, // Replace the last of the duplicate nodes
        DoNothing // Do nothing to the tree
    };

    [Serializable]
    internal class RedBlackTree<T> : IEnumerable<T>
    {
        private readonly IComparer<T> comparer; // interface for comparing elements, only Compare is used.
        private Node root; // The root of the tree. Can be null when tree is empty.

        private int changeStamp; // An integer that is changed every time the tree structurally changes.
        // Used so that enumerations throw an exception if the tree is changed
        // during enumeration.

        private Node[] stack; // A stack of nodes. This is cached locally to avoid constant re-allocated it.

        private Node[] GetNodeStack()
        {
            // Maximum depth needed is 2 * lg count + 1.
            int maxDepth;
            if (ElementCount < 0x400)
                maxDepth = 21;
            else if (ElementCount < 0x10000)
                maxDepth = 41;
            else
                maxDepth = 65;

            if (stack == null || stack.Length < maxDepth)
                stack = new Node[maxDepth];

            return stack;
        }

        [Serializable]
        private class Node
        {
            public Node left, right;
            public T item;

            private const uint REDMASK = 0x80000000;
            private uint count;

            public bool IsRed
            {
                get => (count & REDMASK) != 0;
                set
                {
                    if (value)
                        count |= REDMASK;
                    else
                        count &= ~REDMASK;
                }
            }
            public int Count
            {
                get => (int) (count & ~REDMASK);
                set => count = (count & REDMASK) | (uint) value;
            }

            public void IncrementCount()
            {
                ++count;
            }

            public void DecrementCount()
            {
                Debug.Assert(Count != 0);
                --count;
            }
        }

        internal void StopEnumerations()
        {
            ++changeStamp;
        }

        private void CheckEnumerationStamp(int startStamp)
        {
            if (startStamp != changeStamp)
            {
                throw new InvalidOperationException(Strings.ChangeDuringEnumeration);
            }
        }


        public RedBlackTree(IComparer<T> comparer)
        {
            this.comparer = comparer;
            ElementCount = 0;
            root = null;
        }

        public int ElementCount { get; private set; }

        public bool Find(T key, bool findFirst, bool replace, out T item)
        {
            Node current = root; // current search location in the tree
            Node found = null; // last node found with the key, or null if none.

            while (current != null)
            {
                int compare = comparer.Compare(key, current.item);

                if (compare < 0)
                {
                    current = current.left;
                }
                else if (compare > 0)
                {
                    current = current.right;
                }
                else
                {
                    // Go left/right on equality to find first/last of elements with this key.
                    Debug.Assert(compare == 0);
                    found = current;
                    if (findFirst)
                        current = current.left;
                    else
                        current = current.right;
                }
            }

            if (found != null)
            {
                item = found.item;
                if (replace)
                    found.item = key;
                return true;
            }

            item = default(T);
            return false;
        }

        public bool Insert(T item, DuplicatePolicy dupPolicy, out T previous)
        {
            Node node = root;
            Node parent = null, gparent = null, ggparent = null; // parent, grand, a great-grantparent of node.
            bool wentLeft = false, wentRight = false; // direction from parent to node.
            bool rotated;
            Node duplicateFound = null;

            // The tree may be changed.
            StopEnumerations();

            // We increment counts on the way down the tree. If we end up not inserting an items due
            // to a duplicate, we need a stack to adjust the counts back. We don't need the stack if the duplicate
            // policy means that we will always do an insertion.
            bool needStack = !((dupPolicy == DuplicatePolicy.InsertFirst) || (dupPolicy == DuplicatePolicy.InsertLast));
            Node[] nodeStack = null;
            int nodeStackPtr = 0; // first free item on the stack.
            if (needStack)
                nodeStack = GetNodeStack();

            while (node != null)
            {
                // If we find a node with two red children, split it so it doesn't cause problems
                // when inserting a node.
                if (node.left != null && node.left.IsRed && node.right != null && node.right.IsRed)
                {
                    node = InsertSplit(ggparent, gparent, parent, node, out rotated);

                    if (needStack && rotated)
                    {
                        nodeStackPtr -= 2;
                        if (nodeStackPtr < 0)
                            nodeStackPtr = 0;
                    }
                }

                // Keep track of parent, grandparent, great-grand parent.
                ggparent = gparent;
                gparent = parent;
                parent = node;

                // Compare the key and the node. 
                int compare = comparer.Compare(item, node.item);

                if (compare == 0)
                {
                    // Found a node with the data already. Check duplicate policy.
                    if (dupPolicy == DuplicatePolicy.DoNothing)
                    {
                        previous = node.item;

                        // Didn't insert after all. Return counts back to their previous value.
                        for (int i = 0; i < nodeStackPtr; ++i)
                            nodeStack[i].DecrementCount();

                        return false;
                    }

                    if (dupPolicy == DuplicatePolicy.InsertFirst || dupPolicy == DuplicatePolicy.ReplaceFirst)
                    {
                        // Insert first by treating the key as less than nodes in the tree.
                        duplicateFound = node;
                        compare = -1;
                    }
                    else
                    {
                        Debug.Assert(
                            dupPolicy == DuplicatePolicy.InsertLast || dupPolicy == DuplicatePolicy.ReplaceLast);
                        // Insert last by treating the key as greater than nodes in the tree.
                        duplicateFound = node;
                        compare = 1;
                    }
                }

                Debug.Assert(compare != 0);

                node.IncrementCount();
                if (needStack)
                    nodeStack[nodeStackPtr++] = node;

                // Move to the left or right as needed to find the insertion point.
                if (compare < 0)
                {
                    node = node.left;
                    wentLeft = true;
                    wentRight = false;
                }
                else
                {
                    node = node.right;
                    wentRight = true;
                    wentLeft = false;
                }
            }

            if (duplicateFound != null)
            {
                previous = duplicateFound.item;

                // Are we replacing instread of inserting?
                if (dupPolicy == DuplicatePolicy.ReplaceFirst || dupPolicy == DuplicatePolicy.ReplaceLast)
                {
                    duplicateFound.item = item;

                    // Didn't insert after all. Return counts back to their previous value.
                    for (int i = 0; i < nodeStackPtr; ++i)
                        nodeStack[i].DecrementCount();

                    return false;
                }
            }
            else
            {
                previous = default(T);
            }

            // Create a new node.
            node = new Node();
            node.item = item;
            node.Count = 1;

            // Link the node into the tree.
            if (wentLeft)
                parent.left = node;
            else if (wentRight)
                parent.right = node;
            else
            {
                Debug.Assert(root == null);
                root = node;
            }

            // Maintain the red-black policy.
            InsertSplit(ggparent, gparent, parent, node, out rotated);

            // We've added a node to the tree, so update the count.
            ElementCount += 1;

            return (duplicateFound == null);
        }

        private Node InsertSplit(Node ggparent, Node gparent, Node parent, Node node, out bool rotated)
        {
            if (node != root)
                node.IsRed = true;
            if (node.left != null)
                node.left.IsRed = false;
            if (node.right != null)
                node.right.IsRed = false;

            if (parent != null && parent.IsRed)
            {
                // Since parent is red, gparent can't be null (root is always black). ggparent
                // might be null, however.
                Debug.Assert(gparent != null);

                // if links from gparent and parent are opposite (left/right or right/left),
                // then rotate.
                if ((gparent.left == parent) != (parent.left == node))
                {
                    Rotate(gparent, parent, node);
                    parent = node;
                }

                gparent.IsRed = true;

                // Do a rotate to prevent two red links in a row.
                Rotate(ggparent, gparent, parent);

                parent.IsRed = false;
                rotated = true;
                return parent;
            }

            rotated = false;
            return node;
        }

        private void Rotate(Node node, Node child, Node gchild)
        {
            if (gchild == child.left)
            {
                child.left = gchild.right;
                gchild.right = child;
            }
            else
            {
                Debug.Assert(gchild == child.right);
                child.right = gchild.left;
                gchild.left = child;
            }

            // Restore the counts.
            child.Count = (child.left?.Count ?? 0) + (child.right?.Count ?? 0) +
                          1;
            gchild.Count = (gchild.left?.Count ?? 0) +
                           (gchild.right?.Count ?? 0) + 1;

            if (node == null)
            {
                Debug.Assert(child == root);
                root = gchild;
            }
            else if (child == node.left)
            {
                node.left = gchild;
            }
            else
            {
                Debug.Assert(child == node.right);
                node.right = gchild;
            }
        }
    
        public bool Delete(T key, bool deleteFirst, out T item)
        {
            return DeleteItemFromRange(EqualRangeTester(key), deleteFirst, out item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return EnumerateRange(EntireRangeTester).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

      
        public delegate int RangeTester(T item);

        public RangeTester EqualRangeTester(T equalTo)
        {
            return item => comparer.Compare(item, equalTo);
        }

        public int EntireRangeTester(T item)
        {
            return 0;
        }

        public IEnumerable<T> EnumerateRange(RangeTester rangeTester)
        {
            return EnumerateRangeInOrder(rangeTester, root);
        }

        private IEnumerable<T> EnumerateRangeInOrder(RangeTester rangeTester, Node node)
        {
            int startStamp = changeStamp;

            if (node != null)
            {
                int compare = rangeTester(node.item);

                if (compare >= 0)
                {
                    // At least part of the range may lie to the left.
                    foreach (T item in EnumerateRangeInOrder(rangeTester, node.left))
                    {
                        yield return item;
                        CheckEnumerationStamp(startStamp);
                    }
                }

                if (compare == 0)
                {
                    // The item is within the range.
                    yield return node.item;
                    CheckEnumerationStamp(startStamp);
                }

                if (compare <= 0)
                {
                    // At least part of the range lies to the right.
                    foreach (T item in EnumerateRangeInOrder(rangeTester, node.right))
                    {
                        yield return item;
                        CheckEnumerationStamp(startStamp);
                    }
                }
            }
        }

    
        public bool DeleteItemFromRange(RangeTester rangeTester, bool deleteFirst, out T item)
        {
            Node node; // The current node.
            Node parent; // Parent of the current node.
            Node gparent; // Grandparent of the current node.
            Node sib; // Sibling of the current node.
            Node keyNode; // Node with the key that is being removed.

            // The tree may be changed.
            StopEnumerations();

            if (root == null)
            {
                // Nothing in the tree. Go home now.
                item = default(T);
                return false;
            }

            // We decrement counts on the way down the tree. If we end up not finding an item to delete
            // we need a stack to adjust the counts back. 
            Node[] nodeStack = GetNodeStack();
            int nodeStackPtr = 0; // first free item on the stack.

            // Start at the root.
            node = root;
            sib = parent = gparent = null;
            keyNode = null;

            // Proceed down the tree, making the current node red so it can be removed.
            for (;;)
            {
                Debug.Assert(parent == null || parent.IsRed);
                Debug.Assert(sib == null || !sib.IsRed);
                Debug.Assert(!node.IsRed);

                if ((node.left == null || !node.left.IsRed) && (node.right == null || !node.right.IsRed))
                {
                    // node has two black children (null children are considered black).
                    if (parent == null)
                    {
                        // Special case for the root.
                        Debug.Assert(node == root);
                        node.IsRed = true;
                    }
                    else if ((sib.left == null || !sib.left.IsRed) && (sib.right == null || !sib.right.IsRed))
                    {
                        // sib has two black children.
                        node.IsRed = true;
                        sib.IsRed = true;
                        parent.IsRed = false;
                    }
                    else
                    {
                        if (parent.left == node && (sib.right == null || !sib.right.IsRed))
                        {
                            // sib has a black child on the opposite side as node.
                            Node tleft = sib.left;
                            Rotate(parent, sib, tleft);
                            sib = tleft;
                        }
                        else if (parent.right == node && (sib.left == null || !sib.left.IsRed))
                        {
                            // sib has a black child on the opposite side as node.
                            Node tright = sib.right;
                            Rotate(parent, sib, tright);
                            sib = tright;
                        }

                        // sib has a red child.
                        Rotate(gparent, parent, sib);
                        node.IsRed = true;
                        sib.IsRed = true;
                        sib.left.IsRed = false;
                        sib.right.IsRed = false;

                        sib.DecrementCount();
                        nodeStack[nodeStackPtr - 1] = sib;
                        parent.DecrementCount();
                        nodeStack[nodeStackPtr++] = parent;
                    }
                }

                // Compare the key and move down the tree to the correct child.
                do
                {
                    Node nextNode, nextSib; // Node we've moving to, and it's sibling.

                    node.DecrementCount();
                    nodeStack[nodeStackPtr++] = node;

                    // Determine which way to move in the tree by comparing the 
                    // current item to what we're looking for.
                    int compare = rangeTester(node.item);

                    if (compare == 0)
                    {
                        // We've found the node to remove. Remember it, then keep traversing the
                        // tree to either find the first/last of equal keys, and if needed, the predecessor
                        // or successor (the actual node to be removed).
                        keyNode = node;
                        if (deleteFirst)
                        {
                            nextNode = node.left;
                            nextSib = node.right;
                        }
                        else
                        {
                            nextNode = node.right;
                            nextSib = node.left;
                        }
                    }
                    else if (compare > 0)
                    {
                        nextNode = node.left;
                        nextSib = node.right;
                    }
                    else
                    {
                        nextNode = node.right;
                        nextSib = node.left;
                    }

                    // Have we reached the end of our tree walk?
                    if (nextNode == null)
                        goto FINISHED;

                    // Move down the tree.
                    gparent = parent;
                    parent = node;
                    node = nextNode;
                    sib = nextSib;
                } while (!parent.IsRed && node.IsRed);

                if (!parent.IsRed)
                {
                    Debug.Assert(!node.IsRed);
                    // moved to a black child.
                    Rotate(gparent, parent, sib);

                    sib.DecrementCount();
                    nodeStack[nodeStackPtr - 1] = sib;
                    parent.DecrementCount();
                    nodeStack[nodeStackPtr++] = parent;

                    sib.IsRed = false;
                    parent.IsRed = true;
                    gparent = sib;
                    sib = (parent.left == node) ? parent.right : parent.left;
                }
            }

            FINISHED:
            if (keyNode == null)
            {
                // We never found a node to delete.

                // Return counts back to their previous value.
                for (int i = 0; i < nodeStackPtr; ++i)
                    nodeStack[i].IncrementCount();

                // Color the root black, in case it was colored red above.
                if (root != null)
                    root.IsRed = false;

                item = default(T);
                return false;
            }

            // Return the item from the node we're deleting.
            item = keyNode.item;

            // At a leaf or a node with one child which is a leaf. Remove the node.
            if (keyNode != node)
            {
                // The node we want to delete is interior. Move the item from the
                // node we're actually deleting to the key node.
                keyNode.item = node.item;
            }

            // If we have one child, replace the current with the child, otherwise,
            // replace the current node with null.
            Node replacement;
            if (node.left != null)
            {
                replacement = node.left;
                Debug.Assert(!node.IsRed && replacement.IsRed);
                replacement.IsRed = false;
            }
            else if (node.right != null)
            {
                replacement = node.right;
                Debug.Assert(!node.IsRed && replacement.IsRed);
                replacement.IsRed = false;
            }
            else
                replacement = null;

            if (parent == null)
            {
                Debug.Assert(root == node);
                root = replacement;
            }
            else if (parent.left == node)
                parent.left = replacement;
            else
            {
                Debug.Assert(parent.right == node);
                parent.right = replacement;
            }

            // Color the root black, in case it was colored red above.
            if (root != null)
                root.IsRed = false;

            // Update item count.
            ElementCount -= 1;

            // And we're done.
            return true;
        }
    }
}