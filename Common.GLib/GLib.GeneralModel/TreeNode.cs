using System;
using System.Collections.Generic;
using System.Linq;

namespace GLib.GeneralModel
{
	public class TreeNode<TKey, UValue>
	{
		public TKey Key
		{
			get;
			set;
		}

		public UValue Value
		{
			get;
			set;
		}

		public TKey ParentKey
		{
			get;
			set;
		}

		public TreeNode<TKey, UValue> Parent
		{
			get;
			set;
		}

		public List<TreeNode<TKey, UValue>> Childs
		{
			get;
			set;
		}

		public virtual TreeNode<TKey, UValue> this[TKey key] => GetValue(key);

		public TreeNode(TKey key, UValue value, TKey parentKey)
		{
			Key = key;
			Value = value;
			ParentKey = parentKey;
			Childs = new List<TreeNode<TKey, UValue>>();
		}

		public TreeNode(List<TreeNode<TKey, UValue>> list)
		{
			Init(list, default(TKey));
		}

		public TreeNode(List<TreeNode<TKey, UValue>> list, TKey rootKey)
		{
			Init(list, rootKey);
		}

		protected void Init(List<TreeNode<TKey, UValue>> list, TKey rootKey)
		{
			TreeNode<TKey, UValue> treeNode = Enumerable.FirstOrDefault(list, (TreeNode<TKey, UValue> p) => p.Key.Equals(rootKey));
			if (treeNode == null)
			{
				throw new ApplicationException("parent no find");
			}
			Key = treeNode.Key;
			Value = treeNode.Value;
			ParentKey = treeNode.ParentKey;
			IEnumerable<TreeNode<TKey, UValue>> enumerable = Enumerable.Where(list, (TreeNode<TKey, UValue> p) => p.ParentKey.Equals(rootKey));
			Childs = new List<TreeNode<TKey, UValue>>();
			foreach (TreeNode<TKey, UValue> item2 in enumerable)
			{
				TreeNode<TKey, UValue> treeNode2 = new TreeNode<TKey, UValue>(list, item2.Key);
				treeNode2.Parent = this;
				TreeNode<TKey, UValue> item = treeNode2;
				Childs.Add(item);
			}
		}

		public virtual TreeNode<TKey, UValue> GetValue(TKey key)
		{
			TreeNode<TKey, UValue> treeNode = null;
			if (Key.Equals(key))
			{
				treeNode = this;
			}
			else
			{
				foreach (TreeNode<TKey, UValue> child in Childs)
				{
					treeNode = child.GetValue(key);
					if (treeNode != null)
					{
						break;
					}
				}
			}
			return treeNode;
		}
	}
}
