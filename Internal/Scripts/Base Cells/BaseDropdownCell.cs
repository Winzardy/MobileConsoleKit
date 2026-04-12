namespace MobileConsole.UI
{
	public abstract class BaseDropdownCell : BaseCell
	{
		public delegate void Callback(BaseDropdownCell cell, int index);
		public Callback OnValueChanged;

		public abstract void SetOptions(string[] options);
		public abstract void SetIndex(int index);
		public abstract void SetInteractable(bool interactable);
		public abstract void SetFilterEnabled(bool enabled);

		public void NotifyOnValueChanged(int index)
		{
			if (OnValueChanged != null)
			{
				OnValueChanged(this, index);
			}
		}
	}
}
