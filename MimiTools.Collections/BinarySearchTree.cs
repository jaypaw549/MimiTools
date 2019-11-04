using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MimiTools.Collections
{
    public class BinarySearchTree<T>
    {
        private readonly IComparer<T> Comparer;
        private Node Root;


        private class Node
        {
            internal Node(T value, IComparer<T> comparer, Node parent)
            {
                Red = parent != null;
                Parent = parent;
                Left = null;
                Right = null;
                Value = value;
            }

            private readonly IComparer<T> Comparer;
            private readonly T Value;

            private Node Left;
            private Node Parent;
            private Node Right;

            private bool Red;

            internal void Insert(T value)
            {
                if (Comparer.Compare(Value, value) <= 0)
                {
                    if (Left != null)
                    {
                        Left.Insert(value);
                        return;
                    }

                    Left = new Node(value, Comparer, this);
                    Validate();
                    return;
                }

                if (Right != null)
                {
                    Right.Insert(value);
                    return;
                }

                Right = new Node(value, Comparer, this);
            }

            internal bool Remove(T value)
            {
                int compare = Comparer.Compare(Value, value);
                if (compare < 0)
                    return Left?.Search(value) ?? false;
                else if (compare > 0)
                    return Right?.Search(value) ?? false;

                return false;
            }

            internal bool Search(T value)
            {
                int compare = Comparer.Compare(Value, value);
                if (compare < 0)
                    return Left?.Search(value) ?? false;
                else if (compare > 0)
                    return Right?.Search(value) ?? false;

                return true;
            }

            private void ColorSwap()
            {
                Left.Red = false;
                Right.Red = false;
                Red = true;
                Parent?.Validate();
            }

            private void RotateLeft()
            {
                //Shift Right to parent
                Right.Parent = Parent;
                Parent?.UpdateChild(Right);

                //Make our right node the parent
                Parent = Right;
                Parent.Left = this;

                //Make the new parent's right node our left.
                Right = Right.Left;
                Right.Parent = this;

                //Finally swap red statuses
                Red ^= Parent.Red;
                Parent.Red ^= Red;
                Red ^= Parent.Red;

                //And validate~
                Validate();
            }

            private void RotateRight()
            {
                //Shift Left to parent
                Left.Parent = Parent;
                Parent.UpdateChild(Left);

                //Make our Left node the parent
                Parent = Left;
                Parent.Right = this;

                //Make the new parent's right node our left.
                Left = Left.Right;
                Left.Parent = this;

                //Finally swap red statuses
                Red ^= Parent.Red;
                Parent.Red ^= Red;
                Red ^= Parent.Red;

                //And validate~
                Validate();
            }

            private void UpdateChild(Node node)
            {
                if (Comparer.Compare(node.Value, Value) <= 0)
                    Left = node;
                else
                    Right = node;
            }

            private void Validate()
            {
                if ((Left?.Red ?? false) && (Right?.Red ?? false))
                {
                    ColorSwap();
                    return;
                }

                if ((Left?.Red ?? false) && Red)
                {
                    Parent.RotateRight();
                    return;
                }

                if (Right?.Red ?? false)
                {
                    RotateLeft();
                    return;
                }

                Parent?.Validate();
            }
        }
    }
}
