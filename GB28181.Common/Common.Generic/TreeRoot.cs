using System.Collections.Generic;
using System.Linq;

namespace Common.Generic
{
	public class TreeRoot<TKey, UValue> : TreeNode<TKey, UValue>
	{
		public List<TreeNode<TKey, UValue>> SourceList
		{
			get;
			set;
		}

		public override TreeNode<TKey, UValue> this[TKey key] => GetValue(key);

		public TreeRoot(TKey defKey, UValue value, List<TreeNode<TKey, UValue>> list)
			: base(default(TKey), default(UValue), default(TKey))
		{
			init(defKey, value, list);
		}

		public TreeRoot(List<TreeNode<TKey, UValue>> list)
			: base(default(TKey), default(UValue), default(TKey))
		{
			init(default(TKey), default(UValue), list);
		}

		private void init(TKey defKey, UValue value, List<TreeNode<TKey, UValue>> list)
		{
			SourceList = list;
			IEnumerable<TreeNode<TKey, UValue>> enumerable = Enumerable.Where(list, (TreeNode<TKey, UValue> p) => p.ParentKey.Equals(defKey));
			base.Childs = new List<TreeNode<TKey, UValue>>();
			foreach (TreeNode<TKey, UValue> item2 in enumerable)
			{
				TreeNode<TKey, UValue> treeNode = new TreeNode<TKey, UValue>(list, item2.Key);
				treeNode.Parent = this;
				TreeNode<TKey, UValue> item = treeNode;
				base.Childs.Add(item);
			}
		}

		public override TreeNode<TKey, UValue> GetValue(TKey key)
		{
			TreeNode<TKey, UValue> treeNode = null;
			if (base.Key != null && base.Key.Equals(key))
			{
				treeNode = this;
			}
			else
			{
				foreach (TreeNode<TKey, UValue> child in base.Childs)
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
