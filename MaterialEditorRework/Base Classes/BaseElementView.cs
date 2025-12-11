using UnityEngine;

namespace MaterialEditorRework.Views
{
	public class BaseElementView
	{
		public virtual void Draw(Rect rect){}
		public virtual void Initialize(){}
		public bool IsInitialized = false;
	}
}